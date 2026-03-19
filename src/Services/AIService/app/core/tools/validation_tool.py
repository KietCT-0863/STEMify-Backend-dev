from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class ValidationTool(Tool):
    """
    Validation Tool 
    
    Validates step sequence for logical flow, completeness, and safety.
    """

    def __init__(self):
        super().__init__(
            name="validation",
            description="Validate step-by-step instruction sequence for logical flow, completeness, safety considerations, and correctness. Ensures instructions are clear and safe.",
        )

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - validate_sequence: Validate step sequence

        Parameters:
        - steps: List of steps to validate
        - action_type: Type of action (assembly, usage, disassembly)
        """
        action = parameters.get("action", "validate_sequence")
        try:
            if action == "validate_sequence":
                return await self._validate_sequence(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[ValidationTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _validate_sequence(self, parameters: Dict[str, Any]) -> str:
        """Validate step sequence"""
        steps = parameters.get("steps", [])
        action_type = parameters.get("action_type", "assembly")

        if not steps:
            return json.dumps({"error": "steps is required"})

        issues = []
        warnings = []

        # Check for empty steps
        for i, step in enumerate(steps):
            if not step or (isinstance(step, str) and len(step.strip()) == 0):
                issues.append(f"Step {i+1} is empty")
            elif isinstance(step, dict) and not step.get("description"):
                issues.append(f"Step {i+1} missing description")

        # Check for logical flow (basic checks)
        if len(steps) < 2:
            warnings.append("Sequence has fewer than 2 steps - may be too simple")

        # Check for safety considerations
        steps_text = " ".join([str(s) for s in steps]).lower()
        safety_keywords = ["safety", "warning", "caution", "danger", "careful"]
        has_safety = any(keyword in steps_text for keyword in safety_keywords)
        if not has_safety and action_type in ["assembly", "disassembly"]:
            warnings.append("No safety considerations found in steps")

        # Check for completeness
        if action_type == "assembly":
            if "connect" not in steps_text and "attach" not in steps_text:
                warnings.append("Assembly steps may be missing connection instructions")
        elif action_type == "disassembly":
            if "remove" not in steps_text and "detach" not in steps_text:
                warnings.append("Disassembly steps may be missing removal instructions")

        is_valid = len(issues) == 0

        return json.dumps(
            {
                "valid": is_valid,
                "issues": issues,
                "warnings": warnings,
                "action_type": action_type,
                "num_steps": len(steps),
            }
        )

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["validate_sequence"],
                    "description": "Action to perform",
                    "default": "validate_sequence",
                },
                "steps": {
                    "type": "array",
                    "description": "List of steps to validate",
                    "items": {"type": ["string", "object"]},
                },
                "action_type": {
                    "type": "string",
                    "enum": ["assembly", "usage", "disassembly"],
                    "description": "Type of action",
                    "default": "assembly",
                },
            },
            "required": ["action", "steps"],
        }

