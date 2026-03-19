"""
Orchestrates Gather → Select → Structure → Compress to produce context bundles.
"""

from typing import Optional, Dict, Any, Tuple
import logging
import hashlib
from time import time

from app.core.context.gather import ContextGatherer
from app.core.context.selector import ContextSelector
from app.core.context.structurer import ContextStructurer
from app.core.context.compressor import ContextCompressor
from app.core.context.models import ContextBundle
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class JITContextBuilder:
    """
    Just-in-time context builder with token budget awareness.
    """

    def __init__(
        self,
        gatherer: ContextGatherer,
        selector: ContextSelector,
        structurer: ContextStructurer,
        compressor: ContextCompressor,
        token_budget: Optional[int] = None,
    ):
        self.gatherer = gatherer
        self.selector = selector
        self.structurer = structurer
        self.compressor = compressor
        self.token_budget = token_budget or settings.CONTEXT_MAX_TOKENS

        # in-memory cache for session-scoped context reuse
        # key: (session_id, query_hash) -> (expires_at, ContextBundle)
        self._session_cache: Dict[Tuple[str, str], Tuple[float, ContextBundle]] = {}

    async def build(
        self,
        query: str,
        user_id: Optional[str] = None,
        top_k: int = 10,
        session_id: Optional[str] = None,
    ) -> ContextBundle:
        cache_key: Optional[Tuple[str, str]] = None
        if session_id:
            query_hash = hashlib.md5(query.encode("utf-8")).hexdigest()
            cache_key = (session_id, query_hash)
            cached = self._session_cache.get(cache_key)
            if cached:
                expires_at, bundle = cached
                if time() < expires_at:
                    logger.debug("[JITContextBuilder] Session cache hit")
                    return bundle

        candidates = await self.gatherer.gather(query=query, user_id=user_id, top_k=top_k)
        logger.info(f"[JITContextBuilder] Gathered {len(candidates)} candidates")
        
        selected = self.selector.select(candidates)
        logger.info(f"[JITContextBuilder] Selected {len(selected)} items after selection")
        
        structured = self.structurer.structure(selected)
        logger.info(
            f"[JITContextBuilder] Structured: memory={len(structured.get('memory', []))}, "
            f"retrieval={len(structured.get('retrieval', []))}, other={len(structured.get('other', []))}"
        )
        
        compressed = self.compressor.compress(structured)
        logger.info(
            f"[JITContextBuilder] Compressed: memory={len(compressed.get('memory', []))}, "
            f"retrieval={len(compressed.get('retrieval', []))}, other={len(compressed.get('other', []))}"
        )

        total_tokens = self._estimate_tokens(compressed)
        logger.info(f"[JITContextBuilder] Estimated {total_tokens} tokens")

        notes = None
        if total_tokens > self.token_budget:
            notes = f"Context truncated to meet budget ({total_tokens} > {self.token_budget})."

        bundle = ContextBundle(
            items=[item for section in compressed.values() for item in section],
            total_tokens=total_tokens,
            token_budget=self.token_budget,
            notes=notes,
        )

        if cache_key:
            ttl = getattr(settings, "CONTEXT_REUSE_TTL_SECONDS", 300)
            self._session_cache[cache_key] = (time() + ttl, bundle)
            logger.debug("[JITContextBuilder] Stored bundle in session cache")

        return bundle

    def _estimate_tokens(self, compressed: Dict[str, Any]) -> int:

        chars = 0
        for items in compressed.values():
            for item in items:
                chars += len(item.content)
        return chars // 4

