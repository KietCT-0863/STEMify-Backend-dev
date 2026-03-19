from typing import Optional
from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
import logging

from app.api.http.dependencies import get_student_service
from app.features.student.service import StudentService

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/student", tags=["student"])


class LearningAdviceRequest(BaseModel):
    query: str
    session_id: Optional[str] = None


class ChatRequest(BaseModel):
    query: str
    session_id: Optional[str] = None


@router.post("/learning-advice")
async def get_learning_advice(
    request: LearningAdviceRequest,
    student_id: str = "student_123", 
    student_service: StudentService = Depends(get_student_service)
):
    """
    Get personalized learning advice
    
    Examples:
    - "What should I study next?"
    - "I'm struggling with Python functions"
    - "How am I doing in Math?"
    """
    try:
        result = await student_service.get_learning_advice(
            student_id=student_id,
            query=request.query,
            session_id=request.session_id
        )
        return result
    except Exception as e:
        logger.error(f"[StudentRouter] Error in learning-advice: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))


