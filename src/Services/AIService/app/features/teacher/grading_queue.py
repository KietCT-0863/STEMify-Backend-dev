from typing import Dict, Any, Optional, Callable, List
import logging
import asyncio
import uuid
from datetime import datetime
from enum import Enum

logger = logging.getLogger(__name__)


class GradingJobStatus(str, Enum):
    PENDING = "pending"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"


class GradingJob:
    
    def __init__(
        self,
        job_id: str,
        assignment_attempt_id: int,
        teacher_id: str,
        student_id: Optional[str] = None,
        query: Optional[str] = None,
        session_id: Optional[str] = None,
    ):
        self.job_id = job_id
        self.assignment_attempt_id = assignment_attempt_id
        self.teacher_id = teacher_id
        self.student_id = student_id
        self.query = query
        self.session_id = session_id
        self.status = GradingJobStatus.PENDING
        self.result: Optional[Dict[str, Any]] = None
        self.error: Optional[str] = None
        self.created_at = datetime.utcnow()
        self.started_at: Optional[datetime] = None
        self.completed_at: Optional[datetime] = None
    
    def to_dict(self) -> Dict[str, Any]:
        return {
            "job_id": self.job_id,
            "assignment_attempt_id": self.assignment_attempt_id,
            "teacher_id": self.teacher_id,
            "student_id": self.student_id,
            "status": self.status.value,
            "result": self.result,
            "error": self.error,
            "created_at": self.created_at.isoformat() if self.created_at else None,
            "started_at": self.started_at.isoformat() if self.started_at else None,
            "completed_at": self.completed_at.isoformat() if self.completed_at else None,
        }


class GradingQueue:
    
    def __init__(self, max_concurrent_jobs: int = 5):
        self.max_concurrent_jobs = max_concurrent_jobs
        self.jobs: Dict[str, GradingJob] = {}
        self.queue: asyncio.Queue = asyncio.Queue()
        self.active_jobs: set[str] = set()
        self._worker_task: Optional[asyncio.Task] = None
        self._grading_function: Optional[Callable] = None
       
    def set_grading_function(self, func: Callable) -> None:
        self._grading_function = func
    
    async def start_worker(self) -> None:
        if self._worker_task is None or self._worker_task.done():
            self._worker_task = asyncio.create_task(self._worker_loop())
            logger.info("GradingQueue worker started")
    
    async def stop_worker(self) -> None:
        if self._worker_task and not self._worker_task.done():
            self._worker_task.cancel()
            try:
                await self._worker_task
            except asyncio.CancelledError:
                pass
            logger.info("GradingQueue worker stopped")
    
    async def _worker_loop(self) -> None:
        while True:
            try:
                # Wait for a job
                job = await self.queue.get()
                
                # Check if we can process more jobs
                if len(self.active_jobs) >= self.max_concurrent_jobs:
                    # Put job back and wait
                    await self.queue.put(job)
                    await asyncio.sleep(1)
                    continue
                
                # Process job
                self.active_jobs.add(job.job_id)
                asyncio.create_task(self._process_job(job))
                
            except asyncio.CancelledError:
                break
            except Exception as e:
                logger.error(
                    "Error in grading queue worker loop",
                    extra={"error": str(e)},
                    exc_info=True
                )
                await asyncio.sleep(1)
    
    async def _process_job(self, job: GradingJob) -> None:
        job.status = GradingJobStatus.PROCESSING
        job.started_at = datetime.utcnow()
        
        logger.info(
            "[GradingQueue] Processing job",
            extra={
                "job_id": job.job_id,
                "assignment_attempt_id": job.assignment_attempt_id,
            }
        )
        
        try:
            if not self._grading_function:
                raise ValueError("Grading function not set")
            
            # Call the grading function
            result = await self._grading_function(
                teacher_id=job.teacher_id,
                assignment_attempt_id=job.assignment_attempt_id,
                student_id=job.student_id,
                query=job.query,
                session_id=job.session_id,
            )
            
            job.result = result
            job.status = GradingJobStatus.COMPLETED
            job.completed_at = datetime.utcnow()
            
            logger.info(
                "[GradingQueue] Job completed",
                extra={
                    "job_id": job.job_id,
                    "assignment_attempt_id": job.assignment_attempt_id,
                }
            )
            
        except Exception as e:
            job.status = GradingJobStatus.FAILED
            job.error = str(e)
            job.completed_at = datetime.utcnow()
            
            logger.error(
                "[GradingQueue] Job failed",
                extra={
                    "job_id": job.job_id,
                    "assignment_attempt_id": job.assignment_attempt_id,
                    "error": str(e),
                },
                exc_info=True
            )
        finally:
            self.active_jobs.discard(job.job_id)
    
    async def submit_job(
        self,
        assignment_attempt_id: int,
        teacher_id: str,
        student_id: Optional[str] = None,
        query: Optional[str] = None,
        session_id: Optional[str] = None,
    ) -> str:
        job_id = str(uuid.uuid4())
        job = GradingJob(
            job_id=job_id,
            assignment_attempt_id=assignment_attempt_id,
            teacher_id=teacher_id,
            student_id=student_id,
            query=query,
            session_id=session_id,
        )
        
        self.jobs[job_id] = job
        await self.queue.put(job)
        
        logger.info(
            "[GradingQueue] Job submitted",
            extra={
                "job_id": job_id,
                "assignment_attempt_id": assignment_attempt_id,
            }
        )
        
        return job_id
    
    def get_job_status(self, job_id: str) -> Optional[Dict[str, Any]]:
        job = self.jobs.get(job_id)
        if job:
            return job.to_dict()
        return None
    
    def get_job_result(self, job_id: str) -> Optional[Dict[str, Any]]:
        job = self.jobs.get(job_id)
        if job and job.status == GradingJobStatus.COMPLETED:
            return job.result
        return None
    
    def list_jobs(
        self,
        teacher_id: Optional[str] = None,
        status: Optional[GradingJobStatus] = None,
        limit: int = 100,
    ) -> List[Dict[str, Any]]:
        jobs = list(self.jobs.values())
        
        if teacher_id:
            jobs = [j for j in jobs if j.teacher_id == teacher_id]
        
        if status:
            jobs = [j for j in jobs if j.status == status]
        
        jobs.sort(key=lambda j: j.created_at, reverse=True)
        
        return [j.to_dict() for j in jobs[:limit]]
    
    def cleanup_old_jobs(self, max_age_hours: int = 24) -> int:
        """Remove old completed/failed jobs"""
        cutoff = datetime.utcnow().replace(hour=0, minute=0, second=0, microsecond=0)
        cutoff = cutoff.replace(hour=cutoff.hour - max_age_hours)
        
        removed = 0
        job_ids_to_remove = []
        
        for job_id, job in self.jobs.items():
            if job.completed_at and job.completed_at < cutoff:
                if job.status in (GradingJobStatus.COMPLETED, GradingJobStatus.FAILED):
                    job_ids_to_remove.append(job_id)
        
        for job_id in job_ids_to_remove:
            del self.jobs[job_id]
            removed += 1
        
        if removed > 0:
            logger.info(
                "[GradingQueue] Cleaned up old jobs",
                extra={"removed_count": removed}
            )
        
        return removed

