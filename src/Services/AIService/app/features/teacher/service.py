from typing import Dict, Any, Optional, List
import logging
import re

from app.features.teacher.student_analysis_agent import StudentAnalysisAgent
from app.features.teacher.lesson_analytics_agent import LessonAnalyticsAgent
from app.features.teacher.auto_grading_agent import AutoGradingAgent
from app.core.agent.pool.manager import AgentPoolManager
from app.core.context.builder import JITContextBuilder
from app.core.memory.memory_manager import MemoryManager
from app.core.llm.client import LLMClient
from app.core.data.classroom_repository import ClassroomRepository
from app.core.data.lesson_repository import LessonRepository
from app.core.graph.client import GraphClient
from app.core.graph.builder import GraphBuilder
from app.core.graph.monitor import GraphMonitor
from app.core.reasoning.orchestrator import GraphReasoningOrchestrator
from app.core.tools.sentiment_analysis_tool import SentimentAnalysisTool
from app.core.snapshot.classroom_snapshot_store import (
    ClassroomSnapshotStore,
    ClassroomSnapshotUpdater,
)
from app.core.rag.ingestion_service import IngestionService
from app.features.recommendations.models import (
    InterventionResponse,
    StudentOverview,
)
from app.infrastructure.config.settings import settings
from app.infrastructure.data.grpc_assignment_attempt_client import GrpcAssignmentAttemptClient
from app.features.teacher.direct_grading_pipeline import DirectGradingPipeline

logger = logging.getLogger(__name__)


