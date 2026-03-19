import time
import logging
import numpy as np
from typing import Dict, Any, List, Optional, Tuple
from dataclasses import dataclass, field
from collections import OrderedDict
import threading

logger = logging.getLogger(__name__)


@dataclass
class CacheEntry:
    """A cached query result."""
    query: str
    embedding: np.ndarray
    result: Dict[str, Any]
    timestamp: float = field(default_factory=time.time)
    hits: int = 0


class SemanticQueryCache:
    """
    Cache for memory search results with semantic similarity matching.
    
    Features:
    - Cosine similarity matching (threshold configurable)
    - TTL-based expiration
    - LRU eviction when max size reached
    - Thread-safe operations
    """
    
    def __init__(
        self,
        max_size: int = 100,
        ttl_seconds: float = 300.0,  # 5 minutes default
        similarity_threshold: float = 0.85,  # High threshold for semantic match
        embedding_pipeline=None,
    ):
        """
        Initialize semantic cache.
        
        Args:
            max_size: Maximum number of cached entries
            ttl_seconds: Time-to-live for cache entries
            similarity_threshold: Minimum cosine similarity for cache hit
            embedding_pipeline: EmbeddingPipeline instance (uses singleton if None)
        """
        self.max_size = max_size
        self.ttl_seconds = ttl_seconds
        self.similarity_threshold = similarity_threshold
        self._embedding_pipeline = embedding_pipeline
        
        # OrderedDict for LRU eviction
        self._cache: OrderedDict[str, CacheEntry] = OrderedDict()
        self._lock = threading.Lock()
        
        # Stats
        self._hits = 0
        self._misses = 0
        
        logger.info(
            f"[SemanticCache] Initialized with max_size={max_size}, "
            f"ttl={ttl_seconds}s, threshold={similarity_threshold}"
        )
    
    @property
    def embedding_pipeline(self):
        """Lazy load embedding pipeline."""
        if self._embedding_pipeline is None:
            from app.core.embedding.pipeline import get_embedding_pipeline
            self._embedding_pipeline = get_embedding_pipeline()
        return self._embedding_pipeline
    
    def _get_embedding(self, text: str) -> np.ndarray:
        """Get embedding for text."""
        embeddings = self.embedding_pipeline.encode([text])
        return embeddings[0]
    
    def _cosine_similarity(self, a: np.ndarray, b: np.ndarray) -> float:
        """Calculate cosine similarity between two vectors."""
        norm_a = np.linalg.norm(a)
        norm_b = np.linalg.norm(b)
        if norm_a == 0 or norm_b == 0:
            return 0.0
        return float(np.dot(a, b) / (norm_a * norm_b))
    
    def _is_expired(self, entry: CacheEntry) -> bool:
        """Check if cache entry is expired."""
        return (time.time() - entry.timestamp) > self.ttl_seconds
    
    def _evict_expired(self):
        """Remove expired entries."""
        expired_keys = [
            key for key, entry in self._cache.items()
            if self._is_expired(entry)
        ]
        for key in expired_keys:
            del self._cache[key]
        
        if expired_keys:
            logger.debug(f"[SemanticCache] Evicted {len(expired_keys)} expired entries")
    
    def _evict_lru(self):
        """Evict least recently used entry if cache is full."""
        while len(self._cache) >= self.max_size:
            # Pop oldest (first) item
            oldest_key = next(iter(self._cache))
            del self._cache[oldest_key]
            logger.debug(f"[SemanticCache] LRU evicted: {oldest_key[:50]}...")
    
    def get(self, query: str) -> Optional[Dict[str, Any]]:
        """
        Get cached result for semantically similar query.
        
        Args:
            query: Search query
        
        Returns:
            Cached result if found, None otherwise
        """
        with self._lock:
            self._evict_expired()
            
            # Get query embedding
            try:
                query_embedding = self._get_embedding(query)
            except Exception as e:
                logger.warning(f"[SemanticCache] Failed to get embedding: {e}")
                self._misses += 1
                return None
            
            # Search for similar cached query
            best_match: Optional[CacheEntry] = None
            best_similarity = 0.0
            
            for key, entry in self._cache.items():
                if self._is_expired(entry):
                    continue
                
                similarity = self._cosine_similarity(query_embedding, entry.embedding)
                
                if similarity >= self.similarity_threshold and similarity > best_similarity:
                    best_similarity = similarity
                    best_match = entry
            
            if best_match:
                # Move to end (most recently used)
                # Find and move key
                for key, entry in list(self._cache.items()):
                    if entry is best_match:
                        self._cache.move_to_end(key)
                        break
                
                best_match.hits += 1
                self._hits += 1
                
                logger.info(
                    f"[SemanticCache] HIT (similarity={best_similarity:.3f}): "
                    f"'{query[:50]}...' matched '{best_match.query[:50]}...'"
                )
                
                return best_match.result
            
            self._misses += 1
            logger.debug(f"[SemanticCache] MISS: '{query[:50]}...'")
            return None
    
    def set(self, query: str, result: Dict[str, Any]):
        """
        Cache result for query.
        
        Args:
            query: Search query
            result: Result to cache
        """
        with self._lock:
            self._evict_expired()
            self._evict_lru()
            
            try:
                query_embedding = self._get_embedding(query)
            except Exception as e:
                logger.warning(f"[SemanticCache] Failed to cache: {e}")
                return
            
            # Use query as key (normalized)
            key = query.lower().strip()
            
            entry = CacheEntry(
                query=query,
                embedding=query_embedding,
                result=result,
            )
            
            self._cache[key] = entry
            self._cache.move_to_end(key)
            
            logger.debug(f"[SemanticCache] Cached: '{query[:50]}...'")
    
    def invalidate(self, query: Optional[str] = None):
        """
        Invalidate cache entries.
        
        Args:
            query: Specific query to invalidate, or None to clear all
        """
        with self._lock:
            if query is None:
                self._cache.clear()
                logger.info("[SemanticCache] Cleared all entries")
            else:
                key = query.lower().strip()
                if key in self._cache:
                    del self._cache[key]
                    logger.debug(f"[SemanticCache] Invalidated: '{query[:50]}...'")
    
    def get_stats(self) -> Dict[str, Any]:
        with self._lock:
            total = self._hits + self._misses
            hit_rate = (self._hits / total * 100) if total > 0 else 0
            
            return {
                "size": len(self._cache),
                "max_size": self.max_size,
                "hits": self._hits,
                "misses": self._misses,
                "hit_rate": f"{hit_rate:.1f}%",
                "ttl_seconds": self.ttl_seconds,
                "similarity_threshold": self.similarity_threshold,
            }


_semantic_cache: Optional[SemanticQueryCache] = None
_semantic_cache_lock = threading.Lock()


def get_semantic_cache() -> SemanticQueryCache:
    global _semantic_cache
    if _semantic_cache is None:
        with _semantic_cache_lock:
            if _semantic_cache is None:
                _semantic_cache = SemanticQueryCache()
    return _semantic_cache

