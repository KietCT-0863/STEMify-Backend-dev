"""
Unit tests for LearningProgressTool
"""

import asyncio
import json
from unittest.mock import AsyncMock, MagicMock

from app.core.tools.learning_progress_tool import LearningProgressTool
from app.core.data.classroom_repository import ClassroomRepository


class MockClassroomRepository(ClassroomRepository):
    async def get_classroom_data(self, classroom_id=None, student_id=None, analysis_period_days=None):
        return {
            "students": [
                {
                    "id": "student_123",
                    "total_lessons": 10,
                    "completed_lessons": 5,
                    "completed_lessons_list": ["lesson_1", "lesson_2", "lesson_3"],
                    "achievements": ["first_completion", "perfect_score"]
                }
            ]
        }


def test_learning_progress_tool_get_progress():
    """Test getting learning progress"""
    tool = LearningProgressTool(
        student_id="student_123",
        classroom_repository=MockClassroomRepository()
    )
    
    async def run():
        result = await tool.run({"action": "get_progress"})
        data = json.loads(result)
        assert data["completion_rate"] == 50.0
        assert data["total_lessons"] == 10
        assert data["completed_lessons"] == 5
    
    asyncio.get_event_loop().run_until_complete(run())


def test_learning_progress_tool_get_completed_lessons():
    """Test getting completed lessons"""
    tool = LearningProgressTool(
        student_id="student_123",
        classroom_repository=MockClassroomRepository()
    )
    
    async def run():
        result = await tool.run({"action": "get_completed_lessons"})
        data = json.loads(result)
        assert len(data["lessons"]) == 3
        assert data["count"] == 3
    
    asyncio.get_event_loop().run_until_complete(run())


def test_learning_progress_tool_get_achievements():
    """Test getting achievements"""
    tool = LearningProgressTool(
        student_id="student_123",
        classroom_repository=MockClassroomRepository()
    )
    
    async def run():
        result = await tool.run({"action": "get_achievements"})
        data = json.loads(result)
        assert len(data["achievements"]) == 2
        assert "first_completion" in data["achievements"]
    
    asyncio.get_event_loop().run_until_complete(run())

