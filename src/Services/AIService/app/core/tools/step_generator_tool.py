from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class StepGeneratorTool(Tool):
    """
    Step Generator Tool 
    
    Generates step-by-step instructions for 3D models (assembly, usage, disassembly).
    """

    def __init__(
        self,
        llm: LLMClient,
    ):
        super().__init__(
            name="step_generator",
            description="Generate clear, sequential step-by-step instructions for assembling, using, or disassembling 3D models. Ensures logical flow and safety considerations.",
        )
        self.llm = llm

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - generate_steps: Generate step-by-step instructions

        Parameters:
        - model_id: Model identifier
        - action_type: Type of action (assembly, usage, disassembly)
        - model_data: Model structure data
        - num_steps: Desired number of steps (optional)
        """
        action = parameters.get("action", "generate_steps")
        try:
            if action == "generate_steps":
                return await self._generate_steps(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[StepGeneratorTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _generate_steps(self, parameters: Dict[str, Any]) -> str:
        """Generate step-by-step instructions"""
        model_id = parameters.get("model_id", "")
        action_type = parameters.get("action_type", "assembly")
        model_data = parameters.get("model_data", {})
        num_steps = parameters.get("num_steps", None)

        prompt = f"""Generate clear, sequential step-by-step instructions for {action_type} of a 3D model.

Model ID: {model_id}
Action Type: {action_type}
Model Data:
{json.dumps(model_data, indent=2) if model_data else "No specific model data provided"}

Requirements:
- Logical sequence
- Clear, concise instructions
- Safety considerations
- Visual references where applicable
- Number each step clearly
{f"- Generate approximately {num_steps} steps" if num_steps else ""}

Format each step as:
Step X: [Clear instruction]
- Details or sub-steps if needed
- Safety note if applicable

Generate the step-by-step instructions:"""

        try:
            response = await self.llm.generate(
                messages=[{"role": "user", "content": prompt}]
            )
            return json.dumps(
                {
                    "steps": response,
                    "model_id": model_id,
                    "action_type": action_type,
                    "num_steps": num_steps,
                }
            )
        except Exception as e:
            logger.error(f"Error generating steps: {e}", exc_info=True)
            return json.dumps({"error": f"Failed to generate steps: {str(e)}"})

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["generate_steps"],
                    "description": "Action to perform",
                    "default": "generate_steps",
                },
                "model_id": {
                    "type": "string",
                    "description": "Model identifier",
                },
                "action_type": {
                    "type": "string",
                    "enum": ["assembly", "usage", "disassembly"],
                    "description": "Type of action",
                    "default": "assembly",
                },
                "model_data": {
                    "type": "object",
                    "description": "Model structure data",
                },
                "num_steps": {
                    "type": "integer",
                    "description": "Desired number of steps",
                },
            },
            "required": ["action"],
        }

