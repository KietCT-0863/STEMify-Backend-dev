"""
Content Generation Service
Business logic for generating educational content
"""

import json
import logging
from typing import Optional

from app.core.data.lesson_repository import LessonRepository
from app.core.data.models import LessonDto
from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import LLMMessage
from app.features.content_generation.models import (
    LessonSectionRequest,
    LessonSectionResponse,
    GeneratedLessonSection,
)
from app.features.content_generation.prompts import (
    build_lesson_context,
    build_section_prompt,
)
from app.infrastructure.config.settings import settings
from app.infrastructure.data.grpc_lesson_repository import GrpcLessonRepository

logger = logging.getLogger(__name__)


class ContentGenerationService:
    """
    Service for generating educational content (e.g., new lesson sections).
    """

    def __init__(self, lesson_repository: LessonRepository, llm_client: LLMClient):
        self.lesson_repository = lesson_repository
        self.llm_client = llm_client

    async def generate_lesson_section(
        self, request: LessonSectionRequest
    ) -> LessonSectionResponse:
        """
        Generate a new lesson section from lesson metadata (real or mock).
        """
        lesson = await self._load_lesson(request)
        
        # Check if fallback was used (only applicable for GrpcLessonRepository)
        is_fallback = False
        if isinstance(self.lesson_repository, GrpcLessonRepository):
            is_fallback = self.lesson_repository.was_fallback_used()
        
        context_text = build_lesson_context(lesson)
        lang = request.lang or "vi"
        prompt = build_section_prompt(context_text, lang=lang)

        logger.info("Generating lesson section via LLM client", extra={"lesson_id": lesson.id})

        response = await self.llm_client.generate_remote(
            [
                LLMMessage(
                    role="system",
                    content=settings.CONTENT_GENERATION_SYSTEM_PROMPT,
                ),
                LLMMessage(role="user", content=prompt),
            ],
            temperature=settings.LLM_TEMPERATURE,
            max_tokens=settings.LLM_MAX_TOKENS,
        )

        section = self._parse_section_from_answer(response.content)

        return LessonSectionResponse(
            section=section,
            provider="remote",
            model=response.model,
            is_fallback_data=is_fallback,
        )

    async def _load_lesson(self, request: LessonSectionRequest) -> LessonDto:
        """Retrieve lesson data based on request preferences."""
        if request.force_mock:
            return await self.lesson_repository.get_lesson_with_sections(None)

        if not request.lesson_id:
            raise ValueError("lesson_id is required unless force_mock is True")

        return await self.lesson_repository.get_lesson_with_sections(request.lesson_id)

    def _parse_section_from_answer(self, answer: str) -> GeneratedLessonSection:
        """
        Parse the LLM answer into a GeneratedLessonSection.
        Tries to be robust to code fences or extra text.
        """
        raw = answer.strip()

        # Strip Markdown code fences if present
        if raw.startswith("```"):
            # remove leading/trailing backticks
            raw = raw.strip("`")
            # Drop language tag if present
            if "\n" in raw:
                first_line, rest = raw.split("\n", 1)
                if first_line.strip().startswith("{"):
                    raw = first_line + "\n" + rest
                else:
                    raw = rest

        # Find first JSON object in the text
        json_str = raw
        start = raw.find("{")
        end = raw.rfind("}")
        if start != -1 and end != -1 and end > start:
            json_str = raw[start : end + 1]

        try:
            data = json.loads(json_str)
        except json.JSONDecodeError:
            logger.warning("LLM output is not valid JSON, falling back to defaults")
            return GeneratedLessonSection(
                title="New Section",
                durationMinutes=10,
                description=answer.strip(),
                raw_answer=answer,
            )

        title = str(data.get("title", "New Section"))
        duration = data.get("durationMinutes", 10)
        try:
            duration_int = int(duration)
        except (TypeError, ValueError):
            duration_int = 10

        description = str(data.get("description", "")).strip() or answer.strip()

        return GeneratedLessonSection(
            title=title,
            durationMinutes=duration_int,
            description=description,
            raw_answer=answer,
        )
