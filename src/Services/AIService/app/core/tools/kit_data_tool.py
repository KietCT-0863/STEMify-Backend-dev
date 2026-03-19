from typing import Dict, Any, Optional
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class KitDataTool(Tool):
    """
    Kit Data Tool - MCP-compatible
    
    Retrieves kit specifications and data for description generation.
    """

    def __init__(
        self,
        kit_repository: Optional[Any] = None,  # Can be extended with actual repository
    ):
        super().__init__(
            name="kit_data",
            description="Query kit specifications, components, and metadata for generating descriptions. Provides comprehensive kit information.",
        )
        self.kit_repository = kit_repository

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - get_kit: Get kit specifications by ID
        - get_components: Get kit components list
        - search_kits: Search kits by criteria

        Parameters:
        - kit_id: Kit identifier
        - keyword: Keyword for searching
        """
        action = parameters.get("action", "get_kit")
        try:
            if action == "get_kit":
                return await self._get_kit(parameters)
            elif action == "get_components":
                return await self._get_components(parameters)
            elif action == "search_kits":
                return await self._search_kits(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[KitDataTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _get_kit(self, parameters: Dict[str, Any]) -> str:
        """Get kit specifications"""
        kit_id = parameters.get("kit_id")

        if not kit_id:
            return json.dumps({"error": "kit_id is required"})

        # In production, would fetch from repository
        # For now, return mock data structure
        kit_data = {
            "kit_id": kit_id,
            "name": f"STEM Kit {kit_id}",
            "description": "Educational STEM kit with various components",
            "components": [
                {"name": "Micro:bit", "quantity": 1, "description": "Microcontroller board"},
                {"name": "LEDs", "quantity": 5, "description": "Light emitting diodes"},
                {"name": "Resistors", "quantity": 10, "description": "10k ohm resistors"},
            ],
            "age_range": "8-14",
            "difficulty": "beginner",
            "subjects": ["electronics", "programming"],
        }

        return json.dumps(kit_data)

    async def _get_components(self, parameters: Dict[str, Any]) -> str:
        """Get kit components list"""
        kit_id = parameters.get("kit_id")

        if not kit_id:
            return json.dumps({"error": "kit_id is required"})

        # In production, would fetch from repository
        components = [
            {"name": "Micro:bit", "quantity": 1},
            {"name": "LEDs", "quantity": 5},
            {"name": "Resistors", "quantity": 10},
        ]

        return json.dumps({"kit_id": kit_id, "components": components, "count": len(components)})

    async def _search_kits(self, parameters: Dict[str, Any]) -> str:
        """Search kits by criteria"""
        keyword = parameters.get("keyword", "").lower()

        if not keyword:
            return json.dumps({"error": "keyword is required"})

        # In production, would search repository
        # Mock results
        results = [
            {
                "kit_id": "kit_001",
                "name": "Micro:bit Starter Kit",
                "description": "Complete starter kit for micro:bit programming",
            },
            {
                "kit_id": "kit_002",
                "name": "Electronics Basics Kit",
                "description": "Learn electronics fundamentals",
            },
        ]

        # Filter by keyword
        filtered = [
            r for r in results
            if keyword in r["name"].lower() or keyword in r["description"].lower()
        ]

        return json.dumps({"results": filtered, "count": len(filtered)})

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["get_kit", "get_components", "search_kits"],
                    "description": "Action to perform",
                },
                "kit_id": {
                    "type": "string",
                    "description": "Kit identifier",
                },
                "keyword": {
                    "type": "string",
                    "description": "Keyword for searching kits",
                },
            },
            "required": ["action"],
        }

