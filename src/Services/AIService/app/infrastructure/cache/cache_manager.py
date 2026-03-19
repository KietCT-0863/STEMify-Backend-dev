"""
Cache Manager
Caching for AI responses
Integrated with Multi-Level Cache System
"""

from typing import Any, Optional
import logging

from app.core.cache.multi_level_cache import MultiLevelCache
from app.core.cache.embedding_cache import EmbeddingCache
from app.core.cache.graph_cache import GraphCache
from app.core.cache.agent_cache import AgentResponseCache

logger = logging.getLogger(__name__)


class CacheManager:
    """
    Cache Manager - Wrapper for Multi-Level Cache System
    
    Provides backward-compatible interface while using new multi-level cache.
    """
    
    def __init__(
        self,
        redis_client=None,
        vector_store=None,
        graph_client=None,
        embedding_service=None
    ):
        """
        Initialize cache manager
        
        Args:
            redis_client: Redis client instance (optional)
            vector_store: Vector store client (for embedding cache)
            graph_client: Graph client (for graph cache)
            embedding_service: Embedding service (for agent cache)
        """
        # Initialize multi-level cache
        self.multi_cache = MultiLevelCache(redis_client=redis_client)
        
        # Initialize specialized caches
        self.embedding_cache = EmbeddingCache(
            multi_cache=self.multi_cache,
            vector_store=vector_store
        )
        
        self.graph_cache = GraphCache(
            multi_cache=self.multi_cache,
            graph_client=graph_client
        )
        
        self.agent_cache = AgentResponseCache(
            multi_cache=self.multi_cache,
            embedding_service=embedding_service
        )
        
        logger.info("CacheManager initialized with Multi-Level Cache System")
    
    async def get(self, key: str) -> Optional[Any]:
        """
        Get value from cache (backward compatible)
        
        Args:
            key: Cache key
        
        Returns:
            Cached value or None
        """
        return await self.multi_cache.get(key)
    
    async def set(self, key: str, value: Any, ttl: int = 3600) -> bool:
        """
        Set value in cache (backward compatible)
        
        Args:
            key: Cache key
            value: Value to cache
            ttl: Time to live in seconds
        
        Returns:
            True if successful
        """
        return await self.multi_cache.set(key, value, ttl=ttl)
    
    async def delete(self, key: str) -> bool:
        """
        Delete value from cache (backward compatible)
        
        Args:
            key: Cache key
        
        Returns:
            True if successful
        """
        return await self.multi_cache.delete(key)
    
    async def clear(self) -> bool:
        """
        Clear all cache (backward compatible)
        
        Returns:
            True if successful
        """
        return await self.multi_cache.clear()

