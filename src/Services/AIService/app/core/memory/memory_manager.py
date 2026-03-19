from typing import Dict, Any, List, Optional
import logging

from app.core.memory.types.working import WorkingMemory
from app.core.memory.types.episodic import EpisodicMemory
from app.core.memory.types.semantic import SemanticMemory
from app.core.memory.types.perceptual import PerceptualMemory
from app.core.cache.semantic_cache import SemanticQueryCache, get_semantic_cache

logger = logging.getLogger(__name__)


class MemoryManager:
    """
    Memory Manager
    
    Orchestrates the 4-layer memory system:
    - Working Memory: In-memory, session-based
    - Episodic Memory: Events and experiences (SQLite + Qdrant)
    - Semantic Memory: Knowledge and concepts (Qdrant + Neo4j)
    - Perceptual Memory: Multimodal data (SQLite + Qdrant)
    
    """
    
    def __init__(
        self,
        working_memory: Optional[WorkingMemory] = None,
        episodic_memory: Optional[EpisodicMemory] = None,
        semantic_memory: Optional[SemanticMemory] = None,
        perceptual_memory: Optional[PerceptualMemory] = None,
        semantic_cache: Optional[SemanticQueryCache] = None,
    ):
        """
        Initialize memory manager
        
        Args:
            working_memory: Working memory instance
            episodic_memory: Episodic memory instance
            semantic_memory: Semantic memory instance
            perceptual_memory: Perceptual memory instance
            semantic_cache: Optional semantic cache for query deduplication
        """
        self.working_memory = working_memory or WorkingMemory()
        self.episodic_memory = episodic_memory
        self.semantic_memory = semantic_memory
        self.perceptual_memory = perceptual_memory
        
        # Use provided cache or get singleton
        self._semantic_cache = semantic_cache
        
        logger.info("MemoryManager initialized")
    
    @property
    def semantic_cache(self) -> SemanticQueryCache:
        """Lazy load semantic cache."""
        if self._semantic_cache is None:
            self._semantic_cache = get_semantic_cache()
        return self._semantic_cache
    
    async def add_memory(
        self,
        content: str,
        memory_type: str,
        metadata: Dict[str, Any]
    ) -> str:
       
        if memory_type == "working":
            return await self.working_memory.add(content, metadata)
        
        elif memory_type == "episodic":
            if not self.episodic_memory:
                raise ValueError("Episodic memory not initialized")
            return await self.episodic_memory.add(content, metadata)
        
        elif memory_type == "semantic":
            if not self.semantic_memory:
                raise ValueError("Semantic memory not initialized")
            return await self.semantic_memory.add(content, metadata)
        
        elif memory_type == "perceptual":
            if not self.perceptual_memory:
                raise ValueError("Perceptual memory not initialized")
            content_type = metadata.pop("content_type", "image")
            content_path = metadata.pop("content_path", None)
            return await self.perceptual_memory.add(
                content, metadata, content_type=content_type, content_path=content_path
            )
        
        else:
            raise ValueError(f"Unknown memory type: {memory_type}")
    
    async def retrieve_memories(
        self,
        query: str,
        memory_types: Optional[List[str]] = None,
        limit: int = 5,
        user_id: Optional[str] = None,
        use_cache: bool = True,
        **kwargs
    ) -> Dict[str, List[Dict[str, Any]]]:
        """
        Retrieve memories from multiple layers with semantic caching.
        
        Args:
            query: Search query
            memory_types: List of memory types to search (None = all)
            limit: Maximum results per layer
            user_id: Optional user ID filter
            use_cache: Whether to use semantic cache (default True)
            **kwargs: Additional filters
        
        Returns:
            Dict mapping memory_type to list of memories
        """
        cache_key = f"{query}|{user_id or ''}|{','.join(sorted(memory_types or []))}"
        
        if use_cache:
            cached_result = self.semantic_cache.get(cache_key)
            if cached_result is not None:
                logger.debug(f"[MemoryManager] Cache hit for query: {query[:50]}...")
                return cached_result
        
        results = {}
        
        memory_types = memory_types or ["working", "episodic", "semantic", "perceptual"]
        
        # Search Working Memory
        if "working" in memory_types:
            try:
                working_results = await self.working_memory.search(
                    query, limit=limit, user_id=user_id
                )
                results["working"] = working_results
            except Exception as e:
                logger.warning(f"[MemoryManager] Working memory search failed: {e}")
                results["working"] = []
        
        # Search Episodic Memory
        if "episodic" in memory_types and self.episodic_memory:
            try:
                episodic_results = await self.episodic_memory.search(
                    query, limit=limit, user_id=user_id, **kwargs
                )
                results["episodic"] = episodic_results
            except Exception as e:
                logger.warning(f"[MemoryManager] Episodic memory search failed: {e}")
                results["episodic"] = []
        
        # Search Semantic Memory
        if "semantic" in memory_types and self.semantic_memory:
            try:
                semantic_results = await self.semantic_memory.search(
                    query, limit=limit, user_id=user_id, **kwargs
                )
                results["semantic"] = semantic_results
            except Exception as e:
                logger.warning(f"[MemoryManager] Semantic memory search failed: {e}")
                results["semantic"] = []
        
        # Search Perceptual Memory
        if "perceptual" in memory_types and self.perceptual_memory:
            try:
                perceptual_results = await self.perceptual_memory.search(
                    query, limit=limit, user_id=user_id, **kwargs
                )
                results["perceptual"] = perceptual_results
            except Exception as e:
                logger.warning(f"[MemoryManager] Perceptual memory search failed: {e}")
                results["perceptual"] = []
        
        if use_cache:
            self.semantic_cache.set(cache_key, results)
        
        return results
    
    async def consolidate_memories(
        self,
        user_id: str,
        importance_threshold: float = 0.7
    ) -> Dict[str, int]:
        """
        Consolidate important memories across layers
        
        Args:
            user_id: User ID
            importance_threshold: Minimum importance to consolidate
        
        Returns:
            Dict with counts of consolidated memories per layer
        """
        consolidated = {
            "working": 0,
            "episodic": 0,
            "semantic": 0,
            "perceptual": 0
        }
        
        # Get important working memories
        try:
            important_working = await self.working_memory.get_important(user_id=user_id)
            consolidated["working"] = len(important_working)
        except Exception as e:
            logger.warning(f"[MemoryManager] Working memory consolidation failed: {e}")
        
        # Get important episodic memories
        if self.episodic_memory:
            try:
                important_episodic = await self.episodic_memory.get_important(user_id=user_id)
                consolidated["episodic"] = len(important_episodic)
            except Exception as e:
                logger.warning(f"[MemoryManager] Episodic memory consolidation failed: {e}")
        
        # Semantic and Perceptual don't have get_important methods yet
        # They would be consolidated based on importance in metadata
        
        logger.info(f"[MemoryManager] Consolidated memories for user {user_id}: {consolidated}")
        return consolidated
    
    async def clear_session(self, user_id: Optional[str] = None):
        """
        Clear session-based memories (Working Memory)
        
        Args:
            user_id: Optional user ID filter
        """
        await self.working_memory.clear(user_id=user_id)
        logger.info(f"[MemoryManager] Cleared session memories (user_id: {user_id or 'all'})")
    
    def get_stats(self) -> Dict[str, Any]:
        """Get memory system statistics"""
        stats = {
            "working": self.working_memory.get_stats() if self.working_memory else None,
            "episodic": "initialized" if self.episodic_memory else "not_initialized",
            "semantic": "initialized" if self.semantic_memory else "not_initialized",
            "perceptual": "initialized" if self.perceptual_memory else "not_initialized"
        }
        return stats




