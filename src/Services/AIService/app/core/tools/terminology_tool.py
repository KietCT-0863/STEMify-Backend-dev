from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class TerminologyTool(Tool):
    """
    Terminology Tool 
    
    Provides STEM terminology database access for accurate technical descriptions.
    """

    def __init__(self, terminology_db: Optional[Dict[str, Any]] = None):
        super().__init__(
            name="terminology",
            description="Access STEM terminology database to get accurate technical terms, definitions, and usage examples. Essential for generating precise educational descriptions.",
        )
        self.terminology_db = terminology_db or self._get_default_terminology()

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - get_term: Get definition and usage for a specific term
        - search_terms: Search for terms by keyword
        - get_category: Get all terms in a category

        Parameters:
        - term: Specific term to look up
        - keyword: Keyword for searching
        - category: Category name (e.g., "electronics", "programming")
        """
        action = parameters.get("action", "get_term")
        try:
            if action == "get_term":
                return await self._get_term(parameters)
            elif action == "search_terms":
                return await self._search_terms(parameters)
            elif action == "get_category":
                return await self._get_category(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[TerminologyTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _get_term(self, parameters: Dict[str, Any]) -> str:
        """Get definition and usage for a specific term"""
        term = parameters.get("term", "").lower()

        if not term:
            return json.dumps({"error": "term is required"})

        # Search in terminology database
        for category, terms in self.terminology_db.items():
            if term in terms:
                term_info = terms[term]
                return json.dumps(
                    {
                        "term": term,
                        "category": category,
                        "definition": term_info.get("definition", ""),
                        "usage": term_info.get("usage", ""),
                        "examples": term_info.get("examples", []),
                    }
                )

        return json.dumps({"error": f"Term '{term}' not found in terminology database"})

    async def _search_terms(self, parameters: Dict[str, Any]) -> str:
        """Search for terms by keyword"""
        keyword = parameters.get("keyword", "").lower()

        if not keyword:
            return json.dumps({"error": "keyword is required"})

        results = []
        for category, terms in self.terminology_db.items():
            for term, info in terms.items():
                if keyword in term.lower() or keyword in info.get("definition", "").lower():
                    results.append(
                        {
                            "term": term,
                            "category": category,
                            "definition": info.get("definition", ""),
                        }
                    )

        return json.dumps({"results": results, "count": len(results)})

    async def _get_category(self, parameters: Dict[str, Any]) -> str:
        """Get all terms in a category"""
        category = parameters.get("category", "").lower()

        if not category:
            return json.dumps({"error": "category is required"})

        if category in self.terminology_db:
            terms = self.terminology_db[category]
            return json.dumps({"category": category, "terms": list(terms.keys()), "count": len(terms)})

        return json.dumps({"error": f"Category '{category}' not found"})

    def _get_default_terminology(self) -> Dict[str, Any]:
        """Get default STEM terminology database"""
        return {
            "electronics": {
                "resistor": {
                    "definition": "A passive electrical component that resists the flow of electric current",
                    "usage": "Used to limit current or divide voltage in circuits",
                    "examples": ["10k ohm resistor", "current limiting resistor"],
                },
                "led": {
                    "definition": "Light Emitting Diode - a semiconductor device that emits light when current flows through it",
                    "usage": "Commonly used for indicators and displays",
                    "examples": ["red LED", "LED display"],
                },
            },
            "programming": {
                "variable": {
                    "definition": "A named storage location that holds a value",
                    "usage": "Used to store and manipulate data in programs",
                    "examples": ["int x = 5", "string name = 'John'"],
                },
                "function": {
                    "definition": "A reusable block of code that performs a specific task",
                    "usage": "Used to organize code and avoid repetition",
                    "examples": ["def calculate_sum(a, b)", "function greet()"],
                },
            },
            "microbit": {
                "pin": {
                    "definition": "A physical connection point on the micro:bit board",
                    "usage": "Used to connect external components like LEDs, buttons, sensors",
                    "examples": ["pin 0", "digital pin", "analog pin"],
                },
                "accelerometer": {
                    "definition": "A sensor that measures acceleration and tilt",
                    "usage": "Detects movement and orientation of the micro:bit",
                    "examples": ["accelerometer.get_x()", "tilt detection"],
                },
            },
        }

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["get_term", "search_terms", "get_category"],
                    "description": "Action to perform",
                },
                "term": {
                    "type": "string",
                    "description": "Specific term to look up",
                },
                "keyword": {
                    "type": "string",
                    "description": "Keyword for searching terms",
                },
                "category": {
                    "type": "string",
                    "description": "Category name (e.g., electronics, programming, microbit)",
                },
            },
            "required": ["action"],
        }

