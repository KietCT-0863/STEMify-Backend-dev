"""
Microbit Explain Error API Router
HTTP endpoints for explaining microbit errors
"""

import logging

import grpc
from fastapi import APIRouter, Depends, HTTPException, status
from openai import AuthenticationError as OpenAIAuthenticationError

from app.api.http.dependencies import get_microbit_explain_error_service
from app.features.microbit_explain_error.models import (
    MicrobitExplainErrorRequest,
    MicrobitExplainErrorResponse,
)
from app.features.microbit_explain_error.service import MicrobitExplainErrorService

router = APIRouter(prefix="/microbit", tags=["microbit"])
logger = logging.getLogger(__name__)


@router.post("/explain-error", response_model=MicrobitExplainErrorResponse, summary="Explain a microbit error")
async def explain_microbit_error(
    body: MicrobitExplainErrorRequest,
    service: MicrobitExplainErrorService = Depends(get_microbit_explain_error_service),
) -> MicrobitExplainErrorResponse:
    """
    Explain a microbit error.
    """
    try:
        return await service.explain_microbit_error(body)
    except Exception as e:
        logger.error(f"Error explaining microbit error: {e}")
        raise HTTPException(status_code=500, detail=str(e))