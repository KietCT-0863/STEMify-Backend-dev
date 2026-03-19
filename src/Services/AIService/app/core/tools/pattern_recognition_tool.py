from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool
from app.core.graph.client import GraphClient

logger = logging.getLogger(__name__)


class PatternRecognitionTool(Tool):
    """
    Uses Neo4j to detect learning patterns (consistent struggles/excels).
    """

    def __init__(self, graph_client: GraphClient):
        super().__init__(
            name="pattern_recognition",
            description="Identify learning patterns (struggles/excels) for teacher analytics",
        )
        self.graph_client = graph_client

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - struggles: Topics where student/class struggles consistently.
        - excels: Topics where student/class excels.

        Parameters:
        - scope: 'student' or 'class'
        - student_id (for scope=student)
        - classroom_id (optional for extra filtering)
        """
        action = parameters.get("action", "struggles")
        try:
            if action == "struggles":
                return await self._find_struggles(parameters)
            if action == "excels":
                return await self._find_excels(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[PatternRecognitionTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _check_relationship_exists(self, relationship_type: str) -> bool:
        """Check if relationship type exists in database to avoid Neo4j warnings."""
        try:
            # Use db.relationshipTypes() to check without triggering warnings
            check_query = """
            CALL db.relationshipTypes() YIELD relationshipType
            WHERE relationshipType = $rel_type
            RETURN count(*) as count
            """
            result = await self.graph_client.query_cypher(check_query, {"rel_type": relationship_type})
            return result and len(result) > 0 and result[0].get("count", 0) > 0
        except Exception:
            # If db.relationshipTypes() is not available or fails, fallback to direct check
            # This may still trigger warnings but is better than nothing
            try:
                check_query = f"""
                MATCH ()-[r:{relationship_type}]-()
                RETURN count(r) as count
                LIMIT 1
                """
                result = await self.graph_client.query_cypher(check_query, {})
                return result and len(result) > 0 and result[0].get("count", 0) > 0
            except Exception:
                return False

    async def _find_struggles(self, parameters: Dict[str, Any]) -> str:
        scope = parameters.get("scope", "student")
        student_id = parameters.get("student_id")
        classroom_id = parameters.get("classroom_id")

        if scope == "student" and not student_id:
            return json.dumps({"error": "student_id is required for scope=student"})

        # Check if STRUGGLES_WITH relationships exist to avoid Neo4j warnings
        if not await self._check_relationship_exists("STRUGGLES_WITH"):
            logger.debug("[PatternRecognitionTool] STRUGGLES_WITH relationships not found, returning empty results")
            return json.dumps({"scope": scope, "items": []})

        if scope == "student":
            cypher = """
            MATCH (s:Student {id: $student_id})-[r:STRUGGLES_WITH]->(t:Topic)
            RETURN t.id as topic_id,
                   t.name as topic,
                   r.average_score as severity,
                   r.low_score_count as low_score_count,
                   r.total_attempts as total_attempts
            ORDER BY severity ASC
            LIMIT 20
            """
            params = {"student_id": student_id}
        else:
            cypher = """
            MATCH (c:Classroom {id: $classroom_id})<-[:ENROLLED_IN]-(s:Student)
                  -[r:STRUGGLES_WITH]->(t:Topic)
            WITH t, avg(r.average_score) as avg_severity, count(s) as student_count
            RETURN t.id as topic_id,
                   t.name as topic,
                   avg_severity,
                   student_count
            ORDER BY avg_severity ASC, student_count DESC
            LIMIT 20
            """
            params = {"classroom_id": classroom_id}

        results = await self.graph_client.query_cypher(cypher, params)
        return json.dumps(
            {
                "scope": scope,
                "items": results,
            }
        )

    async def _find_excels(self, parameters: Dict[str, Any]) -> str:
        scope = parameters.get("scope", "student")
        student_id = parameters.get("student_id")
        classroom_id = parameters.get("classroom_id")

        if scope == "student" and not student_id:
            return json.dumps({"error": "student_id is required for scope=student"})

        # Check if EXCELS_AT relationships exist to avoid Neo4j warnings
        if not await self._check_relationship_exists("EXCELS_AT"):
            logger.debug("[PatternRecognitionTool] EXCELS_AT relationships not found, returning empty results")
            return json.dumps({"scope": scope, "items": []})

        if scope == "student":
            cypher = """
            MATCH (s:Student {id: $student_id})-[r:EXCELS_AT]->(t:Topic)
            RETURN t.id as topic_id,
                   t.name as topic,
                   r.average_score as confidence,
                   r.high_score_count as high_score_count,
                   r.total_attempts as total_attempts
            ORDER BY confidence DESC
            LIMIT 20
            """
            params = {"student_id": student_id}
        else:
            cypher = """
            MATCH (c:Classroom {id: $classroom_id})<-[:ENROLLED_IN]-(s:Student)
                  -[r:EXCELS_AT]->(t:Topic)
            WITH t, avg(r.average_score) as avg_confidence, count(s) as student_count
            RETURN t.id as topic_id,
                   t.name as topic,
                   avg_confidence,
                   student_count
            ORDER BY avg_confidence DESC, student_count DESC
            LIMIT 20
            """
            params = {"classroom_id": classroom_id}

        results = await self.graph_client.query_cypher(cypher, params)
        return json.dumps(
            {
                "scope": scope,
                "items": results,
            }
        )

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["struggles", "excels"],
                    "description": "Pattern type to retrieve",
                    "default": "struggles",
                },
                "scope": {
                    "type": "string",
                    "enum": ["student", "class"],
                    "description": "Scope of analysis",
                    "default": "student",
                },
                "student_id": {
                    "type": "string",
                    "description": "Student ID (for scope=student)",
                },
                "classroom_id": {
                    "type": "integer",
                    "description": "Classroom ID (for scope=class or extra filter)",
                },
            },
            "required": [],
        }


