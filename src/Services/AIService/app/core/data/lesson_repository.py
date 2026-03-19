"""
Lesson repository interface
"""

from abc import ABC, abstractmethod
from typing import Optional

from app.core.data.models import LessonDto


class LessonRepository(ABC):
    """Abstract lesson repository retrieving structured lesson data."""

    @abstractmethod
    async def get_lesson_with_sections(self, lesson_id: Optional[str] = None) -> LessonDto:
        """
        Fetch lesson details along with sections/topics/skills.

        Args:
            lesson_id: Identifier of lesson to fetch. Optional for mocks.
        """
        raise NotImplementedError
