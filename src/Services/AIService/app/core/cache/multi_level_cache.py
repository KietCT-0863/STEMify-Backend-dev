from typing import Any, Optional, Dict
from datetime import datetime, timedelta
import logging
import json
import hashlib

logger = logging.getLogger(__name__)


class MultiLevelCache:
    """
    Multi-Level Cache System
    
    Layers:
    - L1 (In-Memory): Hot data, 5min TTL, LRU eviction
    - L2 (Redis): Medium data, 1hr TTL
    - L3 (Embedding): Pre-computed embeddings, 7 days TTL
    - L4 (Graph): Cypher query results, 1hr TTL
    - L5 (Agent Response): Similar query responses, similarity-based matching, 1hr TTL
    """
    
    def __init__(
        self,
        redis_client=None,
        in_memory_max_size: int = 1000,
        default_ttl: int = 3600  
    ):
        """
        Initialize multi-level cache
        
        Args:
            redis_client: Redis client instance (optional)
            in_memory_max_size: Maximum items in L1 cache
            default_ttl: Default TTL in seconds
        """
        # L1: In-memory cache (dict with TTL)
        self._l1_cache: Dict[str, Dict[str, Any]] = {}
        self._l1_max_size = in_memory_max_size
        self._l1_access_order: list = []  # For LRU eviction
        
        # L2: Redis cache
        self._redis_client = redis_client
        self._default_ttl = default_ttl
        
        logger.info(f"MultiLevelCache initialized (L1 max: {in_memory_max_size}, Redis: {redis_client is not None})")
    
    async def get(self, key: str, layer: Optional[str] = None) -> Optional[Any]:
        """
        Get value from cache
        
        Args:
            key: Cache key
            layer: Specific layer to check ("l1", "l2"), or None to check all layers
        
        Returns:
            Cached value or None if not found
        """
        # Try L1 
        if layer is None or layer == "l1":
            l1_value = self._get_l1(key)
            if l1_value is not None:
                logger.debug(f"[Cache] L1 hit: {key}")
                return l1_value
        
        # Try L2 
        if layer is None or layer == "l2":
            if self._redis_client:
                l2_value = await self._get_l2(key)
                if l2_value is not None:
                    logger.debug(f"[Cache] L2 hit: {key}")
                    # Promote to L1
                    await self.set(key, l2_value, ttl=300, layer="l1")  # 5min
                    return l2_value
        
        logger.debug(f"[Cache] Miss: {key}")
        return None
    
    async def set(
        self,
        key: str,
        value: Any,
        ttl: Optional[int] = None,
        layer: Optional[str] = None
    ) -> bool:
        ttl = ttl or self._default_ttl
        
        # Set in L1
        if layer is None or layer == "l1":
            self._set_l1(key, value, ttl)
        
        # Set in L2 
        if (layer is None or layer == "l2") and self._redis_client:
            await self._set_l2(key, value, ttl)
        
        return True
    
    async def delete(self, key: str, layer: Optional[str] = None) -> bool:
        """
        Delete value from cache
        
        Args:
            key: Cache key
            layer: Specific layer to delete ("l1", "l2"), or None to delete from all
        
        """
        deleted = False
        
        # Delete from L1
        if layer is None or layer == "l1":
            if key in self._l1_cache:
                del self._l1_cache[key]
                if key in self._l1_access_order:
                    self._l1_access_order.remove(key)
                deleted = True
        
        # Delete from L2
        if (layer is None or layer == "l2") and self._redis_client:
            try:
                await self._redis_client.delete(key)
                deleted = True
            except Exception as e:
                logger.warning(f"[Cache] Failed to delete from L2: {e}")
        
        return deleted
    
    async def clear(self, layer: Optional[str] = None) -> bool:
        """
        Clear all cache
        
        Args:
            layer: Specific layer to clear ("l1", "l2"), or None to clear all
        
        """
        # Clear L1
        if layer is None or layer == "l1":
            self._l1_cache.clear()
            self._l1_access_order.clear()
        
        # Clear L2
        if (layer is None or layer == "l2") and self._redis_client:
            try:
                await self._redis_client.flushdb()
            except Exception as e:
                logger.warning(f"[Cache] Failed to clear L2: {e}")
        
        logger.info(f"[Cache] Cleared layer: {layer or 'all'}")
        return True
    
    def _get_l1(self, key: str) -> Optional[Any]:
        """Get from L1 (in-memory) cache"""
        if key not in self._l1_cache:
            return None
        
        entry = self._l1_cache[key]
        
        # Check TTL
        if datetime.now() > entry["expires_at"]:
            # Expired, remove it
            del self._l1_cache[key]
            if key in self._l1_access_order:
                self._l1_access_order.remove(key)
            return None
        
        # Update access order (LRU)
        if key in self._l1_access_order:
            self._l1_access_order.remove(key)
        self._l1_access_order.append(key)
        
        return entry["value"]
    
    def _set_l1(self, key: str, value: Any, ttl: int):
        """Set in L1 (in-memory) cache"""
        # Evict if needed (LRU)
        if len(self._l1_cache) >= self._l1_max_size and key not in self._l1_cache:
            # Remove least recently used
            if self._l1_access_order:
                lru_key = self._l1_access_order.pop(0)
                if lru_key in self._l1_cache:
                    del self._l1_cache[lru_key]
        
        expires_at = datetime.now() + timedelta(seconds=ttl)
        self._l1_cache[key] = {
            "value": value,
            "expires_at": expires_at,
            "created_at": datetime.now()
        }
        
        # Update access order
        if key in self._l1_access_order:
            self._l1_access_order.remove(key)
        self._l1_access_order.append(key)
    
    async def _get_l2(self, key: str) -> Optional[Any]:
        try:
            value_str = await self._redis_client.get(key)
            if value_str is None:
                return None
            
            # Deserialize
            return json.loads(value_str)
        except Exception as e:
            logger.warning(f"[Cache] L2 get error: {e}")
            return None
    
    async def _set_l2(self, key: str, value: Any, ttl: int):
        """Set in L2 (Redis) cache"""
        try:
            # Serialize
            value_str = json.dumps(value, default=str)
            await self._redis_client.setex(key, ttl, value_str)
        except Exception as e:
            logger.warning(f"[Cache] L2 set error: {e}")
    
    def _generate_key(self, prefix: str, *args, **kwargs) -> str:
        """Generate cache key from prefix and arguments"""
        key_parts = [prefix]
        key_parts.extend(str(arg) for arg in args)
        key_parts.extend(f"{k}={v}" for k, v in sorted(kwargs.items()))
        key_string = ":".join(key_parts)
        
        if len(key_string) > 250:
            key_hash = hashlib.md5(key_string.encode()).hexdigest()
            return f"{prefix}:{key_hash}"
        
        return key_string
    
    def get_stats(self) -> Dict[str, Any]:
        """Get cache statistics"""
        return {
            "l1_size": len(self._l1_cache),
            "l1_max_size": self._l1_max_size,
            "l2_available": self._redis_client is not None
        }




