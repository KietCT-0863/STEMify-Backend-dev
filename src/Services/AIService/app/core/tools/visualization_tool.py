from typing import Dict, Any, Optional
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class VisualizationTool(Tool):
    """
    Visualization Tool 
    
    Generates visual aids and references for step-by-step instructions.
    """

    def __init__(self):
        super().__init__(
            name="visualization",
            description="Generate visual aids and references for step-by-step instructions. Provides guidance on what visual elements to include.",
        )

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - generate_visual_aids: Generate visual aid suggestions

        Parameters:
        - step_number: Step number
        - step_description: Description of the step
        - model_type: Type of model
        """
        action = parameters.get("action", "generate_visual_aids")
        try:
            if action == "generate_visual_aids":
                return await self._generate_visual_aids(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[VisualizationTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _generate_visual_aids(self, parameters: Dict[str, Any]) -> str:
        """Generate visual aid suggestions"""
        step_number = parameters.get("step_number")
        step_description = parameters.get("step_description", "")
        model_type = parameters.get("model_type", "unknown")

        # Generate visual aid suggestions
        visual_aids = {
            "step_number": step_number,
            "suggestions": [
                f"Diagram showing {step_description}",
                f"Component placement diagram",
                f"Connection diagram for {model_type}",
            ],
            "visual_types": ["diagram", "photo", "illustration"],
            "notes": "Include visual references to help users understand each step",
        }

        return json.dumps(visual_aids)

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["generate_visual_aids"],
                    "description": "Action to perform",
                    "default": "generate_visual_aids",
                },
                "step_number": {
                    "type": "integer",
                    "description": "Step number",
                },
                "step_description": {
                    "type": "string",
                    "description": "Description of the step",
                },
                "model_type": {
                    "type": "string",
                    "description": "Type of model",
                },
            },
            "required": ["action"],
        }

