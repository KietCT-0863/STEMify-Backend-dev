"""
Recommendations API Router
HTTP endpoints for AI-powered student progress analysis and intervention recommendations
"""

import logging

import grpc
from fastapi import APIRouter, Depends, HTTPException, status
from openai import AuthenticationError as OpenAIAuthenticationError

from app.api.http.dependencies import get_recommendations_service
from app.common.exceptions.ai_exceptions import LLMResponseParseError
from app.features.recommendations.models import (
    StudentProgressRequest,
    InterventionResponse,
)
from app.features.recommendations.service import RecommendationsService

router = APIRouter(prefix="/recommendations", tags=["recommendations"])
logger = logging.getLogger(__name__)


@router.post(
    "/analyze-progress",
    response_model=InterventionResponse,
    summary="Analyze student progress and generate intervention recommendations",
)
async def analyze_student_progress(
    body: StudentProgressRequest,
    service: RecommendationsService = Depends(get_recommendations_service),
) -> InterventionResponse:
    """
    Analyze student progress and generate personalized intervention recommendations.
    
    This endpoint:
    - Analyzes student performance data (quizzes, assignments, engagement)
    - Identifies weak topics and learning gaps
    - Generates actionable intervention recommendations
    - Provides prioritized suggestions for teachers
    
    Similar to systems like Khan Academy, ALEKS, IXL, and Renaissance Learning,
    but customized for hands-on and project-based STEM learning.
    
    - Nếu `classroom_id` được cung cấp, lấy dữ liệu từ repository (TODO: implement)
    - Nếu thiếu `classroom_id` hoặc `force_mock=True`, hệ thống dùng mock repository
    - Nếu `student_id` được cung cấp, chỉ phân tích học sinh đó
    - Nếu không, phân tích tất cả học sinh trong lớp
    """
    try:
        return await service.analyze_student_progress(body)
    except ValueError as error:
        logger.warning("Invalid student progress request", exc_info=error)
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(error),
        ) from error
    except LLMResponseParseError as error:
        logger.error("Failed to parse LLM response", exc_info=error)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to parse LLM response: {str(error)}",
        ) from error
    except OpenAIAuthenticationError as error:
        logger.error(
            "LLM API authentication failed",
            extra={
                "classroom_id": body.classroom_id,
                "student_id": body.student_id,
            },
            exc_info=error,
        )
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="LLM API authentication failed. Please check API key configuration.",
        ) from error
    except grpc.aio.AioRpcError as error:
        logger.exception(
            "Resource gRPC call failed",
            extra={
                "classroom_id": body.classroom_id,
                "student_id": body.student_id,
                "grpc_code": error.code().name,
            },
        )
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail="Resource service unavailable. Please try again later.",
        ) from error
    except Exception as error:
        logger.exception("Unexpected error analyzing student progress")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to analyze student progress and generate recommendations.",
        ) from error
