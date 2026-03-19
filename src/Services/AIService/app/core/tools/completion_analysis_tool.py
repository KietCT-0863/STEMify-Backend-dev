from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool
from app.core.data.classroom_repository import ClassroomRepository

logger = logging.getLogger(__name__)


class CompletionAnalysisTool(Tool):
    """
    Analyzes completion distributions (curriculum/course) within a classroom.
    """

    def __init__(self, classroom_repository: ClassroomRepository):
        super().__init__(
            name="completion_analysis",
            description="Analyze completion rate distributions for a classroom",
        )
        self.classroom_repository = classroom_repository

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - curriculum_completion: Completion buckets for curriculum enrollments.

        Parameters:
        - classroom_id (required)
        """
        action = parameters.get("action", "curriculum_completion")
        try:
            if action == "curriculum_completion":
                return await self._curriculum_completion(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[CompletionAnalysisTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _curriculum_completion(self, parameters: Dict[str, Any]) -> str:
        classroom_id = parameters.get("classroom_id")
        if classroom_id is None:
            return json.dumps({"error": "classroom_id is required"})

        data = await self.classroom_repository.get_classroom_data(classroom_id=classroom_id)

        enrollments = data.get("enrollments", {})
        curriculum_enrollments: List[Dict[str, Any]] = enrollments.get("curriculum_enrollments", [])

        # Buckets: 0-25, 25-50, 50-75, 75-100
        buckets = {
            "0_25": 0,
            "25_50": 0,
            "50_75": 0,
            "75_100": 0,
        }

        for e in curriculum_enrollments:
            p = e.get("progress_percentage", 0.0)
            if p < 25:
                buckets["0_25"] += 1
            elif p < 50:
                buckets["25_50"] += 1
            elif p < 75:
                buckets["50_75"] += 1
            else:
                buckets["75_100"] += 1

        result = {
            "classroom_id": classroom_id,
            "total_enrollments": len(curriculum_enrollments),
            "buckets": buckets,
        }
        return json.dumps(result)

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["curriculum_completion"],
                    "description": "Action to perform",
                    "default": "curriculum_completion",
                },
                "classroom_id": {
                    "type": "integer",
                    "description": "Classroom identifier",
                },
            },
            "required": ["classroom_id"],
        }


