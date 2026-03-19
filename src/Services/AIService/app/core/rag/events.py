from typing import Dict, Any, Optional
import logging

from app.core.rag.ingestion_pipeline import IngestionPipeline
from app.core.rag.streaming_hooks import HeavyHitterCounter

logger = logging.getLogger(__name__)


class StreamingRAGEventHandler:
    """
    Simple event handler for driving Streaming RAG ingestion.

    This provides internal entrypoints that other services or
    cron jobs can call to refresh classroom/content data.
    """

    def __init__(
        self,
        ingestion_pipeline: IngestionPipeline,
        heavy_hitter_counter: Optional[HeavyHitterCounter] = None,
    ) -> None:
        self.ingestion_pipeline = ingestion_pipeline
        self.heavy_hitter_counter = heavy_hitter_counter or HeavyHitterCounter()

    async def handle_classroom_update(self, classroom_data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Handle a classroom-level update event by ingesting data and
        marking the classroom as a heavy hitter for future freshness.
        """
        classroom = classroom_data.get("classroom", {})
        classroom_id = classroom.get("id")

        if classroom_id is not None:
            self.heavy_hitter_counter.add(str(classroom_id))
            logger.info(
                "[StreamingRAGEventHandler] Classroom update received for id=%s",
                classroom_id,
            )

        return await self.ingestion_pipeline.ingest(classroom_data)


