from typing import Dict, Any, Optional, List
from collections import defaultdict
import logging
import json

from app.core.agent.plan_solve_agent import PlanAndSolveInsightsAgent
from app.core.tools.registry import ToolRegistry
from app.core.tools.student_data_tool import StudentDataTool
from app.core.tools.performance_analysis_tool import PerformanceAnalysisTool
from app.core.tools.pattern_recognition_tool import PatternRecognitionTool
from app.core.tools.recommendation_tool import RecommendationTool
from app.core.tools.graph_reasoning_tool import GraphReasoningTool
from app.core.data.classroom_repository import ClassroomRepository
from app.core.graph.client import GraphClient
from app.core.memory.memory_manager import MemoryManager
from app.core.reasoning.orchestrator import GraphReasoningOrchestrator
from app.core.snapshot.classroom_snapshot_store import (
    ClassroomSnapshotStore,
    ClassroomSnapshotUpdater,
)
from app.core.llm.client import LLMClient
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class StudentAnalysisAgent(PlanAndSolveInsightsAgent):
    """
    Student Analysis Agent for Teachers

    Plan-and-Solve paradigm for structured student analysis:
    1. Gather comprehensive student data
    2. Analyze performance patterns and trends
    3. Identify strengths and weaknesses
    4. Compare with class average and benchmarks
    5. Generate personalized recommendations
    """

    def __init__(
        self,
        teacher_id: str,
        llm: LLMClient,
        classroom_repository: ClassroomRepository,
        graph_client: GraphClient,
        memory_manager: MemoryManager,
        graph_reasoning_orchestrator: Optional[GraphReasoningOrchestrator] = None,
        use_remote: bool = False,
        classroom_snapshot_store: Optional[ClassroomSnapshotStore] = None,
        classroom_snapshot_updater: Optional[ClassroomSnapshotUpdater] = None,
    ):
        system_prompt_template = (
            settings.TEACHER_STUDENT_ANALYSIS_SYSTEM_PROMPT
            or self._default_system_prompt_template()
        )
        system_prompt = system_prompt_template.format(teacher_id=teacher_id)

        super().__init__(
            name=f"StudentAnalysisAgent_{teacher_id}",
            llm=llm,
            system_prompt=system_prompt,
            use_remote=use_remote,
        )

        tool_registry = ToolRegistry()

        # Student-level and classroom aggregates (backed by snapshot store/updater if available)
        if classroom_snapshot_updater and classroom_snapshot_store:
            student_data_tool = StudentDataTool(
                snapshot_updater=classroom_snapshot_updater,
                snapshot_store=classroom_snapshot_store,
            )
        else:
            # Fallback to direct repository access (no snapshot optimization)
            student_data_tool = StudentDataTool(classroom_repository=classroom_repository)

        tool_registry.register_tool(student_data_tool)

        # Graph-based performance metrics (existing tool)
        tool_registry.register_tool(
            PerformanceAnalysisTool(student_id=None, graph_client=graph_client)
        )

        # Neo4j pattern mining for struggles/excels
        tool_registry.register_tool(
            PatternRecognitionTool(graph_client=graph_client)
        )

        # Recommendations backed by memory + retriever (existing tool)
        tool_registry.register_tool(
            RecommendationTool(
                student_id=None,
                hybrid_retriever=None,
                memory_manager=memory_manager,
            )
        )

        if graph_reasoning_orchestrator is not None:
            tool_registry.register_tool(
                GraphReasoningTool(reasoning_orchestrator=graph_reasoning_orchestrator)
            )

        self.tool_registry = tool_registry
        self.teacher_id = teacher_id

        logger.info("StudentAnalysisAgent initialized for teacher %s", teacher_id)

    async def analyze_student(
        self,
        classroom_id: Optional[int],
        student_id: Optional[str],
        focus: Optional[str] = None,
        force_mock: bool = False,
        analysis_period_days: Optional[int] = 7,
        lang: Optional[str] = "vi",
        context_bundle: Optional[Any] = None,
        classroom_data: Optional[Dict[str, Any]] = None,
    ) -> Dict[str, Any]:
        """
        High-level entrypoint for teacher to analyze a student or a classroom.

        Plan-and-solve pattern with performance constraints:
        - Phase 1: LLM plans (single call)
        - Phase 2: Code executes tools (snapshot, graph, patterns)
        - Phase 3: LLM summarizes (single call)
        """
        if student_id and classroom_id:
            question = (
                f"Analyze student {student_id} in classroom {classroom_id} "
                f"and provide a structured report for the teacher."
            )
        else:
            question = "Analyze classroom learning progress and provide a structured report for the teacher."

        if focus:
            question += f" Focus on: {focus}."

        # Phase 1 – Planning (LLM)
        plan: List[str] = await self._generate_plan(question)

        # Phase 2 – Execute concrete tool pipeline 
        tool_results = await self._execute_analysis_tools(
            classroom_id=classroom_id,
            student_id=student_id,
            force_mock=force_mock,
            analysis_period_days=analysis_period_days,
        )

        # Phase 3 – Summarize for teacher (LLM)
        summary = await self._summarize_for_teacher(
            question=question,
            plan=plan,
            tool_results=tool_results,
            lang=lang,
            context_bundle=context_bundle,
            classroom_data=classroom_data,
        )

        return {
            "answer": summary,
            "path": "plan-solve",
            "plan": plan,
            "tool_results": tool_results,
            "agent_type": "student_analysis",
            "teacher_id": self.teacher_id,
            "classroom_id": classroom_id,
            "student_id": student_id,
        }

    def _default_system_prompt_template(self) -> str:
        return """You are an expert educational analyst assisting teacher {teacher_id}.

Your task is to analyze student performance and provide actionable insights.

Analysis steps:
1. Gather comprehensive student and classroom data
2. Analyze performance patterns and trends (graph + metrics)
3. Identify strengths and weaknesses
4. Compare with class average and benchmarks
5. Generate personalized, evidence-based recommendations

Always explain which data and tools you used for your conclusions."""

    async def _execute_analysis_tools(
        self,
        classroom_id: Optional[int],
        student_id: Optional[str],
        force_mock: bool,
        analysis_period_days: Optional[int],
    ) -> Dict[str, Any]:
        """
        Execute a fixed, efficient tool pipeline:
        - student_data: overview + class_overview (snapshot-backed)
        - performance_analysis: patterns for the student (if available)
        - pattern_recognition: struggles/excels from graph (student/class)
        """
        results: Dict[str, Any] = {}

        # 1) Student & class snapshot-backed data
        student_data_tool = self.tool_registry.get_tool("student_data")

        logger.info(
            "[StudentAnalysisAgent] Executing analysis tools | "
            "classroom_id=%s, student_id=%s, force_mock=%s, analysis_period_days=%s",
            classroom_id,
            student_id,
            force_mock,
            analysis_period_days,
        )
        
        if student_data_tool and classroom_id is not None:
            if student_id:
                try:
                    student_overview_raw = await student_data_tool.run(
                        {
                            "action": "overview",
                            "student_id": student_id,
                            "classroom_id": classroom_id,
                            "force_mock": force_mock,
                            "analysis_period_days": analysis_period_days,
                        }
                    )
                    results["student_overview"] = json.loads(student_overview_raw)
                except Exception as exc:
                    logger.warning(
                        "[StudentAnalysisAgent] student_overview failed: %s", exc
                    )

            # Class-level overview
            try:
                class_overview_raw = await student_data_tool.run(
                    {
                        "action": "class_overview",
                        "classroom_id": classroom_id,
                        "force_mock": force_mock,
                        "analysis_period_days": analysis_period_days,
                    }
                )
                results["class_overview"] = json.loads(class_overview_raw)
                logger.info(
                    "[StudentAnalysisAgent] Class overview: %s",
                    results["class_overview"],
                )
            except Exception as exc:
                logger.warning(
                    "[StudentAnalysisAgent] class_overview failed: %s", exc
                )
            
            try:
                detailed_data_raw = await student_data_tool.run(
                    {
                        "action": "detailed_data",
                        "classroom_id": classroom_id,
                        "force_mock": force_mock,
                        "analysis_period_days": analysis_period_days,
                    }
                )
                results["detailed_classroom_data"] = json.loads(detailed_data_raw)
                logger.info(
                    "[StudentAnalysisAgent] Detailed classroom data (summarized): %s",
                    results["detailed_classroom_data"],
                )
            except Exception as exc:
                logger.warning(
                    "[StudentAnalysisAgent] detailed_data failed: %s", exc
                )

        # 2) Performance analysis (Neo4j patterns per student)
        performance_tool = self.tool_registry.get_tool("performance_analysis")
        logger.info(
            "[StudentAnalysisAgent] Executing performance analysis tools | "
            "student_id=%s",
            student_id,
        )
        if performance_tool and student_id:
            try:
                performance_raw = await performance_tool.run(
                    {
                        "action": "get_patterns",
                        "student_id": student_id,
                    }
                )
                results["performance_patterns"] = json.loads(performance_raw)
            except Exception as exc:
                logger.warning(
                    "[StudentAnalysisAgent] performance_analysis failed: %s", exc
                )

        # 3) Pattern recognition (class-level and/or student-level)
        pattern_tool = self.tool_registry.get_tool("pattern_recognition")
        logger.info(
            "[StudentAnalysisAgent] Executing pattern recognition tools | "
            "student_id=%s",
            student_id,
        )
        if pattern_tool:
            # Student struggles/excels
            if student_id:
                try:
                    struggles_raw = await pattern_tool.run(
                        {
                            "action": "struggles",
                            "scope": "student",
                            "student_id": student_id,
                        }
                    )
                    excels_raw = await pattern_tool.run(
                        {
                            "action": "excels",
                            "scope": "student",
                            "student_id": student_id,
                        }
                    )
                    results["pattern_student_struggles"] = json.loads(struggles_raw)
                    results["pattern_student_excels"] = json.loads(excels_raw)
                except Exception as exc:
                    logger.warning(
                        "[StudentAnalysisAgent] pattern_recognition (student) failed: %s",
                        exc,
                    )

            # Class struggles/excels (if classroom_id is known)
            if classroom_id is not None:
                try:
                    class_struggles_raw = await pattern_tool.run(
                        {
                            "action": "struggles",
                            "scope": "class",
                            "classroom_id": classroom_id,
                        }
                    )
                    class_excels_raw = await pattern_tool.run(
                        {
                            "action": "excels",
                            "scope": "class",
                            "classroom_id": classroom_id,
                        }
                    )
                    logger.info(
                        "[StudentAnalysisAgent] Pattern recognition (class) struggles: %s",
                        class_struggles_raw,
                    )
                    logger.info(
                        "[StudentAnalysisAgent] Pattern recognition (class) excels: %s",
                        class_excels_raw,
                    )
                    results["pattern_class_struggles"] = json.loads(
                        class_struggles_raw
                    )
                    results["pattern_class_excels"] = json.loads(class_excels_raw)
                except Exception as exc:
                    logger.warning(
                        "[StudentAnalysisAgent] pattern_recognition (class) failed: %s",
                        exc,
                    )

        return results

    async def _summarize_for_teacher(
        self,
        question: str,
        plan: List[str],
        tool_results: Dict[str, Any],
        lang: Optional[str],
        context_bundle: Optional[Any] = None,
        classroom_data: Optional[Dict[str, Any]] = None,
    ) -> str:
        """
        Final LLM summarization step that turns structured data into a
        teacher-friendly narrative.
        """
        from datetime import datetime, timezone
        now = datetime.now(timezone.utc)
        current_date_str = now.strftime("%Y-%m-%d")
        current_datetime_str = now.strftime("%Y-%m-%d %H:%M:%S UTC")
        
        from datetime import datetime, timezone
        # Get current date/time for AI to understand timeline context
        now = datetime.now(timezone.utc)
        current_date_str = now.strftime("%Y-%m-%d")
        current_datetime_str = now.strftime("%Y-%m-%d %H:%M:%S UTC")
        
        enrollment_context = {}
        if classroom_data:
            
            students = classroom_data.get("students", [])
            for student in students:
                student_id = student.get("student_id")
                joined_at_str = student.get("joined_at")
                if student_id and joined_at_str:
                    try:
                        if isinstance(joined_at_str, str):
                            if joined_at_str.endswith("Z"):
                                joined_at = datetime.fromisoformat(joined_at_str.replace("Z", "+00:00"))
                            else:
                                joined_at = datetime.fromisoformat(joined_at_str)
                            if joined_at.tzinfo is None:
                                joined_at = joined_at.replace(tzinfo=timezone.utc)
                            
                            days_enrolled = (now - joined_at).days
                            enrollment_context[student_id] = {
                                "days_enrolled": days_enrolled,
                                "joined_at": joined_at_str,
                            }
                    except Exception as e:
                        logger.warning(
                            f"[StudentAnalysisAgent] Failed to parse joined_at for student {student_id}: {e}"
                        )
        
        # Extract RAG context from context_bundle
        rag_context = ""
        if context_bundle and hasattr(context_bundle, "items"):
            rag_items = []
            for item in context_bundle.items[:5]: 
                rag_items.append(f"- {item.content[:200]}...")  # Truncate long content
            if rag_items:
                rag_context = "\n\nRELEVANT CONTEXT FROM KNOWLEDGE BASE:\n" + "\n".join(rag_items)
        
        slimmed_results: Dict[str, Any] = {}

        student_id_to_name: Dict[str, str] = {}
        if classroom_data:
            students = classroom_data.get("students", [])
            for student in students:
                student_id = str(student.get("student_id", "") or student.get("studentId", ""))
                student_name = student.get("student_name") or student.get("studentName")
                if student_id and student_name:
                    student_id_to_name[student_id] = student_name
        
        if student_id_to_name:
            slimmed_results["student_names"] = student_id_to_name

        try:
            # Keep student_overview and class_overview as-is (already compact)
            if tool_results.get("student_overview"):
                slimmed_results["student_overview"] = tool_results["student_overview"]
            if tool_results.get("class_overview"):
                slimmed_results["class_overview"] = tool_results["class_overview"]

            detailed = tool_results.get("detailed_classroom_data") or {}
            if detailed:
                progress_summaries = None
                if "student_progress_summaries" in detailed:
                    progress_summaries = detailed["student_progress_summaries"]
                elif "studentProgressSummaries" in detailed:
                    progress_summaries = detailed["studentProgressSummaries"]
                
                quiz_attempts = detailed.get("quizAttempts", []) or detailed.get("quiz_attempts", [])
                wrong_answers = []
                for attempt in quiz_attempts[:10]:  # Limit to recent 10 attempts
                    student_quiz_id = attempt.get("student_quiz_id")
                    total_score = attempt.get("total_score", 0)
                    status = attempt.get("status")
                    started_at = attempt.get("started_at")
                    question_attempts = attempt.get("question_attempts", [])
                    
                    # Collect wrong answers with question details
                    for qa in question_attempts:
                        if not qa.get("is_correct", True):
                            wrong_answers.append({
                                "student_quiz_id": student_quiz_id,
                                "question_content": qa.get("question_content", ""),
                                "answer_content": qa.get("answer_content", ""),
                                "question_type": qa.get("question_type", ""),
                                "topics": qa.get("topics", []),
                                "quiz_score": total_score,
                                "quiz_status": status,
                                "started_at": started_at,
                            })
                
                # Extract quiz titles from studentQuizzes
                student_quizzes = detailed.get("studentQuizzes", []) or detailed.get("student_quizzes", [])
                quiz_info = []
                for sq in student_quizzes[:20]:  # Limit to 20 quizzes
                    if sq.get("final_score", 0) > 0 or sq.get("attempt_count", 0) > 0:
                        quiz_info.append({
                            "quiz_id": sq.get("id"),
                            "quiz_title": sq.get("quiz_title", ""),
                            "final_score": sq.get("final_score", 0),
                            "attempt_count": sq.get("attempt_count", 0),
                            "student_id": sq.get("student_id"),
                        })
                
                # Extract in-progress sections with names
                section_progress = detailed.get("sectionProgress", []) or detailed.get("section_progress", [])
                in_progress_sections = []
                completed_sections_recent = []
                for sp in section_progress:
                    status_val = sp.get("status", "")
                    if status_val == "InProgress":
                        in_progress_sections.append({
                            "student_id": sp.get("student_id"),
                            "section_id": sp.get("section_id"),
                            "section_name": sp.get("section_name", ""),
                            "last_activity_at": sp.get("last_activity_at"),
                        })
                    elif status_val == "Completed":
                        # Keep recent completed sections (last 10 per student)
                        completed_sections_recent.append({
                            "student_id": sp.get("student_id"),
                            "section_id": sp.get("section_id"),
                            "section_name": sp.get("section_name", ""),
                            "last_activity_at": sp.get("last_activity_at"),
                        })
                
                # Group completed sections by student and keep only recent ones
                student_completed = defaultdict(list)
                for cs in completed_sections_recent:
                    student_completed[cs["student_id"]].append(cs)
                
                recent_completed = []
                for student_id, sections in student_completed.items():
                    # Sort by last_activity_at descending and take top 5
                    sorted_sections = sorted(
                        sections,
                        key=lambda x: x.get("last_activity_at", ""),
                        reverse=True
                    )[:5]
                    recent_completed.extend(sorted_sections)
                
                student_assignments = detailed.get("studentAssignments", []) or detailed.get("student_assignments", [])
                submitted_assignments = []
                for sa in student_assignments:
                    if sa.get("submission_count", 0) > 0:
                        submitted_assignments.append({
                            "student_id": sa.get("student_id"),
                            "final_score": sa.get("final_score", 0),
                            "submitted_at": sa.get("submitted_at"),
                            "due_date": sa.get("due_date"),
                            "submission_count": sa.get("submission_count", 0),
                        })
                
                slimmed_detailed: Dict[str, Any] = {
                    "classroom_id": detailed.get("classroom_id"),
                    "student_progress_summaries": progress_summaries,
                    "quiz_summary": detailed.get("quiz_summary"),
                    "assignment_summary": detailed.get("assignment_summary"),
                    "section_summary": detailed.get("section_summary"),
                    "engagement_summary": detailed.get("engagement_summary"),
                    "wrong_answers": wrong_answers[:20],  
                    "quiz_info": quiz_info,  
                    "in_progress_sections": in_progress_sections,  
                    "recent_completed_sections": recent_completed[:30],  
                    "submitted_assignments": submitted_assignments,  
                }
                slimmed_results["detailed_classroom_data"] = slimmed_detailed

            # Performance and pattern tools usually return already-aggregated JSON.
            for key in [
                "performance_patterns",
                "pattern_student_struggles",
                "pattern_student_excels",
                "pattern_class_struggles",
                "pattern_class_excels",
            ]:
                if key in tool_results:
                    slimmed_results[key] = tool_results[key]

            # Add enrollment context to slimmed_results
            if enrollment_context:
                slimmed_results["enrollment_context"] = enrollment_context
            
            context_json = json.dumps(slimmed_results, ensure_ascii=False)
        except Exception:
            context_json = "{}"

        lang_hint = lang or "vi"

        prompt = (
            f"{self.get_system_prompt()}\n\n"
            f"Teacher question/context:\n{question}\n\n"
            f"Planned steps:\n- " + "\n- ".join(plan or []) + "\n\n"
            f"CURRENT DATE: {current_date_str} ({current_datetime_str}). Use to calculate days since events and identify overdue assignments.\n\n"
            "You also have structured data from tools (JSON below). "
            "Use it as the primary source of truth for metrics and patterns:\n\n"
            f"{context_json}\n\n"
            "IMPORTANT: Use 'student_names' mapping (student_id -> student_name) to convert all student IDs to names. "
            "ALWAYS use student names (e.g., 'Nhan Thanh', 'Man Trieu') instead of student IDs (UUIDs) when referring to students.\n"
            "When you see student_id in wrong_answers, quiz_info, in_progress_sections, submitted_assignments, etc., look up the name in 'student_names'.\n\n"
            "ABSOLUTELY DO NOT expose technical field names or 'Data used' lists. "
            "Do NOT mention keys like class_overview, detailed_classroom_data, student_names, enrollment_context, wrong_answers, quiz_info, in_progress_sections, submitted_assignments. "
            "Never write 'cần kiểm tra ID' or similar. Just speak naturally using student names and plain language.\n\n"
            "CRITICAL DATA INTERPRETATION RULES:\n"
            "- Use the summary fields (quiz_summary, assignment_summary, section_summary) for overall statistics.\n"
            "- Use detailed fields (wrong_answers, quiz_info, in_progress_sections) for specific, actionable insights.\n"
            "- assessment_completion_rate = quizzes/assignments; content_completion_rate = reading/sections.\n"
            "- If content_completion_rate is high but assessment_completion_rate is low, describe as 'engaged with content but needing more assessments', not 'AtRisk'.\n"
            "- Only consider a student truly 'AtRisk' when there is evidence such as:\n"
            "  * consistently low quiz/assignment scores despite sufficient assessment attempts, and/or\n"
            "  * long inactivity (high days_since_last_activity) combined with low content and assessment progress.\n"
            "- If summaries show activity (scores > 0, completed sections), do NOT say 'no data' or 'students haven't submitted'.\n"
            "- When analyzing mistakes or weaknesses, use 'wrong_answers' array to identify SPECIFIC questions students got wrong:\n"
            "  * Mention the actual question content and the wrong answer they selected.\n"
            "  * Group similar mistakes to identify learning gaps (e.g., 'students struggle with questions about servo motor').\n"
            "  * Use 'quiz_info' to identify which quizzes students have attempted and their scores.\n"
            "- When analyzing progress over time:\n"
            "  * Use 'in_progress_sections', 'recent_completed_sections', 'submitted_assignments' with dates.\n"
            "  * Compare dates with today ({current_date_str}) to calculate days since events and identify overdue assignments.\n"
            "  * Use relative terms like '2 ngày trước' when mentioning dates.\n"
            "- When evaluating student status, consider all metrics together, not just one percentage.\n"
            "- For new students (<7 days enrolled), lower completion rates are normal - focus on engagement quality, not quantity.\n"
            f"{rag_context}\n\n"
            "WRITING STYLE:\n"
            "- Use natural Vietnamese, avoid technical terms. Use 'bài kiểm tra', 'bài tập', 'phần học' instead of 'quiz', 'assignment', 'section'.\n"
            "- Write as a helpful colleague, not a technical report. Use student names, not IDs.\n"
            "- Do NOT show internal data sources; weave insights into narrative sentences.\n\n"
            "TASK:\n"
            f"- Write analysis in {lang_hint}: status, strengths, weaknesses (with specific examples from wrong_answers), and 1-3 interventions.\n"
            "- Use wrong_answers, quiz_info, in_progress_sections for specific insights. Analyze patterns over time using dates.\n"
            "- Be precise: if a student got it wrong in one quiz but right in another, say 'hiểu chưa vững' not 'always wrong'.\n"
            "- Do NOT repeat JSON; summarize naturally.\n"
        )

        messages = [{"role": "user", "content": prompt}]
        
        plan_str = ", ".join(plan) if plan else "none"
        tool_results_keys = ", ".join(list(tool_results.keys())) if tool_results else "none"
        tool_results_json = json.dumps(tool_results, ensure_ascii=False) if tool_results else "{}"
                
        logger.info(
            f"[StudentAnalysisAgent] Calling LLM for teacher analysis summary | "
            f"teacher_id={self.teacher_id}, use_remote={self.use_remote}, "
            f"prompt_length={len(prompt)}, messages_count={len(messages)}, "
            f"tool_results_keys=[{tool_results_keys}] | "
            f"question={question}, plan=[{plan_str}] | "
            f"full_prompt={prompt} | "
            f"tool_results={tool_results_json}"
        )
        
        response = await self.llm.generate(
            messages, 
            use_remote=self.use_remote,
            max_tokens=4000  # Increased from default 2000 to allow full response
        )
        
        response_content = response.content if hasattr(response, "content") else str(response)
        finish_reason = getattr(response, "finish_reason", "unknown")
        
        # Log response to debug truncation issues
        logger.info(
            f"[StudentAnalysisAgent] Received LLM response for teacher analysis summary | "
            f"response_length={len(response_content)}, finish_reason={finish_reason} | "
            f"full_response={response_content}"
        )
        
        return response_content


