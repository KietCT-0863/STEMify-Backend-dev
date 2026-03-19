from typing import Dict, Any, Optional
import logging

from app.core.agent.react_agent import ReActTeachingAgent
from app.core.tools.registry import ToolRegistry
from app.core.tools.submission_tool import SubmissionTool
from app.core.tools.rubric_tool import RubricTool
from app.core.tools.answer_comparison_tool import AnswerComparisonTool
from app.core.tools.feedback_generator_tool import FeedbackGeneratorTool
from app.core.tools.score_calculator_tool import ScoreCalculatorTool
from app.core.tools.sentiment_analysis_tool import SentimentAnalysisTool
from app.core.data.classroom_repository import ClassroomRepository
from app.core.memory.memory_manager import MemoryManager
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class AutoGradingAgent(ReActTeachingAgent):
    """
    Automated Grading Agent

    ReAct paradigm for step-by-step evaluation:
    1. Retrieve student submission
    2. Get grading rubric and model answers
    3. Compare with standard answers
    4. Evaluate each criterion
    5. Calculate score
    6. Generate constructive feedback
    """

    def __init__(
        self,
        teacher_id: str,
        llm: LLMClient,
        assignment_attempt_data: Dict[str, Any],
        memory_manager: MemoryManager,
        sentiment_tool: Optional[SentimentAnalysisTool] = None,
        use_remote: bool = False,
    ):
        system_prompt = f"""You are an automated grading assistant for teacher {teacher_id}.

Your task:
1. Retrieve student submission using submission tool (this returns ALL questions - you must grade ALL of them)
2. For EACH question in the submission:
   - Extract the student's answer (from answerText or answerFileUrl)
   - Get grading rubric (try rubric_id like "assignment_1_rubric" or "assignment_rubric_1" or use default)
   - Evaluate the student's answer against the rubric criteria
   - Calculate score for that question based on rubric
   - Generate constructive feedback for that question
3. After grading all questions, calculate the total score
4. Provide a summary of all graded questions

GRADING STRATEGY:
- You can grade based on rubric criteria alone - model answer is helpful but NOT required
- If you have a model answer, use answer_comparison tool to compare
- If you don't have a model answer, evaluate directly against rubric criteria:
  * Accuracy: Is the answer correct? (0-10 points)
  * Completeness: Are all parts addressed? (0-5 points)
  * Clarity: Is the explanation clear? (0-5 points)
- For file submissions (PDF, images, code), evaluate based on:
  * Content quality (if you can infer from file type/name)
  * Completeness (file submitted vs missing)
  * Presentation (if applicable)

IMPORTANT:
- You MUST grade ALL questions in the submission, not just one
- The submission tool returns questionAttempts array - grade each one
- Use assignmentQuestionId to identify which question you're grading
- If a question has answerText, use that; if it has answerFileUrl, note the file type
- Provide separate scores and feedback for each question
- If rubric tool returns an error, use the default rubric criteria (Accuracy, Completeness, Clarity)
- You can proceed with grading even without a model answer - just evaluate against rubric

Always explain briefly how you used the rubric and data to reach your decisions.
When grading file submissions (images, PDFs, documents), analyze the content appropriately."""

        super().__init__(
            name=f"AutoGradingAgent_{teacher_id}",
            llm=llm,
            tool_registry=ToolRegistry(),
            system_prompt=system_prompt,
            max_steps=15,  # Increased to allow grading multiple questions
            use_remote=use_remote,
        )

        self.teacher_id = teacher_id
        self.memory_manager = memory_manager
        self.sentiment_tool = sentiment_tool

        # Core grading tools - pass assignment attempt data to SubmissionTool
        self.tool_registry.register_tool(
            SubmissionTool(assignment_attempt_data=assignment_attempt_data)
        )
        self.tool_registry.register_tool(RubricTool(memory_manager=memory_manager))
        self.tool_registry.register_tool(AnswerComparisonTool(llm_client=llm))
        self.tool_registry.register_tool(FeedbackGeneratorTool(llm_client=llm))
        self.tool_registry.register_tool(ScoreCalculatorTool())

        # Optional sentiment tool for tone adjustment (LIHUAN et al., 2022)
        if sentiment_tool:
            self.tool_registry.register_tool(sentiment_tool)

        logger.info("AutoGradingAgent initialized for teacher %s", teacher_id)

    async def grade_submission(
        self,
        assignment_attempt_id: int,
        student_id: Optional[str] = None,
        focus: Optional[str] = None,
    ) -> Dict[str, Any]:
        """
        High-level entrypoint for automated grading.

        Args:
            assignment_attempt_id: ID of the assignment attempt to grade
            student_id: Optional student ID for personalized context
            focus: optional hint, e.g. "concept understanding", "show work", etc.
        """
        query = f"Grade assignment attempt {assignment_attempt_id}."
        if student_id:
            query += f" Student: {student_id}."
        if focus:
            query += f" Focus on: {focus}."

        result = await self.run(query)
        result["agent_type"] = "auto_grading"
        result["teacher_id"] = self.teacher_id
        result["assignment_attempt_id"] = assignment_attempt_id
        if student_id:
            result["student_id"] = student_id

        return result


