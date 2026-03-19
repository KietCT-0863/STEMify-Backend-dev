from typing import Dict, Any, List
import logging
import json

from app.core.tools.base import Tool
from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import LLMMessage

logger = logging.getLogger(__name__)


class FeedbackGeneratorTool(Tool):
    """
    Generate constructive feedback for a graded submission using LLM.

    Input typically includes:
    - student_answer
    - model_answer
    - rubric_summary / comparison_result
    - optional sentiment preference (tone)
    """

    def __init__(self, llm_client: LLMClient):
        super().__init__(
            name="feedback_generator",
            description="Generate constructive grading feedback for a student",
        )
        self.llm_client = llm_client

    async def run(self, parameters: Dict[str, Any]) -> str:
        student_answer = parameters.get("student_answer")
        model_answer = parameters.get("model_answer")
        comparison_result = parameters.get("comparison_result")
        desired_tone = parameters.get("tone", "supportive")

        if not student_answer:
            return json.dumps({"error": "student_answer is required"})
        
        # Model answer is optional - if not provided, generate feedback based on rubric/comparison only
        if not model_answer:
            logger.info(
                "[FeedbackGeneratorTool] No model answer provided, generating feedback based on rubric/comparison only"
            )
            model_answer = "Not provided - evaluate based on rubric criteria and student's answer quality"

        system_prompt = (
            "You are an educational assistant giving feedback to a student. "
            "Be clear, kind, and growth-oriented. Avoid giving direct final exam answers if not appropriate."
        )

        user_payload = {
            "student_answer": student_answer,
            "model_answer": model_answer,
            "comparison_result": comparison_result,
            "tone": desired_tone,
        }

        user_prompt = (
            "Based on the following data, write feedback in JSON format with these fields:\n"
            "- brief_overview (string): Brief summary of the feedback\n"
            "- strengths (array of strings): List of strengths in the student's work\n"
            "- areas_for_improvement (array of strings): List of areas that need improvement\n"
            "- next_steps (array of strings): Suggested next steps for the student\n\n"
            f"DATA:\n{json.dumps(user_payload, ensure_ascii=False)}\n\n"
            "Respond with valid JSON only, no additional text."
        )

        try:
            messages: List[LLMMessage] = [
                LLMMessage(role="system", content=system_prompt),
                LLMMessage(role="user", content=user_prompt)
            ]
            
            response = await self.llm_client.generate_remote(messages)
            
            # Try to parse JSON from response
            content = response.content.strip()
            
            # Remove markdown code blocks if present
            if content.startswith("```json"):
                content = content[7:]  # Remove ```json
            if content.startswith("```"):
                content = content[3:]  # Remove ```
            if content.endswith("```"):
                content = content[:-3]  # Remove closing ```
            content = content.strip()
            
            try:
                feedback_data = json.loads(content)
            except json.JSONDecodeError:
                # If JSON parsing fails, create a simple feedback structure
                logger.warning(
                    "[FeedbackGeneratorTool] Failed to parse JSON from LLM response, creating fallback structure"
                )
                feedback_data = {
                    "brief_overview": content,
                    "strengths": [],
                    "areas_for_improvement": [],
                    "next_steps": []
                }
            
            return json.dumps(feedback_data)
        except Exception as e:
            logger.error("[FeedbackGeneratorTool] LLM error: %s", e, exc_info=True)
            return json.dumps({"error": str(e)})

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "student_answer": {
                    "type": "string",
                    "description": "Student's answer text",
                },
                "model_answer": {
                    "type": "string",
                    "description": "Reference/standard answer text (optional - if not provided, feedback will be based on rubric criteria only)",
                },
                "comparison_result": {
                    "type": "object",
                    "description": "Optional structured comparison from AnswerComparisonTool",
                },
                "tone": {
                    "type": "string",
                    "description": "Desired feedback tone (e.g., supportive, neutral, direct)",
                    "default": "supportive",
                },
            },
            "required": ["student_answer"],
        }


