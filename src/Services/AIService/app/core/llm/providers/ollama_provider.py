"""
Ollama Provider
Local LLM provider using Ollama (Llama 3.1 8B)
"""

import httpx
import logging
from typing import List, Optional, Dict, Any

from app.core.llm.providers.base_provider import BaseLLMProvider, LLMMessage, LLMResponse

logger = logging.getLogger(__name__)


class OllamaProvider(BaseLLMProvider):
    """
    Ollama provider for local LLM inference
    
    Requires Ollama to be running locally with model installed:
    - ollama pull llama3.1:8b
    """
    
    def __init__(
        self,
        model: str = "llama3.1:8b",
        base_url: str = "http://localhost:11434",
        temperature: float = 0.7,
        max_tokens: int = 2000
    ):
        """
        Initialize Ollama provider
        
        Args:
            model: Model name (default: llama3.1:8b)
            base_url: Ollama API base URL
            temperature: Sampling temperature
            max_tokens: Maximum tokens to generate
        """
        super().__init__(model, temperature, max_tokens)
        self.base_url = base_url.rstrip('/')
        self.client = httpx.AsyncClient(timeout=60.0)
    
    async def generate(
        self,
        messages: List[LLMMessage],
        **kwargs
    ) -> LLMResponse:
        """
        Generate text completion using Ollama
        
        Args:
            messages: List of messages
            **kwargs: Additional parameters
            
        Returns:
            LLMResponse with generated content
        """
        try:
            # Convert messages to Ollama format
            prompt = self._messages_to_prompt(messages)
            
            # Prepare request
            request_data = {
                "model": self.model,
                "prompt": prompt,
                "stream": False,
                "options": {
                    "temperature": kwargs.get("temperature", self.temperature),
                    "num_predict": kwargs.get("max_tokens", self.max_tokens),
                }
            }
            
            # Log request data before API call (full prompt, no truncate)
            temperature = request_data["options"]["temperature"]
            max_tokens = request_data["options"]["num_predict"]
            
            logger.info(
                f"[OllamaProvider] Sending request to Ollama API | "
                f"model={self.model}, base_url={self.base_url}, "
                f"prompt_length={len(prompt)}, temperature={temperature}, max_tokens={max_tokens}, "
                f"messages_count={len(messages)} | full_prompt={prompt}"
            )
            
            # Make request
            response = await self.client.post(
                f"{self.base_url}/api/generate",
                json=request_data
            )
            response.raise_for_status()
            
            result = response.json()
            
            response_content = result.get("response", "")
            finish_reason = result.get("done", True) and "stop" or "length"
            
            # Log response data
            logger.info(
                f"[OllamaProvider] Received response from Ollama API | "
                f"model={self.model}, finish_reason={finish_reason}, "
                f"prompt_tokens={result.get('prompt_eval_count', 0)}, "
                f"completion_tokens={result.get('eval_count', 0)}, "
                f"response_length={len(response_content)} | "
                f"full_response={response_content}"
            )
            
            return LLMResponse(
                content=response_content,
                model=self.model,
                usage={
                    "prompt_tokens": result.get("prompt_eval_count", 0),
                    "completion_tokens": result.get("eval_count", 0),
                    "total_tokens": result.get("prompt_eval_count", 0) + result.get("eval_count", 0)
                },
                finish_reason=finish_reason,
                cost=0.0  # Local LLM has no cost
            )
            
        except httpx.HTTPError as e:
            logger.error(f"Ollama API error: {e}")
            raise
        except Exception as e:
            logger.error(f"Unexpected error in Ollama provider: {e}")
            raise
    
    async def generate_stream(
        self,
        messages: List[LLMMessage],
        **kwargs
    ) -> Any:
        """
        Generate streaming text completion
        
        Args:
            messages: List of messages
            **kwargs: Additional parameters
            
        Yields:
            Response chunks
        """
        try:
            prompt = self._messages_to_prompt(messages)
            
            request_data = {
                "model": self.model,
                "prompt": prompt,
                "stream": True,
                "options": {
                    "temperature": kwargs.get("temperature", self.temperature),
                    "num_predict": kwargs.get("max_tokens", self.max_tokens),
                }
            }
            
            async with self.client.stream(
                "POST",
                f"{self.base_url}/api/generate",
                json=request_data
            ) as response:
                response.raise_for_status()
                async for line in response.aiter_lines():
                    if line:
                        import json
                        chunk = json.loads(line)
                        if chunk.get("response"):
                            yield chunk.get("response", "")
                            
        except Exception as e:
            logger.error(f"Ollama streaming error: {e}")
            raise
    
    def _messages_to_prompt(self, messages: List[LLMMessage]) -> str:
       
        prompt_parts: list[str] = []

        for msg in messages:

            if isinstance(msg, dict):
                role = msg.get("role", "user")
                content = msg.get("content", "")
            else:
                role = getattr(msg, "role", "user")
                content = getattr(msg, "content", "")

            if not content:
                continue

            if role == "system":
                prompt_parts.append(f"System: {content}")
            elif role == "assistant":
                prompt_parts.append(f"Assistant: {content}")
            else:
                prompt_parts.append(f"User: {content}")
        
        # Add final assistant prompt
        prompt_parts.append("Assistant:")
        
        return "\n\n".join(prompt_parts)
    
    def is_local(self) -> bool:
        """Ollama is a local provider"""
        return True
    
    async def close(self):
        """Close HTTP client"""
        await self.client.aclose()














