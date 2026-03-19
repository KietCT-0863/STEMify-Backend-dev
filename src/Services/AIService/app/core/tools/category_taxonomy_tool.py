from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class CategoryTaxonomyTool(Tool):
    """
    Category Taxonomy Tool - MCP-compatible
    
    Provides access to STEM category taxonomy for organized categorization.
    """

    def __init__(self, taxonomy: Optional[Dict[str, Any]] = None):
        super().__init__(
            name="category_taxonomy",
            description="Access STEM category taxonomy to get hierarchical category structures. Provides organized categorization framework for STEM content.",
        )
        self.taxonomy = taxonomy or self._get_default_taxonomy()

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - get_taxonomy: Get full taxonomy structure
        - get_category: Get specific category and subcategories
        - search_categories: Search categories by keyword

        Parameters:
        - category_path: Category path (e.g., "STEM/Electronics")
        - keyword: Keyword for searching
        - level: Taxonomy level (top, mid, leaf)
        """
        action = parameters.get("action", "get_taxonomy")
        try:
            if action == "get_taxonomy":
                return await self._get_taxonomy(parameters)
            elif action == "get_category":
                return await self._get_category(parameters)
            elif action == "search_categories":
                return await self._search_categories(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[CategoryTaxonomyTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _get_taxonomy(self, parameters: Dict[str, Any]) -> str:
        """Get full taxonomy structure"""
        level = parameters.get("level", "all")

        if level == "top":
            # Return only top-level categories
            top_level = {k: {"name": v.get("name", k)} for k, v in self.taxonomy.items()}
            return json.dumps({"taxonomy": top_level, "level": "top"})
        else:
            return json.dumps({"taxonomy": self.taxonomy, "level": "all"})

    async def _get_category(self, parameters: Dict[str, Any]) -> str:
        """Get specific category and subcategories"""
        category_path = parameters.get("category_path", "")

        if not category_path:
            return json.dumps({"error": "category_path is required"})

        # Navigate taxonomy by path
        parts = category_path.split("/")
        current = self.taxonomy

        for part in parts:
            if part in current:
                current = current[part]
            else:
                return json.dumps({"error": f"Category path not found: {category_path}"})

        return json.dumps({"category": current, "path": category_path})

    async def _search_categories(self, parameters: Dict[str, Any]) -> str:
        """Search categories by keyword"""
        keyword = parameters.get("keyword", "").lower()

        if not keyword:
            return json.dumps({"error": "keyword is required"})

        results = []
        self._search_recursive(self.taxonomy, keyword, "", results)

        return json.dumps({"results": results, "count": len(results)})

    def _search_recursive(
        self, taxonomy: Dict[str, Any], keyword: str, path: str, results: List[Dict[str, Any]]
    ):
        """Recursively search taxonomy"""
        for key, value in taxonomy.items():
            current_path = f"{path}/{key}" if path else key
            name = value.get("name", key).lower()

            if keyword in name or keyword in key.lower():
                results.append({"path": current_path, "name": value.get("name", key), "data": value})

            # Search subcategories
            if "subcategories" in value:
                self._search_recursive(value["subcategories"], keyword, current_path, results)

    def _get_default_taxonomy(self) -> Dict[str, Any]:
        """Get default STEM category taxonomy"""
        return {
            "STEM": {
                "name": "STEM",
                "subcategories": {
                    "Science": {
                        "name": "Science",
                        "subcategories": {
                            "Physics": {"name": "Physics"},
                            "Chemistry": {"name": "Chemistry"},
                            "Biology": {"name": "Biology"},
                        },
                    },
                    "Technology": {
                        "name": "Technology",
                        "subcategories": {
                            "Programming": {"name": "Programming"},
                            "Electronics": {"name": "Electronics"},
                            "Robotics": {"name": "Robotics"},
                        },
                    },
                    "Engineering": {
                        "name": "Engineering",
                        "subcategories": {
                            "Mechanical": {"name": "Mechanical Engineering"},
                            "Electrical": {"name": "Electrical Engineering"},
                            "Software": {"name": "Software Engineering"},
                        },
                    },
                    "Mathematics": {
                        "name": "Mathematics",
                        "subcategories": {
                            "Algebra": {"name": "Algebra"},
                            "Geometry": {"name": "Geometry"},
                            "Statistics": {"name": "Statistics"},
                        },
                    },
                },
            },
            "Age_Level": {
                "name": "Age Level",
                "subcategories": {
                    "Elementary": {"name": "Elementary (6-10)"},
                    "Middle": {"name": "Middle School (11-14)"},
                    "High": {"name": "High School (15-18)"},
                },
            },
            "Difficulty": {
                "name": "Difficulty",
                "subcategories": {
                    "Beginner": {"name": "Beginner"},
                    "Intermediate": {"name": "Intermediate"},
                    "Advanced": {"name": "Advanced"},
                },
            },
        }

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["get_taxonomy", "get_category", "search_categories"],
                    "description": "Action to perform",
                },
                "category_path": {
                    "type": "string",
                    "description": "Category path (e.g., 'STEM/Electronics')",
                },
                "keyword": {
                    "type": "string",
                    "description": "Keyword for searching categories",
                },
                "level": {
                    "type": "string",
                    "enum": ["top", "mid", "leaf", "all"],
                    "description": "Taxonomy level",
                    "default": "all",
                },
            },
            "required": ["action"],
        }

