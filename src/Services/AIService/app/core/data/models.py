"""
Data Transfer Objects for core data layer
"""

from typing import List, Optional

from pydantic import BaseModel


class LessonSectionDto(BaseModel):
    """Normalized representation of a lesson section."""

    id: Optional[str] = None
    title: str
    description: str
    duration_minutes: Optional[int] = None


class LessonDto(BaseModel):
    """Lesson metadata with nested sections and taxonomies."""

    id: Optional[str] = None
    title: str
    description: str
    learning_outcomes: List[str] = []
    requirements: List[str] = []
    skills: List[str] = []
    topics: List[str] = []
    standards: List[str] = []
    sections: List[LessonSectionDto] = []

