from typing import Dict, Any, Optional, List
import logging
import json
import asyncio
import time
from datetime import datetime

from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import LLMMessage
from app.core.tools.rubric_tool import RubricTool
from app.core.tools.answer_comparison_tool import AnswerComparisonTool
from app.core.tools.feedback_generator_tool import FeedbackGeneratorTool
from app.core.tools.score_calculator_tool import ScoreCalculatorTool
from app.core.memory.memory_manager import MemoryManager

logger = logging.getLogger(__name__)


class DirectGradingPipeline:

    def __init__(
        self,
        llm: LLMClient,
        memory_manager: MemoryManager,
        rubric_tool: Optional[RubricTool] = None,
        answer_comparison_tool: Optional[AnswerComparisonTool] = None,
        feedback_generator_tool: Optional[FeedbackGeneratorTool] = None,
        score_calculator_tool: Optional[ScoreCalculatorTool] = None,
    ):
        self.llm = llm
        self.memory_manager = memory_manager
        
        # Initialize tools if not provided
        self.rubric_tool = rubric_tool or RubricTool(memory_manager=memory_manager)
        self.answer_comparison_tool = answer_comparison_tool or AnswerComparisonTool(llm_client=llm)
        self.feedback_generator_tool = feedback_generator_tool or FeedbackGeneratorTool(llm_client=llm)
        self.score_calculator_tool = score_calculator_tool or ScoreCalculatorTool()
        
        logger.info("DirectGradingPipeline initialized")

    async def grade(
        self,
        assignment_attempt_data: Dict[str, Any],
        rubric_data: Optional[Dict[str, Any]] = None,
        rubric_id: Optional[str] = None,
        model_answers: Optional[Dict[int, str]] = None,
    ) -> Dict[str, Any]:
        start_time = time.time()
        questions = assignment_attempt_data.get("questionAttempts", [])
        
        logger.info(
            "[DirectGradingPipeline] Starting grading",
            extra={
                "assignmentAttemptId": assignment_attempt_data.get("id"),
                "questionCount": len(questions),
            }
        )
        
        # Get rubric if not provided
        if not rubric_data and rubric_id:
            rubric_result = await self.rubric_tool.run({"rubric_id": rubric_id})
            try:
                rubric_json = json.loads(rubric_result)
                rubric_data = rubric_json.get("rubric") or rubric_json
            except Exception as e:
                logger.warning(
                    "[DirectGradingPipeline] Failed to parse rubric, using default",
                    extra={"error": str(e)}
                )
                rubric_data = None
        
        grading_tasks = [
            self.grade_question_parallel(
                question=qa,
                rubric=rubric_data,
                model_answer=model_answers.get(qa.get("assignmentQuestionId")) if model_answers else None,
            )
            for qa in questions
        ]
        
        question_results = await asyncio.gather(*grading_tasks, return_exceptions=True)
        
        processed_results = []
        total_score = 0.0
        max_score = 0.0
        
        for i, result in enumerate(question_results):
            question_id = questions[i].get("assignmentQuestionId")
            if isinstance(result, Exception):
                logger.error(
                    "[DirectGradingPipeline] Error grading question",
                    extra={
                        "questionId": question_id,
                        "error": str(result)
                    },
                    exc_info=True
                )
                processed_results.append({
                    "questionId": question_id,
                    "error": str(result),
                    "score": 0.0,
                })
            else:
                processed_results.append(result)
                total_score += result.get("score", 0.0)
                max_score += result.get("maxScore", 0.0)
        
        elapsed_time = time.time() - start_time
        
        logger.info(
            "[DirectGradingPipeline] Grading completed",
            extra={
                "assignmentAttemptId": assignment_attempt_data.get("id"),
                "questionCount": len(questions),
                "totalScore": total_score,
                "maxScore": max_score,
                "elapsedTime": elapsed_time,
            }
        )
        
        # Performance metrics
        metrics = {
            "elapsedTime": elapsed_time,
            "questionCount": len(questions),
            "llmCalls": self._estimate_llm_calls(questions, model_answers),
            "timestamp": datetime.utcnow().isoformat(),
        }
        
        return {
            "assignmentAttemptId": assignment_attempt_data.get("id"),
            "questions": processed_results,
            "totalScore": total_score,
            "maxScore": max_score,
            "percentage": (total_score / max_score * 100.0) if max_score > 0 else 0.0,
            "gradingMethod": "direct_pipeline",
            "metrics": metrics,
        }
    
    def _estimate_llm_calls(self, questions: List[Dict[str, Any]], model_answers: Optional[Dict[int, str]]) -> int:
        """Estimate number of LLM calls made"""
        if not model_answers:
            # No model answers - only feedback generation
            return len(questions)
        
        return min(len(questions) * 2, len(questions) + 1)  # Batch comparison reduces calls

    async def grade_question_parallel(
        self,
        question: Dict[str, Any],
        rubric: Optional[Dict[str, Any]],
        model_answer: Optional[str] = None,
    ) -> Dict[str, Any]:
        question_id = question.get("assignmentQuestionId")
        student_answer = question.get("answerText") or ""
        
        # Extract answer from file if needed
        answer_file_url = question.get("answerFileUrl")
        if answer_file_url and not student_answer:
            # Note: File content should be pre-processed by SubmissionTool
            # For now, we'll use the URL as a placeholder
            student_answer = f"[File submission: {answer_file_url}]"
        
        # Get rubric criteria
        rubric_criteria = []
        if rubric:
            criteria_list = rubric.get("criteria", [])
            if isinstance(criteria_list, list):
                rubric_criteria = [
                    c.get("criterion") if isinstance(c, dict) else str(c)
                    for c in criteria_list
                ]
        
        # Parallel: comparison and feedback generation (if model answer available)
        if model_answer:
            comparison_task = self.answer_comparison_tool.run({
                "student_answer": student_answer,
                "model_answer": model_answer,
                "rubric_criteria": rubric_criteria,
            })
            feedback_task = None  # Will be generated after comparison
        else:
            # No model answer - evaluate based on rubric only
            comparison_task = None
            feedback_task = self.feedback_generator_tool.run({
                "student_answer": student_answer,
                "model_answer": None,
                "comparison_result": None,
                "tone": "supportive",
            })
        
        # Execute comparison if available
        comparison_result = None
        if comparison_task:
            comparison_result_str = await comparison_task
            try:
                comparison_result = json.loads(comparison_result_str)
            except Exception as e:
                logger.warning(
                    "[DirectGradingPipeline] Failed to parse comparison result",
                    extra={"questionId": question_id, "error": str(e)}
                )
            
            # Generate feedback based on comparison
            if comparison_result and not comparison_result.get("error"):
                feedback_task = self.feedback_generator_tool.run({
                    "student_answer": student_answer,
                    "model_answer": model_answer,
                    "comparison_result": comparison_result,
                    "tone": "supportive",
                })
        
        # Execute feedback generation
        feedback_result = None
        if feedback_task:
            feedback_result_str = await feedback_task
            try:
                feedback_result = json.loads(feedback_result_str)
            except Exception as e:
                logger.warning(
                    "[DirectGradingPipeline] Failed to parse feedback result",
                    extra={"questionId": question_id, "error": str(e)}
                )
        
        # Calculate score
        score = self._calculate_score_from_comparison(
            comparison_result=comparison_result,
            rubric=rubric,
        )
        
        return {
            "questionId": question_id,
            "score": score["achieved"],
            "maxScore": score["max"],
            "feedback": feedback_result,
            "comparison": comparison_result,
        }

    def _calculate_score_from_comparison(
        self,
        comparison_result: Optional[Dict[str, Any]],
        rubric: Optional[Dict[str, Any]],
    ) -> Dict[str, float]:
        if not rubric:
            # Default scoring if no rubric
            if comparison_result and comparison_result.get("overall_similarity") is not None:
                similarity = comparison_result.get("overall_similarity", 0.0)
                return {
                    "achieved": similarity * 10.0,  # Scale to 0-10
                    "max": 10.0,
                }
            return {"achieved": 0.0, "max": 10.0}
        
        criteria = rubric.get("criteria", [])
        if not criteria:
            return {"achieved": 0.0, "max": 10.0}
        
        total_achieved = 0.0
        total_max = 0.0
        
        if comparison_result and comparison_result.get("per_criterion"):
            per_criterion = comparison_result.get("per_criterion", {})
            for criterion_data in criteria:
                criterion_name = criterion_data.get("criterion") if isinstance(criterion_data, dict) else str(criterion_data)
                max_points = float(criterion_data.get("max_points", 0.0)) if isinstance(criterion_data, dict) else 10.0
                
                criterion_result = per_criterion.get(criterion_name, {})
                if isinstance(criterion_result, dict):
                    achieved_points = float(criterion_result.get("score", 0.0))
                else:
                    # Fallback: use overall similarity if available
                    similarity = comparison_result.get("overall_similarity", 0.5)
                    achieved_points = similarity * max_points
                
                total_achieved += achieved_points
                total_max += max_points
        else:
            # No comparison result - use default scoring
            for criterion_data in criteria:
                max_points = float(criterion_data.get("max_points", 0.0)) if isinstance(criterion_data, dict) else 10.0
                total_max += max_points
            # Give partial credit based on answer presence
            total_achieved = total_max * 0.5  # 50% for attempting
        
        return {
            "achieved": total_achieved,
            "max": total_max,
        }

    async def batch_compare_answers(
        self,
        questions: List[Dict[str, Any]],
        model_answers: Dict[int, str],
        rubric: Optional[Dict[str, Any]],
    ) -> Dict[int, Dict[str, Any]]:
        # Extract rubric criteria
        rubric_criteria = []
        if rubric:
            criteria_list = rubric.get("criteria", [])
            if isinstance(criteria_list, list):
                rubric_criteria = [
                    c.get("criterion") if isinstance(c, dict) else str(c)
                    for c in criteria_list
                ]
        
        # Prepare batch comparison prompt
        comparisons = []
        for qa in questions:
            question_id = qa.get("assignmentQuestionId")
            student_answer = qa.get("answerText") or ""
            model_answer = model_answers.get(question_id)
            
            if model_answer:
                comparisons.append({
                    "questionId": question_id,
                    "studentAnswer": student_answer,
                    "modelAnswer": model_answer,
                })
        
        if not comparisons:
            return {}
        
        system_prompt = (
            "You are an expert grader. Compare multiple student answers with their model answers. "
            "For each comparison, provide: overall_similarity (0-1), summary, and per_criterion scores if rubric criteria are provided. "
            "Respond in JSON format with a list of comparison results."
        )
        
        user_prompt = (
            f"Compare the following {len(comparisons)} student answers with their model answers:\n\n"
        )
        
        for i, comp in enumerate(comparisons, 1):
            user_prompt += (
                f"Question {i} (ID: {comp['questionId']}):\n"
                f"STUDENT_ANSWER:\n{comp['studentAnswer']}\n\n"
                f"MODEL_ANSWER:\n{comp['modelAnswer']}\n\n"
            )
        
        if rubric_criteria:
            user_prompt += f"\nRUBRIC_CRITERIA:\n- " + "\n- ".join(rubric_criteria)
        
        user_prompt += (
            "\n\nRespond with a JSON array where each element has:\n"
            "- questionId: The question ID\n"
            "- overall_similarity: Number 0-1\n"
            "- summary: Brief summary\n"
            "- per_criterion: Object with criterion scores\n"
            "Respond with valid JSON only, no additional text."
        )
        
        try:
            messages: List[LLMMessage] = [
                LLMMessage(role="system", content=system_prompt),
                LLMMessage(role="user", content=user_prompt)
            ]
            
            response = await self.llm.generate_remote(messages)
            content = response.content.strip()
            
            # Remove markdown code blocks if present
            if content.startswith("```json"):
                content = content[7:]
            if content.startswith("```"):
                content = content[3:]
            if content.endswith("```"):
                content = content[:-3]
            content = content.strip()
            
            batch_results = json.loads(content)
            
            # Map results back to question IDs
            result_map = {}
            if isinstance(batch_results, list):
                for result in batch_results:
                    qid = result.get("questionId")
                    if qid:
                        result_map[qid] = result
            elif isinstance(batch_results, dict):
                # Single result or dict format
                for qa in questions:
                    qid = qa.get("assignmentQuestionId")
                    result_map[qid] = batch_results
            
            return result_map
            
        except Exception as e:
            logger.error(
                "[DirectGradingPipeline] Batch comparison failed, falling back to individual comparisons",
                extra={"error": str(e)},
                exc_info=True
            )
            # Fallback to individual comparisons
            return await self._fallback_individual_comparisons(questions, model_answers, rubric)

    async def _fallback_individual_comparisons(
        self,
        questions: List[Dict[str, Any]],
        model_answers: Dict[int, str],
        rubric: Optional[Dict[str, Any]],
    ) -> Dict[int, Dict[str, Any]]:
        """Fallback to individual comparisons if batch fails"""
        rubric_criteria = []
        if rubric:
            criteria_list = rubric.get("criteria", [])
            if isinstance(criteria_list, list):
                rubric_criteria = [
                    c.get("criterion") if isinstance(c, dict) else str(c)
                    for c in criteria_list
                ]
        
        comparison_tasks = []
        question_ids = []
        
        for qa in questions:
            question_id = qa.get("assignmentQuestionId")
            student_answer = qa.get("answerText") or ""
            model_answer = model_answers.get(question_id)
            
            if model_answer:
                question_ids.append(question_id)
                comparison_tasks.append(
                    self.answer_comparison_tool.run({
                        "student_answer": student_answer,
                        "model_answer": model_answer,
                        "rubric_criteria": rubric_criteria,
                    })
                )
        
        results = await asyncio.gather(*comparison_tasks, return_exceptions=True)
        
        result_map = {}
        for i, result_str in enumerate(results):
            if isinstance(result_str, Exception):
                continue
            try:
                result = json.loads(result_str)
                result_map[question_ids[i]] = result
            except Exception:
                continue
        
        return result_map

