"""
Microbit Explain Error Service
Service for explaining microbit errors
"""

import logging

from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import BaseLLMProvider, LLMMessage
from app.features.microbit_explain_error.models import (
    MicrobitExplainErrorRequest,
    MicrobitExplainErrorResponse,
)
from app.features.microbit_explain_error.prompts import (
    build_microbit_explain_error_prompt,
)
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)

class MicrobitExplainErrorService:
    def __init__(self, llm_client: LLMClient):
        self.llm_client = llm_client

    async def explain_microbit_error(self, request: MicrobitExplainErrorRequest) -> MicrobitExplainErrorResponse:
        """
        Explain a microbit error
        """
        try:
            remote_provider = self.llm_client.get_remote_provider()
            provider_name = self._resolve_provider_name(remote_provider)

            response = await self.llm_client.generate_remote(
                messages=[
                    LLMMessage(
                        role="system",
                        content=settings.MICROBIT_EXPLAIN_ERROR_SYSTEM_PROMPT,
                    ),
                    LLMMessage(
                        role="user",
                        content=build_microbit_explain_error_prompt(
                            error_message=request.error_message,
                            language=request.language
                        )
                    )
                ]
            )
            return MicrobitExplainErrorResponse(
                explanation=response.content.strip(),
                provider=provider_name,
                model=response.model,
            )
        except Exception as e:
            logger.error(f"Error explaining microbit error: {e}")
            raise e

    def _resolve_provider_name(self, provider: BaseLLMProvider | None) -> str:
        """
        Resolve a user-friendly provider name for observability.
        """
        if provider is None:
            return "unknown"
        return getattr(provider, "provider_name", provider.__class__.__name__)