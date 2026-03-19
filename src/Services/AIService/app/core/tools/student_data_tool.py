from typing import Dict, Any, Optional
import logging
import json

from app.core.tools.base import Tool
from app.core.data.classroom_repository import ClassroomRepository
from app.core.snapshot.classroom_snapshot_store import (
    ClassroomSnapshotStore,
    ClassroomSnapshotUpdater,
)

logger = logging.getLogger(__name__)


class StudentDataTool(Tool):

    def __init__(
        self,
        classroom_repository: Optional[ClassroomRepository] = None,
        snapshot_updater: Optional[ClassroomSnapshotUpdater] = None,
        snapshot_store: Optional[ClassroomSnapshotStore] = None,
    ):
        super().__init__(
            name="student_data",
            description=(
                "Query comprehensive student performance and engagement data for teacher analysis. "
                "Backed by ClassroomSnapshotStore when available to avoid excessive gRPC calls."
            ),
        )
        # For backward compatibility, allow direct repository usage if snapshot infra is not wired yet.
        self.classroom_repository = classroom_repository
        self.snapshot_updater = snapshot_updater
        self.snapshot_store = snapshot_store

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - overview: Overall stats for a student in a classroom.
        - class_overview: Aggregated class stats.
        - detailed_data: Detailed classroom data including quizAttempts, assignments, sectionProgress, etc.

        Required parameters:
        - student_id (for overview)
        - classroom_id (for both)
        """
        action = parameters.get("action", "overview")
        try:
            if action == "overview":
                return await self._student_overview(parameters)
            if action == "class_overview":
                return await self._class_overview(parameters)
            if action == "detailed_data":
                return await self._detailed_data(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[StudentDataTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _student_overview(self, parameters: Dict[str, Any]) -> str:
        student_id = parameters.get("student_id")
        classroom_id = parameters.get("classroom_id")
        force_mock = bool(parameters.get("force_mock", False))
        analysis_period_days = parameters.get("analysis_period_days")
        if not student_id:
            return json.dumps({"error": "student_id is required"})

        data = await self._get_classroom_data(
            classroom_id=classroom_id,
            student_id=student_id,
            force_mock=force_mock,
            analysis_period_days=analysis_period_days,
        )

        students = data.get("students", [])
        student = next((s for s in students if s.get("id") == student_id), None)
        if not student:
            return json.dumps({"error": "Student not found"})

        progress_summaries = data.get("student_progress_summaries", []) or data.get(
            "studentProgressSummaries", []
        )
        progress_summary = next(
            (
                s
                for s in progress_summaries
                if s.get("student_id") == student_id
                or s.get("studentId") == student_id
            ),
            None,
        )

        # Normalize key metrics for analysis
        overview = {
            "student_id": student_id,
            "classroom_id": classroom_id,
            "total_lessons": student.get("total_lessons"),
            "completed_lessons": student.get("completed_lessons"),
            "completion_rate": student.get("completion_rate"),
            "average_score": student.get("average_score"),
            "engagement_score": student.get("engagement_score"),
            "last_active_at": student.get("last_active_at"),
        }

        if progress_summary:
            overview["assessment_completion_rate"] = (
                progress_summary.get("assessment_completion_rate")
                or progress_summary.get("assessmentCompletionRate")
            )
            overview["content_completion_rate"] = (
                progress_summary.get("content_completion_rate")
                or progress_summary.get("contentCompletionRate")
            )
            overview["total_assessments"] = progress_summary.get(
                "total_assessments"
            ) or progress_summary.get("totalAssessments")
            overview["completed_assessments"] = progress_summary.get(
                "completed_assessments"
            ) or progress_summary.get("completedAssessments")
            overview["total_sections"] = progress_summary.get("total_sections") or progress_summary.get(
                "totalSections"
            )
            overview["completed_sections"] = progress_summary.get(
                "completed_sections"
            ) or progress_summary.get("completedSections")
        return json.dumps(overview)

    async def _class_overview(self, parameters: Dict[str, Any]) -> str:
        classroom_id = parameters.get("classroom_id")
        force_mock = bool(parameters.get("force_mock", False))
        analysis_period_days = parameters.get("analysis_period_days")
        if not classroom_id:
            return json.dumps({"error": "classroom_id is required"})

        data = await self._get_classroom_data(
            classroom_id=classroom_id,
            student_id=None,
            force_mock=force_mock,
            analysis_period_days=analysis_period_days,
        )

        students = data.get("students", [])
        count = len(students)
        if count == 0:
            return json.dumps({"classroom_id": classroom_id, "student_count": 0})

        progress_summaries = data.get("student_progress_summaries", []) or data.get(
            "studentProgressSummaries", []
        )

        if progress_summaries:
            assessment_rates = []
            content_rates = []
            for s in progress_summaries:
                ar = s.get("assessment_completion_rate") or s.get(
                    "assessmentCompletionRate"
                )
                cr = s.get("content_completion_rate") or s.get(
                    "contentCompletionRate"
                )
                if ar is not None:
                    assessment_rates.append(float(ar))
                if cr is not None:
                    content_rates.append(float(cr))

            avg_assessment = (
                sum(assessment_rates) / len(assessment_rates) if assessment_rates else 0.0
            )
            avg_content = (
                sum(content_rates) / len(content_rates) if content_rates else 0.0
            )
            avg_score = sum(s.get("average_score", 0.0) for s in students) / count

            overview = {
                "classroom_id": classroom_id,
                "student_count": count,
                "average_assessment_completion_rate": round(avg_assessment, 3),
                "average_content_completion_rate": round(avg_content, 3),
                "average_score": round(avg_score, 2),
            }
        else:
            avg_completion = sum(
                s.get("completion_rate", 0.0) for s in students
            ) / count
            avg_score = sum(s.get("average_score", 0.0) for s in students) / count

            overview = {
                "classroom_id": classroom_id,
                "student_count": count,
                "average_completion_rate": round(avg_completion, 2),
                "average_score": round(avg_score, 2),
            }
        return json.dumps(overview)

    async def _detailed_data(self, parameters: Dict[str, Any]) -> str:
        classroom_id = parameters.get("classroom_id")
        force_mock = bool(parameters.get("force_mock", False))
        analysis_period_days = parameters.get("analysis_period_days")
        if not classroom_id:
            return json.dumps({"error": "classroom_id is required"})

        data = await self._get_classroom_data(
            classroom_id=classroom_id,
            student_id=None,
            force_mock=force_mock,
            analysis_period_days=analysis_period_days,
        )

        quiz_attempts = []
        student_assignments = []
        section_progress = []
        engagement_metrics = []
        student_quizzes = []
        
        if isinstance(data, dict):
            # Try nested structure first (from GrpcClassroomRepository)
            if "quizzes" in data and isinstance(data["quizzes"], dict):
                quiz_attempts = data["quizzes"].get("quiz_attempts", [])
                student_quizzes = data["quizzes"].get("student_quizzes", [])
            if "assignments" in data and isinstance(data["assignments"], dict):
                student_assignments = data["assignments"].get("student_assignments", [])
            if "progress" in data and isinstance(data["progress"], dict):
                section_progress = data["progress"].get("section_progress", [])
            if "time_metrics" in data and isinstance(data["time_metrics"], dict):
                engagement_metrics = data["time_metrics"].get("engagement_metrics", [])
            
            # Fallback to flat structure (if data is already flattened)
            if not quiz_attempts:
                quiz_attempts = data.get("quizAttempts", []) or data.get("quiz_attempts", [])
            if not student_assignments:
                student_assignments = data.get("studentAssignments", []) or data.get("student_assignments", [])
            if not section_progress:
                section_progress = data.get("sectionProgress", []) or data.get("section_progress", [])
            if not engagement_metrics:
                engagement_metrics = data.get("engagementMetrics", []) or data.get("engagement_metrics", [])
            if not student_quizzes:
                student_quizzes = data.get("studentQuizzes", []) or data.get("student_quizzes", [])

        progress_summaries = (
            data.get("student_progress_summaries") 
            or data.get("studentProgressSummaries")
            or []
        )

        detailed = {
            "classroom_id": classroom_id,
            "student_progress_summaries": progress_summaries,
            "quizAttempts": quiz_attempts,
            "studentAssignments": student_assignments,
            "sectionProgress": section_progress,
            "engagementMetrics": engagement_metrics,
            "studentQuizzes": student_quizzes,
        }

        # Calculate summary statistics for AI understanding
        quiz_attempts = detailed["quizAttempts"]
        if quiz_attempts:
            quiz_scores = [q.get("totalScore") or q.get("total_score", 0) for q in quiz_attempts if q.get("totalScore") or q.get("total_score")]
            detailed["quiz_summary"] = {
                "total_attempts": len(quiz_attempts),
                "completed_attempts": len([q for q in quiz_attempts if q.get("status") == "Passed" or q.get("status") == "Completed"]),
                "average_score": round(sum(quiz_scores) / len(quiz_scores), 2) if quiz_scores else 0,
                "score_range": {"min": min(quiz_scores), "max": max(quiz_scores)} if quiz_scores else None,
            }

        assignments = detailed["studentAssignments"]
        if assignments:
            assignment_scores = [a.get("finalScore") or a.get("final_score", 0) for a in assignments if a.get("finalScore") or a.get("final_score")]
            detailed["assignment_summary"] = {
                "total_assignments": len(assignments),
                "submitted_count": len([a for a in assignments if a.get("finalScore") or a.get("final_score")]),
                "average_score": round(sum(assignment_scores) / len(assignment_scores), 2) if assignment_scores else 0,
                "score_range": {"min": min(assignment_scores), "max": max(assignment_scores)} if assignment_scores else None,
            }

        section_progress = detailed["sectionProgress"]
        if section_progress:
            completed_sections = [s for s in section_progress if s.get("status") == "Completed"]
            detailed["section_summary"] = {
                "total_sections": len(section_progress),
                "completed_count": len(completed_sections),
                "in_progress_count": len([s for s in section_progress if s.get("status") == "InProgress"]),
                "locked_count": len([s for s in section_progress if s.get("status") == "Locked"]),
            }

        engagement = detailed["engagementMetrics"]
        if engagement:
            completion_rates = [e.get("completionRate") or e.get("completion_rate", 0) for e in engagement if e.get("completionRate") or e.get("completion_rate")]
            detailed["engagement_summary"] = {
                "students_with_data": len(engagement),
                "average_completion_rate": round(sum(completion_rates) / len(completion_rates), 3) if completion_rates else 0,
                "active_students": len([e for e in engagement if (e.get("activeDaysLast7Days") or e.get("active_days_last_7_days", 0)) > 0]),
            }

        return json.dumps(detailed, ensure_ascii=False)

    async def _get_classroom_data(
        self,
        *,
        classroom_id: Optional[int],
        student_id: Optional[str],
        force_mock: bool = False,
        analysis_period_days: Optional[int] = None,
    ) -> Dict[str, Any]:
        """
        Fetch classroom data, preferring snapshot-based access when available.

        - If snapshot_updater is configured: use it with per-classroom cooldown.
        - Else: fall back to direct ClassroomRepository access.
        """
        if not force_mock and self.snapshot_updater and self.snapshot_store and classroom_id is not None:
            snapshot = await self.snapshot_updater.get_or_refresh_snapshot(
                classroom_id=classroom_id,
                student_id=student_id,
                analysis_period_days=analysis_period_days,
            )
            return snapshot.data

        if not self.classroom_repository:
            logger.error(
                "[StudentDataTool] Neither snapshot_updater nor classroom_repository is configured"
            )
            return {}

        # Force mock by omitting classroom_id so repository falls back to mock data.
        effective_classroom_id = None if force_mock else classroom_id

        return await self.classroom_repository.get_classroom_data(
            classroom_id=effective_classroom_id,
            student_id=student_id,
            analysis_period_days=analysis_period_days,
        )

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["overview", "class_overview", "detailed_data"],
                    "description": "Action to perform",
                    "default": "overview",
                },
                "student_id": {
                    "type": "string",
                    "description": "Student identifier (for overview)",
                },
                "classroom_id": {
                    "type": "integer",
                    "description": "Classroom identifier",
                },
                "force_mock": {
                    "type": "boolean",
                    "description": "Force using mock data regardless of classroom_id",
                    "default": False,
                },
                "analysis_period_days": {
                    "type": "integer",
                    "description": "Number of days to look back for analysis",
                    "minimum": 1,
                    "maximum": 90,
                },
            },
            "required": [],
        }


