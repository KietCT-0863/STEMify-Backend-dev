"""
OpenAI Provider
Remote LLM provider using OpenAI API (GPT-4o)
"""

import logging
from typing import List, Optional, Dict, Any

from openai import AsyncOpenAI
from app.core.llm.providers.base_provider import BaseLLMProvider, LLMMessage, LLMResponse

logger = logging.getLogger(__name__)


class OpenAIProvider(BaseLLMProvider):
    """
    OpenAI provider for remote LLM inference
    
    Uses GPT-4o for high-quality responses
    """
    
    # Cost per 1M tokens (as of 2024)
    COST_PER_TOKEN = {
        "gpt-4o": {"input": 2.50, "output": 10.00},  # per 1M tokens
        "gpt-4o-mini": {"input": 0.15, "output": 0.60},
        "gpt-4-turbo": {"input": 10.00, "output": 30.00},
    }
    
    def __init__(
        self,
        model: str = "gpt-4o",
        api_key: Optional[str] = None,
        base_url: str = "https://api.openai.com/v1",
        temperature: float = 0.7,
        max_tokens: int = 2000
    ):
        """
        Initialize OpenAI provider
        
        Args:
            model: Model name (default: gpt-4o)
            api_key: OpenAI API key
            base_url: OpenAI API base URL
            temperature: Sampling temperature
            max_tokens: Maximum tokens to generate
        """
        super().__init__(model, temperature, max_tokens)
        self.api_key = api_key
        self.base_url = base_url
        self.client = AsyncOpenAI(
            api_key=api_key,
            base_url=base_url
        )
    
    async def generate(
        self,
        messages: List[LLMMessage],
        **kwargs
    ) -> LLMResponse:
        """
        Generate text completion using OpenAI
        
        Args:
            messages: List of messages
            **kwargs: Additional parameters
            
        Returns:
            LLMResponse with generated content
        """
        try:
            # Convert messages to OpenAI format
            openai_messages = [
                {"role": msg.get("role") if isinstance(msg, dict) else msg.role, 
                 "content": msg.get("content") if isinstance(msg, dict) else msg.content}
                for msg in messages
            ]
            
            # Log request data before API call
            temperature = kwargs.get("temperature", self.temperature)
            max_tokens = kwargs.get("max_tokens", self.max_tokens)
            
            # Prepare messages summary for logging (full content, no truncate)
            messages_summary = []
            for msg in openai_messages:
                role = msg.get("role", "unknown")
                content = msg.get("content", "")
                # Log full content without truncation
                messages_summary.append(f"{role}: {content} (length={len(content)})")
            
            logger.info(
                f"[OpenAIProvider] Sending request to OpenAI API | "
                f"model={self.model}, base_url={self.base_url}, messages_count={len(openai_messages)}, "
                f"temperature={temperature}, max_tokens={max_tokens} | "
                f"messages=[{'; '.join(messages_summary)}]"
            )
            
            # Make API call
            response = await self.client.chat.completions.create(
                model=self.model,
                messages=openai_messages,
                temperature=kwargs.get("temperature", self.temperature),
                max_tokens=kwargs.get("max_tokens", self.max_tokens),
                stream=False
            )
            
            # Extract response
            choice = response.choices[0]
            usage = response.usage
            
            # Calculate cost
            cost = self._calculate_cost(
                usage.prompt_tokens if usage else 0,
                usage.completion_tokens if usage else 0
            )
            
            response_content = choice.message.content or ""
            finish_reason = choice.finish_reason
            
            # Log response data
            logger.info(
                f"[OpenAIProvider] Received response from OpenAI API | "
                f"model={self.model}, finish_reason={finish_reason}, "
                f"prompt_tokens={usage.prompt_tokens if usage else 0}, "
                f"completion_tokens={usage.completion_tokens if usage else 0}, "
                f"total_tokens={usage.total_tokens if usage else 0}, "
                f"response_length={len(response_content)}, cost={cost} | "
                f"full_response={response_content}"
            )
            
            return LLMResponse(
                content=response_content,
                model=self.model,
                usage={
                    "prompt_tokens": usage.prompt_tokens if usage else 0,
                    "completion_tokens": usage.completion_tokens if usage else 0,
                    "total_tokens": usage.total_tokens if usage else 0
                },
                finish_reason=finish_reason,
                cost=cost
            )
            
        except Exception as e:
            error_msg = str(e)
            logger.error(
                f"OpenAI API error: {error_msg}",
                extra={
                    "base_url": self.base_url,
                    "model": self.model,
                    "api_key_prefix": self.api_key[:10] + "..." if self.api_key and len(self.api_key) > 10 else "N/A"
                }
            )
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
            openai_messages = [
                {"role": msg.get("role") if isinstance(msg, dict) else msg.role, 
                 "content": msg.get("content") if isinstance(msg, dict) else msg.content}
                for msg in messages
            ]
            
            stream = await self.client.chat.completions.create(
                model=self.model,
                messages=openai_messages,
                temperature=kwargs.get("temperature", self.temperature),
                max_tokens=kwargs.get("max_tokens", self.max_tokens),
                stream=True
            )
            
            async for chunk in stream:
                if chunk.choices[0].delta.content:
                    yield chunk.choices[0].delta.content
                    
        except Exception as e:
            logger.error(f"OpenAI streaming error: {e}")
            raise
    
    def _calculate_cost(self, prompt_tokens: int, completion_tokens: int) -> float:
        """
        Calculate cost based on token usage
        
        Args:
            prompt_tokens: Number of input tokens
            completion_tokens: Number of output tokens
            
        Returns:
            Cost in USD
        """
        if self.model not in self.COST_PER_TOKEN:
            return 0.0
        
        costs = self.COST_PER_TOKEN[self.model]
        input_cost = (prompt_tokens / 1_000_000) * costs["input"]
        output_cost = (completion_tokens / 1_000_000) * costs["output"]
        
        return input_cost + output_cost
    
    def is_local(self) -> bool:
        """OpenAI is a remote provider"""
        return False
    
    async def close(self):
        """Close OpenAI client"""
        await self.client.close()
