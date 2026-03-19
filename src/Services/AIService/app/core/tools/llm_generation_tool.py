"""
LLM Generation Tool
MCP-compatible tool wrapping LLMClient
"""

from typing import Dict, Any, List
import logging

from app.core.tools.base import Tool
from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import LLMMessage

logger = logging.getLogger(__name__)


class LLMGenerationTool(Tool):
    """LLM Generation Tool - MCP-compatible"""
    
    def __init__(self, llm_client: LLMClient):
        super().__init__(
            name="llm_generate",
            description="Generate text using language model"
        )
        self.llm_client = llm_client
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """Execute LLM generation"""
        prompt = parameters.get("prompt", "")
        context = parameters.get("context", "")
        use_remote = parameters.get("use_remote", False)
        
        try:
            messages: List[LLMMessage] = []
            if context:
                messages.append({"role": "system", "content": context})
            messages.append({"role": "user", "content": prompt})
            
            response = await self.llm_client.generate(messages, use_remote=use_remote)
            return response.content if hasattr(response, 'content') else str(response)
        except Exception as e:
            logger.error(f"[LLMGenerationTool] Error: {e}")
            return f"Error: {str(e)}"
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "prompt": {
                    "type": "string",
                    "description": "Prompt for text generation"
                },
                "context": {
                    "type": "string",
                    "description": "Optional context/system message"
                },
                "use_remote": {
                    "type": "boolean",
                    "description": "Use remote LLM (default: false, uses local)",
                    "default": False
                }
            },
            "required": ["prompt"]
        }




