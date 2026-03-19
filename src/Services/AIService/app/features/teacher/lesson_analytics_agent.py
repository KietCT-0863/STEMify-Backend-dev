from typing import Dict, Any, Optional
import logging

from app.core.agent.plan_solve_agent import PlanAndSolveInsightsAgent
from app.core.tools.registry import ToolRegistry
from app.core.tools.lesson_data_tool import LessonDataTool
from app.core.tools.engagement_analysis_tool import EngagementAnalysisTool
from app.core.tools.completion_analysis_tool import CompletionAnalysisTool
from app.core.tools.performance_trend_tool import PerformanceTrendTool
from app.core.tools.graph_reasoning_tool import GraphReasoningTool
from app.core.data.lesson_repository import LessonRepository
from app.core.data.classroom_repository import ClassroomRepository
from app.core.graph.client import GraphClient
from app.core.reasoning.orchestrator import GraphReasoningOrchestrator
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class LessonAnalyticsAgent(PlanAndSolveInsightsAgent):
    """
    Lesson Analytics Agent

    Plan-and-Solve paradigm for structured lesson analysis:
    - Submission rate
    - Engagement level
    - Completion time / effort (proxied via progress + attempts)
    - Score history / performance trend
    """

    def __init__(
        self,
        teacher_id: str,
        llm: LLMClient,
        lesson_repository: LessonRepository,
        classroom_repository: ClassroomRepository,
        graph_client: GraphClient,
        graph_reasoning_orchestrator: Optional[GraphReasoningOrchestrator] = None,
        use_remote: bool = False,
    ):
        system_prompt = f"""You are a lesson analytics expert assisting teacher {teacher_id}.

Analyze lesson performance across multiple dimensions:
1. Submission rate (tỉ lệ nộp bài)
2. Engagement level (mức tương tác)
3. Completion / progress patterns
4. Effort level (số lần thử, thời gian)
5. Score history (lịch sử điểm, xu hướng)

Use the available tools to fetch data and then synthesize clear, actionable insights."""

        super().__init__(
            name=f"LessonAnalyticsAgent_{teacher_id}",
            llm=llm,
            system_prompt=system_prompt,
            use_remote=use_remote,
        )

        tool_registry = ToolRegistry()

        tool_registry.register_tool(
            LessonDataTool(lesson_repository=lesson_repository)
        )
        tool_registry.register_tool(
            EngagementAnalysisTool(classroom_repository=classroom_repository)
        )
        tool_registry.register_tool(
            CompletionAnalysisTool(classroom_repository=classroom_repository)
        )
        tool_registry.register_tool(
            PerformanceTrendTool(graph_client=graph_client)
        )

        if graph_reasoning_orchestrator is not None:
            tool_registry.register_tool(
                GraphReasoningTool(reasoning_orchestrator=graph_reasoning_orchestrator)
            )

        self.tool_registry = tool_registry
        self.teacher_id = teacher_id

        logger.info("LessonAnalyticsAgent initialized for teacher %s", teacher_id)

    async def analyze_lesson(
        self,
        lesson_id: str,
        classroom_id: Optional[int] = None,
        focus: Optional[str] = None,
    ) -> Dict[str, Any]:
        """
        High-level entrypoint for lesson analytics.

        focus: optional hint, e.g. "engagement drops", "completion bottlenecks".
        """
        query = f"Analyze lesson {lesson_id} for teacher {self.teacher_id}."
        if classroom_id is not None:
            query += f" Classroom context: {classroom_id}."
        if focus:
            query += f" Focus on: {focus}."

        result = await self.run(query)
        result["agent_type"] = "lesson_analytics"
        result["teacher_id"] = self.teacher_id
        result["lesson_id"] = lesson_id
        if classroom_id is not None:
            result["classroom_id"] = classroom_id

        return result


