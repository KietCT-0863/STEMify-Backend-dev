from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class ClassificationTool(Tool):
    """
    Classification Tool - MCP-compatible
    
    Classifies content into categories using LLM-based classification.
    """

    def __init__(
        self,
        llm: LLMClient,
    ):
        super().__init__(
            name="classification",
            description="Classify STEM content into appropriate categories using LLM. Analyzes content and assigns it to relevant categories from taxonomy.",
        )
        self.llm = llm

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - classify: Classify content into categories

        Parameters:
        - content: Content to classify
        - content_type: Type of content (course, lesson, kit, model)
        - taxonomy: Available taxonomy structure
        - max_categories: Maximum number of categories to assign
        """
        action = parameters.get("action", "classify")
        try:
            if action == "classify":
                return await self._classify_content(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[ClassificationTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _classify_content(self, parameters: Dict[str, Any]) -> str:
        """Classify content into categories"""
        content = parameters.get("content", {})
        content_type = parameters.get("content_type", "unknown")
        taxonomy = parameters.get("taxonomy", {})
        max_categories = parameters.get("max_categories", 5)

        prompt = f"""Classify the following {content_type} content into appropriate STEM categories.

Content:
{json.dumps(content, indent=2) if isinstance(content, dict) else str(content)}

Available Taxonomy:
{json.dumps(taxonomy, indent=2) if taxonomy else "Standard STEM categories"}

Requirements:
- Assign content to the most relevant categories
- Use hierarchical categories when appropriate
- Consider subject area, age level, and difficulty
- Maximum {max_categories} categories

Return JSON format:
{{
    "categories": [
        {{"path": "STEM/Electronics", "confidence": 0.9}},
        {{"path": "Age_Level/Middle", "confidence": 0.8}}
    ],
    "reasoning": "Brief explanation of classification"
}}"""

        try:
            response = await self.llm.generate(
                messages=[{"role": "user", "content": prompt}]
            )
            # Try to parse JSON from response
            response_text = response.content if hasattr(response, 'content') else str(response)
            
            # Extract JSON if wrapped in markdown
            if "```json" in response_text:
                json_str = response_text.split("```json")[1].split("```")[0].strip()
            elif "```" in response_text:
                json_str = response_text.split("```")[1].split("```")[0].strip()
            else:
                json_str = response_text.strip()

            try:
                classification = json.loads(json_str)
                return json.dumps(classification)
            except json.JSONDecodeError:
                # Fallback: return structured response
                return json.dumps(
                    {
                        "categories": [
                            {"path": "STEM/Technology", "confidence": 0.7}
                        ],
                        "reasoning": "LLM classification response (parsed as text)",
                        "raw_response": response_text,
                    }
                )
        except Exception as e:
            logger.error(f"Error classifying content: {e}", exc_info=True)
            return json.dumps({"error": f"Failed to classify content: {str(e)}"})

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["classify"],
                    "description": "Action to perform",
                    "default": "classify",
                },
                "content": {
                    "type": ["object", "string"],
                    "description": "Content to classify",
                },
                "content_type": {
                    "type": "string",
                    "enum": ["course", "lesson", "kit", "model"],
                    "description": "Type of content",
                },
                "taxonomy": {
                    "type": "object",
                    "description": "Available taxonomy structure",
                },
                "max_categories": {
                    "type": "integer",
                    "description": "Maximum number of categories to assign",
                    "default": 5,
                },
            },
            "required": ["action", "content"],
        }

