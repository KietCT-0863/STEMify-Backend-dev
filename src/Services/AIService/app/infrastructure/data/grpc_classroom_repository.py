

import logging
import sys
from pathlib import Path
from typing import Dict, Any, Optional
from datetime import datetime, timedelta

import grpc  # type: ignore
from google.protobuf import wrappers_pb2  # type: ignore

_CURRENT_DIR = Path(__file__).resolve()
_APP_DIR = _CURRENT_DIR.parent.parent.parent
_GENERATED_DIR = _APP_DIR / "grpc" / "generated"
_CLASSROOM_GEN_DIR = _GENERATED_DIR / "Classroom"
for _p in (str(_GENERATED_DIR), str(_CLASSROOM_GEN_DIR)):
    if _p not in sys.path:
        sys.path.insert(0, _p)

from app.core.data.classroom_repository import ClassroomRepository
from app.infrastructure.data.fixtures.mock_classroom_data import get_mock_classroom_data

from app.grpc.generated.Classroom.Protos.Classroom import classrooms_pb2
from app.grpc.generated.Classroom.Protos.Classroom import classrooms_pb2_grpc

logger = logging.getLogger(__name__)


class GrpcClassroomRepository(ClassroomRepository):

    def __init__(
        self,
        endpoint: str,
        fallback: Optional[Dict[str, Any]] = None,
        use_tls: bool = False,
        cert_path: Optional[str] = None,
        authority_override: Optional[str] = None,
    ):
        sanitized_endpoint = endpoint.strip()
        if sanitized_endpoint.startswith(("http://", "https://")):
            sanitized_endpoint = sanitized_endpoint.split("://", 1)[1]
            logger.warning(
                "Removed protocol prefix from gRPC endpoint",
                extra={"original": endpoint, "sanitized": sanitized_endpoint}
            )
        if ":" not in sanitized_endpoint:
            logger.warning(
                "gRPC endpoint missing port number. gRPC may use default port.",
                extra={"endpoint": sanitized_endpoint}
            )
        
        self.endpoint = sanitized_endpoint
        self.fallback_data = fallback
        self.use_tls = use_tls
        self.cert_path = cert_path
        self.authority_override = authority_override
        self._channel: Optional[grpc.aio.Channel] = None
        self._classroom_stub: Optional[classrooms_pb2_grpc.GrpcClassroomStub] = None
        self._last_call_used_fallback: bool = False

    def was_fallback_used(self) -> bool:
        """Check if the last call used fallback data."""
        return self._last_call_used_fallback

    async def get_classroom_data(
        self,
        classroom_id: Optional[int] = None,
        student_id: Optional[str] = None,
        analysis_period_days: Optional[int] = None
    ) -> Dict[str, Any]:
        # Reset fallback flag at the start of each call
        self._last_call_used_fallback = False

        if not classroom_id:
            logger.warning("Classroom ID missing. Falling back to mock data.")
            self._last_call_used_fallback = True
            data = get_mock_classroom_data()
            if analysis_period_days:
                self._adjust_analysis_period(data, analysis_period_days)
            return data

        logger.debug(
            "Requesting classroom data via gRPC",
            extra={"classroom_id": classroom_id, "endpoint": self.endpoint},
        )

        try:
            # Try to fetch complete learning snapshot first
            await self._ensure_channel()
            try:
                snapshot_proto = await self._fetch_classroom_learning_snapshot(
                    classroom_id, student_id, analysis_period_days
                )
                # Map snapshot response to expected format
                data = self._map_snapshot_to_classroom_data(snapshot_proto)
                
                logger.info(
                    "Successfully fetched classroom learning snapshot via gRPC",
                    extra={"classroom_id": classroom_id}
                )
                self._last_call_used_fallback = False
                return data
            except grpc.aio.AioRpcError as snapshot_error:
                # If snapshot endpoint not available, fall back to basic endpoints
                if snapshot_error.code() == grpc.StatusCode.UNIMPLEMENTED:
                    logger.warning(
                        "Learning snapshot endpoint not available, using basic endpoints",
                        extra={"classroom_id": classroom_id}
                    )
                    classroom_proto = await self._fetch_classroom(classroom_id)
                    statistics_proto = await self._fetch_classroom_statistics(classroom_id)
                    
                    # Map proto responses to expected format
                    data = self._map_to_classroom_data(
                        classroom_proto,
                        statistics_proto,
                        student_id,
                        analysis_period_days
                    )
                    
                    logger.info(
                        "Successfully fetched classroom data via gRPC (basic endpoints)",
                        extra={"classroom_id": classroom_id}
                    )
                    return data
                else:
                    raise
            
        except grpc.aio.AioRpcError as error:
            error_details = {
                "classroom_id": classroom_id,
                "endpoint": self.endpoint,
                "status_code": error.code().name if hasattr(error.code(), 'name') else str(error.code()),
                "status_value": error.code().value[0] if hasattr(error.code(), 'value') else None,
                "details": error.details(),
                "use_tls": self.use_tls,
            }
            logger.exception(
                "gRPC error fetching classroom data",
                extra=error_details,
            )
            
            if error.code() == grpc.StatusCode.UNAVAILABLE:
                logger.error(
                    f"gRPC service unavailable. Endpoint: {self.endpoint}. "
                    f"This may indicate: 1) Service is not running, 2) Network connectivity issue, "
                    f"3) Port not accessible, 4) DNS resolution failure.",
                    extra=error_details
                )
            
            # Always use fallback on gRPC error
            logger.warning("Falling back to mock data due to gRPC error.")
            self._last_call_used_fallback = True
            data = get_mock_classroom_data()
            if analysis_period_days:
                self._adjust_analysis_period(data, analysis_period_days)
            return data
            raise

    def _adjust_analysis_period(self, data: Dict[str, Any], days: int) -> None:
        """Adjust analysis period in data if specified."""
        to_date = datetime.utcnow()
        from_date = to_date - timedelta(days=days)
        
        data["analysis_period"] = {
            "from_date": from_date.isoformat() + "Z",
            "to_date": to_date.isoformat() + "Z",
            "days_back": days,
        }

    async def _fetch_classroom(self, classroom_id: int) -> classrooms_pb2.GrpcClassroomResponse:
        stub = await self._get_classroom_stub()
        request = classrooms_pb2.GetClassroomRequest(id=classroom_id)
        response = await stub.GetClassroomById(request)
        return response

    def _map_snapshot_to_classroom_data(
        self,
        snapshot_proto: classrooms_pb2.GrpcClassroomLearningSnapshotResponse,
    ) -> Dict[str, Any]:
        """
        Map learning snapshot proto response to expected classroom data format.
        This is the preferred method as it provides complete data in a single call.
        """
        # Map classroom basic info
        classroom_data = {
            "classroom": {
                "id": snapshot_proto.classroom.id,
                "name": snapshot_proto.classroom.name,
            },
            "students": [],
            "enrollments": {
                "curriculum_enrollments": [],
                "course_enrollments": [],
            },
            "quizzes": {
                "student_quizzes": [],
                "quiz_attempts": [],
            },
            "assignments": {
                "student_assignments": [],
            },
            "time_metrics": {
                "engagement_metrics": [],
            },
            "progress": {
                "section_progress": [],
            },
            "topics_catalog": [],
            "analysis_period": {
                "from_date": snapshot_proto.analysis_period.from_date,
                "to_date": snapshot_proto.analysis_period.to_date,
                "days_back": snapshot_proto.analysis_period.days_back,
            },
        }

        # Map students
        for student_proto in snapshot_proto.students:
            student_data = {
                "student_id": student_proto.student_id,
                "student_name": student_proto.student_name,
            }
            if student_proto.HasField("joined_at") and student_proto.joined_at.value:
                student_data["joined_at"] = student_proto.joined_at.value
            classroom_data["students"].append(student_data)

        # Map enrollments
        for student_proto in snapshot_proto.students:
            for enrollment_proto in student_proto.enrollments:
                enrollment_data = {
                    "student_id": enrollment_proto.student_id,
                    "progress_percentage": enrollment_proto.progress_percentage,
                }
                enrollment_type = None
                if enrollment_proto.HasField("enrollment_type") and enrollment_proto.enrollment_type.value:
                    enrollment_type = enrollment_proto.enrollment_type.value
                try:
                    if enrollment_proto.HasField("course_id") and enrollment_proto.course_id.value:
                        enrollment_data["course_id"] = enrollment_proto.course_id.value
                except ValueError:
                    if hasattr(enrollment_proto, "course_id") and enrollment_proto.course_id:
                        enrollment_data["course_id"] = enrollment_proto.course_id
                
                if enrollment_proto.HasField("curriculum_name") and enrollment_proto.curriculum_name.value:
                    enrollment_data["curriculum_name"] = enrollment_proto.curriculum_name.value
                    classroom_data["enrollments"]["curriculum_enrollments"].append(enrollment_data)
                elif enrollment_type == "course":
                    classroom_data["enrollments"]["course_enrollments"].append(enrollment_data)

        # Map student quizzes
        for quiz_proto in snapshot_proto.student_quizzes:
            quiz_data = {
                "id": quiz_proto.id,
                "student_id": quiz_proto.student_id,
                "final_score": quiz_proto.final_score,
            }
            try:
                if quiz_proto.HasField("quiz_id") and quiz_proto.quiz_id.value:
                    quiz_data["quiz_id"] = quiz_proto.quiz_id.value
            except ValueError:
                if hasattr(quiz_proto, "quiz_id") and quiz_proto.quiz_id:
                    quiz_data["quiz_id"] = quiz_proto.quiz_id
            if quiz_proto.quiz_title:
                quiz_data["quiz_title"] = quiz_proto.quiz_title
            if quiz_proto.HasField("quiz_description") and quiz_proto.quiz_description.value:
                quiz_data["quiz_description"] = quiz_proto.quiz_description.value
            if quiz_proto.attempt_count > 0:
                quiz_data["attempt_count"] = quiz_proto.attempt_count
            classroom_data["quizzes"]["student_quizzes"].append(quiz_data)

        # Map quiz attempts
        for attempt_proto in snapshot_proto.quiz_attempts:
            attempt_data = {
                "student_quiz_id": attempt_proto.student_quiz_id,
                "attempt_number": attempt_proto.attempt_number,
                "total_score": attempt_proto.total_score,
                "status": attempt_proto.status,
                "time_spent_minutes": attempt_proto.time_spent_minutes,
            }
            if attempt_proto.HasField("started_at") and attempt_proto.started_at.value:
                attempt_data["started_at"] = attempt_proto.started_at.value
            if attempt_proto.HasField("completed_at") and attempt_proto.completed_at.value:
                attempt_data["completed_at"] = attempt_proto.completed_at.value

            # Map question attempts
            attempt_data["question_attempts"] = []
            for qa_proto in attempt_proto.question_attempts:
                qa_data = {
                    "question_id": qa_proto.question_id,
                    "is_correct": qa_proto.is_correct,
                    "topics": list(qa_proto.topics),
                }
                if qa_proto.question_content:
                    qa_data["question_content"] = qa_proto.question_content
                if qa_proto.question_type:
                    qa_data["question_type"] = qa_proto.question_type
                if qa_proto.answer_content:
                    qa_data["answer_content"] = qa_proto.answer_content
                if qa_proto.is_selected:
                    qa_data["is_selected"] = qa_proto.is_selected
                attempt_data["question_attempts"].append(qa_data)

            classroom_data["quizzes"]["quiz_attempts"].append(attempt_data)

        # Map student assignments
        for assignment_proto in snapshot_proto.student_assignments:
            assignment_data = {
                "student_id": assignment_proto.student_id,
                "final_score": assignment_proto.final_score,
                "submission_count": assignment_proto.submission_count,
            }
            if assignment_proto.HasField("submitted_at") and assignment_proto.submitted_at.value:
                assignment_data["submitted_at"] = assignment_proto.submitted_at.value
            if assignment_proto.HasField("due_date") and assignment_proto.due_date.value:
                assignment_data["due_date"] = assignment_proto.due_date.value

            # Map question attempts
            assignment_data["question_attempts"] = []
            for qa_proto in assignment_proto.question_attempts:
                qa_data = {
                    "question_id": qa_proto.question_id,
                    "points": qa_proto.points,
                    "topics": list(qa_proto.topics),
                }
                if qa_proto.question_content:
                    qa_data["question_content"] = qa_proto.question_content
                if qa_proto.answer_text:
                    qa_data["answer_text"] = qa_proto.answer_text
                if qa_proto.HasField("feedback") and qa_proto.feedback.value:
                    qa_data["feedback"] = qa_proto.feedback.value

                # Map rubric scores
                qa_data["rubric_scores"] = []
                for rubric_proto in qa_proto.rubric_scores:
                    rubric_data = {
                        "id": rubric_proto.id,
                        "rubric_criterion_id": rubric_proto.rubric_criterion_id,
                        "criterion_name": rubric_proto.criterion_name,
                        "max_points": rubric_proto.max_points,
                        "points": rubric_proto.points,
                    }
                    if rubric_proto.HasField("criterion_description") and rubric_proto.criterion_description.value:
                        rubric_data["criterion_description"] = rubric_proto.criterion_description.value
                    qa_data["rubric_scores"].append(rubric_data)

                assignment_data["question_attempts"].append(qa_data)

            classroom_data["assignments"]["student_assignments"].append(assignment_data)

        # Map engagement metrics
        for engagement_proto in snapshot_proto.engagement_metrics:
            engagement_data = {
                "student_id": engagement_proto.student_id,
                "completion_rate": engagement_proto.completion_rate,
                "days_since_last_activity": engagement_proto.days_since_last_activity,
                "active_days_last_7_days": engagement_proto.active_days_last_7_days,
                "avg_session_duration_minutes": engagement_proto.avg_session_duration_minutes,
            }
            classroom_data["time_metrics"]["engagement_metrics"].append(engagement_data)

        # Map section progress
        for progress_proto in snapshot_proto.section_progress:
            progress_data = {
                "student_id": progress_proto.student_id,
                "section_id": progress_proto.section_id,
                "section_name": progress_proto.section_name,
                "status": progress_proto.status,
            }
            if progress_proto.HasField("last_activity_at") and progress_proto.last_activity_at.value:
                progress_data["last_activity_at"] = progress_proto.last_activity_at.value
            classroom_data["progress"]["section_progress"].append(progress_data)

        # Map topics catalog
        for topic_proto in snapshot_proto.topics_catalog:
            topic_data = {
                "topic_id": topic_proto.topic_id,
                "topic_name": topic_proto.topic_name,
            }
            if topic_proto.HasField("parent_topic_id") and topic_proto.parent_topic_id.value:
                topic_data["parent_topic_id"] = topic_proto.parent_topic_id.value

            # Map lessons
            topic_data["lessons"] = []
            for lesson_proto in topic_proto.lessons:
                lesson_data = {
                    "lesson_title": lesson_proto.lesson_title,
                }
                if lesson_proto.HasField("lesson_description") and lesson_proto.lesson_description.value:
                    lesson_data["lesson_description"] = lesson_proto.lesson_description.value
                topic_data["lessons"].append(lesson_data)

            # Map sections
            topic_data["sections"] = []
            for section_proto in topic_proto.sections:
                section_data = {
                    "section_title": section_proto.section_title,
                    "contents": [],
                }
                for content_proto in section_proto.contents:
                    content_data = {
                        "content_type": content_proto.content_type,
                        "content_title": content_proto.content_title,
                    }
                    section_data["contents"].append(content_data)
                topic_data["sections"].append(section_data)

            classroom_data["topics_catalog"].append(topic_data)

        classroom_data["student_progress_summaries"] = []
        
        if hasattr(snapshot_proto, "student_progress_summaries") and snapshot_proto.student_progress_summaries:
            for summary_proto in snapshot_proto.student_progress_summaries:
                summary_data = {
                    "student_id": summary_proto.student_id,
                    "assessment_completion_rate": summary_proto.assessment_completion_rate,
                    "total_assessments": summary_proto.total_assessments,
                    "completed_assessments": summary_proto.completed_assessments,
                    "content_completion_rate": summary_proto.content_completion_rate,
                    "total_sections": summary_proto.total_sections,
                    "completed_sections": summary_proto.completed_sections,
                }
                classroom_data["student_progress_summaries"].append(summary_data)
        else:
            student_ids = set()
            
            for student_data in classroom_data["students"]:
                student_ids.add(student_data["student_id"])
            
            for quiz_data in classroom_data["quizzes"]["student_quizzes"]:
                if quiz_data.get("student_id"):
                    student_ids.add(quiz_data["student_id"])
            
            for assignment_data in classroom_data["assignments"]["student_assignments"]:
                if assignment_data.get("student_id"):
                    student_ids.add(assignment_data["student_id"])
            
            for progress_data in classroom_data["progress"]["section_progress"]:
                if progress_data.get("student_id"):
                    student_ids.add(progress_data["student_id"])
            
            for engagement_data in classroom_data["time_metrics"]["engagement_metrics"]:
                if engagement_data.get("student_id"):
                    student_ids.add(engagement_data["student_id"])
            
            for student_id in student_ids:
                student_quizzes_map = {
                    quiz_data["id"]: quiz_data 
                    for quiz_data in classroom_data["quizzes"]["student_quizzes"]
                    if quiz_data["student_id"] == student_id
                }
                total_quizzes = set(student_quizzes_map.keys())
                
                completed_quizzes = set()
                for attempt in classroom_data["quizzes"]["quiz_attempts"]:
                    student_quiz_id = attempt.get("student_quiz_id")
                    if student_quiz_id in student_quizzes_map:
                        if attempt.get("status") == "Passed" or attempt.get("completed_at"):
                            completed_quizzes.add(student_quiz_id)
                
                completed_assignments = 0
                total_assignments = 0
                for assignment_data in classroom_data["assignments"]["student_assignments"]:
                    if assignment_data["student_id"] == student_id:
                        total_assignments += 1
                        if assignment_data.get("submission_count", 0) > 0:
                            completed_assignments += 1
                
                total_assessments = len(total_quizzes) + total_assignments
                completed_assessments = len(completed_quizzes) + completed_assignments
                assessment_completion_rate = (
                    completed_assessments / total_assessments 
                    if total_assessments > 0 else 0.0
                )
                
                # Count sections progress
                total_sections = 0
                completed_sections = 0
                for progress_data in classroom_data["progress"]["section_progress"]:
                    if progress_data["student_id"] == student_id:
                        total_sections += 1
                        if progress_data.get("status") == "Completed":
                            completed_sections += 1
                
                content_completion_rate = (
                    completed_sections / total_sections 
                    if total_sections > 0 else 0.0
                )
                
                summary_data = {
                    "student_id": student_id,
                    "assessment_completion_rate": assessment_completion_rate,
                    "total_assessments": total_assessments,
                    "completed_assessments": completed_assessments,
                    "content_completion_rate": content_completion_rate,
                    "total_sections": total_sections,
                    "completed_sections": completed_sections,
                }
                classroom_data["student_progress_summaries"].append(summary_data)

        logger.info(
            "Mapped learning snapshot to classroom data format",
            extra={
                "students_count": len(classroom_data["students"]),
                "quizzes_count": len(classroom_data["quizzes"]["student_quizzes"]),
                "assignments_count": len(classroom_data["assignments"]["student_assignments"]),
                "progress_summaries_count": len(classroom_data["student_progress_summaries"]),
            }
        )

        return classroom_data

    async def _fetch_classroom_statistics(
        self, classroom_id: int
    ) -> classrooms_pb2.GrpcClassroomStatisticResponse:
        stub = await self._get_classroom_stub()
        request = classrooms_pb2.GetClassroomRequest(id=classroom_id)
        response = await stub.GetClassroomStatistic(request)
        return response

    async def _fetch_classroom_learning_snapshot(
        self,
        classroom_id: int,
        student_id: Optional[str],
        analysis_period_days: Optional[int],
    ) -> classrooms_pb2.GrpcClassroomLearningSnapshotResponse:
        stub = await self._get_classroom_stub()
        request = classrooms_pb2.GetClassroomLearningSnapshotRequest(
            classroom_id=classroom_id
        )
        if student_id:
            request.student_id.value = student_id
        if analysis_period_days:
            request.days_back.value = analysis_period_days
        response = await stub.GetClassroomLearningSnapshot(request)
        return response

    def _map_to_classroom_data(
        self,
        classroom_proto: classrooms_pb2.GrpcClassroomResponse,
        statistics_proto: classrooms_pb2.GrpcClassroomStatisticResponse,
        student_id: Optional[str],
        analysis_period_days: Optional[int],
    ) -> Dict[str, Any]:
        """
        Map gRPC proto responses to classroom data dictionary format.
        
        NOTE: Current proto only provides basic classroom info and statistics.
        For full analysis, we need additional data from other services:
        - Quiz attempts with question_attempts (from Quiz Service)
        - Assignment attempts with rubric_scores (from Assignment Service)
        - Engagement metrics (from Analytics/Engagement Service)
        - Section progress (from Progress Service)
        - Topics catalog (from Resource Service)
        
        This implementation provides a basic structure that can be extended
        when additional gRPC endpoints become available.
        """
        to_date = datetime.utcnow()
        from_date = to_date - timedelta(days=analysis_period_days) if analysis_period_days else to_date - timedelta(days=7)
        
        # Map students
        students = []
        if classroom_proto.students:
            for student in classroom_proto.students:
                student_data = {
                    "student_id": student.id,
                    "student_name": student.name,
                }
                # Check if imageUrl is set (proto3 uses HasField for wrapper types)
                if student.HasField("imageUrl") and student.imageUrl.value:
                    student_data["image_url"] = student.imageUrl.value
                students.append(student_data)
        
        # Filter by student_id if specified
        if student_id:
            students = [s for s in students if s["student_id"] == student_id]
        
        # Map enrollments (basic - would need additional endpoints for full data)
        enrollments = {
            "curriculum_enrollments": [],
            "course_enrollments": [],
        }
        
        # If course info is available, create basic enrollment
        if classroom_proto.HasField("course") and classroom_proto.course.id > 0:
            for student in students:
                enrollments["course_enrollments"].append({
                    "student_id": student["student_id"],
                    "progress_percentage": 0.0,  # Would need progress endpoint
                })
        
        # Build basic classroom data structure
        # NOTE: This is a minimal implementation. Full data requires additional service calls.
        data = {
            "classroom": {
                "id": classroom_proto.id,
                "name": classroom_proto.name,
            },
            "students": students,
            "enrollments": enrollments,
            "quizzes": {
                "student_quizzes": [],
                "quiz_attempts": [],
            },
            "assignments": {
                "student_assignments": [],
            },
            "time_metrics": {
                "engagement_metrics": [],
            },
            "progress": {
                "section_progress": [],
            },
            "topics_catalog": [],
            "analysis_period": {
                "from_date": from_date.isoformat() + "Z",
                "to_date": to_date.isoformat() + "Z",
                "days_back": analysis_period_days or 7,
            },
        }
        
        # Add statistics if available (for reference, not full data)
        if statistics_proto:
            logger.debug(
                "Received classroom statistics",
                extra={
                    "quiz_avg": statistics_proto.quizStatistic.averageScore if statistics_proto.HasField("quizStatistic") else None,
                    "assignment_avg": statistics_proto.assignmentStatistic.averageScore if statistics_proto.HasField("assignmentStatistic") else None,
                }
            )
        
        # Log warning about incomplete data
        logger.warning(
            "gRPC Classroom service provides basic info only. "
            "Full analysis requires additional service calls to Quiz, Assignment, "
            "Engagement, and Progress services. Using fallback for missing data.",
            extra={"classroom_id": classroom_proto.id}
        )
        
        # Merge with fallback data for missing fields
        fallback_data = get_mock_classroom_data()
        
        # Override with real data where available
        if students:
            # Filter fallback students to match real students
            fallback_data["students"] = [
                s for s in fallback_data["students"]
                if any(rs["student_id"] == s["student_id"] for rs in students)
            ] or fallback_data["students"][:len(students)]
        
        # Use fallback for detailed data (quizzes, assignments, etc.)
        data["quizzes"] = fallback_data.get("quizzes", data["quizzes"])
        data["assignments"] = fallback_data.get("assignments", data["assignments"])
        data["time_metrics"] = fallback_data.get("time_metrics", data["time_metrics"])
        data["progress"] = fallback_data.get("progress", data["progress"])
        data["topics_catalog"] = fallback_data.get("topics_catalog", data["topics_catalog"])
        
        # Mark that we used fallback for detailed data
        self._last_call_used_fallback = True
        
        return data

    async def _get_classroom_stub(self) -> classrooms_pb2_grpc.GrpcClassroomStub:
        """Get classroom service stub."""
        await self._ensure_channel()
        if self._classroom_stub is None:
            raise RuntimeError("Classroom stub not initialized")
        return self._classroom_stub

    async def _ensure_channel(self) -> None:
        """Ensure gRPC channel is established."""
        if self._channel is None:
            if self.use_tls:
                credentials = self._get_ssl_credentials()
                options = []
                if self.authority_override:
                    options.append(
                        ("grpc.ssl_target_name_override", self.authority_override)
                    )
                self._channel = grpc.aio.secure_channel(
                    self.endpoint,
                    credentials,
                    options=options or None,
                )
                logger.info(
                    "Established secure gRPC channel",
                    extra={"endpoint": self.endpoint, "override": self.authority_override},
                )
            else:
                self._channel = grpc.aio.insecure_channel(self.endpoint)
                logger.info(
                    "Established insecure gRPC channel",
                    extra={"endpoint": self.endpoint},
                )

            # Initialize stub
            self._classroom_stub = classrooms_pb2_grpc.GrpcClassroomStub(self._channel)

    def _get_ssl_credentials(self) -> grpc.ChannelCredentials:
        """Get SSL credentials for secure channel."""
        if not hasattr(self, '_ssl_credentials') or self._ssl_credentials is None:
            root_certs = self._load_root_certificates()
            self._ssl_credentials = grpc.ssl_channel_credentials(root_certificates=root_certs)
        return self._ssl_credentials

    def _load_root_certificates(self) -> Optional[bytes]:
        """Load root certificates from file."""
        if not self.cert_path:
            logger.debug("No custom gRPC certificate path provided; using system trust store.")
            return None

        cert_file = Path(self.cert_path)
        if not cert_file.exists():
            logger.warning(
                "Provided gRPC certificate path does not exist. Falling back to system trust store.",
                extra={"cert_path": self.cert_path},
            )
            return None

        try:
            data = cert_file.read_bytes()
            logger.info(
                "Loaded custom gRPC root certificate.",
                extra={"cert_path": self.cert_path},
            )
            return data
        except OSError as error:
            logger.warning(
                "Failed to read gRPC certificate file. Falling back to system trust store.",
                extra={"cert_path": self.cert_path, "error": str(error)},
            )
            return None




