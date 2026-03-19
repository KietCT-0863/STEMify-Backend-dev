from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timedelta
from typing import Any, Dict, Optional

import logging

from app.core.data.classroom_repository import ClassroomRepository


logger = logging.getLogger(__name__)


@dataclass
class ClassroomSnapshot:

    classroom_id: int
    data: Dict[str, Any]
    last_full_refresh_at: datetime
    last_delta_at: Optional[datetime] = None


class ClassroomSnapshotStore:


    def __init__(self, full_refresh_cooldown_seconds: int = 60) -> None:
        self._snapshots: Dict[int, ClassroomSnapshot] = {}
        self._cooldown = timedelta(seconds=full_refresh_cooldown_seconds)

    def get_snapshot(self, classroom_id: int) -> Optional[ClassroomSnapshot]:
        return self._snapshots.get(classroom_id)

    def set_full_snapshot(self, classroom_id: int, data: Dict[str, Any]) -> ClassroomSnapshot:
        now = datetime.utcnow()
        snapshot = ClassroomSnapshot(
            classroom_id=classroom_id,
            data=data,
            last_full_refresh_at=now,
            last_delta_at=now,
        )
        self._snapshots[classroom_id] = snapshot
        logger.info(
            "[ClassroomSnapshotStore] Stored full snapshot",
            extra={"classroom_id": classroom_id},
        )
        return snapshot

    def update_with_delta(self, classroom_id: int, delta: Dict[str, Any]) -> Optional[ClassroomSnapshot]:
      
        snapshot = self._snapshots.get(classroom_id)
        if not snapshot:
            logger.warning(
                "[ClassroomSnapshotStore] Tried to apply delta to missing snapshot",
                extra={"classroom_id": classroom_id},
            )
            return None

        snapshot.data.update(delta)
        snapshot.last_delta_at = datetime.utcnow()
        logger.debug(
            "[ClassroomSnapshotStore] Applied delta update",
            extra={"classroom_id": classroom_id},
        )
        return snapshot

    def needs_full_refresh(self, classroom_id: int) -> bool:
        
        snapshot = self._snapshots.get(classroom_id)
        if snapshot is None:
            return True

        age = datetime.utcnow() - snapshot.last_full_refresh_at
        return age >= self._cooldown


class ClassroomSnapshotUpdater:

    def __init__(
        self,
        classroom_repository: ClassroomRepository,
        snapshot_store: ClassroomSnapshotStore,
    ) -> None:
        self._classroom_repository = classroom_repository
        self._snapshot_store = snapshot_store

    async def get_or_refresh_snapshot(
        self,
        classroom_id: int,
        *,
        student_id: Optional[str] = None,
        analysis_period_days: Optional[int] = None,
        force_full_refresh: bool = False,
    ) -> ClassroomSnapshot:
       
        if classroom_id is None:
            raise ValueError("classroom_id is required for snapshot refresh")

        snapshot = self._snapshot_store.get_snapshot(classroom_id)

        if not force_full_refresh and snapshot and not self._snapshot_store.needs_full_refresh(classroom_id):
            logger.info(
                "[ClassroomSnapshotUpdater] Using cached snapshot within cooldown window",
                extra={"classroom_id": classroom_id},
            )
            return snapshot

        logger.info(
            "[ClassroomSnapshotUpdater] Performing full refresh for classroom",
            extra={
                "classroom_id": classroom_id,
                "reason": "force" if force_full_refresh else "cooldown_expired_or_missing",
            },
        )

        data = await self._classroom_repository.get_classroom_data(
            classroom_id=classroom_id,
            student_id=student_id,
            analysis_period_days=analysis_period_days,
        )
        return self._snapshot_store.set_full_snapshot(classroom_id, data)


