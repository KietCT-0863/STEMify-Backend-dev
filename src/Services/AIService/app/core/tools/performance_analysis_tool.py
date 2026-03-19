from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool
from app.core.graph.client import GraphClient

logger = logging.getLogger(__name__)


class PerformanceAnalysisTool(Tool):
    """
    Analyze student performance, strengths, weaknesses, and learning patterns.
    
    Available actions:
    - get_strengths: Topics/subjects student excels at
    - get_weaknesses: Topics/subjects student struggles with  
    - get_metrics: Performance metrics per topic
    - get_patterns / get_analysis: Combined strengths + weaknesses overview
    """
    
    VALID_ACTIONS = ["get_strengths", "get_weaknesses", "get_metrics", "get_patterns", "get_analysis"]
    
    def __init__(
        self,
        student_id: Optional[str],
        graph_client: Optional[GraphClient] = None
    ):
        super().__init__(
            name="performance_analysis",
            description=(
                "Analyze student performance. "
                "Actions: get_strengths (topics student excels at), "
                "get_weaknesses (topics student struggles with), "
                "get_metrics (scores per topic), "
                "get_patterns or get_analysis (combined overview). "
                "Example: {\"action\": \"get_patterns\"}"
            )
        )
        self.student_id = student_id
        self.graph_client = graph_client
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Execute tool

        Actions:
        - get_strengths: Get topics/subjects student excels at
        - get_weaknesses: Get topics/subjects student struggles with
        - get_metrics: Get performance metrics per topic/subject
        - get_patterns / get_analysis: Get learning patterns (struggles, excels)

        Parameters:
        - student_id: Optional override for the student to analyze. If not provided,
          falls back to the student_id configured at tool initialization.
        """
        action = parameters.get("action", "get_patterns")
        effective_student_id = parameters.get("student_id") or self.student_id

        if not effective_student_id:
            return json.dumps(
                {
                    "error": "student_id is required either in parameters or at tool initialization",
                    "valid_actions": self.VALID_ACTIONS,
                }
            )
        
        try:
            if action == "get_strengths":
                return await self._get_strengths(effective_student_id)
            elif action == "get_weaknesses":
                return await self._get_weaknesses(effective_student_id)
            elif action == "get_metrics":
                return await self._get_metrics(effective_student_id)
            elif action in ("get_patterns", "get_analysis"):
                # get_analysis is alias for get_patterns
                return await self._get_patterns(effective_student_id)
            else:
                # Provide helpful error with valid actions
                return json.dumps({
                    "error": f"Unknown action: '{action}'",
                    "valid_actions": self.VALID_ACTIONS,
                    "hint": "Use get_patterns for combined strengths/weaknesses overview"
                })
        except Exception as e:
            logger.error(f"[PerformanceAnalysisTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})
    
    async def _get_strengths(self, student_id: str) -> str:
        """Get topics/subjects student excels at"""
        if not self.graph_client:
            return json.dumps({
                "strengths": [],
                "note": "GraphClient not available"
            })
        
        try:
            # EXCELS_AT relationships have: average_score, high_score_count, total_attempts
            cypher = """
            MATCH (s:Student {id: $student_id})-[r:EXCELS_AT]->(t:Topic)
            RETURN t.name as topic, t.id as topic_id, r.average_score as confidence,
                   r.high_score_count as high_score_count, r.total_attempts as total_attempts
            ORDER BY r.average_score DESC
            LIMIT 10
            """
            
            results = await self.graph_client.query_cypher(
                cypher,
                {"student_id": student_id}
            )
            
            strengths = [
                {
                    "topic": row.get("topic", ""),
                    "topic_id": row.get("topic_id", ""),
                    "confidence": row.get("confidence", 0.0)  # Using average_score as confidence
                }
                for row in results
            ]
            
            return json.dumps({
                "strengths": strengths,
                "count": len(strengths),
                    "student_id": student_id
            })
        except Exception as e:
            logger.error(f"[PerformanceAnalysisTool] Error getting strengths: {e}")
            return json.dumps({"error": str(e)})
    
    async def _get_weaknesses(self, student_id: str) -> str:
        if not self.graph_client:
            return json.dumps({
                "weaknesses": [],
                "note": "GraphClient not available"
            })
        
        try:
            # STRUGGLES_WITH relationships have: average_score, low_score_count, total_attempts
            cypher = """
            MATCH (s:Student {id: $student_id})-[r:STRUGGLES_WITH]->(t:Topic)
            RETURN t.name as topic, t.id as topic_id, r.average_score as severity,
                   r.low_score_count as low_score_count, r.total_attempts as total_attempts
            ORDER BY r.average_score ASC
            LIMIT 10
            """
            
            results = await self.graph_client.query_cypher(
                cypher,
                {"student_id": student_id}
            )
            
            weaknesses = [
                {
                    "topic": row.get("topic", ""),
                    "topic_id": row.get("topic_id", ""),
                    "severity": row.get("severity", 0.0)  # Using average_score as severity (lower = more severe)
                }
                for row in results
            ]
            
            return json.dumps({
                "weaknesses": weaknesses,
                "count": len(weaknesses),
                    "student_id": student_id
            })
        except Exception as e:
            logger.error(f"[PerformanceAnalysisTool] Error getting weaknesses: {e}")
            return json.dumps({"error": str(e)})
    
    async def _get_metrics(self, student_id: str) -> str:
        if not self.graph_client:
            return json.dumps({
                "metrics": [],
                "note": "GraphClient not available"
            })
        
        try:
            cypher = """
            MATCH (s:Student {id: $student_id})-[:ENROLLED_IN]->(c:Classroom)
            -[:HAS_LESSON]->(l:Lesson)-[:COVERS]->(t:Topic)
            WITH s, t, l
            MATCH (s)-[:ATTEMPTED]->(a:Attempt)-[:ON_LESSON]->(l)
            WITH t, 
                 COUNT(a) as attempt_count,
                 AVG(a.score) as avg_score,
                 MAX(a.score) as max_score,
                 MIN(a.score) as min_score
            RETURN t.name as topic, 
                   t.id as topic_id,
                   attempt_count,
                   round(avg_score, 2) as avg_score,
                   max_score,
                   min_score
            ORDER BY avg_score DESC
            """
            
            results = await self.graph_client.query_cypher(
                cypher,
                {"student_id": student_id}
            )
            
            metrics = [
                {
                    "topic": row.get("topic", ""),
                    "topic_id": row.get("topic_id", ""),
                    "attempt_count": row.get("attempt_count", 0),
                    "avg_score": row.get("avg_score", 0.0),
                    "max_score": row.get("max_score", 0.0),
                    "min_score": row.get("min_score", 0.0)
                }
                for row in results
            ]
            
            return json.dumps({
                "metrics": metrics,
                "count": len(metrics),
                    "student_id": student_id
            })
        except Exception as e:
            logger.error(f"[PerformanceAnalysisTool] Error getting metrics: {e}")
            return json.dumps({"error": str(e)})
    
    async def _get_patterns(self, student_id: str) -> str:
        if not self.graph_client:
            return json.dumps({
                "patterns": {},
                "note": "GraphClient not available"
            })
        
        try:
            strengths_result = await self._get_strengths(student_id)
            weaknesses_result = await self._get_weaknesses(student_id)
            
            strengths_data = json.loads(strengths_result)
            weaknesses_data = json.loads(weaknesses_result)
            
            return json.dumps({
                "strengths": strengths_data.get("strengths", []),
                "weaknesses": weaknesses_data.get("weaknesses", []),
                "student_id": self.student_id
            })
        except Exception as e:
            logger.error(f"[PerformanceAnalysisTool] Error getting patterns: {e}")
            return json.dumps({"error": str(e)})
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": self.VALID_ACTIONS,
                    "description": (
                        "Action: get_strengths, get_weaknesses, get_metrics, "
                        "get_patterns (or get_analysis as alias)"
                    ),
                    "default": "get_patterns"
                }
            },
            "required": []
        }

