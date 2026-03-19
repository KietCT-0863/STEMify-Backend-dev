from typing import Dict, Any, List, Optional
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class StructureValidatorTool(Tool):
    """
    Structure Validator Tool - MCP-compatible
    
    Validates curriculum structure to ensure it follows educational best practices.
    """

    def __init__(self):
        super().__init__(
            name="structure_validator",
            description="Validate curriculum structure to ensure it follows educational best practices. Checks for required components, logical flow, and completeness.",
        )

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - validate_curriculum: Validate entire curriculum structure
        - validate_module: Validate a single module
        - validate_lesson: Validate a single lesson

        Parameters:
        - structure: The structure to validate (curriculum, module, or lesson)
        - structure_type: Type of structure (curriculum, module, lesson)
        """
        action = parameters.get("action", "validate_curriculum")
        try:
            if action == "validate_curriculum":
                return await self._validate_curriculum(parameters)
            elif action == "validate_module":
                return await self._validate_module(parameters)
            elif action == "validate_lesson":
                return await self._validate_lesson(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[StructureValidatorTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _validate_curriculum(self, parameters: Dict[str, Any]) -> str:
        """Validate entire curriculum structure"""
        structure = parameters.get("structure", {})

        required_fields = ["title", "subject", "level", "modules", "duration_weeks"]
        missing_fields = [field for field in required_fields if field not in structure]

        issues = []
        if missing_fields:
            issues.append(f"Missing required fields: {', '.join(missing_fields)}")

        # Validate modules
        modules = structure.get("modules", [])
        if not modules:
            issues.append("Curriculum must have at least one module")
        else:
            for i, module in enumerate(modules):
                if not isinstance(module, dict):
                    issues.append(f"Module {i+1} must be an object")
                elif "title" not in module:
                    issues.append(f"Module {i+1} missing title")
                elif "lessons" not in module:
                    issues.append(f"Module {i+1} missing lessons")

        # Validate duration
        duration = structure.get("duration_weeks")
        if duration and (not isinstance(duration, int) or duration <= 0):
            issues.append("Duration must be a positive integer")

        is_valid = len(issues) == 0
        return json.dumps(
            {
                "valid": is_valid,
                "issues": issues,
                "structure_type": "curriculum",
            }
        )

    async def _validate_module(self, parameters: Dict[str, Any]) -> str:
        """Validate a single module"""
        structure = parameters.get("structure", {})

        required_fields = ["title", "lessons", "objectives"]
        missing_fields = [field for field in required_fields if field not in structure]

        issues = []
        if missing_fields:
            issues.append(f"Missing required fields: {', '.join(missing_fields)}")

        # Validate lessons
        lessons = structure.get("lessons", [])
        if not lessons:
            issues.append("Module must have at least one lesson")
        else:
            for i, lesson in enumerate(lessons):
                if not isinstance(lesson, dict):
                    issues.append(f"Lesson {i+1} must be an object")
                elif "title" not in lesson:
                    issues.append(f"Lesson {i+1} missing title")

        # Validate objectives
        objectives = structure.get("objectives", [])
        if not objectives:
            issues.append("Module must have learning objectives")

        is_valid = len(issues) == 0
        return json.dumps(
            {
                "valid": is_valid,
                "issues": issues,
                "structure_type": "module",
            }
        )

    async def _validate_lesson(self, parameters: Dict[str, Any]) -> str:
        """Validate a single lesson"""
        structure = parameters.get("structure", {})

        required_fields = ["title", "objectives", "content"]
        missing_fields = [field for field in required_fields if field not in structure]

        issues = []
        if missing_fields:
            issues.append(f"Missing required fields: {', '.join(missing_fields)}")

        # Validate objectives
        objectives = structure.get("objectives", [])
        if not objectives:
            issues.append("Lesson must have learning objectives")
        elif not isinstance(objectives, list):
            issues.append("Objectives must be a list")

        # Validate content
        content = structure.get("content")
        if not content:
            issues.append("Lesson must have content")
        elif isinstance(content, str) and len(content.strip()) < 50:
            issues.append("Lesson content is too short (minimum 50 characters)")

        # Check for assessment
        if "assessment" not in structure:
            issues.append("Warning: Lesson should have assessment questions")

        is_valid = len(issues) == 0
        return json.dumps(
            {
                "valid": is_valid,
                "issues": issues,
                "structure_type": "lesson",
            }
        )

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["validate_curriculum", "validate_module", "validate_lesson"],
                    "description": "Action to perform",
                },
                "structure": {
                    "type": "object",
                    "description": "The structure to validate (curriculum, module, or lesson object)",
                },
            },
            "required": ["action", "structure"],
        }

