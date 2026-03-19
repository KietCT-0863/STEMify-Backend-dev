from typing import Dict, Any, List, Optional
from datetime import datetime
import logging

logger = logging.getLogger(__name__)


class WorkingMemory:
    """
    Working Memory (Layer 1)
    
    In-memory, session-based temporary memory.
    Max items: 50
    Auto-cleanup after session
    """
    
    def __init__(self, max_items: int = 50):
        """
        Initialize working memory
        
        Args:
            max_items: Maximum number of items to store
        """
        self.max_items = max_items
        self._memories: Dict[str, Dict[str, Any]] = {}
        self._access_order: List[str] = []  # For LRU eviction
    
    async def add(self, content: str, metadata: Dict[str, Any]) -> str:
        """
        Add memory item
        
        Args:
            content: Memory content
            metadata: Memory metadata
        
        Returns:
            Memory ID
        """
        import uuid
        memory_id = str(uuid.uuid4())
        
        # Evict if needed (LRU)
        if len(self._memories) >= self.max_items:
            if self._access_order:
                lru_id = self._access_order.pop(0)
                if lru_id in self._memories:
                    del self._memories[lru_id]
        
        self._memories[memory_id] = {
            "content": content,
            "metadata": metadata,
            "created_at": datetime.now(),
            "access_count": 0
        }
        
        self._access_order.append(memory_id)
        
        logger.debug(f"[WorkingMemory] Added memory: {memory_id}")
        return memory_id
    
    async def get(self, memory_id: str) -> Optional[Dict[str, Any]]:
        """
        Get memory item
        
        Args:
            memory_id: Memory ID
        
        Returns:
            Memory item or None
        """
        if memory_id not in self._memories:
            return None
        
        memory = self._memories[memory_id]
        memory["access_count"] += 1
        
        # Update access order
        if memory_id in self._access_order:
            self._access_order.remove(memory_id)
        self._access_order.append(memory_id)
        
        return memory
    
    async def search(
        self,
        query: str,
        limit: int = 5,
        user_id: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        """
        Search memories by content
        
        Args:
            query: Search query
            limit: Maximum results
            user_id: Optional user ID filter
        
        Returns:
            List of matching memories
        """
        results = []
        query_lower = query.lower()
        
        for memory_id, memory in self._memories.items():
            # Filter by user_id if provided
            if user_id and memory.get("metadata", {}).get("user_id") != user_id:
                continue
            
            # Simple keyword matching
            content_lower = memory["content"].lower()
            if query_lower in content_lower:
                results.append({
                    **memory,
                    "memory_id": memory_id,
                    "relevance_score": 0.7  # Simple match
                })
        
        # Sort by access count and recency
        results.sort(key=lambda x: (
            x.get("access_count", 0) * 0.5 +
            (datetime.now() - x.get("created_at", datetime.now())).total_seconds() * -0.0001
        ), reverse=True)
        
        return results[:limit]
    
    async def get_important(self, user_id: Optional[str] = None) -> List[Dict[str, Any]]:
        """
        Get important memories (high access count or recent)
        
        Args:
            user_id: Optional user ID filter
        
        Returns:
            List of important memories
        """
        important = []
        
        for memory_id, memory in self._memories.items():
            # Filter by user_id if provided
            if user_id and memory.get("metadata", {}).get("user_id") != user_id:
                continue
            
            # Consider important if accessed multiple times or high importance in metadata
            importance = memory.get("metadata", {}).get("importance", 0.5)
            access_count = memory.get("access_count", 0)
            
            if importance > 0.7 or access_count > 3:
                important.append({
                    **memory,
                    "memory_id": memory_id
                })
        
        return important
    
    async def clear(self, user_id: Optional[str] = None):
        """
        Clear memories (session cleanup)
        
        Args:
            user_id: Optional user ID filter
        """
        if user_id:
            # Clear only for specific user
            to_remove = []
            for memory_id, memory in self._memories.items():
                if memory.get("metadata", {}).get("user_id") == user_id:
                    to_remove.append(memory_id)
            
            for memory_id in to_remove:
                del self._memories[memory_id]
                if memory_id in self._access_order:
                    self._access_order.remove(memory_id)
        else:
            # Clear all
            self._memories.clear()
            self._access_order.clear()
        
        logger.info(f"[WorkingMemory] Cleared memories (user_id: {user_id or 'all'})")
    
    def get_stats(self) -> Dict[str, Any]:
        """Get memory statistics"""
        return {
            "total_items": len(self._memories),
            "max_items": self.max_items,
            "usage_percent": (len(self._memories) / self.max_items) * 100 if self.max_items > 0 else 0
        }




