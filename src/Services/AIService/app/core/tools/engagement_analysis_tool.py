from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool
from app.core.data.classroom_repository import ClassroomRepository

logger = logging.getLogger(__name__)


class EngagementAnalysisTool(Tool):
    """
    Analyzes engagement metrics (basic aggregated view) using classroom data.
    """

    def __init__(self, classroom_repository: ClassroomRepository):
        super().__init__(
            name="engagement_analysis",
            description="Analyze engagement patterns for lessons/classroom",
        )
        self.classroom_repository = classroom_repository

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - classroom_engagement: Engagement summary for a classroom.

        Parameters:
        - classroom_id (required)
        - analysis_period_days (optional)
        """
        action = parameters.get("action", "classroom_engagement")
        try:
            if action == "classroom_engagement":
                return await self._classroom_engagement(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[EngagementAnalysisTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _classroom_engagement(self, parameters: Dict[str, Any]) -> str:
        classroom_id = parameters.get("classroom_id")
        analysis_period_days = parameters.get("analysis_period_days")
        if classroom_id is None:
            return json.dumps({"error": "classroom_id is required"})

        data = await self.classroom_repository.get_classroom_data(
            classroom_id=classroom_id,
            analysis_period_days=analysis_period_days,
        )

        # For now, derive simple engagement stats from enrollments and quizzes if present.
        enrollments = data.get("enrollments", {})
        curriculum_enrollments: List[Dict[str, Any]] = enrollments.get("curriculum_enrollments", [])

        student_count = len({e.get("student_id") for e in curriculum_enrollments}) or 0
        avg_progress = 0.0
        if curriculum_enrollments and student_count:
            total_progress = sum(e.get("progress_percentage", 0.0) for e in curriculum_enrollments)
            avg_progress = total_progress / len(curriculum_enrollments)

        quizzes = data.get("quizzes", {})
        student_quizzes: List[Dict[str, Any]] = quizzes.get("student_quizzes", [])
        quiz_attempts: List[Dict[str, Any]] = quizzes.get("quiz_attempts", [])

        engagement = {
            "classroom_id": classroom_id,
            "student_count": student_count,
            "average_curriculum_progress": round(avg_progress, 2),
            "quiz_participation_count": len(student_quizzes),
            "attempt_count": len(quiz_attempts),
        }
        if analysis_period_days:
            engagement["analysis_period_days"] = analysis_period_days

        return json.dumps(engagement)

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["classroom_engagement"],
                    "description": "Action to perform",
                    "default": "classroom_engagement",
                },
                "classroom_id": {
                    "type": "integer",
                    "description": "Classroom identifier",
                },
                "analysis_period_days": {
                    "type": "integer",
                    "description": "Optional analysis window in days",
                },
            },
            "required": ["classroom_id"],
        }


