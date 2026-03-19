from typing import Optional
from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
import logging

from app.api.http.dependencies import get_teacher_service
from app.features.teacher.service import TeacherService
from app.features.recommendations.models import InterventionResponse

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/teacher", tags=["teacher"])


class StudentAnalysisRequest(BaseModel):

    classroom_id: Optional[int] = None
    student_id: Optional[str] = None
    force_mock: bool = False
    analysis_period_days: Optional[int] = 7
    lang: Optional[str] = "vi"

    query: Optional[str] = None
    session_id: Optional[str] = None


class LessonAnalyticsRequest(BaseModel):
    lesson_id: str
    classroom_id: Optional[int] = None
    query: Optional[str] = None
    session_id: Optional[str] = None


class AutoGradingRequest(BaseModel):
    assignmentattemptId: int
    student_id: Optional[str] = None
    query: Optional[str] = None
    session_id: Optional[str] = None
    use_agent: bool = False


class BuildGraphRequest(BaseModel):
    classroom_id: int
    force_rebuild: bool = False


class TriggerProgressEventRequest(BaseModel):
    """Request model for triggering test progress event"""
    classroom_id: int
    student_id: str
    course_enrollment_id: int = 1
    course_id: int = 1
    progress_percentage: int = 50
    status: str = "InProgress"


