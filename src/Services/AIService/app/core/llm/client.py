"""
LLM Client
Unified interface for LLM providers (Local and Remote)
"""

import logging
from typing import Optional, List

from app.core.llm.providers.base_provider import BaseLLMProvider, LLMMessage, LLMResponse
from app.core.llm.providers.deepseek_provider import DeepSeekProvider
from app.core.llm.providers.ollama_provider import OllamaProvider
from app.core.llm.providers.openai_provider import OpenAIProvider
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class LLMClient:
    """
    Unified LLM client that manages both local and remote providers
    
    Automatically selects provider based on use case:
    - Local (Ollama) for simple tasks
    - Remote (OpenAI) for complex tasks
    """
    
    def __init__(
        self,
        local_provider: Optional[BaseLLMProvider] = None,
        remote_provider: Optional[BaseLLMProvider] = None
    ):
        if local_provider is None:
            self.local_provider = OllamaProvider(
                model=settings.OLLAMA_MODEL if hasattr(settings, 'OLLAMA_MODEL') else "llama3.1:8b",
                base_url=getattr(settings, 'OLLAMA_BASE_URL', 'http://localhost:11434'),
                temperature=settings.LLM_TEMPERATURE,
                max_tokens=settings.LLM_MAX_TOKENS
            )
        else:
            self.local_provider = local_provider
        
        if remote_provider is None:
            self.remote_provider = self._build_remote_provider()
        else:
            self.remote_provider = remote_provider
    
    async def generate(
        self,
        messages: List[LLMMessage],
        use_remote: bool = False,
        **kwargs
    ) -> LLMResponse:
        provider = self.remote_provider if use_remote else self.local_provider
        
        if provider is None:
            if use_remote:
                raise ValueError("Remote provider not available")
            else:
                raise ValueError("Local provider not available")
        
        # Log request data before calling LLM
        provider_name = provider.__class__.__name__
        provider_type = "remote" if use_remote else "local"
        model = getattr(provider, "model", "unknown")
        
        # Prepare messages summary for logging (full content, no truncate)
        messages_summary = []
        for msg in messages:
            if isinstance(msg, dict):
                role = msg.get("role", "unknown")
                content = msg.get("content", "")
            else:
                role = getattr(msg, "role", "unknown")
                content = getattr(msg, "content", "")
            
            # Log full content without truncation
            messages_summary.append(f"{role}: {content} (length={len(content)})")
        
        kwargs_str = ", ".join([f"{k}={v}" for k, v in kwargs.items()]) if kwargs else "none"
        
        logger.info(
            f"[LLMClient] Calling LLM ({provider_type}: {provider_name}) | "
            f"model={model}, messages_count={len(messages)}, kwargs=[{kwargs_str}] | "
            f"messages=[{'; '.join(messages_summary)}]"
        )
        
        return await provider.generate(messages, **kwargs)
    
    async def generate_local(
        self,
        messages: List[LLMMessage],
        **kwargs
    ) -> LLMResponse:
        """Generate using local provider"""
        return await self.generate(messages, use_remote=False, **kwargs)
    
    async def generate_remote(
        self,
        messages: List[LLMMessage],
        **kwargs
    ) -> LLMResponse:
        """Generate using remote provider"""
        return await self.generate(messages, use_remote=True, **kwargs)
    
    def get_local_provider(self) -> Optional[BaseLLMProvider]:
        """Get local provider instance"""
        return self.local_provider
    
    def get_remote_provider(self) -> Optional[BaseLLMProvider]:
        """Get remote provider instance"""
        return self.remote_provider
    
    async def close(self):
        """Close all providers"""
        if self.local_provider:
            await self.local_provider.close()
        if self.remote_provider:
            await self.remote_provider.close()

    def _build_remote_provider(self) -> Optional[BaseLLMProvider]:
        """
        Select and instantiate the configured remote provider.
        """
        preferred = getattr(settings, "LLM_REMOTE_PROVIDER", "deepseek").lower()
        fallback_chain = self._build_remote_preference_chain(preferred)

        for candidate in fallback_chain:
            provider = self._create_remote_provider(candidate)
            if provider:
                logger.info("Initialized %s remote provider", candidate)
                return provider

        logger.warning("No remote LLM provider configured or API keys missing")
        return None

    def _build_remote_preference_chain(self, preferred: str) -> List[str]:
        """
        Build an ordered list of provider names to try based on preference.
        """
        supported = ["deepseek", "openai"]
        chain: List[str] = []

        if preferred in supported:
            chain.append(preferred)
        else:
            chain.append("deepseek")

        for provider_name in supported:
            if provider_name not in chain:
                chain.append(provider_name)

        return chain

    def _create_remote_provider(self, provider_name: str) -> Optional[BaseLLMProvider]:
        """
        Instantiate a remote provider based on name if credentials exist.
        """
        if provider_name == "deepseek":
            api_key = getattr(settings, "DEEPSEEK_API_KEY", None)
            if not api_key:
                logger.debug("DeepSeek API key not provided; skipping DeepSeek provider")
                return None

            return DeepSeekProvider(
                model=settings.DEEPSEEK_MODEL,
                api_key=api_key,
                base_url=settings.DEEPSEEK_BASE_URL,
                temperature=settings.LLM_TEMPERATURE,
                max_tokens=settings.LLM_MAX_TOKENS,
            )

        if provider_name == "openai":
            api_key = getattr(settings, "OPENAI_API_KEY", None)
            if not api_key:
                logger.debug("OpenAI API key not provided; skipping OpenAI provider")
                return None

            return OpenAIProvider(
                model=settings.LLM_MODEL,
                api_key=api_key,
                base_url=settings.OPENAI_BASE_URL,
                temperature=settings.LLM_TEMPERATURE,
                max_tokens=settings.LLM_MAX_TOKENS,
            )

        logger.debug("Provider %s is not supported", provider_name)
        return None
