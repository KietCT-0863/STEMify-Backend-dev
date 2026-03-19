"""
Recommendations Service
Business logic for analyzing student progress and generating intervention recommendations
"""

import json
import logging
import re
import asyncio
from typing import Dict, Any, Optional, List

from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import LLMMessage
from app.core.rag.ingestion_pipeline import IngestionPipeline
from app.core.rag.document_processor import DocumentProcessor
from app.core.embedding.pipeline import EmbeddingPipeline
from app.core.graph.builder import GraphBuilder
from app.core.graph.client import GraphClient
from app.core.graph.monitor import GraphMonitor
from app.core.vector_store.client import VectorStoreClient
from app.core.data.classroom_repository import ClassroomRepository
from app.common.exceptions.ai_exceptions import LLMResponseParseError
from app.features.recommendations.models import (
    StudentProgressRequest,
    InterventionResponse,
    StudentInterventionReport,
    StudentProgressMetrics,
    InterventionRecommendation,
    InterventionType,
    InterventionPriority,
    WeakTopic,
    StudentOverview,
)
from app.features.recommendations.prompts import (
    build_classroom_context,
    build_intervention_prompt,
)
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class RecommendationsService:
    """
    Service for analyzing student progress and generating intervention recommendations.
    """

    def __init__(
        self,
        llm_client: LLMClient,
        classroom_repository: ClassroomRepository,
        ingestion_pipeline: Optional[IngestionPipeline] = None,
    ):
        self.llm_client = llm_client
        self.classroom_repository = classroom_repository
        self.ingestion_pipeline = ingestion_pipeline

    async def analyze_student_progress(
        self, request: StudentProgressRequest
    ) -> InterventionResponse:
        """
        Analyze student progress and generate intervention recommendations.
        
        Steps:
        1. Load classroom data (from repository or mock)
        2. Optionally ingest data into RAG system for enhanced analysis
        3. Build context from classroom data
        4. Generate recommendations via LLM
        5. Parse and return structured response
        """
        # Step 1: Load classroom data from repository
        if request.force_mock or request.classroom_id is None:
            classroom_id = None
        else:
            classroom_id = request.classroom_id
        
        classroom_data = await self.classroom_repository.get_classroom_data(
            classroom_id=classroom_id,
            student_id=request.student_id,
            analysis_period_days=request.analysis_period_days
        )

        try:
            classroom_data_preview = json.dumps(classroom_data)
        except Exception:
            classroom_data_preview = "unserializable"

        logger.info(
            "Classroom repository response " + classroom_data_preview,
            
        )
        
        is_fallback = False
        if hasattr(self.classroom_repository, 'was_fallback_used'):
            is_fallback = self.classroom_repository.was_fallback_used()
        elif request.force_mock or request.classroom_id is None:
            is_fallback = True
        
        # Step 2: Optionally ingest data for RAG-based analysis
        if self.ingestion_pipeline:
            try:
                logger.info(
                    "Ingesting classroom data for enhanced analysis",
                    extra={"classroom_id": request.classroom_id},
                )
                await self.ingestion_pipeline.ingest(classroom_data)
            except Exception as e:
                logger.warning(
                    f"Failed to ingest data for RAG analysis: {e}",
                    exc_info=True,
                )
                # Continue without RAG enhancement
        
        # Step 3: precompute metrics and build context/batches
        if settings.RECOMMENDATIONS_BACKEND_COMPUTE_METRICS:
            precomputed_metrics = self._compute_student_metrics(classroom_data)
            classroom_data["precomputed_metrics"] = precomputed_metrics
        else:
            precomputed_metrics = None

        # Step 4: Generate recommendations via LLM (batched or single-call)
        if settings.RECOMMENDATIONS_ENABLE_BATCHING:
            model_name, raw_content = await self._generate_reports_batched(
                classroom_data, request
            )
        else:
            model_name, raw_content = await self._generate_reports_single(
                classroom_data, request
            )

        direct_llm_response = self._try_parse_llm_response(raw_content)
        if direct_llm_response:
            return direct_llm_response

        raise LLMResponseParseError(
            "LLM response is not valid InterventionResponse JSON. "
            "Please ensure the LLM returns the expected schema."
        )


    def _compute_student_metrics(
        self, classroom_data: Dict[str, Any]
    ) -> Dict[str, Dict[str, Any]]:
        students = classroom_data.get("students", [])
        enrollments = classroom_data.get("enrollments", {})
        curriculum_enrollments = enrollments.get("curriculum_enrollments", [])
        course_enrollments = enrollments.get("course_enrollments", [])

        quizzes = classroom_data.get("quizzes", {})
        student_quizzes = quizzes.get("student_quizzes", [])

        assignments = classroom_data.get("assignments", {})
        student_assignments = assignments.get("student_assignments", [])

        time_metrics = classroom_data.get("time_metrics", {})
        engagement_metrics = time_metrics.get("engagement_metrics", [])

        students_by_id = {s.get("student_id"): s for s in students}

        curriculum_by_student: Dict[str, list[Dict[str, Any]]] = {}
        for ce in curriculum_enrollments:
            sid = ce.get("student_id")
            if sid:
                curriculum_by_student.setdefault(sid, []).append(ce)

        course_by_student: Dict[str, list[Dict[str, Any]]] = {}
        for ce in course_enrollments:
            sid = ce.get("student_id")
            if sid:
                course_by_student.setdefault(sid, []).append(ce)

        quizzes_by_student: Dict[str, list[Dict[str, Any]]] = {}
        for sq in student_quizzes:
            sid = sq.get("student_id")
            if sid:
                quizzes_by_student.setdefault(sid, []).append(sq)

        assignments_by_student: Dict[str, list[Dict[str, Any]]] = {}
        for sa in student_assignments:
            sid = sa.get("student_id")
            if sid:
                assignments_by_student.setdefault(sid, []).append(sa)

        engagement_by_student: Dict[str, Dict[str, Any]] = {
            em.get("student_id"): em for em in engagement_metrics if em.get("student_id")
        }

        # Weak topics: reuse quiz/topic structure similar to prompt logic
        quiz_attempts = quizzes.get("quiz_attempts", [])
        student_topic_performance: Dict[str, Dict[str, Dict[str, int]]] = {}
        for sq in student_quizzes:
            sid = sq.get("student_id")
            if not sid:
                continue
            for attempt in quiz_attempts:
                if attempt.get("student_quiz_id") != sq.get("id"):
                    continue
                for qa in attempt.get("question_attempts", []):
                    topics_list = qa.get("topics", [])
                    is_correct = qa.get("is_correct", False)
                    if not topics_list:
                        continue
                    student_topic_performance.setdefault(sid, {})
                    for topic_name in topics_list:
                        stats = student_topic_performance[sid].setdefault(
                            topic_name, {"correct": 0, "total": 0}
                        )
                        stats["total"] += 1
                        if is_correct:
                            stats["correct"] += 1

        min_attempts = settings.RECOMMENDATIONS_WEAK_TOPIC_MIN_ATTEMPTS
        max_topics = settings.RECOMMENDATIONS_WEAK_TOPIC_MAX_TOPICS

        metrics_by_student: Dict[str, Dict[str, Any]] = {}

        for sid, student in students_by_id.items():
            # overall_progress_percentage
            curriculum_list = curriculum_by_student.get(sid, [])
            course_list = course_by_student.get(sid, [])

            curriculum_progress_values = [
                ce.get("progress_percentage", 0) for ce in curriculum_list
            ]
            course_progress_values = [
                ce.get("progress_percentage", 0) for ce in course_list
            ]

            def _avg(values: list[float]) -> float:
                return float(sum(values) / len(values)) if values else 0.0

            curriculum_progress = _avg(curriculum_progress_values)
            course_progress = _avg(course_progress_values)

            if curriculum_progress_values and course_progress_values:
                overall_progress = (curriculum_progress + course_progress) / 2.0
            elif curriculum_progress_values:
                overall_progress = curriculum_progress
            elif course_progress_values:
                overall_progress = course_progress
            else:
                overall_progress = 0.0

            # average_score from quizzes + assignments
            quiz_scores = [
                sq.get("final_score")
                for sq in quizzes_by_student.get(sid, [])
                if sq.get("final_score") is not None
            ]
            assignment_scores = [
                sa.get("final_score")
                for sa in assignments_by_student.get(sid, [])
                if sa.get("final_score") is not None
            ]
            all_scores = [float(s) for s in quiz_scores + assignment_scores]
            if all_scores:
                average_score = float(sum(all_scores) / len(all_scores))
            else:
                average_score = 0.0

            # engagement metrics
            engagement = engagement_by_student.get(sid, {})
            completion_rate = float(engagement.get("completion_rate", 0.0))
            engagement_score = completion_rate
            days_since_last_activity = int(engagement.get("days_since_last_activity", 0))

            # weak topics
            weak_topics_data = []
            for topic_name, stats in student_topic_performance.get(sid, {}).items():
                total = stats["total"]
                if total < min_attempts:
                    continue
                correct = stats["correct"]
                correct_rate = correct / total if total > 0 else 0.0
                mastery_score = correct_rate
                weak_topics_data.append(
                    {
                        "topic_id": None,  # topic_id mapping can be added in later phases
                        "topic_name": topic_name,
                        "mastery_score": float(mastery_score),
                        "attempts_count": int(total),
                        "correct_rate": float(correct_rate),
                    }
                )

            # Sort weak topics by mastery_score ascending (weakest first) and cap
            weak_topics_data.sort(key=lambda t: t["mastery_score"])
            weak_topics_data = weak_topics_data[:max_topics]

            metrics_by_student[sid] = {
                "student_id": sid,
                "student_name": student.get("student_name", ""),
                "overall_progress_percentage": float(overall_progress),
                "average_score": float(average_score),
                "completion_rate": completion_rate,
                "engagement_score": engagement_score,
                "days_since_last_activity": days_since_last_activity,
                "weak_topics": weak_topics_data,
            }

        return metrics_by_student

    def _build_student_overviews(
        self, reports: List[StudentInterventionReport]
    ) -> List[StudentOverview]:
        """
        Build high-level per-student overview objects from detailed reports.

        This step intentionally keeps the logic simple and explainable:
        - progressPercent: rounded overall_progress_percentage
        - currentStatus: derived from highest-priority recommendation or engagement
        - statusText: uses the report summary
        - interventionText: uses description of the highest-priority recommendation
        """

        def _status_from_report(report: StudentInterventionReport) -> str:
            metrics = report.progress_metrics

            # If there is any CRITICAL/HIGH recommendation, mark as AtRisk
            priorities = [rec.priority for rec in report.recommendations]
            if InterventionPriority.CRITICAL in priorities or InterventionPriority.HIGH in priorities:
                return "AtRisk"

            # If progress or engagement is low, also treat as AtRisk
            if metrics.overall_progress_percentage < 50 or metrics.engagement_score < 0.3:
                return "AtRisk"

            # Otherwise consider Good for now
            return "Good"

        def _pick_top_recommendation(report: StudentInterventionReport) -> Optional[InterventionRecommendation]:
            if not report.recommendations:
                return None

            # Sort by priority severity (CRITICAL > HIGH > MEDIUM > LOW)
            priority_order = {
                InterventionPriority.CRITICAL: 0,
                InterventionPriority.HIGH: 1,
                InterventionPriority.MEDIUM: 2,
                InterventionPriority.LOW: 3,
            }
            sorted_recs = sorted(
                report.recommendations,
                key=lambda r: priority_order.get(r.priority, 99),
            )
            return sorted_recs[0]

        overviews: List[StudentOverview] = []
        for report in reports:
            metrics = report.progress_metrics
            progress_percent = int(round(metrics.overall_progress_percentage))
            current_status = _status_from_report(report)

            top_rec = _pick_top_recommendation(report)
            if top_rec is not None:
                intervention_text = top_rec.description or top_rec.title
            else:
                intervention_text = (
                    "No specific interventions were generated. Monitor the student's progress and provide support if needed."
                )

            overview = StudentOverview(
                studentId=report.student_id,
                progressPercent=max(0, min(100, progress_percent)),
                currentStatus=current_status,
                statusText=report.summary or "No detailed status summary available.",
                currentSection=None,
                interventionText=intervention_text,
            )
            overviews.append(overview)

        return overviews

    def _build_overview_text(
        self,
        students: List[StudentOverview],
        is_fallback: bool,
    ) -> str:
        """Build the top-level overviewText string similar to AI_Analysis.json."""
        if not students:
            return "No students were analyzed. Please check the input data or try again later."

        at_risk = [s for s in students if s.currentStatus == "AtRisk"]
        good = [s for s in students if s.currentStatus == "Good"]

        parts: List[str] = []
        parts.append(
            "Overall, the class shows {} students in good standing and {} marked as at risk based on recent activity and performance.".format(
                len(good),
                len(at_risk),
            )
        )

        if at_risk:
            parts.append(
                "Additional support may be required for students flagged as At-Risk, especially those with low progress or engagement."
            )

        if is_fallback:
            parts.append(
                "Note: This overview is based on fallback/mock data because full classroom data was not available."
            )

        return " ".join(parts)

    def _build_ai_insights_text(
        self,
        students: List[StudentOverview],
    ) -> str:
        """
        Build aiInsightsText: a short narrative about the class.

        For now this is derived from aggregated student statuses instead of asking the LLM again.
        """
        if not students:
            return "No AI insights are available because no students were analyzed."

        average_progress = sum(s.progressPercent for s in students) / len(students)
        at_risk_count = sum(1 for s in students if s.currentStatus == "AtRisk")

        return (
            "The class demonstrates an average progress of {:.1f}%. "
            "There are {} students currently flagged as At-Risk who may benefit from targeted interventions. "
            "Consider reviewing their detailed status and intervention suggestions for more context.".format(
                average_progress,
                at_risk_count,
            )
        )

    def _build_prompt_classroom_data(
        self, classroom_data: Dict[str, Any]
    ) -> Dict[str, Any]:
        """
        Build a trimmed version of classroom_data to control prompt size.

        Phase 1: only limit the number of students mentioned in the prompt,
        according to configuration. The full classroom_data is still used for
        fallback/default reports when JSON parsing fails.
        """
        max_students = settings.RECOMMENDATIONS_MAX_STUDENTS_PER_CALL
        if max_students <= 0:
            return classroom_data

        students = classroom_data.get("students", [])
        if len(students) <= max_students:
            return classroom_data

        # Simple strategy (Phase 1): take the first N students.
        trimmed_students = students[:max_students]

        # Shallow copy to avoid mutating original classroom_data
        prompt_data: Dict[str, Any] = dict(classroom_data)
        prompt_data["students"] = trimmed_students
        return prompt_data

    async def _generate_reports_single(
        self,
        classroom_data: Dict[str, Any],
        request: StudentProgressRequest,
    ) -> tuple[str, str]:
        """
        Single-call path: one LLM call for the (possibly trimmed) classroom data.
        Returns (model_name, raw_content).
        """
        prompt_classroom_data = self._build_prompt_classroom_data(classroom_data)
        context_text = build_classroom_context(prompt_classroom_data)
        prompt = build_intervention_prompt(context_text, lang=request.lang or "vi")

        logger.info(
            "Generating intervention recommendations via LLM (single call)",
            extra={
                "classroom_id": request.classroom_id,
                "student_id": request.student_id,
                "analysis_period_days": request.analysis_period_days,
            },
        )

        logger.info(
            "LLM single-call prompt " + prompt,
        )

        temperature = (
            settings.RECOMMENDATIONS_LLM_TEMPERATURE
            if settings.RECOMMENDATIONS_LLM_TEMPERATURE is not None
            else settings.LLM_TEMPERATURE
        )
        max_tokens = settings.LLM_MAX_TOKENS * settings.RECOMMENDATIONS_LLM_MAX_TOKENS_MULTIPLIER

        response = await self.llm_client.generate_remote(
            [
                LLMMessage(
                    role="system",
                    content=settings.RECOMMENDATIONS_SYSTEM_PROMPT
                    or "You are an expert educational consultant specializing in STEM education and student progress analysis.",
                ),
                LLMMessage(role="user", content=prompt),
            ],
            temperature=temperature,
            max_tokens=max_tokens,
        )

        logger.info("LLM response received %s", response.content)

        return response.model, response.content

    async def _generate_reports_batched(
        self,
        classroom_data: Dict[str, Any],
        request: StudentProgressRequest,
    ) -> tuple[str, str]:
        """
        Batched path: multiple LLM calls, each handling a subset of students.
        Returns (last_model_name, last_raw_content).
        """
        students = classroom_data.get("students", [])
        if not students:
            return settings.LLM_MODEL, ""

        batch_size = max(1, settings.RECOMMENDATIONS_BATCH_SIZE)
        # Also respect the per-call cap to avoid exceeding prompt limits
        batch_size = min(batch_size, settings.RECOMMENDATIONS_MAX_STUDENTS_PER_CALL)

        last_model_name = settings.LLM_MODEL
        last_raw_content: str = ""

        precomputed_metrics = classroom_data.get("precomputed_metrics", {})

        # Build batch descriptors
        batches: list[dict[str, Any]] = []
        for start in range(0, len(students), batch_size):
            batch_students = students[start : start + batch_size]
            batch_ids = {s.get("student_id") for s in batch_students}

            batch_data: Dict[str, Any] = dict(classroom_data)
            batch_data["students"] = batch_students
            if precomputed_metrics:
                batch_data["precomputed_metrics"] = {
                    sid: m for sid, m in precomputed_metrics.items() if sid in batch_ids
                }

            batches.append(
                {
                    "index": start // batch_size,
                    "start": start,
                    "size": len(batch_students),
                    "data": batch_data,
                }
            )

        # Semaphore to limit parallel batch execution
        max_parallel = max(1, settings.RECOMMENDATIONS_MAX_PARALLEL_BATCHES)
        semaphore = asyncio.Semaphore(max_parallel)

        async def _run_one_batch(batch: dict[str, Any]) -> tuple[Optional[str], Optional[str]]:
            async with semaphore:
                idx = batch["index"]
                batch_data = batch["data"]

                context_text = build_classroom_context(batch_data)
                prompt = build_intervention_prompt(context_text, lang=request.lang or "vi")

                logger.info(
                    "Generating intervention recommendations via LLM (batch)",
                    extra={
                        "classroom_id": request.classroom_id,
                        "student_id": request.student_id,
                        "analysis_period_days": request.analysis_period_days,
                        "batch_index": idx,
                        "batch_size": batch["size"],
                    },
                )

                logger.info(
                    "LLM batch prompt " + prompt,
                )

                try:
                    temperature = (
                        settings.RECOMMENDATIONS_LLM_TEMPERATURE
                        if settings.RECOMMENDATIONS_LLM_TEMPERATURE is not None
                        else settings.LLM_TEMPERATURE
                    )
                    max_tokens = (
                        settings.LLM_MAX_TOKENS
                        * settings.RECOMMENDATIONS_LLM_MAX_TOKENS_MULTIPLIER
                    )

                    response = await self.llm_client.generate_remote(
                        [
                            LLMMessage(
                                role="system",
                                content=settings.RECOMMENDATIONS_SYSTEM_PROMPT
                                or "You are an expert educational consultant specializing in STEM education and student progress analysis.",
                            ),
                            LLMMessage(role="user", content=prompt),
                        ],
                        temperature=temperature,
                        max_tokens=max_tokens,
                    )
                    logger.info("LLM batch response received %s", response.content)

                    return response.model, response.content
                except Exception as e:  # noqa: BLE001
                    logger.error(
                        "LLM batch call failed: %s",
                        e,
                        exc_info=True,
                    )
                    raise

        # Run batches in parallel with bounded concurrency
        results = await asyncio.gather(
            *[_run_one_batch(batch) for batch in batches],
            return_exceptions=False,
        )

        for model_name, raw_content in results:
            if model_name:
                last_model_name = model_name
            if raw_content:
                last_raw_content = raw_content

        return last_model_name, last_raw_content

    def _try_parse_llm_response(
        self, content: str
    ) -> Optional[InterventionResponse]:
        content = content.strip()
        
        markdown_start = re.search(r'```(?:json)?\s*', content)
        if markdown_start:
            start_pos = markdown_start.end()
            markdown_end = content.find('```', start_pos)
            if markdown_end != -1:
                # Extract content between markdown markers
                json_content = content[start_pos:markdown_end].strip()
                # Find balanced braces in the extracted content
                brace_start = json_content.find('{')
                if brace_start != -1:
                    brace_count = 0
                    brace_end = brace_start
                    for i in range(brace_start, len(json_content)):
                        if json_content[i] == '{':
                            brace_count += 1
                        elif json_content[i] == '}':
                            brace_count -= 1
                            if brace_count == 0:
                                brace_end = i
                                json_str = json_content[brace_start:brace_end + 1]
                                break
                    else:
                        json_str = None
                else:
                    json_str = None
            else:
                json_str = None
        else:
            json_str = None
        
        # If not found in markdown, try to find JSON object directly (match balanced braces)
        if not json_str:
            brace_start = content.find('{')
            if brace_start != -1:
                brace_count = 0
                brace_end = brace_start
                for i in range(brace_start, len(content)):
                    if content[i] == '{':
                        brace_count += 1
                    elif content[i] == '}':
                        brace_count -= 1
                        if brace_count == 0:
                            brace_end = i
                            break
                if brace_count == 0:
                    json_str = content[brace_start:brace_end + 1]
                else:
                    json_str = content
            else:
                json_str = content
        
        try:
            data = json.loads(json_str)
            return InterventionResponse.model_validate(data)
        except Exception as e:
            logger.warning(f"Failed to parse LLM response: {e}")
            logger.debug(f"Response content: {content[:500]}")
            return None

    def _parse_recommendations_from_answer(
        self, answer: str, classroom_data: Dict[str, Any]
    ) -> list[StudentInterventionReport]:
        """
        Deprecated: the service now expects the LLM to return the final InterventionResponse schema directly.
        """
        raise LLMResponseParseError(
            "Custom parsing is disabled. Ensure the LLM returns InterventionResponse JSON."
        )