@router.post("/student-analysis", response_model=InterventionResponse)
async def student_analysis(
    request: StudentAnalysisRequest,
    teacher_id: str = "teacher_123",
    teacher_service: TeacherService = Depends(get_teacher_service),
) -> InterventionResponse:
    try:
        result = await teacher_service.analyze_student(
            teacher_id=teacher_id,
            classroom_id=request.classroom_id,
            student_id=request.student_id,
            query=request.query,
            session_id=request.session_id,
            force_mock=request.force_mock,
            analysis_period_days=request.analysis_period_days,
            lang=request.lang,
        )
        return result
    except Exception as e:
        logger.error("[TeacherRouter] Error in student-analysis: %s", e, exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/auto-grading")
async def auto_grading(
    request: AutoGradingRequest,
    teacher_id: str = "teacher_123",
    teacher_service: TeacherService = Depends(get_teacher_service),
):
    """
    Run automated grading for a specific assignment attempt.
    
    Request body:
    - assignmentattemptId: ID of the assignment attempt to grade (required)
    - student_id: Optional student ID for personalized context from memory
    - query: Optional query/focus for grading
    - session_id: Optional session ID for context building
    """
    try:
        result = await teacher_service.auto_grade(
            teacher_id=teacher_id,
            assignment_attempt_id=request.assignmentattemptId,
            student_id=request.student_id,
            query=request.query,
            session_id=request.session_id,
            use_agent=request.use_agent,
        )
        return result
    except ValueError as e:
        error_msg = str(e)
        logger.error("[TeacherRouter] Validation error in auto-grading: %s", error_msg, exc_info=True)
        if "AssignmentAttemptClient" in error_msg:
            raise HTTPException(
                status_code=503,
                detail="Auto-grading service is currently unavailable. AssignmentAttempt proto files may not be generated yet."
            )
        raise HTTPException(status_code=400, detail=error_msg)
    except Exception as e:
        logger.error("[TeacherRouter] Error in auto-grading: %s", e, exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/build-graph")
async def build_graph(
    request: BuildGraphRequest,
    teacher_id: str = "teacher_123",
    teacher_service: TeacherService = Depends(get_teacher_service),
):
    """
    Build or rebuild the knowledge graph for a classroom.
    
    This creates all graph nodes and relationships including:
    - Level 1-4: Curriculum, Course, Lesson, Section, Content, Quiz, Assignment, Attempts
    - Level 5: Performance relationships (STRUGGLES_WITH, EXCELS_AT)
    
    This is required before using pattern recognition features that rely on
    STRUGGLES_WITH and EXCELS_AT relationships.
    """
    try:
        result = await teacher_service.build_classroom_graph(
            classroom_id=request.classroom_id,
            force_rebuild=request.force_rebuild,
        )
        return result
    except Exception as e:
        logger.error("[TeacherRouter] Error in build-graph: %s", e, exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/test/trigger-progress-event")
async def trigger_progress_event(
    request: TriggerProgressEventRequest,
    direct: bool = False,  # Query parameter: ?direct=true
):
    """
    Test endpoint to trigger ClassroomStudentProgressUpdatedEvent.
    
    This can work in two modes:
    1. RabbitMQ mode (default): Publishes event to RabbitMQ
    2. Direct mode (direct=true): Directly calls event handler (useful when RabbitMQ unavailable)
    
    The event will trigger:
    1. Snapshot refresh
    2. RAG ingestion (with debouncing - waits 5 minutes before ingesting)
    
    Use this to test the event-driven ingestion flow.
    """
    from app.core.snapshot.events import ClassroomEvent
    from app.api.http.dependencies import get_classroom_snapshot_event_handler
    
    # Create event payload matching C# event structure
    event_data = {
        "StudentId": request.student_id,
        "ClassroomId": request.classroom_id,
        "CourseEnrollmentId": request.course_enrollment_id,
        "CourseId": request.course_id,
        "ProgressPercentage": request.progress_percentage,
        "Status": request.status,
    }
    
    if direct:
        # Direct mode: Call event handler directly (bypass RabbitMQ)
        try:
            event_handler = get_classroom_snapshot_event_handler()
            classroom_event = ClassroomEvent(
                type="STUDENT_PROGRESS_UPDATED",
                classroom_id=request.classroom_id,
                student_id=request.student_id,
                payload={
                    "course_enrollment_id": request.course_enrollment_id,
                    "course_id": request.course_id,
                    "progress_percentage": request.progress_percentage,
                    "status": request.status,
                },
            )
            await event_handler.handle_event(classroom_event)
            
            logger.info(
                f"[TeacherRouter] Test progress event handled directly for classroom {request.classroom_id}, "
                f"student {request.student_id}"
            )
            
            return {
                "success": True,
                "message": "Event handled directly (bypassed RabbitMQ)",
                "event_data": event_data,
                "note": "Event was processed directly. RAG ingestion will be scheduled with 5-minute debounce.",
            }
        except Exception as e:
            logger.error("[TeacherRouter] Error handling event directly: %s", e, exc_info=True)
            raise HTTPException(status_code=500, detail=f"Failed to handle event: {str(e)}")
    else:
        # RabbitMQ mode: Publish to RabbitMQ
        try:
            import aio_pika
            import json
            from app.infrastructure.config.settings import settings
            
            exchange_name = "EventBus.Messages:ClassroomStudentProgressUpdatedEvent"
            routing_key = "EventBus.Messages:ClassroomStudentProgressUpdatedEvent"
            
            # Connect and publish
            connection = await aio_pika.connect_robust(settings.RABBITMQ_URL)
            
            try:
                async with connection:
                    channel = await connection.channel()
                    
                    # Declare exchange
                    try:
                        exchange = await channel.declare_exchange(
                            exchange_name,
                            aio_pika.ExchangeType.TOPIC,
                            durable=True,
                        )
                    except Exception:
                        exchange = await channel.declare_exchange(
                            exchange_name,
                            aio_pika.ExchangeType.FANOUT,
                            durable=True,
                        )
                    
                    # Publish message
                    message_body = json.dumps(event_data).encode("utf-8")
                    message = aio_pika.Message(
                        body=message_body,
                        content_type="application/json",
                        delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
                    )
                    
                    await exchange.publish(
                        message,
                        routing_key=routing_key,
                    )
                    
                    logger.info(
                        f"[TeacherRouter] Test progress event published to RabbitMQ for classroom {request.classroom_id}, "
                        f"student {request.student_id}"
                    )
                    
                    return {
                        "success": True,
                        "message": "Event published to RabbitMQ successfully",
                        "event_data": event_data,
                        "note": "Event will be consumed by ClassroomProgressEventConsumer. "
                               "RAG ingestion will be scheduled with 5-minute debounce.",
                    }
            finally:
                await connection.close()
                
        except Exception as e:
            logger.warning(
                f"[TeacherRouter] Failed to publish to RabbitMQ: {e}. "
                f"Hint: Use ?direct=true to test without RabbitMQ"
            )
            raise HTTPException(
                status_code=503,
                detail=f"Failed to publish event to RabbitMQ: {str(e)}. "
                       f"Use ?direct=true query parameter to test without RabbitMQ."
            )


