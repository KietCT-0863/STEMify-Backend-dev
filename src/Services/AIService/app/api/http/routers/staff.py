from typing import Optional
from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
import logging

from app.api.http.dependencies import get_staff_service
from app.features.staff.service import StaffService

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/staff", tags=["staff"])


class GenerateCourseRequest(BaseModel):
    subject: str
    level: str
    duration: str
    requirements: Optional[dict] = None
    session_id: Optional[str] = None


class Generate3DDescriptionRequest(BaseModel):
    image_path: Optional[str] = None
    image_base64: Optional[str] = None
    model_type: str = "unknown"
    context: Optional[str] = None
    session_id: Optional[str] = None


class GenerateKitDescriptionRequest(BaseModel):
    kit_id: str
    context: Optional[str] = None
    session_id: Optional[str] = None


class GenerateStepDescriptionRequest(BaseModel):
    model_id: str
    action_type: str = "assembly"  # assembly, usage, disassembly
    model_data: Optional[dict] = None
    session_id: Optional[str] = None


class GenerateCategoriesRequest(BaseModel):
    content_type: str  # course, lesson, kit, model
    scope: str = "comprehensive"  # basic, comprehensive, advanced
    content_items: Optional[list] = None
    session_id: Optional[str] = None




@router.post("/generate-step-description")
async def generate_step_description(
    request: GenerateStepDescriptionRequest,
    staff_id: str = "staff_123",
    staff_service: StaffService = Depends(get_staff_service),
):
    """
    Generate step-by-step instructions using StepDescriptionAgent.
    """
    try:
        result = await staff_service.generate_step_description(
            staff_id=staff_id,
            model_id=request.model_id,
            action_type=request.action_type,
            model_data=request.model_data,
            session_id=request.session_id,
        )
        return result
    except Exception as e:
        logger.error("[StaffRouter] Error in generate-step-description: %s", e, exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))
