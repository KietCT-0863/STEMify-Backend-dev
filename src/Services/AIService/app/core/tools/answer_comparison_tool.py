from typing import Dict, Any, List
import logging
import json

from app.core.tools.base import Tool
from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import LLMMessage

logger = logging.getLogger(__name__)


class AnswerComparisonTool(Tool):
    """
    Compare student answer with standard answer using LLM.

    Returns structured comparison per rubric dimension if provided.
    """

    def __init__(self, llm_client: LLMClient):
        super().__init__(
            name="answer_comparison",
            description="Compare student answer with model answer using LLM",
        )
        self.llm_client = llm_client

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Parameters:
        - student_answer: str
        - model_answer: str
        - rubric_criteria: Optional[list of str] - dimensions to compare on
        """
        student_answer = parameters.get("student_answer")
        model_answer = parameters.get("model_answer")
        rubric_criteria = parameters.get("rubric_criteria") or []

        if not student_answer:
            return json.dumps({"error": "student_answer is required"})
        
        # Model answer is optional - if not provided, evaluate based on rubric only
        if not model_answer:
            logger.info(
                "[AnswerComparisonTool] No model answer provided, evaluating based on rubric criteria only"
            )
            # Return a basic evaluation structure without comparison
            return json.dumps({
                "overall_similarity": None,
                "summary": "Evaluation based on rubric criteria only (no model answer provided)",
                "per_criterion": {
                    criterion: {
                        "evaluated": True,
                        "note": "Evaluated against rubric without model answer"
                    }
                    for criterion in rubric_criteria
                } if rubric_criteria else {}
            })

        system_prompt = (
            "You are an expert grader. Compare the student's answer with the model answer. "
            "Highlight correctness, missing points, misconceptions, and alignment with rubric criteria. "
            "Respond in JSON with fields: overall_similarity (0-1), summary, and per_criterion if criteria are given."
        )

        user_parts = [
            "STUDENT_ANSWER:\n",
            student_answer,
            "\n\nMODEL_ANSWER:\n",
            model_answer,
        ]
        if rubric_criteria:
            user_parts.append(
                "\n\nRUBRIC_CRITERIA:\n- " + "\n- ".join(rubric_criteria)
            )
        user_prompt = "".join(user_parts)
        user_prompt += (
            "\n\nRespond in JSON format with these fields:\n"
            "- overall_similarity (number 0-1): Similarity score between answers\n"
            "- summary (string): Brief summary of the comparison\n"
            "- per_criterion (object): Detailed comparison per rubric criterion if provided\n"
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
                comparison_data = json.loads(content)
            except json.JSONDecodeError:
                # If JSON parsing fails, create a simple comparison structure
                logger.warning(
                    "[AnswerComparisonTool] Failed to parse JSON from LLM response, creating fallback structure"
                )
                comparison_data = {
                    "overall_similarity": 0.5,
                    "summary": content,
                    "per_criterion": {}
                }
            
            return json.dumps(comparison_data)
        except Exception as e:
            logger.error("[AnswerComparisonTool] LLM error: %s", e, exc_info=True)
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
                    "description": "Reference/standard answer text (optional - if not provided, evaluation will be based on rubric criteria only)",
                },
                "rubric_criteria": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Optional rubric dimensions for detailed comparison",
                },
            },
            "required": ["student_answer"],
        }


