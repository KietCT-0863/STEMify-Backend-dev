from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, Optional

import logging

from app.core.snapshot.classroom_snapshot_store import (
    ClassroomSnapshotStore,
    ClassroomSnapshotUpdater,
)
from app.core.rag.ingestion_service import IngestionService


logger = logging.getLogger(__name__)


@dataclass
class ClassroomEvent:
    type: str
    classroom_id: int
    student_id: Optional[str] = None
    payload: Optional[Dict[str, Any]] = None


class ClassroomSnapshotEventHandler:
    def __init__(
        self,
        snapshot_store: ClassroomSnapshotStore,
        snapshot_updater: ClassroomSnapshotUpdater,
        ingestion_service: Optional[IngestionService] = None,
    ) -> None:
        self._snapshot_store = snapshot_store
        self._snapshot_updater = snapshot_updater
        self._ingestion_service = ingestion_service

    async def handle_event(self, event: ClassroomEvent) -> None:
        try:
            classroom_id = event.classroom_id
            if classroom_id is None:
                logger.warning(
                    "[ClassroomSnapshotEventHandler] Missing classroom_id on event",
                    extra={"event_type": event.type},
                )
                return

            payload = event.payload or {}
            force_full_refresh = bool(payload.get("force_full_refresh", True))
            analysis_period_days = payload.get("analysis_period_days")

            if payload:
                snapshot = self._snapshot_store.update_with_delta(
                    classroom_id=classroom_id,
                    delta=payload,
                )
                if snapshot and not force_full_refresh:
                    logger.debug(
                        "[ClassroomSnapshotEventHandler] Applied delta update from event",
                        extra={
                            "classroom_id": classroom_id,
                            "event_type": event.type,
                            "force_full_refresh": force_full_refresh,
                        },
                    )
                    return

           
            logger.info(
                "[ClassroomSnapshotEventHandler] Falling back to snapshot refresh from event",
                extra={
                    "classroom_id": classroom_id,
                    "event_type": event.type,
                    "force_full_refresh": force_full_refresh,
                },
            )
            await self._snapshot_updater.get_or_refresh_snapshot(
                classroom_id=classroom_id,
                student_id=event.student_id,
                analysis_period_days=analysis_period_days,
                force_full_refresh=force_full_refresh,
            )
            if self._ingestion_service:
                try:
                    await self._ingestion_service.schedule_ingestion(
                        classroom_id=classroom_id,
                        force=False,
                    )
                except Exception as ingestion_error:
                    logger.warning(
                        f"[ClassroomSnapshotEventHandler] Failed to schedule ingestion for "
                        f"classroom {classroom_id}: {ingestion_error}",
                        exc_info=True
                    )
        except Exception as exc:
            logger.exception(
                "[ClassroomSnapshotEventHandler] Failed to handle event",
                extra={"event_type": getattr(event, "type", None)},
                exc_info=exc,
            )


