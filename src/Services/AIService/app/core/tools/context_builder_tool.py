"""
Context Builder Tool
MCP-compatible tool wrapping JITContextBuilder (GSSC + JIT).
"""

from typing import Dict, Any
import json
import logging

from app.core.tools.base import Tool
from app.core.context.builder import JITContextBuilder

logger = logging.getLogger(__name__)


class ContextBuilderTool(Tool):
    """Context Builder Tool - MCP-compatible"""

    def __init__(self, builder: JITContextBuilder):
        super().__init__(
            name="context_builder",
            description="Builds structured context (GSSC/JIT) within token budget",
        )
        self.builder = builder

    async def run(self, parameters: Dict[str, Any]) -> str:
        query = parameters.get("query", "")
        user_id = parameters.get("user_id")
        session_id = parameters.get("session_id")
        top_k = parameters.get("top_k", 10)
        try:
            bundle = await self.builder.build(
                query=query,
                user_id=user_id,
                top_k=top_k,
                session_id=session_id,
            )
            return json.dumps(
                {
                    "items": [
                        {
                            "content": item.content,
                            "score": item.score,
                            "source": item.source,
                            "metadata": item.metadata,
                        }
                        for item in bundle.items
                    ],
                    "total_tokens": bundle.total_tokens,
                    "token_budget": bundle.token_budget,
                    "notes": bundle.notes,
                }
            )
        except Exception as e:
            logger.error(f"[ContextBuilderTool] Error: {e}")
            return json.dumps({"error": str(e)})

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "query": {"type": "string", "description": "User question/query"},
                "user_id": {"type": "string", "description": "Optional user ID"},
                "session_id": {"type": "string", "description": "Optional session ID for context reuse"},
                "top_k": {"type": "integer", "description": "Candidates to gather", "default": 10},
            },
            "required": ["query"],
        }

