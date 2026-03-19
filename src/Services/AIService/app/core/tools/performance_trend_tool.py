from typing import Dict, Any, Optional, List
import logging
import json
from datetime import datetime

from app.core.tools.base import Tool
from app.core.graph.client import GraphClient

logger = logging.getLogger(__name__)


class PerformanceTrendTool(Tool):
    """
    Uses Neo4j to compute simple time-based performance trends.
    """

    def __init__(self, graph_client: GraphClient):
        super().__init__(
            name="performance_trend",
            description="Analyze performance trends (e.g., average score over time)",
        )
        self.graph_client = graph_client

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - lesson_trend: Average score per time window for a lesson.

        Parameters:
        - lesson_id (required)
        """
        action = parameters.get("action", "lesson_trend")
        try:
            if action == "lesson_trend":
                return await self._lesson_trend(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[PerformanceTrendTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _lesson_trend(self, parameters: Dict[str, Any]) -> str:
        lesson_id = parameters.get("lesson_id")
        if not lesson_id:
            return json.dumps({"error": "lesson_id is required"})

        # Simple Cypher: Attempt nodes over time for a lesson
        cypher = """
        MATCH (l:Lesson {id: $lesson_id})<-[:ON_LESSON]-(a:Attempt)
        WHERE a.score IS NOT NULL AND a.completed_at IS NOT NULL
        WITH date(a.completed_at) as day, avg(a.score) as avg_score, count(a) as attempts
        RETURN day, avg_score, attempts
        ORDER BY day ASC
        """
        results = await self.graph_client.query_cypher(cypher, {"lesson_id": lesson_id})

        trend_points: List[Dict[str, Any]] = []
        for row in results:
            day = row.get("day")
            if isinstance(day, datetime):
                day_str = day.date().isoformat()
            else:
                day_str = str(day)
            trend_points.append(
                {
                    "day": day_str,
                    "average_score": row.get("avg_score", 0.0),
                    "attempts": row.get("attempts", 0),
                }
            )

        return json.dumps(
            {
                "lesson_id": lesson_id,
                "points": trend_points,
            }
        )

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["lesson_trend"],
                    "description": "Action to perform",
                    "default": "lesson_trend",
                },
                "lesson_id": {
                    "type": "string",
                    "description": "Lesson identifier",
                },
            },
            "required": ["lesson_id"],
        }


