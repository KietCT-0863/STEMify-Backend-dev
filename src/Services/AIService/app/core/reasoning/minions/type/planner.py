"""
Planner Minion
Decomposes questions, extracts entities/constraints, and creates reasoning plan
"""

import re
from typing import Dict, Any, List
from datetime import datetime, timedelta

from app.core.reasoning.minions.type.base import BaseMinion
from app.core.reasoning.models import ReasoningPlan, Entity, Constraint, EntityType
from app.core.graph.entity_extractor import EntityExtractor
from app.core.llm.models import LLMMessage

import logging

logger = logging.getLogger(__name__)


class PlannerMinion(BaseMinion):
    """Planner minion: decomposes questions and creates execution plan
    
    Uses Local LLM (Ollama) for entity extraction and plan refinement
    """
    
    @property
    def name(self) -> str:
        return "Planner"
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.entity_extractor = EntityExtractor()
    
    async def execute(self, context: Dict[str, Any]) -> Dict[str, Any]:
        """Create reasoning plan from question"""
        question = context.get("question", "")
        self._log(f"Planning for question: {question[:100]}...")
        
        # Extract entities using entity extractor
        entity_tuples = self.entity_extractor.extract_entities(question)
        entities = []
        for entity_type_str, identifier in entity_tuples:
            try:
                entity_type = EntityType[entity_type_str.upper()]
                entities.append(Entity(
                    type=entity_type,
                    identifier=identifier
                ))
            except KeyError:
                continue
        
        # Use Local LLM to refine entity extraction if available
        if self.llm_client and self.llm_client.get_local_provider():
            entities = await self._refine_entities_with_llm(question, entities)
        
        # Extract constraints
        constraints = self._extract_constraints(question)
        
        # Determine strategy
        intent = self.entity_extractor.detect_intent(question)
        strategy = self._determine_strategy(intent, entities, constraints)
        
        # Determine focus areas
        focus_areas = self._determine_focus_areas(question, intent, entities)
        
        # Use Local LLM to refine focus areas if available
        if self.llm_client and self.llm_client.get_local_provider():
            focus_areas = await self._refine_focus_areas_with_llm(question, focus_areas)
        
        # Create plan
        plan = ReasoningPlan(
            question=question,
            entities=entities,
            constraints=constraints,
            max_hops=3,  # Default to 3 hops max
            focus_areas=focus_areas,
            strategy=strategy
        )
        
        plan_description = self._format_plan_description(plan)
        
        self._log(f"Created plan with {len(entities)} entities, {len(constraints)} constraints")
        
        return {
            "plan": plan,
            "plan_description": plan_description,
            "intent": intent
        }
    
    def _extract_constraints(self, question: str) -> List[Constraint]:
        """Extract temporal and threshold constraints from question"""
        constraints = []
        question_lower = question.lower()
        
        # Time range constraints
        # Pattern: "last week", "past month", "since 2024-01-01", "between X and Y"
        time_patterns = [
            (r'last\s+(\d+)\s+(day|week|month|year)s?', self._parse_relative_time),
            (r'past\s+(\d+)\s+(day|week|month|year)s?', self._parse_relative_time),
            (r'since\s+(\d{4}-\d{2}-\d{2})', self._parse_since_date),
            (r'between\s+(\d{4}-\d{2}-\d{2})\s+and\s+(\d{4}-\d{2}-\d{2})', self._parse_date_range),
        ]
        
        for pattern, parser in time_patterns:
            matches = re.finditer(pattern, question_lower)
            for match in matches:
                constraint = parser(match)
                if constraint:
                    constraints.append(constraint)
        
        # Threshold constraints
        # Pattern: "score > 70", "below 60%", "above average"
        threshold_patterns = [
            (r'score\s*(>|>=|<|<=|=)\s*(\d+)', "score"),
            (r'(\d+)%\s*(above|below)', "percentage"),
            (r'(above|below)\s*average', "average_comparison"),
        ]
        
        for pattern, field in threshold_patterns:
            matches = re.finditer(pattern, question_lower)
            for match in matches:
                constraint = self._parse_threshold(match, field)
                if constraint:
                    constraints.append(constraint)
        
        return constraints
    
    def _parse_relative_time(self, match: re.Match) -> Constraint:
        """Parse relative time like 'last 7 days'"""
        try:
            amount = int(match.group(1))
            unit = match.group(2)
            
            # Convert to days
            days_map = {"day": 1, "week": 7, "month": 30, "year": 365}
            days = amount * days_map.get(unit, 1)
            
            # Calculate start date
            start_date = (datetime.utcnow() - timedelta(days=days)).isoformat()
            end_date = datetime.utcnow().isoformat()
            
            return Constraint(
                type="time_range",
                field="timestamp",
                value={"start": start_date, "end": end_date}
            )
        except Exception:
            return None
    
    def _parse_since_date(self, match: re.Match) -> Constraint:
        """Parse 'since YYYY-MM-DD'"""
        try:
            start_date = match.group(1)
            end_date = datetime.utcnow().isoformat()
            
            return Constraint(
                type="time_range",
                field="timestamp",
                value={"start": start_date, "end": end_date}
            )
        except Exception:
            return None
    
    def _parse_date_range(self, match: re.Match) -> Constraint:
        """Parse 'between YYYY-MM-DD and YYYY-MM-DD'"""
        try:
            start_date = match.group(1)
            end_date = match.group(2)
            
            return Constraint(
                type="time_range",
                field="timestamp",
                value={"start": start_date, "end": end_date}
            )
        except Exception:
            return None
    
    def _parse_threshold(self, match: re.Match, field: str) -> Constraint:
        """Parse threshold constraints"""
        try:
            if field == "score":
                operator = match.group(1)
                value = float(match.group(2))
                return Constraint(
                    type="threshold",
                    field="score",
                    value=value,
                    operator=operator
                )
            elif field == "percentage":
                value = float(match.group(1))
                direction = match.group(2)
                operator = ">=" if direction == "above" else "<="
                return Constraint(
                    type="threshold",
                    field="percentage",
                    value=value,
                    operator=operator
                )
        except Exception:
            pass
        return None
    
    def _determine_strategy(self, intent: str, entities: List[Entity], constraints: List[Constraint]) -> str:
        """Determine reasoning strategy based on question characteristics"""
        if intent in ["struggling", "need_help", "performing_poorly"]:
            return "causal_analysis"
        
        # If we have temporal constraints, focus on temporal analysis
        if any(c.type == "time_range" for c in constraints):
            return "temporal_analysis"
        
        # Default to causal analysis
        return "causal_analysis"
    
    def _determine_focus_areas(self, question: str, intent: str, entities: List[Entity]) -> List[str]:
        """Determine focus areas for reasoning"""
        focus_areas = []
        question_lower = question.lower()
        
        # Topic mastery
        if any(word in question_lower for word in ["topic", "mastery", "understanding", "chủ đề"]):
            focus_areas.append("topic_mastery")
        
        # Scores
        if any(word in question_lower for word in ["score", "grade", "performance", "điểm"]):
            focus_areas.append("scores")
        
        # Progress
        if any(word in question_lower for word in ["progress", "improvement", "tiến bộ"]):
            focus_areas.append("progress")
        
        # Engagement
        if any(word in question_lower for word in ["engagement", "participation", "tham gia"]):
            focus_areas.append("engagement")
        
        # Default focus areas if none specified
        if not focus_areas:
            focus_areas = ["scores", "topic_mastery"]
        
        return focus_areas
    
    def _format_plan_description(self, plan: ReasoningPlan) -> str:
        """Format plan as human-readable description"""
        parts = [f"Question: {plan.question}"]
        
        if plan.entities:
            entity_str = ", ".join([f"{e.type.value}:{e.identifier}" for e in plan.entities])
            parts.append(f"Entities: {entity_str}")
        
        if plan.constraints:
            constraint_str = ", ".join([f"{c.type}({c.field})" for c in plan.constraints])
            parts.append(f"Constraints: {constraint_str}")
        
        parts.append(f"Strategy: {plan.strategy}")
        parts.append(f"Focus: {', '.join(plan.focus_areas)}")
        parts.append(f"Max hops: {plan.max_hops}")
        
        return " | ".join(parts)
    
    async def _refine_entities_with_llm(self, question: str, entities: List[Entity]) -> List[Entity]:
        """Use Local LLM to refine entity extraction"""
        if not self.llm_client:
            return entities
        
        try:
            prompt = f"""Analyze this question and extract educational entities (Student, Classroom, Topic, Quiz, Assignment).

Question: {question}

Current entities found: {[f"{e.type.value}:{e.identifier}" for e in entities]}

If any entities are missing or incorrectly identified, provide a JSON list of entities in format:
[{{"type": "STUDENT|CLASSROOM|TOPIC|QUIZ|ASSIGNMENT", "identifier": "name or id"}}]

Only return the JSON array, nothing else."""

            messages = [
                LLMMessage(role="system", content="You are an expert at extracting educational entities from questions."),
                LLMMessage(role="user", content=prompt)
            ]
            
            response = await self.llm_client.generate_local(messages, max_tokens=500)
            
            # Parse response and merge with existing entities
            import json
            try:
                llm_entities = json.loads(response.content)
                for item in llm_entities:
                    try:
                        entity_type = EntityType[item["type"]]
                        # Only add if not already present
                        if not any(e.type == entity_type and e.identifier == item["identifier"] for e in entities):
                            entities.append(Entity(type=entity_type, identifier=item["identifier"]))
                    except (KeyError, ValueError):
                        continue
            except json.JSONDecodeError:
                self._log("Failed to parse LLM response for entity refinement", "WARNING")
        
        except Exception as e:
            self._log(f"Error refining entities with LLM: {e}", "WARNING")
        
        return entities
    
    async def _refine_focus_areas_with_llm(self, question: str, focus_areas: List[str]) -> List[str]:
        """Use Local LLM to refine focus areas"""
        if not self.llm_client:
            return focus_areas
        
        try:
            prompt = f"""Analyze this educational question and determine the focus areas.

Question: {question}
Current focus areas: {', '.join(focus_areas)}

Focus areas can be: topic_mastery, scores, progress, engagement

Return a JSON array of focus areas that are most relevant to this question.
Example: ["scores", "topic_mastery"]

Only return the JSON array, nothing else."""

            messages = [
                LLMMessage(role="system", content="You are an expert at analyzing educational questions."),
                LLMMessage(role="user", content=prompt)
            ]
            
            response = await self.llm_client.generate_local(messages, max_tokens=200)
            
            import json
            try:
                llm_focus_areas = json.loads(response.content)
                if isinstance(llm_focus_areas, list):
                    # Merge with existing, remove duplicates
                    combined = list(set(focus_areas + llm_focus_areas))
                    return combined
            except json.JSONDecodeError:
                self._log("Failed to parse LLM response for focus areas", "WARNING")
        
        except Exception as e:
            self._log(f"Error refining focus areas with LLM: {e}", "WARNING")
        
        return focus_areas

