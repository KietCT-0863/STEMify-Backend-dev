"""
Minions Tool
MCP-compatible tool wrapping MinionsCoordinator
"""

from typing import Dict, Any
import json
import logging

from app.core.tools.base import Tool
from app.core.reasoning.minions.coordinator import MinionsCoordinator

logger = logging.getLogger(__name__)


class MinionsTool(Tool):
    def __init__(self, coordinator: MinionsCoordinator):
        super().__init__(
            name="minions_protocol",
            description="Run HazyResearch Minions Protocol (Decompose → Execute → Aggregate)",
        )
        self.coordinator = coordinator

    async def run(self, parameters: Dict[str, Any]) -> str:
        question = parameters.get("question", "")
        try:
            result = await self.coordinator.run(question)
            return json.dumps(result)
        except Exception as e:
            logger.error(f"[MinionsTool] Error: {e}")
            return json.dumps({"error": str(e)})

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "question": {"type": "string", "description": "Question to answer"},
            },
            "required": ["question"],
        }

