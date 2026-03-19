from typing import Dict, Any, Optional
import logging
import json

from app.core.tools.base import Tool
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class DescriptionGeneratorTool(Tool):
    """
    Description Generator Tool 
    
    Generates educational descriptions using LLM based on image analysis and terminology.
    """

    def __init__(
        self,
        llm: LLMClient,
    ):
        super().__init__(
            name="description_generator",
            description="Generate clear, accurate, educational descriptions for 3D models, kits, or components using LLM. Incorporates terminology and analysis results.",
        )
        self.llm = llm

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - generate: Generate description based on analysis and context

        Parameters:
        - analysis: Image or model analysis results
        - terminology: Relevant terminology information
        - context: Additional context (optional)
        - description_type: Type of description (technical, educational, step-by-step)
        """
        action = parameters.get("action", "generate")
        try:
            if action == "generate":
                return await self._generate_description(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[DescriptionGeneratorTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _generate_description(self, parameters: Dict[str, Any]) -> str:
        """Generate description based on analysis and context"""
        analysis = parameters.get("analysis", {})
        terminology = parameters.get("terminology", {})
        context = parameters.get("context", "")
        description_type = parameters.get("description_type", "educational")

        # Build prompt
        prompt = f"""Generate a {description_type} description for a 3D model or component.

Analysis results:
{json.dumps(analysis, indent=2)}

Terminology:
{json.dumps(terminology, indent=2) if terminology else "Use standard STEM terminology"}

Additional context:
{context if context else "None"}

Requirements:
- Clear and accurate technical descriptions
- Educational context and explanations
- Proper use of STEM terminology
- Step-by-step if applicable
- Suitable for educational materials

Generate the description:"""

        try:
            response = await self.llm.generate(
                messages=[{"role": "user", "content": prompt}]
            )
            return json.dumps(
                {
                    "description": response,
                    "description_type": description_type,
                    "analysis_used": bool(analysis),
                    "terminology_used": bool(terminology),
                }
            )
        except Exception as e:
            logger.error(f"Error generating description: {e}", exc_info=True)
            return json.dumps({"error": f"Failed to generate description: {str(e)}"})

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["generate"],
                    "description": "Action to perform",
                    "default": "generate",
                },
                "analysis": {
                    "type": "object",
                    "description": "Image or model analysis results",
                },
                "terminology": {
                    "type": "object",
                    "description": "Relevant terminology information",
                },
                "context": {
                    "type": "string",
                    "description": "Additional context for description",
                },
                "description_type": {
                    "type": "string",
                    "enum": ["technical", "educational", "step-by-step"],
                    "description": "Type of description to generate",
                    "default": "educational",
                },
            },
            "required": ["action"],
        }

