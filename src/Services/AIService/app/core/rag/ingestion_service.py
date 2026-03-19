import asyncio
from typing import Dict, Any, Optional, Set
from datetime import datetime, timedelta
import logging

from app.core.rag.ingestion_pipeline import IngestionPipeline
from app.core.data.classroom_repository import ClassroomRepository

logger = logging.getLogger(__name__)


class IngestionService:    
    def __init__(
        self,
        ingestion_pipeline: IngestionPipeline,
        classroom_repository: ClassroomRepository,
        debounce_seconds: int = 300,  # 5 minutes default
        ingestion_ttl_hours: int = 24,  # Consider ingestion stale after 24h
    ):
        self.ingestion_pipeline = ingestion_pipeline
        self.classroom_repository = classroom_repository
        self.debounce_seconds = debounce_seconds
        self.ingestion_ttl_hours = ingestion_ttl_hours
        
        self._pending_tasks: Dict[int, asyncio.Task] = {}
        self._ingestion_status: Dict[int, datetime] = {}
        
        self._ingesting: Set[int] = set()
        
        logger.info(
            f"[IngestionService] Initialized with debounce={debounce_seconds}s, "
            f"ttl={ingestion_ttl_hours}h"
        )
    
    async def schedule_ingestion(
        self,
        classroom_id: int,
        force: bool = False,
    ) -> bool:
        if not force and self._is_recently_ingested(classroom_id):
            logger.debug(
                f"[IngestionService] Classroom {classroom_id} already ingested recently, skipping"
            )
            return False
        
        if classroom_id in self._pending_tasks:
            task = self._pending_tasks[classroom_id]
            if not task.done():
                task.cancel()
                logger.debug(
                    f"[IngestionService] Cancelled pending ingestion for classroom {classroom_id}"
                )
        
        if force:
            asyncio.create_task(self._ingest_classroom(classroom_id))
            return True
        else:
            async def debounced_ingest():
                try:
                    await asyncio.sleep(self.debounce_seconds)
                    await self._ingest_classroom(classroom_id)
                except asyncio.CancelledError:
                    logger.debug(
                        f"[IngestionService] Debounced ingestion cancelled for classroom {classroom_id}"
                    )
                    raise
            
            self._pending_tasks[classroom_id] = asyncio.create_task(debounced_ingest())
            logger.info(
                f"[IngestionService] Scheduled ingestion for classroom {classroom_id} "
                f"(debounce={self.debounce_seconds}s)"
            )
            return True
    
    async def ensure_ingested(
        self,
        classroom_id: int,
        max_wait_seconds: int = 0,
    ) -> bool:
       
        if self._is_recently_ingested(classroom_id):
            return True
        
        if classroom_id in self._ingesting:
            if max_wait_seconds > 0:
                wait_interval = 1
                waited = 0
                while classroom_id in self._ingesting and waited < max_wait_seconds:
                    await asyncio.sleep(wait_interval)
                    waited += wait_interval
                return self._is_recently_ingested(classroom_id)
            return True
        
        logger.info(
            f"[IngestionService] Triggering immediate ingestion for classroom {classroom_id} "
            f"(lazy loading)"
        )
        asyncio.create_task(self._ingest_classroom(classroom_id))
        return True
    
    def _is_recently_ingested(self, classroom_id: int) -> bool:
        if classroom_id not in self._ingestion_status:
            return False
        
        ingestion_time = self._ingestion_status[classroom_id]
        age = datetime.utcnow() - ingestion_time
        return age < timedelta(hours=self.ingestion_ttl_hours)
    
    async def _ingest_classroom(self, classroom_id: int) -> None:

        # Prevent concurrent ingestion of same classroom
        if classroom_id in self._ingesting:
            logger.warning(
                f"[IngestionService] Classroom {classroom_id} is already being ingested, skipping"
            )
            return
        
        self._ingesting.add(classroom_id)
        
        try:
            logger.info(f"[IngestionService] Starting ingestion for classroom {classroom_id}")
            
            # Fetch classroom data
            classroom_data = await self.classroom_repository.get_classroom_data(
                classroom_id=classroom_id,
                student_id=None,
                analysis_period_days=None,
            )
            
            if not classroom_data:
                logger.warning(
                    f"[IngestionService] No data found for classroom {classroom_id}, skipping ingestion"
                )
                return
            
            # Perform ingestion
            result = await self.ingestion_pipeline.ingest(classroom_data)
            
            if result.get("errors"):
                logger.error(
                    f"[IngestionService] Ingestion completed with errors for classroom {classroom_id}: "
                    f"{result['errors']}"
                )
            else:
                # Mark as ingested
                self._ingestion_status[classroom_id] = datetime.utcnow()
                logger.info(
                    f"[IngestionService] Successfully ingested classroom {classroom_id}: "
                    f"{result.get('documents_stored', 0)} documents, "
                    f"{result.get('graph_nodes', 0)} graph nodes"
                )
            
            # Clean up pending task
            if classroom_id in self._pending_tasks:
                del self._pending_tasks[classroom_id]
                
        except Exception as e:
            logger.error(
                f"[IngestionService] Error ingesting classroom {classroom_id}: {e}",
                exc_info=True
            )
        finally:
            self._ingesting.discard(classroom_id)
    
    def get_ingestion_status(self, classroom_id: int) -> Optional[datetime]:
        return self._ingestion_status.get(classroom_id)
    
    def is_ingesting(self, classroom_id: int) -> bool:
        return classroom_id in self._ingesting

