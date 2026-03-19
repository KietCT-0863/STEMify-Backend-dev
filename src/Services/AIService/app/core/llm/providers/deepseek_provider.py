"""
DeepSeek Provider
Remote LLM provider using DeepSeek's OpenAI-compatible API.
"""

import logging
from typing import List, Optional, Dict, Any

from openai import AsyncOpenAI

from app.core.llm.providers.base_provider import (
    BaseLLMProvider,
    LLMMessage,
    LLMResponse,
)

logger = logging.getLogger(__name__)


class DeepSeekProvider(BaseLLMProvider):
    """
    DeepSeek provider for remote LLM inference.

    DeepSeek exposes an OpenAI-compatible API surface, so we reuse the OpenAI client.
    """

    COST_PER_TOKEN = {
        "deepseek-chat": {"input": 0.27, "output": 1.10},  # USD per 1M tokens (example pricing)
    }

    def __init__(
        self,
        model: str = "deepseek-chat",
        api_key: Optional[str] = None,
        base_url: str = "https://api.deepseek.com/v1",
        temperature: float = 0.7,
        max_tokens: int = 2000,
    ):
        """
        Initialize DeepSeek provider.

        Args:
            model: Model name (default: deepseek-chat)
            api_key: DeepSeek API key
            base_url: DeepSeek API base URL
            temperature: Sampling temperature
            max_tokens: Maximum tokens to generate
        """
        super().__init__(model, temperature, max_tokens)
        self.api_key = api_key
        self.base_url = base_url
        self.client = AsyncOpenAI(api_key=api_key, base_url=base_url)

    async def generate(
        self,
        messages: List[LLMMessage],
        **kwargs,
    ) -> LLMResponse:
        """
        Generate text completion using DeepSeek.
        """
        try:
            deepseek_messages = [
                {"role": msg.get("role") if isinstance(msg, dict) else msg.role, 
                 "content": msg.get("content") if isinstance(msg, dict) else msg.content}
                for msg in messages
            ]

            temperature = kwargs.get("temperature", self.temperature)
            max_tokens = kwargs.get("max_tokens", self.max_tokens)
            
            messages_summary = []
            for msg in deepseek_messages:
                role = msg.get("role", "unknown")
                content = msg.get("content", "")
                messages_summary.append(f"{role}: {content} (length={len(content)})")
            
            other_params = {k: v for k, v in kwargs.items() if k not in ["temperature", "max_tokens"]}
            other_params_str = ", ".join([f"{k}={v}" for k, v in other_params.items()]) if other_params else "none"
            
            logger.info(
                f"[DeepSeekProvider] Sending request to DeepSeek API | "
                f"model={self.model}, messages_count={len(deepseek_messages)}, "
                f"temperature={temperature}, max_tokens={max_tokens}, other_params=[{other_params_str}] | "
                f"messages=[{'; '.join(messages_summary)}]"
            )

            response = await self.client.chat.completions.create(
                model=self.model,
                messages=deepseek_messages,
                temperature=kwargs.get("temperature", self.temperature),
                max_tokens=kwargs.get("max_tokens", self.max_tokens),
                stream=False,
            )

            choice = response.choices[0]
            usage = response.usage
            cost = self._calculate_cost(
                usage.prompt_tokens if usage else 0,
                usage.completion_tokens if usage else 0,
            )

            response_content = choice.message.content or ""
            finish_reason = choice.finish_reason
            
            # Log response data
            logger.info(
                f"[DeepSeekProvider] Received response from DeepSeek API | "
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
                    "total_tokens": usage.total_tokens if usage else 0,
                },
                finish_reason=finish_reason,
                cost=cost,
            )
        except Exception as exc:
            logger.error("DeepSeek API error: %s", exc)
            raise

    async def generate_stream(
        self,
        messages: List[LLMMessage],
        **kwargs,
    ) -> Any:
        """
        Generate streaming text completion.
        """
        try:
            deepseek_messages = [
                {"role": msg.get("role") if isinstance(msg, dict) else msg.role, 
                 "content": msg.get("content") if isinstance(msg, dict) else msg.content}
                for msg in messages
            ]

            stream = await self.client.chat.completions.create(
                model=self.model,
                messages=deepseek_messages,
                temperature=kwargs.get("temperature", self.temperature),
                max_tokens=kwargs.get("max_tokens", self.max_tokens),
                stream=True,
            )

            async for chunk in stream:
                if chunk.choices[0].delta.content:
                    yield chunk.choices[0].delta.content
        except Exception as exc:
            logger.error("DeepSeek streaming error: %s", exc)
            raise

    def _calculate_cost(self, prompt_tokens: int, completion_tokens: int) -> float:
        """
        Calculate usage cost based on DeepSeek pricing.
        """
        if self.model not in self.COST_PER_TOKEN:
            return 0.0

        costs = self.COST_PER_TOKEN[self.model]
        input_cost = (prompt_tokens / 1_000_000) * costs["input"]
        output_cost = (completion_tokens / 1_000_000) * costs["output"]
        return input_cost + output_cost

    def is_local(self) -> bool:
        """DeepSeek is a remote provider."""
        return False

    async def close(self):
        """Close DeepSeek client."""
        await self.client.close()

