"""
Mock lesson repository
Uses mock fixture to provide lesson data until real data source is wired.
"""

from typing import Optional

from app.core.data.lesson_repository import LessonRepository
from app.core.data.models import LessonDto, LessonSectionDto
from app.infrastructure.data.fixtures.mock_lesson_data import get_mock_lec_sec_data


class MockLessonRepository(LessonRepository):
    """Fallback repository backed by the mock fixture."""

    async def get_lesson_with_sections(self, lesson_id: Optional[str] = None) -> LessonDto:
        lesson = get_mock_lec_sec_data()

        sections = [
            LessonSectionDto(
                id=str(idx),
                title=section.get("title", ""),
                description=section.get("description", ""),
                duration_minutes=section.get("durationMinutes"),
            )
            for idx, section in enumerate(lesson.get("sections", []), start=1)
        ]

        return LessonDto(
            id=lesson_id,
            title=lesson.get("title", ""),
            description=lesson.get("description", ""),
            learning_outcomes=lesson.get("learningOutcomes", []),
            requirements=lesson.get("requirements", []),
            skills=lesson.get("skills", []),
            topics=lesson.get("topics", []),
            standards=lesson.get("standards", []),
            sections=sections,
        )

