from typing import Dict, Any, Optional
import logging
import json

from app.core.tools.base import Tool
from app.core.data.lesson_repository import LessonRepository

logger = logging.getLogger(__name__)


class LessonDataTool(Tool):
    def __init__(self, lesson_repository: LessonRepository):
        super().__init__(
            name="lesson_data",
            description="Fetch lesson metadata (sections, topics, skills) for analytics",
        )
        self.lesson_repository = lesson_repository

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - overview: Get basic lesson details and sections.

        Required parameters:
        - lesson_id
        """
        action = parameters.get("action", "overview")
        try:
            if action == "overview":
                return await self._lesson_overview(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[LessonDataTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _lesson_overview(self, parameters: Dict[str, Any]) -> str:
        lesson_id = parameters.get("lesson_id")
        if not lesson_id:
            return json.dumps({"error": "lesson_id is required"})

        lesson = await self.lesson_repository.get_lesson_with_sections(lesson_id=lesson_id)

        overview = {
            "lesson_id": lesson.id,
            "title": lesson.title,
            "description": lesson.description,
            "learning_outcomes": lesson.learning_outcomes,
            "requirements": lesson.requirements,
            "skills": lesson.skills,
            "topics": lesson.topics,
            "sections": [
                {
                    "id": s.id,
                    "title": s.title,
                    "description": s.description,
                    "duration_minutes": s.duration_minutes,
                }
                for s in lesson.sections
            ],
        }
        return json.dumps(overview)

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["overview"],
                    "description": "Action to perform",
                    "default": "overview",
                },
                "lesson_id": {
                    "type": "string",
                    "description": "Lesson identifier",
                },
            },
            "required": ["lesson_id"],
        }


