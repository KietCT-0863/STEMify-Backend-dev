from typing import Dict, Any, Optional
import logging
import json

from app.core.tools.base import Tool
from app.core.llm.client import LLMClient
from app.core.cache.agent_cache import AgentResponseCache
from app.core.llm.providers.base_provider import LLMMessage

logger = logging.getLogger(__name__)


class ExplanationTool(Tool):
    """
    Explanation Tool - MCP-compatible
    
    Generate step-by-step explanations for concepts.
    Uses LLM (local-first via Minions Protocol) for generation.
    Structures explanations with examples and analogies.
    Caches explanations in AgentResponseCache.
    """
    
    def __init__(
        self,
        llm_client: LLMClient,
        agent_cache: Optional[AgentResponseCache] = None
    ):
        super().__init__(
            name="explanation",
            description="Generate step-by-step explanations for concepts with examples and analogies"
        )
        self.llm_client = llm_client
        self.agent_cache = agent_cache
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Execute tool
        
        Generates explanation for a concept or question.
        """
        concept = parameters.get("concept", "")
        level = parameters.get("level", "intermediate")  # beginner, intermediate, advanced
        include_examples = parameters.get("include_examples", True)
        include_analogies = parameters.get("include_analogies", True)
        
        if not concept:
            return json.dumps({"error": "Concept is required"})
        
        try:
            # Check cache first
            cache_key = f"explanation:{concept}:{level}"
            if self.agent_cache:
                cached = await self.agent_cache.get_similar_response(
                    query=concept,
                    agent_type="explanation"
                )
                if cached:
                    logger.debug(f"[ExplanationTool] Cache hit for: {concept}")
                    return cached.get("response", {}).get("content", "")
            
            # Build prompt
            prompt_parts = [f"Explain the concept: {concept}"]
            
            if level == "beginner":
                prompt_parts.append("Use simple language suitable for beginners.")
            elif level == "advanced":
                prompt_parts.append("Use technical language suitable for advanced learners.")
            
            if include_examples:
                prompt_parts.append("Include practical examples.")
            
            if include_analogies:
                prompt_parts.append("Use analogies to make it easier to understand.")
            
            prompt_parts.append("Structure the explanation in clear steps.")
            
            system_prompt = """You are an expert educator. Your task is to explain concepts clearly and effectively.
Break down complex ideas into simple, understandable steps.
Use examples and analogies when helpful.
Make sure the explanation is accurate and educational."""
            
            messages: List[LLMMessage] = [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": " ".join(prompt_parts)}
            ]
            
            response = await self.llm_client.generate(messages, use_remote=False)
            
            explanation = response.content if hasattr(response, 'content') else str(response)
            
            if self.agent_cache:
                await self.agent_cache.cache_response(
                    query=concept,
                    response={"content": explanation, "level": level},
                    agent_type="explanation",
                    ttl=3600  # 1 hour
                )
            
            return explanation
        except Exception as e:
            logger.error(f"[ExplanationTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "concept": {
                    "type": "string",
                    "description": "Concept or question to explain"
                },
                "level": {
                    "type": "string",
                    "enum": ["beginner", "intermediate", "advanced"],
                    "description": "Difficulty level of explanation",
                    "default": "intermediate"
                },
                "include_examples": {
                    "type": "boolean",
                    "description": "Include practical examples",
                    "default": True
                },
                "include_analogies": {
                    "type": "boolean",
                    "description": "Include analogies",
                    "default": True
                }
            },
            "required": ["concept"]
        }

