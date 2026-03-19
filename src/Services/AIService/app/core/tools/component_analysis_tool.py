from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class ComponentAnalysisTool(Tool):
    """
    Component Analysis Tool - MCP-compatible
    
    Analyzes kit components to understand relationships, usage, and educational value.
    """

    def __init__(self):
        super().__init__(
            name="component_analysis",
            description="Analyze kit components to understand relationships, usage patterns, and educational value. Helps generate comprehensive kit descriptions.",
        )

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - analyze_components: Analyze component list
        - get_relationships: Get component relationships
        - get_usage_patterns: Get usage patterns

        Parameters:
        - components: List of components
        - kit_id: Kit identifier (optional)
        """
        action = parameters.get("action", "analyze_components")
        try:
            if action == "analyze_components":
                return await self._analyze_components(parameters)
            elif action == "get_relationships":
                return await self._get_relationships(parameters)
            elif action == "get_usage_patterns":
                return await self._get_usage_patterns(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[ComponentAnalysisTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _analyze_components(self, parameters: Dict[str, Any]) -> str:
        """Analyze component list"""
        components = parameters.get("components", [])

        if not components:
            return json.dumps({"error": "components is required"})

        analysis = {
            "total_components": len(components),
            "component_types": {},
            "complexity": self._calculate_complexity(components),
            "educational_value": self._assess_educational_value(components),
        }

        # Count component types
        for component in components:
            comp_type = component.get("type", "unknown")
            if comp_type not in analysis["component_types"]:
                analysis["component_types"][comp_type] = 0
            analysis["component_types"][comp_type] += 1

        return json.dumps(analysis)

    async def _get_relationships(self, parameters: Dict[str, Any]) -> str:
        """Get component relationships"""
        components = parameters.get("components", [])

        if not components:
            return json.dumps({"error": "components is required"})

        # Analyze relationships (in production, would use knowledge graph)
        relationships = []
        for i, comp1 in enumerate(components):
            for comp2 in components[i + 1 :]:
                relationship = self._find_relationship(comp1, comp2)
                if relationship:
                    relationships.append(relationship)

        return json.dumps({"relationships": relationships, "count": len(relationships)})

    async def _get_usage_patterns(self, parameters: Dict[str, Any]) -> str:
        """Get usage patterns"""
        components = parameters.get("components", [])

        if not components:
            return json.dumps({"error": "components is required"})

        # Identify common usage patterns
        patterns = []
        if any("microbit" in str(c).lower() for c in components):
            patterns.append("microbit_programming")
        if any("led" in str(c).lower() for c in components):
            patterns.append("led_circuits")
        if any("sensor" in str(c).lower() for c in components):
            patterns.append("sensor_reading")

        return json.dumps({"patterns": patterns, "count": len(patterns)})

    def _calculate_complexity(self, components: List[Dict[str, Any]]) -> str:
        """Calculate kit complexity"""
        num_components = len(components)
        if num_components < 5:
            return "simple"
        elif num_components < 15:
            return "medium"
        else:
            return "complex"

    def _assess_educational_value(self, components: List[Dict[str, Any]]) -> str:
        """Assess educational value"""
        # Simple heuristic
        has_microcontroller = any("micro" in str(c).lower() for c in components)
        has_sensors = any("sensor" in str(c).lower() for c in components)
        has_actuators = any("led" in str(c).lower() or "motor" in str(c).lower() for c in components)

        if has_microcontroller and has_sensors and has_actuators:
            return "high"
        elif has_microcontroller or (has_sensors and has_actuators):
            return "medium"
        else:
            return "basic"

    def _find_relationship(self, comp1: Dict[str, Any], comp2: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Find relationship between two components"""
        name1 = str(comp1.get("name", "")).lower()
        name2 = str(comp2.get("name", "")).lower()

        # Simple relationship detection
        if "microbit" in name1 and ("led" in name2 or "sensor" in name2):
            return {
                "component1": comp1.get("name"),
                "component2": comp2.get("name"),
                "relationship": "controls",
            }
        elif "resistor" in name1 and "led" in name2:
            return {
                "component1": comp1.get("name"),
                "component2": comp2.get("name"),
                "relationship": "protects",
            }

        return None

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["analyze_components", "get_relationships", "get_usage_patterns"],
                    "description": "Action to perform",
                },
                "components": {
                    "type": "array",
                    "description": "List of components",
                    "items": {"type": "object"},
                },
                "kit_id": {
                    "type": "string",
                    "description": "Kit identifier (optional)",
                },
            },
            "required": ["action"],
        }

