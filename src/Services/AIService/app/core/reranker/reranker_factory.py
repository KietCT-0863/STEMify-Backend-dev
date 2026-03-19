"""
Reranker Factory
Factory for creating reranker instances
"""

from typing import Optional
import logging

from app.core.reranker.base_reranker import BaseReranker
from app.core.reranker.cohere_reranker import CohereReranker
from app.core.reranker.cross_encoder_reranker import CrossEncoderReranker
# LLMReranker imported lazily (may not be available)
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class RerankerFactory:
    """Factory for creating reranker instances"""
    
    @staticmethod
    def create_reranker(
        provider: str = None,
        **kwargs
    ) -> Optional[BaseReranker]:
        """
        Create reranker instance based on provider
        
        Args:
            provider: Reranker provider ("cohere", "local", "llm", "none")
            **kwargs: Additional arguments for specific reranker
        
        Returns:
            Reranker instance or None if provider is "none"
        """
        provider = provider or settings.RERANKER_PROVIDER
        
        if provider == "none":
            logger.info("Reranker disabled (provider=none)")
            return None
        
        elif provider == "cohere":
            logger.info("Creating Cohere reranker")
            try:
                return CohereReranker(
                    api_key=kwargs.get("api_key"),
                    model=kwargs.get("model")
                )
            except Exception as e:
                logger.error(f"Error creating Cohere reranker: {e}")
                logger.warning("Falling back to cross-encoder reranker")
                return RerankerFactory.create_reranker("local", **kwargs)
        
        elif provider == "local":
            logger.info("Creating cross-encoder reranker")
            try:
                return CrossEncoderReranker(
                    model_name=kwargs.get("model_name"),
                    device=kwargs.get("device")
                )
            except Exception as e:
                logger.error(f"Error creating cross-encoder reranker: {e}")
                return None
        
        elif provider == "llm":
            logger.info("Creating LLM reranker")
            try:
                # Try to import LLMReranker (may fail if LLM not implemented)
                from app.core.reranker.llm_reranker import LLMReranker
                llm_client = kwargs.get("llm_client")
                return LLMReranker(llm_client=llm_client)
            except ImportError as e:
                logger.error(f"LLM reranker not available: {e}")
                logger.warning("Falling back to cross-encoder reranker")
                return RerankerFactory.create_reranker("local", **kwargs)
            except Exception as e:
                logger.error(f"Error creating LLM reranker: {e}")
                logger.warning("Falling back to cross-encoder reranker")
                return RerankerFactory.create_reranker("local", **kwargs)
        
        else:
            logger.warning(f"Unknown reranker provider: {provider}, disabling reranker")
            return None


def create_reranker(provider: str = None, **kwargs) -> Optional[BaseReranker]:
    """Convenience function to create reranker"""
    return RerankerFactory.create_reranker(provider, **kwargs)

