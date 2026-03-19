"""
Query Complexity Analyzer
Analyzes queries to determine if they are simple (direct answer) or complex (needs reasoning)
"""

from typing import Dict, List, Optional
from dataclasses import dataclass
from enum import Enum
import re
import logging

from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class ComplexityClassification(str, Enum):
    """Query complexity classification"""
    SIMPLE = "simple"
    COMPLEX = "complex"
    UNKNOWN = "unknown"


@dataclass
class QueryComplexity:
    """Query complexity analysis result"""
    classification: ComplexityClassification
    score: float  # 0.0 (simple) to 1.0 (complex)
    factors: Dict[str, float]  # Individual factor scores
    reasoning: str  # Explanation of classification


class QueryComplexityAnalyzer:
    """
    Analyzes query complexity using heuristics
    
    Simple Query Indicators:
    - Single entity (student, classroom, topic)
    - Direct questions ("What is...", "Show me...")
    - Factual queries
    - Short queries (< 20 words)
    
    Complex Query Indicators:
    - Multiple entities
    - Causal questions ("Why...", "How...", "What causes...")
    - Comparative questions ("Compare...", "Which is better...")
    - Reasoning required ("Why does student X struggle with topic Y?")
    - Long queries (> 30 words)
    """
    
    def __init__(
        self,
        simple_threshold: Optional[float] = None,
        complex_threshold: Optional[float] = None,
        word_count_simple: Optional[int] = None,
        word_count_complex: Optional[int] = None
    ):
        """
        Initialize complexity analyzer
        
        Args:
            simple_threshold: Score below this is classified as simple (defaults to settings)
            complex_threshold: Score above this is classified as complex (defaults to settings)
            word_count_simple: Queries with fewer words are more likely simple (defaults to settings)
            word_count_complex: Queries with more words are more likely complex (defaults to settings)
        """
        self.simple_threshold = simple_threshold or settings.COMPLEXITY_SIMPLE_THRESHOLD
        self.complex_threshold = complex_threshold or settings.COMPLEXITY_COMPLEX_THRESHOLD
        self.word_count_simple = word_count_simple or settings.COMPLEXITY_WORD_COUNT_SIMPLE
        self.word_count_complex = word_count_complex or settings.COMPLEXITY_WORD_COUNT_COMPLEX
        
        # Simple query patterns
        self.simple_patterns = {
            "direct_questions": [
                r"^(what|which|who|when|where)\s+is",
                r"^(show|list|give|tell)\s+me",
                r"^(what|which|who)\s+(are|is)\s+",
                r"^(how many|how much)\s+",
            ],
            "factual_queries": [
                r"^(what|which)\s+(is|are)\s+the\s+",
                r"^(show|display|get)\s+(me\s+)?(the\s+)?",
            ],
            "single_entity_indicators": [
                r"^(student|classroom|topic|quiz|assignment)\s+\w+",
                r"^(the\s+)?(student|classroom|topic)\s+\w+",
            ]
        }
        
        # Complex query patterns
        self.complex_patterns = {
            "causal_questions": [
                r"^(why|how|what causes?|what makes?|what leads? to)",
                r"^(explain|analyze|investigate)\s+(why|how)",
                r"^(what is the reason|what are the reasons)",
            ],
            "comparative_questions": [
                r"^(compare|comparison|difference|differences|similar|similarities)",
                r"^(which is better|which is worse|which performs better)",
                r"^(better than|worse than|compared to)",
            ],
            "reasoning_required": [
                r"^(why does|why do|why is|why are)",
                r"^(how does|how do|how is|how are)",
                r"^(what if|what would happen|what might)",
                r"^(predict|forecast|estimate|recommend)",
            ],
            "multiple_entities": [
                r"(student|classroom|topic).*?(and|or|,).*?(student|classroom|topic)",
                r"(all|every|each)\s+(students?|classrooms?|topics?)",
            ]
        }
        
        # Entity extraction patterns (for counting)
        self.entity_patterns = [
            r"(?:^|\s)(student|classroom|topic|quiz|assignment|lesson|course|curriculum)\s+\w+",
            r"(?:^|\s)(students?|classrooms?|topics?|quizzes?|assignments?|lessons?|courses?|curricula)",
        ]
    
    def analyze(self, query: str) -> QueryComplexity:
        """
        Analyze query complexity
        
        Args:
            query: User query string
            
        Returns:
            QueryComplexity with classification, score, and reasoning
        """
        query_lower = query.lower().strip()
        
        if not query_lower:
            return QueryComplexity(
                classification=ComplexityClassification.UNKNOWN,
                score=0.5,
                factors={},
                reasoning="Empty query"
            )
        
        factors = {}
        reasoning_parts = []
        
        # Factor 1: Word count
        word_count = len(query_lower.split())
        word_count_score = self._calculate_word_count_score(word_count)
        factors["word_count"] = word_count_score
        reasoning_parts.append(f"Word count: {word_count} (score: {word_count_score:.2f})")
        
        # Factor 2: Simple pattern matching
        simple_score = self._check_simple_patterns(query_lower)
        factors["simple_patterns"] = simple_score
        if simple_score > 0.3:
            reasoning_parts.append(f"Simple patterns detected (score: {simple_score:.2f})")
        
        # Factor 3: Complex pattern matching
        complex_score = self._check_complex_patterns(query_lower)
        factors["complex_patterns"] = complex_score
        if complex_score > 0.3:
            reasoning_parts.append(f"Complex patterns detected (score: {complex_score:.2f})")
        
        # Factor 4: Entity count
        entity_count = self._count_entities(query_lower)
        entity_score = min(entity_count / 3.0, 1.0)  # Normalize to 0-1
        factors["entity_count"] = entity_score
        reasoning_parts.append(f"Entities found: {entity_count} (score: {entity_score:.2f})")
        
        # Factor 5: Question type
        question_type_score = self._analyze_question_type(query_lower)
        factors["question_type"] = question_type_score
        if question_type_score > 0.5:
            reasoning_parts.append(f"Complex question type (score: {question_type_score:.2f})")
        
        # Calculate weighted final score
        # Weights: word_count (0.2), simple_patterns (0.2), complex_patterns (0.3), 
        #          entity_count (0.15), question_type (0.15)
        final_score = (
            word_count_score * 0.2 +
            (1.0 - simple_score) * 0.2 +  # Invert simple score (higher = less complex)
            complex_score * 0.3 +
            entity_score * 0.15 +
            question_type_score * 0.15
        )
        
        # Clamp to [0, 1]
        final_score = max(0.0, min(1.0, final_score))
        
        # Classify
        if final_score < self.simple_threshold:
            classification = ComplexityClassification.SIMPLE
        elif final_score > self.complex_threshold:
            classification = ComplexityClassification.COMPLEX
        else:
            classification = ComplexityClassification.UNKNOWN
        
        reasoning = "; ".join(reasoning_parts) if reasoning_parts else "No specific indicators"
        reasoning += f" → Final score: {final_score:.2f} → {classification.value}"
        
        logger.debug(f"Query complexity analysis: '{query[:50]}...' → {classification.value} (score: {final_score:.2f})")
        
        return QueryComplexity(
            classification=classification,
            score=final_score,
            factors=factors,
            reasoning=reasoning
        )
    
    def _calculate_word_count_score(self, word_count: int) -> float:
        """Calculate complexity score based on word count"""
        if word_count <= self.word_count_simple:
            # Short queries are more likely simple
            return 0.2
        elif word_count >= self.word_count_complex:
            # Long queries are more likely complex
            return 0.8
        else:
            # Linear interpolation between simple and complex thresholds
            ratio = (word_count - self.word_count_simple) / (self.word_count_complex - self.word_count_simple)
            return 0.2 + (ratio * 0.6)
    
    def _check_simple_patterns(self, query: str) -> float:
        """Check for simple query patterns"""
        score = 0.0
        
        for pattern_type, patterns in self.simple_patterns.items():
            for pattern in patterns:
                if re.search(pattern, query, re.IGNORECASE):
                    score += 0.2
                    break
        
        return min(score, 1.0)
    
    def _check_complex_patterns(self, query: str) -> float:
        """Check for complex query patterns"""
        score = 0.0
        
        for pattern_type, patterns in self.complex_patterns.items():
            for pattern in patterns:
                if re.search(pattern, query, re.IGNORECASE):
                    if pattern_type == "causal_questions":
                        score += 0.3
                    elif pattern_type == "comparative_questions":
                        score += 0.25
                    elif pattern_type == "reasoning_required":
                        score += 0.35
                    elif pattern_type == "multiple_entities":
                        score += 0.2
                    break
        
        return min(score, 1.0)
    
    def _count_entities(self, query: str) -> int:
        """Count number of entities mentioned in query"""
        entities = set()
        
        for pattern in self.entity_patterns:
            matches = re.findall(pattern, query, re.IGNORECASE)
            for match in matches:
                if isinstance(match, tuple):
                    entities.update([m.lower() for m in match if m])
                else:
                    entities.add(match.lower())
        
        return len(entities)
    
    def _analyze_question_type(self, query: str) -> float:
        """Analyze question type complexity"""
        score = 0.0
        
        # Causal questions are complex
        if re.search(r"^(why|how|what causes?|what makes?)", query, re.IGNORECASE):
            score += 0.4
        
        # Comparative questions are complex
        if re.search(r"(compare|comparison|difference|better|worse)", query, re.IGNORECASE):
            score += 0.3
        
        # Predictive/recommendation questions are complex
        if re.search(r"(predict|forecast|recommend|suggest|should)", query, re.IGNORECASE):
            score += 0.3
        
        # Direct factual questions are simple
        if re.search(r"^(what|which|who|when|where)\s+(is|are)", query, re.IGNORECASE):
            score -= 0.2
        
        return max(0.0, min(1.0, score))
    
    def is_simple(self, query: str) -> bool:
        """Quick check if query is simple"""
        complexity = self.analyze(query)
        return complexity.classification == ComplexityClassification.SIMPLE
    
    def is_complex(self, query: str) -> bool:
        """Quick check if query is complex"""
        complexity = self.analyze(query)
        return complexity.classification == ComplexityClassification.COMPLEX

