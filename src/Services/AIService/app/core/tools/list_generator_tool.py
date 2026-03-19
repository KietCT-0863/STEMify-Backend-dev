from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class ListGeneratorTool(Tool):
    """
    List Generator Tool - MCP-compatible
    
    Generates categorized lists of STEM content based on categories.
    """

    def __init__(self):
        super().__init__(
            name="list_generator",
            description="Generate categorized lists of STEM content. Organizes content into hierarchical category structures with proper formatting.",
        )

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - generate_list: Generate categorized list

        Parameters:
        - content_items: List of content items to categorize
        - categories: Category assignments for items
        - format: Output format (hierarchical, flat, tree)
        - scope: Scope of list (basic, comprehensive, advanced)
        """
        action = parameters.get("action", "generate_list")
        try:
            if action == "generate_list":
                return await self._generate_list(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[ListGeneratorTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _generate_list(self, parameters: Dict[str, Any]) -> str:
        """Generate categorized list"""
        content_items = parameters.get("content_items", [])
        categories = parameters.get("categories", {})
        format_type = parameters.get("format", "hierarchical")
        scope = parameters.get("scope", "comprehensive")

        if not content_items:
            return json.dumps({"error": "content_items is required"})

        # Organize items by category
        categorized = {}
        for item in content_items:
            item_id = item.get("id", "unknown")
            item_categories = categories.get(item_id, [])

            for cat in item_categories:
                cat_path = cat.get("path", "Uncategorized")
                if cat_path not in categorized:
                    categorized[cat_path] = []
                categorized[cat_path].append(item)

        # Format based on requested format
        if format_type == "hierarchical":
            result = self._format_hierarchical(categorized)
        elif format_type == "tree":
            result = self._format_tree(categorized)
        else:
            result = self._format_flat(categorized)

        return json.dumps(
            {
                "list": result,
                "format": format_type,
                "scope": scope,
                "total_items": len(content_items),
                "categories_used": len(categorized),
            }
        )

    def _format_hierarchical(self, categorized: Dict[str, List[Dict[str, Any]]]) -> Dict[str, Any]:
        """Format as hierarchical structure"""
        hierarchy = {}
        for cat_path, items in categorized.items():
            parts = cat_path.split("/")
            current = hierarchy

            for part in parts:
                if part not in current:
                    current[part] = {}
                current = current[part]

            current["items"] = items
            current["count"] = len(items)

        return hierarchy

    def _format_tree(self, categorized: Dict[str, List[Dict[str, Any]]]) -> List[Dict[str, Any]]:
        """Format as tree structure"""
        tree = []
        for cat_path, items in categorized.items():
            parts = cat_path.split("/")
            tree.append(
                {
                    "path": cat_path,
                    "name": parts[-1],
                    "level": len(parts),
                    "items": items,
                    "count": len(items),
                }
            )
        return sorted(tree, key=lambda x: (x["level"], x["path"]))

    def _format_flat(self, categorized: Dict[str, List[Dict[str, Any]]]) -> List[Dict[str, Any]]:
        """Format as flat list"""
        flat = []
        for cat_path, items in categorized.items():
            for item in items:
                flat.append({"category": cat_path, "item": item})
        return flat

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["generate_list"],
                    "description": "Action to perform",
                    "default": "generate_list",
                },
                "content_items": {
                    "type": "array",
                    "description": "List of content items to categorize",
                    "items": {"type": "object"},
                },
                "categories": {
                    "type": "object",
                    "description": "Category assignments for items (item_id -> list of categories)",
                },
                "format": {
                    "type": "string",
                    "enum": ["hierarchical", "flat", "tree"],
                    "description": "Output format",
                    "default": "hierarchical",
                },
                "scope": {
                    "type": "string",
                    "enum": ["basic", "comprehensive", "advanced"],
                    "description": "Scope of list",
                    "default": "comprehensive",
                },
            },
            "required": ["action", "content_items"],
        }

