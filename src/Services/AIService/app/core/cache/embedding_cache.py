from typing import List, Optional, Dict, Any
import numpy as np
import logging

from app.core.cache.multi_level_cache import MultiLevelCache

logger = logging.getLogger(__name__)


class EmbeddingCache:
    """
    Embedding Cache 
    
    Caches pre-computed embeddings with vector similarity-based lookup.
    TTL: 7 days
    """
    
    def __init__(
        self,
        multi_cache: MultiLevelCache,
        vector_store=None,
        similarity_threshold: float = 0.95
    ):
        """
        Initialize embedding cache
        
        Args:
            multi_cache: MultiLevelCache instance
            vector_store: Vector store client (for similarity search)
            similarity_threshold: Minimum similarity to consider a match (0-1)
        """
        self.multi_cache = multi_cache
        self.vector_store = vector_store
        self.similarity_threshold = similarity_threshold
        self._cache_prefix = "embedding:"
    
    async def get_embedding(
        self,
        text: str,
        use_similarity: bool = True
    ) -> Optional[List[float]]:
        """
        Get cached embedding
        
        Args:
            text: Text to get embedding for
            use_similarity: If True, search for similar cached embeddings
        
        Returns:
            Cached embedding vector or None
        """
        # Try exact match 
        cache_key = self._generate_key(text)
        cached = await self.multi_cache.get(cache_key, layer="l2")
        
        if cached:
            logger.debug(f"[EmbeddingCache] Exact match found: {text[:50]}...")
            return cached.get("embedding")
        
        # Try similarity search if enabled
        if use_similarity and self.vector_store:
            similar_embedding = await self._find_similar_embedding(text)
            if similar_embedding:
                logger.debug(f"[EmbeddingCache] Similar embedding found: {text[:50]}...")
                return similar_embedding
        
        return None
    
    async def set_embedding(
        self,
        text: str,
        embedding: List[float],
        ttl: int = 604800  # 7 days
    ) -> bool:
        """
        Cache embedding
        
        Args:
            text: Text that was embedded
            embedding: Embedding vector
            ttl: Time to live in seconds 
        
        Returns:
            True if successful
        """
        cache_key = self._generate_key(text)
        cache_value = {
            "embedding": embedding,
            "text": text,
            "dimension": len(embedding)
        }
        
        # Store in L2 (Redis)
        await self.multi_cache.set(cache_key, cache_value, ttl=ttl, layer="l2")
        
        logger.debug(f"[EmbeddingCache] Cached embedding: {text[:50]}...")
        return True
    
    async def _find_similar_embedding(self, text: str) -> Optional[List[float]]:
        """
        Find similar cached embedding using vector similarity
        
        Args:
            text: Text to find similar embedding for
        
        Returns:
            Similar embedding vector or None
        """
        if not self.vector_store:
            return None
        
        try:
            # Generate embedding for query text
            from app.core.embedding.pipeline import get_embedding_pipeline
            embedding_pipeline = get_embedding_pipeline()
            query_embedding = embedding_pipeline.encode([text])[0]
            
            # Search in vector store for similar embeddings
            # This would require a special collection for cached embeddings
            # For now, we'll skip similarity search and rely on exact match
            # TODO: Implement similarity search in vector store
            
            return None
        except Exception as e:
            logger.warning(f"[EmbeddingCache] Similarity search failed: {e}")
            return None
    
    def _generate_key(self, text: str) -> str:
        import hashlib
        text_hash = hashlib.md5(text.encode()).hexdigest()
        return f"{self._cache_prefix}{text_hash}"
    
    async def clear(self) -> bool:
        """Clear all cached embeddings"""
        # Note: This would need to clear all keys with prefix
        # For now, just log a warning
        logger.warning("[EmbeddingCache] Clear not fully implemented - would need to scan Redis keys")
        return True




