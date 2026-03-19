"""
Causal Minion
Tests causal hypotheses and detects patterns
"""

from typing import Dict, Any, List
import logging

from app.core.reasoning.minions.type.base import BaseMinion
from app.core.reasoning.models import CausalFinding, ReasoningPlan
from app.core.llm.models import LLMMessage

logger = logging.getLogger(__name__)


class CausalMinion(BaseMinion):
    """Causal minion: tests causal hypotheses and detects patterns
    
    Uses Remote LLM (GPT-4o) for complex causal analysis
    """
    
    @property
    def name(self) -> str:
        return "Causal"
    
    async def execute(self, context: Dict[str, Any]) -> Dict[str, Any]:
        """Test causal hypotheses from graph data"""
        plan: ReasoningPlan = context.get("plan")
        graph_sample = context.get("graph_sample", {})
        
        if not plan or not graph_sample:
            self._log("Missing plan or graph_sample", "WARNING")
            return {"causal_findings": []}
        
        self._log("Testing causal hypotheses")
        
        # Generate hypotheses based on focus areas
        hypotheses = self._generate_hypotheses(plan)
        
        # Test each hypothesis
        findings = []
        for hypothesis in hypotheses:
            # Use Remote LLM for complex analysis if available
            if self.llm_client and self.llm_client.get_remote_provider():
                finding = await self._test_hypothesis_with_llm(hypothesis, graph_sample, plan)
            else:
                finding = await self._test_hypothesis(hypothesis, graph_sample, plan)
            
            if finding:
                findings.append(finding)
        
        self._log(f"Found {len(findings)} causal findings")
        
        return {"causal_findings": findings}
    
    async def _test_hypothesis_with_llm(
        self,
        hypothesis: str,
        graph_sample: Dict[str, Any],
        plan: ReasoningPlan
    ) -> CausalFinding:
        """Test hypothesis using Remote LLM for deeper analysis"""
        if not self.llm_client:
            return await self._test_hypothesis(hypothesis, graph_sample, plan)
        
        try:
            # Prepare graph data summary
            nodes_summary = f"{len(graph_sample.get('nodes', []))} nodes"
            edges_summary = f"{len(graph_sample.get('edges', []))} relationships"
            
            # Sample some node types
            node_types = {}
            for node in graph_sample.get('nodes', [])[:20]:  # Sample first 20
                node_type = node.get('type', 'Unknown')
                node_types[node_type] = node_types.get(node_type, 0) + 1
            
            prompt = f"""Analyze this causal hypothesis based on educational graph data.

Hypothesis: {hypothesis.replace('_', ' ').title()}

Graph Data:
- {nodes_summary}, {edges_summary}
- Node types: {', '.join(f'{k}({v})' for k, v in node_types.items())}

Focus Areas: {', '.join(plan.focus_areas)}

Analyze whether this hypothesis is supported by the data. Consider:
1. Correlation strength
2. Temporal precedence (if applicable)
3. Supporting evidence
4. Counter-evidence
5. Confidence level (0.0 to 1.0)

Return a JSON object with this structure:
{{
    "confidence": 0.75,
    "correlation_strength": 0.65,
    "support": ["evidence 1", "evidence 2"],
    "counter": ["counter-evidence 1"],
    "temporal_precedence": true/false
}}

Only return the JSON object, nothing else."""

            messages = [
                LLMMessage(
                    role="system",
                    content="You are an expert in causal analysis of educational data. Analyze hypotheses based on graph data patterns."
                ),
                LLMMessage(role="user", content=prompt)
            ]
            
            response = await self.llm_client.generate_remote(messages, max_tokens=1000, temperature=0.3)
            
            import json
            try:
                result = json.loads(response.content)
                
                return CausalFinding(
                    hypothesis=hypothesis,
                    support=result.get("support", []),
                    counter=result.get("counter", []),
                    confidence=float(result.get("confidence", 0.5)),
                    temporal_precedence=result.get("temporal_precedence", False),
                    correlation_strength=float(result.get("correlation_strength", 0.0))
                )
            except (json.JSONDecodeError, KeyError, ValueError) as e:
                self._log(f"Failed to parse LLM response for hypothesis {hypothesis}: {e}", "WARNING")
                # Fallback to standard method
                return await self._test_hypothesis(hypothesis, graph_sample, plan)
        
        except Exception as e:
            self._log(f"Error testing hypothesis with LLM: {e}, falling back to standard method", "WARNING")
            return await self._test_hypothesis(hypothesis, graph_sample, plan)
    
    def _generate_hypotheses(self, plan: ReasoningPlan) -> List[str]:
        """Generate causal hypotheses based on focus areas"""
        hypotheses = []
        
        # Topic mastery → scores
        if "topic_mastery" in plan.focus_areas or "scores" in plan.focus_areas:
            hypotheses.append("topic_mastery_affects_scores")
            hypotheses.append("low_topic_mastery_causes_low_scores")
        
        # Scores → progress
        if "scores" in plan.focus_areas or "progress" in plan.focus_areas:
            hypotheses.append("scores_affect_progress")
            hypotheses.append("low_scores_impede_progress")
        
        # Engagement → outcomes
        if "engagement" in plan.focus_areas:
            hypotheses.append("engagement_affects_outcomes")
            hypotheses.append("low_engagement_causes_poor_outcomes")
        
        # Temporal patterns
        if any(c.type == "time_range" for c in plan.constraints):
            hypotheses.append("temporal_improvement_pattern")
            hypotheses.append("temporal_decline_pattern")
        
        return hypotheses
    
    async def _test_hypothesis(
        self,
        hypothesis: str,
        graph_sample: Dict[str, Any],
        plan: ReasoningPlan
    ) -> CausalFinding:
        """Test a specific causal hypothesis"""
        nodes = graph_sample.get("nodes", [])
        edges = graph_sample.get("edges", [])
        
        support_evidence = []
        counter_evidence = []
        confidence = 0.0
        temporal_precedence = False
        correlation_strength = 0.0
        
        if hypothesis == "topic_mastery_affects_scores":
            result = self._test_topic_mastery_scores(nodes, edges)
            support_evidence = result["support"]
            counter_evidence = result["counter"]
            confidence = result["confidence"]
            correlation_strength = result["correlation"]
        
        elif hypothesis == "low_topic_mastery_causes_low_scores":
            result = self._test_low_mastery_low_scores(nodes, edges)
            support_evidence = result["support"]
            counter_evidence = result["counter"]
            confidence = result["confidence"]
            correlation_strength = result["correlation"]
        
        elif hypothesis == "scores_affect_progress":
            result = self._test_scores_progress(nodes, edges)
            support_evidence = result["support"]
            counter_evidence = result["counter"]
            confidence = result["confidence"]
            correlation_strength = result["correlation"]
        
        elif hypothesis == "engagement_affects_outcomes":
            result = self._test_engagement_outcomes(nodes, edges)
            support_evidence = result["support"]
            counter_evidence = result["counter"]
            confidence = result["confidence"]
        
        elif hypothesis == "temporal_improvement_pattern":
            result = self._test_temporal_improvement(nodes, edges)
            support_evidence = result["support"]
            counter_evidence = result["counter"]
            confidence = result["confidence"]
            temporal_precedence = result.get("temporal_precedence", False)
        
        else:
            # Default: low confidence
            confidence = 0.1
        
        return CausalFinding(
            hypothesis=hypothesis,
            support=support_evidence,
            counter=counter_evidence,
            confidence=confidence,
            temporal_precedence=temporal_precedence,
            correlation_strength=correlation_strength
        )
    
    def _test_topic_mastery_scores(self, nodes: List[Dict], edges: List[Dict]) -> Dict[str, Any]:
        """Test: Topic mastery affects scores"""
        # Find students with STRUGGLES_WITH relationships
        struggling_students = {}
        topic_scores = {}
        
        for edge in edges:
            if edge.get("rel") == "STRUGGLES_WITH":
                student_id = edge.get("from")
                topic_id = edge.get("to")
                avg_score = edge.get("properties", {}).get("average_score", 0)
                
                if student_id not in struggling_students:
                    struggling_students[student_id] = []
                struggling_students[student_id].append({
                    "topic_id": topic_id,
                    "score": avg_score
                })
                topic_scores[topic_id] = avg_score
        
        # Find students with EXCELS_AT relationships
        excelling_students = {}
        for edge in edges:
            if edge.get("rel") == "EXCELS_AT":
                student_id = edge.get("from")
                topic_id = edge.get("to")
                avg_score = edge.get("properties", {}).get("average_score", 0)
                
                if student_id not in excelling_students:
                    excelling_students[student_id] = []
                excelling_students[student_id].append({
                    "topic_id": topic_id,
                    "score": avg_score
                })
                topic_scores[topic_id] = avg_score
        
        # Calculate correlation
        support = []
        counter = []
        
        if struggling_students:
            support.append(f"{len(struggling_students)} students struggling with topics")
            avg_struggling_score = sum(
                sum(t["score"] for t in topics) / len(topics) if topics else 0
                for topics in struggling_students.values()
            ) / len(struggling_students) if struggling_students else 0
            support.append(f"Average struggling score: {avg_struggling_score:.2f}")
        
        if excelling_students:
            avg_excelling_score = sum(
                sum(t["score"] for t in topics) / len(topics) if topics else 0
                for topics in excelling_students.values()
            ) / len(excelling_students) if excelling_students else 0
            support.append(f"Average excelling score: {avg_excelling_score:.2f}")
        
        # Calculate confidence based on evidence strength
        confidence = 0.5
        if struggling_students and excelling_students:
            confidence = 0.7
        if len(struggling_students) > 3 or len(excelling_students) > 3:
            confidence = min(0.9, confidence + 0.1)
        
        # Calculate correlation
        correlation = 0.0
        if topic_scores:
            scores_list = list(topic_scores.values())
            if len(scores_list) > 1:
                stats = self.math_tool.stats(scores_list)
                # Simple correlation estimate based on variance
                correlation = 0.6 if stats["std"] > 10 else 0.3
        
        return {
            "support": support,
            "counter": counter,
            "confidence": confidence,
            "correlation": correlation
        }
    
    def _test_low_mastery_low_scores(self, nodes: List[Dict], edges: List[Dict]) -> Dict[str, Any]:
        """Test: Low topic mastery causes low scores"""
        # Similar to above but focus on low scores
        low_score_attempts = []
        
        for node in nodes:
            if node.get("type") in ["QuizAttempt", "AssignmentAttempt"]:
                score = node.get("properties", {}).get("score", 0)
                if score < 60:  # Threshold for low score
                    low_score_attempts.append({
                        "node_id": node.get("id"),
                        "score": score
                    })
        
        support = []
        if low_score_attempts:
            support.append(f"{len(low_score_attempts)} low-score attempts found")
            avg_low_score = sum(a["score"] for a in low_score_attempts) / len(low_score_attempts)
            support.append(f"Average low score: {avg_low_score:.2f}")
        
        confidence = 0.6 if low_score_attempts else 0.2
        correlation = 0.5 if low_score_attempts else 0.0
        
        return {
            "support": support,
            "counter": [],
            "confidence": confidence,
            "correlation": correlation
        }
    
    def _test_scores_progress(self, nodes: List[Dict], edges: List[Dict]) -> Dict[str, Any]:
        """Test: Scores affect progress"""
        # Find progress nodes and their associated scores
        progress_nodes = [n for n in nodes if "Progress" in n.get("type", "")]
        
        support = []
        if progress_nodes:
            support.append(f"{len(progress_nodes)} progress nodes found")
        
        confidence = 0.5 if progress_nodes else 0.2
        correlation = 0.4
        
        return {
            "support": support,
            "counter": [],
            "confidence": confidence,
            "correlation": correlation
        }
    
    def _test_engagement_outcomes(self, nodes: List[Dict], edges: List[Dict]) -> Dict[str, Any]:
        """Test: Engagement affects outcomes"""
        # Count attempts as proxy for engagement
        attempt_nodes = [n for n in nodes if "Attempt" in n.get("type", "")]
        
        support = []
        if attempt_nodes:
            support.append(f"{len(attempt_nodes)} attempts found (engagement proxy)")
        
        confidence = 0.5 if attempt_nodes else 0.2
        correlation = 0.4
        
        return {
            "support": support,
            "counter": [],
            "confidence": confidence,
            "correlation": correlation
        }
    
    def _test_temporal_improvement(self, nodes: List[Dict], edges: List[Dict]) -> Dict[str, Any]:
        """Test: Temporal improvement pattern"""
        # Extract timestamps and scores
        attempts_with_time = []
        
        for node in nodes:
            if "Attempt" in node.get("type", ""):
                props = node.get("properties", {})
                score = props.get("score")
                timestamp = props.get("completed_at") or props.get("submitted_at") or props.get("started_at")
                
                if score is not None and timestamp:
                    attempts_with_time.append({
                        "node_id": node.get("id"),
                        "score": float(score),
                        "timestamp": timestamp
                    })
        
        # Sort by timestamp
        attempts_with_time.sort(key=lambda x: x["timestamp"])
        
        support = []
        temporal_precedence = False
        
        if len(attempts_with_time) >= 2:
            scores = [a["score"] for a in attempts_with_time]
            stats = self.math_tool.stats(scores)
            
            if stats["trend"] > 0:
                support.append(f"Positive trend detected: {stats['trend']:.2f}")
                temporal_precedence = True
            elif stats["trend"] < 0:
                support.append(f"Negative trend detected: {stats['trend']:.2f}")
                temporal_precedence = True
        
        confidence = 0.6 if temporal_precedence else 0.3
        correlation = abs(stats.get("trend", 0)) if attempts_with_time else 0.0
        
        return {
            "support": support,
            "counter": [],
            "confidence": confidence,
            "correlation": correlation,
            "temporal_precedence": temporal_precedence
        }

