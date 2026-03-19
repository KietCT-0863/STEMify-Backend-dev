"""
Content Generation API Router
HTTP endpoints for AI content generation (lesson sections, etc.)
"""

import logging

import grpc
from fastapi import APIRouter, Depends, HTTPException, status
from openai import AuthenticationError as OpenAIAuthenticationError

from app.api.http.dependencies import get_content_generation_service
from app.features.content_generation.models import (
    LessonSectionRequest,
    LessonSectionResponse,
)
from app.features.content_generation.service import ContentGenerationService

router = APIRouter(prefix="/content", tags=["content-generation"])
logger = logging.getLogger(__name__)


@router.post(
    "/lesson-section",
    response_model=LessonSectionResponse,
    summary="Generate a new lesson section for a lesson (real or mock)",
)
async def generate_lesson_section(
    body: LessonSectionRequest,
    service: ContentGenerationService = Depends(get_content_generation_service),
) -> LessonSectionResponse:
    """
    Generate ONE new lesson section based on lesson metadata.

    - Nếu `lesson_id` được cung cấp, lấy dữ liệu từ repository
    - Nếu thiếu `lesson_id` hoặc force_mock=True, hệ thống dùng mock repository
    - Gọi LLM thông qua LLMClient để sinh section mới
    """
    try:
        return await service.generate_lesson_section(body)
    except ValueError as error:
        logger.warning("Invalid lesson section request", exc_info=error)
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(error),
        ) from error
    except OpenAIAuthenticationError as error:
        logger.error(
            "LLM API authentication failed",
            extra={"lesson_id": body.lesson_id},
            exc_info=error,
        )
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="LLM API authentication failed. Please check API key configuration.",
        ) from error
    except grpc.aio.AioRpcError as error:
        logger.exception(
            "Resource gRPC call failed",
            extra={"lesson_id": body.lesson_id, "grpc_code": error.code().name},
        )
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail="Resource service unavailable. Please try again later." + str(error),
        ) from error
    except Exception as error:  
        logger.exception("Unexpected error generating lesson section")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to generate lesson section.",
        ) from error