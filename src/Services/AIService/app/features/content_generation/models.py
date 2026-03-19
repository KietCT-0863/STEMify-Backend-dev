"""
Content Generation Models
Domain models for content generation
"""

from typing import Optional

from pydantic import BaseModel, Field


class LessonSectionRequest(BaseModel):
    """
    Request model for generating a new lesson section.
    """

    lesson_id: Optional[str] = Field(
        default=None,
         )
    force_mock: bool = Field(
        default=False,
       )
    lang: Optional[str] = Field(
        default="vi",
        description="Language code for the response (e.g., 'vi' for Vietnamese, 'en' for English).",
    )


class GeneratedLessonSection(BaseModel):
    """Structured representation of a generated lesson section."""

    title: str = Field(..., description="Section title")
    durationMinutes: int = Field(..., ge=0, description="Section duration in minutes")
    description: str = Field(..., description="Section description")

    raw_answer: Optional[str] = Field(
        default=None,
        description="Raw LLM answer (for debugging / inspection).",
    )


class LessonSectionResponse(BaseModel):
    """API response wrapper for generated lesson section."""

    section: GeneratedLessonSection
    provider: str = Field(default="llm-remote", description="LLM provider used")
    model: str = Field(default="gpt-4o-mini", description="Model name used")
    is_fallback_data: bool = Field(
        default=False,
        description="True if fallback mock data was used instead of real gRPC data",
    )