class TeacherService:
    """
    Service layer orchestrating teacher-facing agents:
    - StudentAnalysisAgent
    - LessonAnalyticsAgent
    - AutoGradingAgent

    Integrates:
    - JITContextBuilder (context engineering + reuse)
    - MemoryManager (episodic/semantic)
    - AgentPoolManager (pooling if extended in future)
    """

    def __init__(
        self,
        llm: LLMClient,
        context_builder: JITContextBuilder,
        memory_manager: MemoryManager,
        agent_pool_manager: AgentPoolManager,
        classroom_repository: ClassroomRepository,
        lesson_repository: LessonRepository,
        graph_client: GraphClient,
        graph_reasoning_orchestrator: GraphReasoningOrchestrator,
        assignment_attempt_client: Optional[GrpcAssignmentAttemptClient] = None,
        sentiment_tool: Optional[SentimentAnalysisTool] = None,
        classroom_snapshot_store: Optional[ClassroomSnapshotStore] = None,
        classroom_snapshot_updater: Optional[ClassroomSnapshotUpdater] = None,
        direct_grading_pipeline: Optional[DirectGradingPipeline] = None,
        ingestion_service: Optional[IngestionService] = None,
    ):
        self.llm = llm
        self.context_builder = context_builder
        self.memory_manager = memory_manager
        self.agent_pool_manager = agent_pool_manager
        self.classroom_repository = classroom_repository
        self.lesson_repository = lesson_repository
        self.graph_client = graph_client
        self.graph_reasoning_orchestrator = graph_reasoning_orchestrator
        self.assignment_attempt_client = assignment_attempt_client
        self.sentiment_tool = sentiment_tool
        self.classroom_snapshot_store = classroom_snapshot_store
        self.classroom_snapshot_updater = classroom_snapshot_updater
        self.direct_grading_pipeline = direct_grading_pipeline
        self.ingestion_service = ingestion_service

        logger.info("TeacherService initialized")

    async def build_classroom_graph(
        self,
        classroom_id: int,
        force_rebuild: bool = False,
    ) -> Dict[str, Any]:
        """
        Build or rebuild the knowledge graph for a classroom.
        
        This creates all graph nodes and relationships including:
        - Level 1-4: Curriculum, Course, Lesson, Section, Content, Quiz, Assignment, Attempts
        - Level 5: Performance relationships (STRUGGLES_WITH, EXCELS_AT)
        
        Args:
            classroom_id: The classroom ID to build graph for
            force_rebuild: If True, rebuild even if graph already exists
            
        Returns:
            Summary dict with node counts, relationship counts, and conflicts
        """
        try:
            # Get classroom data
            logger.info(f"[TeacherService] Building graph for classroom {classroom_id}")
            classroom_data = await self.classroom_repository.get_classroom_data(
                classroom_id=classroom_id,
                student_id=None,
                analysis_period_days=None,
            )
            
            if not classroom_data:
                logger.warning(f"[TeacherService] No data found for classroom {classroom_id}")
                return {
                    "error": f"No data found for classroom {classroom_id}",
                    "classroom_id": classroom_id,
                }
            
            # Initialize graph builder
            monitor = GraphMonitor(
                log_level=settings.GRAPH_MONITOR_LOG_LEVEL,
                enable_detection=settings.GRAPH_CONFLICT_DETECTION,
            )
            graph_builder = GraphBuilder(self.graph_client, monitor)
            
            # Build graph
            result = await graph_builder.build_graph(classroom_data)
            
            logger.info(
                f"[TeacherService] Graph built for classroom {classroom_id}",
                extra={
                    "classroom_id": classroom_id,
                    "nodes_created": result.get("nodes_created", 0),
                    "relationships_created": result.get("relationships_created", 0),
                    "level_5_relationships": result.get("level_5", {}).get("total", 0),
                }
            )
            
            return {
                "success": True,
                "classroom_id": classroom_id,
                **result,
            }
            
        except Exception as e:
            logger.error(
                f"[TeacherService] Error building graph for classroom {classroom_id}: {e}",
                exc_info=True,
            )
            return {
                "success": False,
                "error": str(e),
                "classroom_id": classroom_id,
            }

    async def analyze_student(
        self,
        teacher_id: str,
        classroom_id: Optional[int],
        student_id: Optional[str],
        query: Optional[str] = None,
        session_id: Optional[str] = None,
        force_mock: bool = False,
        analysis_period_days: Optional[int] = 7,
        lang: Optional[str] = "vi",
    ) -> InterventionResponse:
        question = query or (
            f"Analyze student {student_id} in classroom {classroom_id} and suggest interventions."
            if student_id and classroom_id
            else "Analyze classroom learning progress and provide insights for the teacher."
        )

        agent = StudentAnalysisAgent(
            teacher_id=teacher_id,
            llm=self.llm,
            classroom_repository=self.classroom_repository,
            graph_client=self.graph_client,
            memory_manager=self.memory_manager,
            graph_reasoning_orchestrator=self.graph_reasoning_orchestrator,
            use_remote=settings.TEACHER_AGENTS_USE_REMOTE,
            classroom_snapshot_store=self.classroom_snapshot_store,
            classroom_snapshot_updater=self.classroom_snapshot_updater,
        )

        # Build context bundle 
        context_bundle = await self.context_builder.build(
            query=question,
            user_id=teacher_id,
            top_k=10,
            session_id=session_id,
        )

        logger.info(
            "[TeacherService] Context bundle for teacher %s: %s",
            teacher_id,
            context_bundle,
        )
        
        if classroom_id and self.ingestion_service:
            try:
                await self.ingestion_service.ensure_ingested(
                    classroom_id=classroom_id,
                    max_wait_seconds=0,  
                )
            except Exception as e:
                logger.warning(
                    f"[TeacherService] Failed to ensure ingestion for classroom {classroom_id}: {e}"
                )

        classroom_data = None
        if classroom_id:
            try:
                if self.classroom_snapshot_updater and self.classroom_snapshot_store:
                    snapshot = await self.classroom_snapshot_updater.get_or_refresh_snapshot(
                        classroom_id=classroom_id,
                        student_id=None,  
                        analysis_period_days=analysis_period_days,
                        force_full_refresh=False,  
                    )
                    classroom_data = snapshot.data if snapshot else None
            except Exception as e:
                logger.warning(
                    "[TeacherService] Failed to pre-load classroom data: %s", e
                )

        result = await agent.analyze_student(
            classroom_id=classroom_id,
            student_id=student_id,
            focus=query,
            force_mock=force_mock,
            analysis_period_days=analysis_period_days,
            lang=lang,
            context_bundle=context_bundle,
            classroom_data=classroom_data,
        )
        
        result["teacher_id"] = teacher_id

        if not classroom_data and classroom_id:
            try:
                if self.classroom_snapshot_store:
                    snapshot = self.classroom_snapshot_store.get_snapshot(classroom_id)
                    if snapshot:
                        classroom_data = snapshot.data
                       
            except Exception as e:
                logger.warning(
                    "[TeacherService] Failed to get classroom data from cache: %s", e
                )
        try:
            await self.memory_manager.add_memory(
                content=f"Teacher analysis run for student {student_id or 'all students'}",
                memory_type="episodic",
                metadata={
                    "type": "teacher_student_analysis",
                    "teacher_id": teacher_id,
                    "student_id": student_id,
                    "classroom_id": classroom_id,
                    "session_id": session_id,
                    "context_tokens": context_bundle.total_tokens,
                },
            )
        except Exception as e:
            logger.warning("[TeacherService] Failed to store student analysis: %s", e)

      
        intervention = self._map_student_analysis_to_intervention_response(
            result, classroom_data=classroom_data
        )

        return intervention


    async def auto_grade(
        self,
        teacher_id: str,
        assignment_attempt_id: int,
        student_id: Optional[str] = None,
        query: Optional[str] = None,
        session_id: Optional[str] = None,
        use_agent: bool = False,
    ) -> Dict[str, Any]:
        if not self.assignment_attempt_client:
            raise ValueError("AssignmentAttemptClient is required for auto-grading")
        
        try:
            # Fetch assignment attempt data
            assignment_attempt_data = await self.assignment_attempt_client.get_assignment_attempt_by_id(
                assignment_attempt_id
            )
            question_count = len(assignment_attempt_data.get("questionAttempts", []))
            question_ids = [qa.get("assignmentQuestionId") for qa in assignment_attempt_data.get("questionAttempts", [])]
            used_fallback = self.assignment_attempt_client.was_fallback_used() if hasattr(self.assignment_attempt_client, 'was_fallback_used') else False
            
            logger.info(
                "[TeacherService] Fetched assignment attempt data",
                extra={
                    "attempt_id": assignment_attempt_id,
                    "question_count": question_count,
                    "question_ids": question_ids,
                    "used_fallback_mock": used_fallback,
                }
            )
        except Exception as e:
            logger.error(
                "Failed to fetch assignment attempt",
                extra={"attempt_id": assignment_attempt_id, "error": str(e)},
                exc_info=True
            )
            raise
        
        rubric_id = f"assignment_{assignment_attempt_id}_rubric"
        
        if not use_agent and self.direct_grading_pipeline:
            result = await self.direct_grading_pipeline.grade(
                assignment_attempt_data=assignment_attempt_data,
                rubric_id=rubric_id,
                model_answers=None,  
            )
            
            result.setdefault("metadata", {})
            result["metadata"]["grading_method"] = "direct_pipeline"
            result["metadata"]["teacher_id"] = teacher_id
            result["metadata"]["student_id"] = student_id
            result["metadata"]["assignment_attempt_id"] = assignment_attempt_id
            
            if "metrics" in result:
                metrics = result["metrics"]
                logger.info(
                    "[TeacherService] Grading performance metrics",
                    extra={
                        "assignment_attempt_id": assignment_attempt_id,
                        "elapsed_time": metrics.get("elapsedTime"),
                        "question_count": metrics.get("questionCount"),
                        "llm_calls": metrics.get("llmCalls"),
                    }
                )
            
            try:
                await self.memory_manager.add_memory(
                    content=f"Auto grading run for assignment attempt {assignment_attempt_id}",
                    memory_type="episodic",
                    metadata={
                        "type": "teacher_auto_grading",
                        "teacher_id": teacher_id,
                        "student_id": student_id,
                        "assignment_attempt_id": assignment_attempt_id,
                        "session_id": session_id,
                        "grading_method": "direct_pipeline",
                    },
                )
            except Exception as e:
                logger.warning("[TeacherService] Failed to store auto grading: %s", e)
            
            return result
        
        # Fallback to agent-based approach (for complex cases or if pipeline not available)
        question = (
            query
            or f"Automatically grade assignment attempt {assignment_attempt_id}."
        )
        if student_id:
            question += f" Student: {student_id}."

        # Build context with student-specific data if student_id provided
        context_bundle = await self.context_builder.build(
            query=question,
            user_id=teacher_id,
            top_k=10,
            session_id=session_id,
        )
        
        # If student_id provided, try to get personalized context from snapshot
        if student_id and self.classroom_snapshot_updater:
            try:
                # Try to get classroom_id from StudentAssignment if available
                # For now, we'll use the snapshot with student_id filter
                # Note: This requires knowing classroom_id - may need to fetch StudentAssignment
                pass  # TODO: Fetch StudentAssignment to get classroom_id if needed
            except Exception as e:
                logger.warning(
                    "[TeacherService] Failed to get personalized student context: %s", e
                )

        # Create agent with assignment attempt data
        agent = AutoGradingAgent(
            teacher_id=teacher_id,
            llm=self.llm,
            assignment_attempt_data=assignment_attempt_data,
            memory_manager=self.memory_manager,
            sentiment_tool=self.sentiment_tool,
            use_remote=settings.TEACHER_AGENTS_USE_REMOTE,
        )

        result = await agent.grade_submission(
            assignment_attempt_id=assignment_attempt_id,
            student_id=student_id,
            focus=query,
        )

        try:
            await self.memory_manager.add_memory(
                content=f"Auto grading run for assignment attempt {assignment_attempt_id}",
                memory_type="episodic",
                metadata={
                    "type": "teacher_auto_grading",
                    "teacher_id": teacher_id,
                    "student_id": student_id,
                    "assignment_attempt_id": assignment_attempt_id,
                    "session_id": session_id,
                    "context_tokens": context_bundle.total_tokens,
                    "grading_method": "agent",
                },
            )
        except Exception as e:
            logger.warning("[TeacherService] Failed to store auto grading: %s", e)

        result.setdefault("metadata", {})
        result["metadata"]["context_bundle"] = {
            "total_tokens": context_bundle.total_tokens,
            "token_budget": context_bundle.token_budget,
            "items_count": len(context_bundle.items),
        }
        result["metadata"]["grading_method"] = "agent"

        return result

    def _map_student_analysis_to_intervention_response(
        self, result: Dict[str, Any], classroom_data: Optional[Dict[str, Any]] = None
    ) -> InterventionResponse:
        """
        Map StudentAnalysisAgent output into InterventionResponse so that
        /teacher/student-analysis is compatible with the existing FE contract
        of /recommendations/analyze-progress.

        When no student_id is provided, analyzes all students in the classroom
        and returns a list of StudentOverview for each student.
        """
        # Extract basic fields
        answer: str = str(result.get("answer", "") or "").strip()
        tool_results: Dict[str, Any] = result.get("tool_results") or {}
        student_id: Optional[str] = result.get("student_id")
        classroom_id: Optional[int] = result.get("classroom_id")
        teacher_id: Optional[str] = result.get("teacher_id")

        # Build student ID to name mapping for post-processing
        student_id_to_name: Dict[str, str] = {}
        if classroom_data:
            all_students = classroom_data.get("students", [])
            for student in all_students:
                sid = str(student.get("student_id", "") or student.get("studentId", ""))
                sname = student.get("student_name") or student.get("studentName")
                if sid and sname:
                    student_id_to_name[sid] = sname

        # Post-process answer to replace IDs with names
        answer = self._replace_ids_with_names(answer, student_id_to_name, teacher_id)

        # Log answer to debug truncation issues
        logger.info(
            f"[TeacherService] Mapping student analysis result | "
            f"answer_length={len(answer)}, student_id={student_id}, classroom_id={classroom_id} | "
            f"full_answer={answer}"
        )

        student_overview: Dict[str, Any] = tool_results.get("student_overview") or {}
        class_overview: Dict[str, Any] = tool_results.get("class_overview") or {}

        # Improved split logic: Extract a meaningful overview
        # Strategy: Find first substantial paragraph (not just title)
        overview_text = ""
        ai_insights_text = ""
        
        # Remove markdown title formatting if present
        cleaned_answer = answer.strip()
        
        # Try to find first substantial section (after title/header)
        # Look for section markers like "###", "**1.", "**2.", or first paragraph after title
        section_markers = ["\n\n###", "\n###", "\n**1.", "\n**2.", "\n**3.", "\n---\n", "\n\n---"]
        
        split_pos = -1
        for marker in section_markers:
            pos = cleaned_answer.find(marker)
            if pos > 50:  # Ensure we skip title and get to actual content
                split_pos = pos
                break
        
        if split_pos > 0:
            # Split at section marker
            overview_text = cleaned_answer[:split_pos].strip()
            ai_insights_text = cleaned_answer[split_pos:].strip()
        elif "\n\n" in cleaned_answer:
            # Split at first paragraph break, but ensure overview is substantial
            parts = cleaned_answer.split("\n\n", 1)
            if len(parts) == 2:
                first_part = parts[0].strip()
                # If first part is too short (likely just title), take more
                if len(first_part) < 100:
                    # Take first 2 paragraphs or first 300 chars as overview
                    overview_text = cleaned_answer[:min(300, len(cleaned_answer))].strip()
                    ai_insights_text = cleaned_answer[min(300, len(cleaned_answer)):].strip()
                else:
                    overview_text = first_part
                    ai_insights_text = parts[1].strip()
        else:
            # No clear split point - use first 200-300 chars as overview
            if len(cleaned_answer) > 300:
                overview_text = cleaned_answer[:300].strip()
                ai_insights_text = cleaned_answer[300:].strip()
            else:
                # Short answer - use entire as insights, extract first sentence as overview
                if "." in cleaned_answer:
                    first_sentence = cleaned_answer.split(".", 1)[0].strip()
                    if len(first_sentence) > 20:  # Ensure it's not just title
                        overview_text = first_sentence + "."
                        ai_insights_text = cleaned_answer
                    else:
                        overview_text = cleaned_answer[:min(150, len(cleaned_answer))].strip()
                        ai_insights_text = cleaned_answer
                else:
                    overview_text = cleaned_answer[:min(150, len(cleaned_answer))].strip()
                    ai_insights_text = cleaned_answer
        
        # Clean up: Remove markdown formatting from overview if it's just a title
        if overview_text.startswith("**") and overview_text.endswith("**"):
            # It's just a title, take more content
            if len(ai_insights_text) > 0:
                # Combine title with first part of insights
                overview_text = overview_text + " " + ai_insights_text[:200].strip()
                if len(ai_insights_text) > 200:
                    ai_insights_text = ai_insights_text[200:].strip()
                else:
                    ai_insights_text = ""
        
        # Ensure overview ends properly
        if overview_text and not overview_text.endswith((".", "!", "?")):
            overview_text += "."
        
        # Fallback if overview is still too short
        if len(overview_text) < 50:
            # Use first substantial paragraph from insights
            if ai_insights_text:
                # Find first sentence or first 200 chars
                if "." in ai_insights_text:
                    first_sent = ai_insights_text.split(".", 1)[0].strip()
                    if len(first_sent) > 20:
                        overview_text = first_sent + "."
                    else:
                        overview_text = ai_insights_text[:200].strip() + "..."
                else:
                    overview_text = ai_insights_text[:200].strip() + "..."
            else:
                overview_text = "Không có phân tích chi tiết."
        
        # Ensure insights has content
        if not ai_insights_text:
            ai_insights_text = overview_text
        
        # Log final mapped values
        logger.info(
            f"[TeacherService] Mapped analysis result | "
            f"overview_text_length={len(overview_text)}, ai_insights_text_length={len(ai_insights_text)}"
        )

        # Helper function for safe float conversion
        def _safe_float(val: Any, default: float = 0.0) -> float:
            try:
                return float(val)
            except Exception:
                return default

        # Helper function to create StudentOverview from student data
        def _create_student_overview(
            sid: str,
            student_data: Dict[str, Any],
            use_overview_text: bool = False
        ) -> StudentOverview:
            from datetime import datetime, timezone
            
            completion_rate = _safe_float(student_data.get("completion_rate", 0.0))
            average_score = _safe_float(student_data.get("average_score", 0.0))
            engagement_score = _safe_float(
                student_data.get("engagement_score", completion_rate)
            )

            # Prefer completion rate as primary progress metric; fallback to average score
            progress_percent = (
                completion_rate * 100.0 if completion_rate > 0.0 else average_score
            )

            # Calculate days enrolled if joined_at is available
            days_enrolled = None
            joined_at_str = student_data.get("joined_at")
            if joined_at_str:
                try:
                    if isinstance(joined_at_str, str):
                        if joined_at_str.endswith("Z"):
                            joined_at = datetime.fromisoformat(joined_at_str.replace("Z", "+00:00"))
                        else:
                            joined_at = datetime.fromisoformat(joined_at_str)
                        if joined_at.tzinfo is None:
                            joined_at = joined_at.replace(tzinfo=timezone.utc)
                        days_enrolled = (datetime.now(timezone.utc) - joined_at).days
                except Exception:
                    pass
            
            is_new_student = days_enrolled is not None and days_enrolled < 7
            
            if is_new_student:
                if (engagement_score < 0.1 and progress_percent < 5.0) or \
                   (average_score > 0 and average_score < 30.0 and progress_percent < 10.0):
                    current_status = "AtRisk"
                elif progress_percent < 30.0:
                    current_status = "NeedsSupport"
                elif progress_percent < 60.0:
                    current_status = "Good"
                else:
                    current_status = "Excellent"
            else:
                # For established students, use original thresholds
                if progress_percent < 50.0 or engagement_score < 0.3:
                    current_status = "AtRisk"
                elif progress_percent < 70.0:
                    current_status = "NeedsSupport"
                elif progress_percent < 90.0:
                    current_status = "Good"
                else:
                    current_status = "Excellent"

            status_text = (
                f"Học sinh hiện đạt khoảng {progress_percent:.1f}% tiến độ, "
                f"mức độ tham gia (engagement) khoảng {engagement_score:.2f}. "
                f"Trạng thái tổng thể: {current_status}."
            )

            # Use overview_text for single student, or generate generic for multiple students
            if use_overview_text:
                intervention_text = overview_text
            else:
                student_name = student_data.get("student_name", sid)
                intervention_text = (
                    f"Học sinh {student_name} đang ở mức {current_status}. "
                    f"Tiến độ: {progress_percent:.1f}%."
                )

            return StudentOverview(
                studentId=sid,
                progressPercent=progress_percent,
                currentStatus=current_status,
                statusText=status_text,
                currentSection=None,
                interventionText=intervention_text,
            )

        # Build per-student overview
        students: List[StudentOverview] = []

        if student_id:
            # Single student analysis
            sid = str(student_id)
            
            # Try to get student name from classroom_data if available
            student_name = None
            if classroom_data:
                all_students = classroom_data.get("students", [])
                student_info = next(
                    (s for s in all_students if str(s.get("student_id", "") or s.get("studentId", "")) == sid),
                    None
                )
                if student_info:
                    student_name = student_info.get("student_name") or student_info.get("studentName")
            
            joined_at = None
            if classroom_data:
                all_students = classroom_data.get("students", [])
                student_info = next(
                    (s for s in all_students if str(s.get("student_id", "") or s.get("studentId", "")) == sid),
                    None
                )
                if student_info:
                    joined_at = student_info.get("joined_at") or student_info.get("joinedAt")
            
            student_data = {
                "completion_rate": student_overview.get("completion_rate", 0.0),
                "average_score": student_overview.get("average_score", 0.0),
                "engagement_score": student_overview.get("engagement_score"),
                "student_name": student_name,
                "joined_at": joined_at,  # Include enrollment date
            }
            students.append(_create_student_overview(sid, student_data, use_overview_text=True))
        elif classroom_data:
            # Multiple students analysis - analyze all students from classroom data
            all_students = classroom_data.get("students", [])
            
            if not all_students:
                logger.warning(
                    "[TeacherService] No students found in classroom data for classroom_id=%s",
                    classroom_id
                )
            else:
                # Get engagement metrics if available
                engagement_metrics = {}
                time_metrics = classroom_data.get("time_metrics", {})
                engagement_list = time_metrics.get("engagement_metrics", [])
                for em in engagement_list:
                    em_student_id = em.get("student_id")
                    if em_student_id:
                        engagement_metrics[str(em_student_id)] = em
                
                # Create StudentOverview for each student
                for student in all_students:
                    sid = str(student.get("student_id", "") or student.get("studentId", ""))
                    if not sid:
                        continue
                    
                    # Get engagement metrics for this student
                    em = engagement_metrics.get(sid, {})
                    
                    # Calculate completion rate from student data
                    # Try multiple sources for student metrics
                    completion_rate = (
                        _safe_float(em.get("completion_rate"))
                        or _safe_float(student.get("completion_rate"))
                        or 0.0
                    )
                    
                    average_score = (
                        _safe_float(student.get("average_score"))
                        or _safe_float(em.get("average_score"))
                        or 0.0
                    )
                    
                    engagement_score = (
                        _safe_float(em.get("engagement_score"))
                        or _safe_float(student.get("engagement_score"))
                        or completion_rate
                    )
                    
                    student_data = {
                        "student_id": sid,
                        "student_name": student.get("student_name") or student.get("studentName") or sid,
                        "completion_rate": completion_rate,
                        "average_score": average_score,
                        "engagement_score": engagement_score,
                        "joined_at": student.get("joined_at") or student.get("joinedAt"),  # Include enrollment date
                    }
                    
                    students.append(_create_student_overview(sid, student_data, use_overview_text=False))

        # Post-process overview and insights to replace IDs with names
        overview_text = self._replace_ids_with_names(overview_text, student_id_to_name, teacher_id)
        ai_insights_text = self._replace_ids_with_names(ai_insights_text, student_id_to_name, teacher_id)

        return InterventionResponse(
            overviewText=overview_text,
            students=students,
            aiInsightsText=ai_insights_text,
        )

    def _replace_ids_with_names(
        self, text: str, student_id_to_name: Dict[str, str], teacher_id: Optional[str]
    ) -> str:
       
        if not text:
            return text
        
        result = text
        
        # Replace teacher_id patterns (e.g., "teacher_123", "teacher_id: teacher_123")
        if teacher_id:
            # Replace specific patterns to avoid false positives
            result = result.replace(f"teacher {teacher_id}", "giáo viên")
            result = result.replace(f"teacher_{teacher_id}", "giáo viên")
            result = result.replace(f"Dành cho giáo viên: {teacher_id}", "Dành cho giáo viên")
            result = result.replace(f"teacher_id: {teacher_id}", "giáo viên")
            # Replace standalone teacher_id only if it's a UUID-like pattern (contains underscores or hyphens)
            # This avoids replacing teacher_id that might appear in other contexts
            if "_" in teacher_id or "-" in teacher_id:
                result = re.sub(rf'\b{re.escape(teacher_id)}\b', "giáo viên", result)
        
        # Replace student IDs with names
        for sid, sname in student_id_to_name.items():
            # Replace full UUID patterns with word boundaries
            result = re.sub(rf'\b{re.escape(sid)}\b', sname, result)
            # Replace patterns like "Học sinh A (da398a3c...)" or "học sinh da398a3c..."
            result = re.sub(rf'học sinh\s+{re.escape(sid)}', f'học sinh {sname}', result, flags=re.IGNORECASE)
            result = re.sub(rf'Học sinh\s+{re.escape(sid)}', f'Học sinh {sname}', result)
            # Replace patterns like "(da398a3c...)" with "(Tên học sinh)"
            result = re.sub(rf'\({re.escape(sid)}\)', f'({sname})', result)
            # Replace patterns like "da398a3c..." at start of sentences or after punctuation
            result = re.sub(rf'([\.\s]+){re.escape(sid)}([\.\s,]+)', rf'\1{sname}\2', result)
        
        return result


