from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class CurriculumTemplateTool(Tool):
    """
    Curriculum Template Tool - MCP-compatible
    
    Retrieves curriculum templates for course generation.
    Templates can be stored in memory or retrieved from external sources.
    """

    def __init__(
        self,
        templates: Optional[List[Dict[str, Any]]] = None,
    ):
        super().__init__(
            name="curriculum_template",
            description="Retrieve curriculum templates for course generation. Provides structured templates for different subjects and levels.",
        )
        # Default templates if none provided
        self.templates = templates or self._get_default_templates()

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - get_template: Get a specific template by subject and level
        - list_templates: List all available templates
        - search_templates: Search templates by criteria

        Parameters:
        - subject: Subject name (e.g., "Math", "Science")
        - level: Education level (e.g., "Elementary", "Middle", "High")
        - template_id: Specific template ID
        """
        action = parameters.get("action", "get_template")
        try:
            if action == "get_template":
                return await self._get_template(parameters)
            elif action == "list_templates":
                return await self._list_templates(parameters)
            elif action == "search_templates":
                return await self._search_templates(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[CurriculumTemplateTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _get_template(self, parameters: Dict[str, Any]) -> str:
        subject = parameters.get("subject")
        level = parameters.get("level")
        template_id = parameters.get("template_id")

        if template_id:
            template = next((t for t in self.templates if t.get("id") == template_id), None)
            if template:
                return json.dumps(template)
            return json.dumps({"error": f"Template {template_id} not found"})

        if subject and level:
            template = next(
                (
                    t
                    for t in self.templates
                    if t.get("subject", "").lower() == subject.lower()
                    and t.get("level", "").lower() == level.lower()
                ),
                None,
            )
            if template:
                return json.dumps(template)
            return json.dumps({"error": f"Template for {subject} {level} not found"})

        return json.dumps({"error": "Either template_id or (subject and level) required"})

    async def _list_templates(self, parameters: Dict[str, Any]) -> str:
        return json.dumps({"templates": self.templates, "count": len(self.templates)})

    async def _search_templates(self, parameters: Dict[str, Any]) -> str:
        subject = parameters.get("subject", "").lower()
        level = parameters.get("level", "").lower()
        keywords = parameters.get("keywords", "").lower()

        results = []
        for template in self.templates:
            match = True
            if subject and subject not in template.get("subject", "").lower():
                match = False
            if level and level not in template.get("level", "").lower():
                match = False
            if keywords:
                description = template.get("description", "").lower()
                if keywords not in description:
                    match = False

            if match:
                results.append(template)

        return json.dumps({"templates": results, "count": len(results)})

    def _get_default_templates(self) -> List[Dict[str, Any]]:
        return [
            {
                "id": "stem_elementary",
                "subject": "STEM",
                "level": "Elementary",
                "description": "Elementary STEM curriculum template with hands-on activities",
                "structure": {
                    "modules": ["Introduction", "Hands-on Activities", "Assessment"],
                    "duration_weeks": 8,
                    "lessons_per_week": 2,
                },
            },
            {
                "id": "math_middle",
                "subject": "Math",
                "level": "Middle",
                "description": "Middle school math curriculum template",
                "structure": {
                    "modules": ["Concepts", "Practice", "Projects", "Assessment"],
                    "duration_weeks": 12,
                    "lessons_per_week": 3,
                },
            },
            {
                "id": "science_high",
                "subject": "Science",
                "level": "High",
                "description": "High school science curriculum template",
                "structure": {
                    "modules": ["Theory", "Experiments", "Analysis", "Assessment"],
                    "duration_weeks": 16,
                    "lessons_per_week": 4,
                },
            },
        ]

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["get_template", "list_templates", "search_templates"],
                    "description": "Action to perform",
                },
                "subject": {
                    "type": "string",
                    "description": "Subject name (e.g., Math, Science, STEM)",
                },
                "level": {
                    "type": "string",
                    "description": "Education level (e.g., Elementary, Middle, High)",
                },
                "template_id": {
                    "type": "string",
                    "description": "Specific template ID",
                },
                "keywords": {
                    "type": "string",
                    "description": "Keywords for searching templates",
                },
            },
            "required": ["action"],
        }

