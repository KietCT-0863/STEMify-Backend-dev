from typing import Optional

from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
import logging

from app.api.http.dependencies import get_classroom_snapshot_event_handler
from app.core.snapshot.events import ClassroomEvent, ClassroomSnapshotEventHandler


logger = logging.getLogger(__name__)

router = APIRouter(prefix="/internal/events", tags=["internal-events"])


class ClassroomProgressUpdatedEventDTO(BaseModel):

    student_id: str
    classroom_id: Optional[int] = None
    course_enrollment_id: int
    course_id: int
    progress_percentage: int
    status: str


@router.post("/classroom-progress")
async def handle_classroom_progress_event(
    event: ClassroomProgressUpdatedEventDTO,
    handler: ClassroomSnapshotEventHandler = Depends(
        get_classroom_snapshot_event_handler
    ),
) -> dict:
    if event.classroom_id is None:
        raise HTTPException(
            status_code=400,
            detail="classroom_id is required for snapshot updates",
        )

    classroom_event = ClassroomEvent(
        type="STUDENT_PROGRESS_UPDATED",
        classroom_id=event.classroom_id,
        student_id=event.student_id,
        payload={
            "course_enrollment_id": event.course_enrollment_id,
            "course_id": event.course_id,
            "progress_percentage": event.progress_percentage,
            "status": event.status,
        },
    )

    await handler.handle_event(classroom_event)

    return {"status": "ok"}



