
import asyncio
import logging
from contextlib import asynccontextmanager
from typing import Optional

from app.api.http.routers import microbit_explain_error, microbit_analyze_project, events
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from app.api.http.routers import content_generation, recommendations, student, teacher, staff
from app.api.http.dependencies import get_classroom_snapshot_event_handler
from app.infrastructure.events.rabbitmq_consumer import ClassroomProgressEventConsumer
from app.infrastructure.config.settings import settings


logging.basicConfig(
       level=logging.INFO,
       format="%(asctime)s [%(name)s] %(levelname)s: %(message)s",
   )

logger = logging.getLogger(__name__)

# Global consumer instance
_consumer: Optional[ClassroomProgressEventConsumer] = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    global _consumer
    
    if settings.ENABLE_EVENT_CONSUMER:
        try:
            event_handler = get_classroom_snapshot_event_handler()
            _consumer = ClassroomProgressEventConsumer(event_handler=event_handler)
            await _consumer.connect()
            await _consumer.setup_queue()
            await _consumer.start_consuming()
            logger.info("[Main] Event consumer started successfully")
        except Exception as e:
            logger.error(
                "[Main] Failed to start event consumer: %s",
                e,
                exc_info=True,
            )
            # Continue without consumer if it fails
            _consumer = None
    else:
        logger.info("[Main] Event consumer is disabled")

    yield
    
    logger.info("[Main] Shutting down AI Service...")
    if _consumer:
        try:
            await _consumer.stop_consuming()
            logger.info("[Main] Event consumer stopped")
        except Exception as e:
            logger.error(
                "[Main] Error stopping event consumer: %s",
                e,
                exc_info=True,
            )


app = FastAPI(
    title="STEMify AI Service",
    version="1.0.0",
    description="AI Service for STEMify platform",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Health check endpoint
@app.get("/health")
async def health_check():
    """Health check endpoint for container orchestration."""
    return JSONResponse(content={"status": "healthy", "service": "ai-service"})

# HTTP routers
app.include_router(content_generation.router, prefix="/api/v1")
app.include_router(recommendations.router, prefix="/api/v1")
app.include_router(microbit_explain_error.router, prefix="/api/v1")
app.include_router(student.router)
app.include_router(teacher.router, prefix="/api/v1")
app.include_router(staff.router, prefix="/api/v1")
app.include_router(microbit_analyze_project.router, prefix="/api/v1")
app.include_router(events.router, prefix="/api/v1")