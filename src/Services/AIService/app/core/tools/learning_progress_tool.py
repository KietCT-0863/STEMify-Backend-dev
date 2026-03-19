from typing import Dict, Any, Optional
import logging
import json

from app.core.tools.base import Tool
from app.core.data.classroom_repository import ClassroomRepository

logger = logging.getLogger(__name__)


class LearningProgressTool(Tool):
    """
    Learning Progress Tool - MCP-compatible
    
    Query student learning progress, completed lessons, and achievements.
    Integrates with ClassroomRepository (gRPC) for data access.
    
    Available actions:
    - get_progress: Overall learning progress (completion rate, total/completed lessons)
    - get_completed_lessons: List of completed lessons
    - get_achievements: Achievements and milestones
    """
    
    VALID_ACTIONS = ["get_progress", "get_completed_lessons", "get_achievements"]
    
    def __init__(
        self,
        student_id: str,
        classroom_repository: Optional[ClassroomRepository] = None
    ):
        super().__init__(
            name="learning_progress",
            description=(
                "Query student learning progress. "
                "Actions: get_progress (completion rate), "
                "get_completed_lessons (list of completed), "
                "get_achievements (milestones). "
                "Example: {\"action\": \"get_progress\"}"
            )
        )
        self.student_id = student_id
        self.classroom_repository = classroom_repository
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Execute tool
        
        Actions:
        - get_progress: Get overall learning progress
        - get_completed_lessons: Get list of completed lessons
        - get_achievements: Get achievements/milestones
        """
        action = parameters.get("action", "get_progress")
        
        try:
            if action == "get_progress":
                return await self._get_progress(parameters)
            elif action == "get_completed_lessons":
                return await self._get_completed_lessons(parameters)
            elif action == "get_achievements":
                return await self._get_achievements(parameters)
            else:
                return json.dumps({
                    "error": f"Unknown action: '{action}'",
                    "valid_actions": self.VALID_ACTIONS,
                    "hint": "Use get_progress for overall learning progress"
                })
        except Exception as e:
            logger.error(f"[LearningProgressTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})
    
    async def _get_progress(self, parameters: Dict[str, Any]) -> str:
        """Get overall learning progress"""
        if not self.classroom_repository:
            # Return marker for early exit - no useful data available
            return json.dumps({
                "MISSING_REQUIRED_DATA": True,
                "completion_rate": 0,
                "total_lessons": 0,
                "completed_lessons": 0,
                "message": "I don't have access to your classroom data. Please make sure you're enrolled in a classroom.",
                "note": "ClassroomRepository not available"
            })
        
        try:
            data = await self.classroom_repository.get_classroom_data(
                student_id=self.student_id
            )
            
            # Extract progress from data structure
            students = data.get("students", [])
            student_data = next(
                (s for s in students if s.get("id") == self.student_id),
                None
            )
            
            if not student_data:
                return json.dumps({
                    "completion_rate": 0,
                    "total_lessons": 0,
                    "completed_lessons": 0,
                    "note": "Student not found"
                })
            
            # Calculate completion rate
            total_lessons = student_data.get("total_lessons", 0)
            completed_lessons = student_data.get("completed_lessons", 0)
            completion_rate = (
                (completed_lessons / total_lessons * 100)
                if total_lessons > 0 else 0
            )
            
            return json.dumps({
                "completion_rate": round(completion_rate, 2),
                "total_lessons": total_lessons,
                "completed_lessons": completed_lessons,
                "student_id": self.student_id
            })
        except Exception as e:
            logger.error(f"[LearningProgressTool] Error getting progress: {e}")
            return json.dumps({"error": str(e)})
    
    async def _get_completed_lessons(self, parameters: Dict[str, Any]) -> str:
        """Get list of completed lessons"""
        if not self.classroom_repository:
            return json.dumps({
                "lessons": [],
                "note": "ClassroomRepository not available"
            })
        
        try:
            data = await self.classroom_repository.get_classroom_data(
                student_id=self.student_id
            )
            
            students = data.get("students", [])
            student_data = next(
                (s for s in students if s.get("id") == self.student_id),
                None
            )
            
            if not student_data:
                return json.dumps({"lessons": []})
            
            completed_lessons = student_data.get("completed_lessons_list", [])
            
            return json.dumps({
                "lessons": completed_lessons,
                "count": len(completed_lessons),
                "student_id": self.student_id
            })
        except Exception as e:
            logger.error(f"[LearningProgressTool] Error getting completed lessons: {e}")
            return json.dumps({"error": str(e)})
    
    async def _get_achievements(self, parameters: Dict[str, Any]) -> str:
        """Get achievements/milestones"""
        if not self.classroom_repository:
            return json.dumps({
                "achievements": [],
                "note": "ClassroomRepository not available"
            })
        
        try:
            data = await self.classroom_repository.get_classroom_data(
                student_id=self.student_id
            )
            
            students = data.get("students", [])
            student_data = next(
                (s for s in students if s.get("id") == self.student_id),
                None
            )
            
            if not student_data:
                return json.dumps({"achievements": []})
            
            achievements = student_data.get("achievements", [])
            
            return json.dumps({
                "achievements": achievements,
                "count": len(achievements),
                "student_id": self.student_id
            })
        except Exception as e:
            logger.error(f"[LearningProgressTool] Error getting achievements: {e}")
            return json.dumps({"error": str(e)})
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": self.VALID_ACTIONS,
                    "description": (
                        "Action: get_progress (completion rate), "
                        "get_completed_lessons (list), get_achievements (milestones)"
                    ),
                    "default": "get_progress"
                }
            },
            "required": []
        }

