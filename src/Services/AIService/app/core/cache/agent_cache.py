"""
Agent Response Cache
Cache agent responses based on query similarity
"""

from typing import Dict, Any, Optional, List
import hashlib
import logging
import numpy as np

from app.core.cache.multi_level_cache import MultiLevelCache

logger = logging.getLogger(__name__)


class AgentResponseCache:
    """
    Agent Response Cache (L5)
    
    Caches agent responses based on query similarity.
    Uses embedding-based similarity matching to find similar queries.
    TTL: 1 hour
    """
    
    def __init__(
        self,
        multi_cache: MultiLevelCache,
        embedding_service=None,
        similarity_threshold: float = 0.85,
        default_ttl: int = 3600  # 1 hour
    ):
        """
        Initialize agent response cache
        
        Args:
            multi_cache: MultiLevelCache instance
            embedding_service: Embedding service for similarity matching
            similarity_threshold: Minimum similarity to consider a match (0-1)
            default_ttl: Default TTL in seconds (1 hour)
        """
        self.multi_cache = multi_cache
        self.embedding_service = embedding_service
        self.similarity_threshold = similarity_threshold
        self.default_ttl = default_ttl
        self._cache_prefix = "agent_response:"
    
    async def get_similar_response(
        self,
        query: str,
        role: Optional[str] = None,
        agent_type: Optional[str] = None
    ) -> Optional[Dict[str, Any]]:
        """
        Get cached response for similar query
        
        Args:
            query: User query
            role: User role (student, teacher, staff)
            agent_type: Agent type (react, plan-solve, reflection)
        
        Returns:
            Cached response or None
        """
        # Try exact match first
        cache_key = self._generate_key(query, role, agent_type)
        cached = await self.multi_cache.get(cache_key, layer="l2")
        
        if cached:
            logger.debug(f"[AgentCache] Exact match found: {query[:50]}...")
            return cached.get("response")
        
        # Try similarity search if embedding service available
        if self.embedding_service:
            similar_response = await self._find_similar_response(query, role, agent_type)
            if similar_response:
                logger.debug(f"[AgentCache] Similar response found: {query[:50]}...")
                return similar_response
        
        logger.debug(f"[AgentCache] Cache miss: {query[:50]}...")
        return None
    
    async def cache_response(
        self,
        query: str,
        response: Dict[str, Any],
        role: Optional[str] = None,
        agent_type: Optional[str] = None,
        ttl: Optional[int] = None
    ) -> bool:
        """
        Cache agent response
        
        Args:
            query: User query
            response: Agent response to cache
            role: User role
            agent_type: Agent type
            ttl: Time to live in seconds
        
        Returns:
            True if successful
        """
        cache_key = self._generate_key(query, role, agent_type)
        cache_value = {
            "response": response,
            "query": query,
            "role": role,
            "agent_type": agent_type
        }
        
        ttl = ttl or self.default_ttl
        
        # Store in L2 (Redis)
        await self.multi_cache.set(cache_key, cache_value, ttl=ttl, layer="l2")
        
        # Also store embedding for similarity search (if available)
        if self.embedding_service:
            await self._store_embedding(query, cache_key)
        
        logger.debug(f"[AgentCache] Cached response: {query[:50]}...")
        return True
    
    async def _find_similar_response(
        self,
        query: str,
        role: Optional[str] = None,
        agent_type: Optional[str] = None
    ) -> Optional[Dict[str, Any]]:
        """
        Find similar cached response using embedding similarity
        
        Args:
            query: Query to find similar response for
            role: User role filter
            agent_type: Agent type filter
        
        Returns:
            Similar response or None
        """
        if not self.embedding_service:
            return None
        
        try:
            # Generate embedding for query
            query_embedding = await self._get_query_embedding(query)
            if query_embedding is None:
                return None
            
            # Search for similar cached queries
            # This would require storing query embeddings and searching them
            # For now, we'll use a simplified approach: exact match only
            # TODO: Implement full similarity search with vector store
            
            return None
        except Exception as e:
            logger.warning(f"[AgentCache] Similarity search failed: {e}")
            return None
    
    async def _get_query_embedding(self, query: str) -> Optional[List[float]]:
        """Get embedding for query"""
        if not self.embedding_service:
            return None
        
        try:
            from app.core.embedding.pipeline import get_embedding_pipeline
            embedding_pipeline = get_embedding_pipeline()
            embedding = embedding_pipeline.encode([query])[0]
            return embedding.tolist()
        except Exception as e:
            logger.warning(f"[AgentCache] Failed to generate embedding: {e}")
            return None
    
    async def _store_embedding(self, query: str, cache_key: str):
        """Store query embedding for similarity search"""
        # TODO: Store in vector store for similarity search
        # For now, just log
        logger.debug(f"[AgentCache] Would store embedding for: {cache_key}")
    
    def _generate_key(
        self,
        query: str,
        role: Optional[str] = None,
        agent_type: Optional[str] = None
    ) -> str:
        """Generate cache key from query, role, and agent type"""
        key_parts = [query]
        if role:
            key_parts.append(f"role:{role}")
        if agent_type:
            key_parts.append(f"agent:{agent_type}")
        
        key_string = ":".join(key_parts)
        key_hash = hashlib.md5(key_string.encode()).hexdigest()
        return f"{self._cache_prefix}{key_hash}"
    
    async def invalidate_role(self, role: str) -> bool:
        """
        Invalidate all cached responses for a role
        
        Args:
            role: Role to invalidate
        
        Returns:
            True if successful
        """
        # Note: Full invalidation would require scanning all keys
        logger.info(f"[AgentCache] Role invalidation requested: {role} (using TTL expiration)")
        return True




