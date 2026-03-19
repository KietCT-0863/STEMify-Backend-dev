from typing import List, Dict, Any, Optional
import hashlib
import json
import logging

from app.core.cache.multi_level_cache import MultiLevelCache

logger = logging.getLogger(__name__)


class GraphCache:
    """
    Graph Cache (L4)
    
    Caches Cypher query results and graph traversal results.
    TTL: 1 hour
    """
    
    def __init__(
        self,
        multi_cache: MultiLevelCache,
        graph_client=None,
        default_ttl: int = 3600  
    ):
        """
        Initialize graph cache
        
        Args:
            multi_cache: MultiLevelCache instance
            graph_client: Graph client (for validation)
            default_ttl: Default TTL in seconds 
        """
        self.multi_cache = multi_cache
        self.graph_client = graph_client
        self.default_ttl = default_ttl
        self._cache_prefix = "graph:"
    
    async def get_query_result(
        self,
        cypher_query: str,
        parameters: Optional[Dict[str, Any]] = None
    ) -> Optional[Any]:
        """
        Get cached Cypher query result
        
        Args:
            cypher_query: Cypher query string
            parameters: Query parameters (if any)
        
        Returns:
            Cached query result or None
        """
        cache_key = self._generate_key(cypher_query, parameters)
        cached = await self.multi_cache.get(cache_key, layer="l2")
        
        if cached:
            logger.debug(f"[GraphCache] Cache hit: {cypher_query[:50]}...")
            return cached.get("result")
        
        logger.debug(f"[GraphCache] Cache miss: {cypher_query[:50]}...")
        return None
    
    async def set_query_result(
        self,
        cypher_query: str,
        result: Any,
        parameters: Optional[Dict[str, Any]] = None,
        ttl: Optional[int] = None
    ) -> bool:
        """
        Cache Cypher query result
        
        Args:
            cypher_query: Cypher query string
            result: Query result to cache
            parameters: Query parameters (if any)
            ttl: Time to live in seconds (default: 1 hour)
        
        Returns:
            True if successful
        """
        cache_key = self._generate_key(cypher_query, parameters)
        cache_value = {
            "result": result,
            "query": cypher_query,
            "parameters": parameters
        }
        
        ttl = ttl or self.default_ttl
        await self.multi_cache.set(cache_key, cache_value, ttl=ttl, layer="l2")
        
        logger.debug(f"[GraphCache] Cached result: {cypher_query[:50]}...")
        return True
    
    async def get_traversal_result(
        self,
        start_node_id: str,
        relationship_type: str,
        max_depth: int,
        filters: Optional[Dict[str, Any]] = None
    ) -> Optional[Any]:
        """
        Get cached graph traversal result
        
        Args:
            start_node_id: Starting node ID
            relationship_type: Type of relationship to traverse
            max_depth: Maximum traversal depth
            filters: Optional filters
        
        Returns:
            Cached traversal result or None
        """
        cache_key = self._generate_traversal_key(
            start_node_id,
            relationship_type,
            max_depth,
            filters
        )
        cached = await self.multi_cache.get(cache_key, layer="l2")
        
        if cached:
            logger.debug(f"[GraphCache] Traversal cache hit: {start_node_id}")
            return cached.get("result")
        
        return None
    
    async def set_traversal_result(
        self,
        start_node_id: str,
        relationship_type: str,
        max_depth: int,
        result: Any,
        filters: Optional[Dict[str, Any]] = None,
        ttl: Optional[int] = None
    ) -> bool:
        """
        Cache graph traversal result
        
        Args:
            start_node_id: Starting node ID
            relationship_type: Type of relationship
            max_depth: Maximum depth
            result: Traversal result
            filters: Optional filters
            ttl: Time to live in seconds
        
        Returns:
            True if successful
        """
        cache_key = self._generate_traversal_key(
            start_node_id,
            relationship_type,
            max_depth,
            filters
        )
        cache_value = {
            "result": result,
            "start_node": start_node_id,
            "relationship": relationship_type,
            "max_depth": max_depth,
            "filters": filters
        }
        
        ttl = ttl or self.default_ttl
        await self.multi_cache.set(cache_key, cache_value, ttl=ttl, layer="l2")
        
        logger.debug(f"[GraphCache] Cached traversal: {start_node_id}")
        return True
    
    def _generate_key(self, cypher_query: str, parameters: Optional[Dict[str, Any]] = None) -> str:
        key_parts = [cypher_query]
        if parameters:
            sorted_params = json.dumps(parameters, sort_keys=True)
            key_parts.append(sorted_params)
        
        key_string = ":".join(key_parts)
        
        if len(key_string) > 250:
            key_hash = hashlib.md5(key_string.encode()).hexdigest()
            return f"{self._cache_prefix}query:{key_hash}"
        
        key_hash = hashlib.md5(key_string.encode()).hexdigest()
        return f"{self._cache_prefix}query:{key_hash}"
    
    def _generate_traversal_key(
        self,
        start_node_id: str,
        relationship_type: str,
        max_depth: int,
        filters: Optional[Dict[str, Any]] = None
    ) -> str:
        key_parts = [
            f"traversal:{start_node_id}",
            f"rel:{relationship_type}",
            f"depth:{max_depth}"
        ]
        
        if filters:
            sorted_filters = json.dumps(filters, sort_keys=True)
            key_parts.append(f"filters:{sorted_filters}")
        
        key_string = ":".join(key_parts)
        key_hash = hashlib.md5(key_string.encode()).hexdigest()
        return f"{self._cache_prefix}{key_hash}"
    
    async def invalidate_node(self, node_id: str) -> bool:
        """
        Invalidate cache entries related to a node
        
        Args:
            node_id: Node ID to invalidate
        
        Returns:
            True if successful
        """
        # Note: Full invalidation would require scanning all keys
        # For now, we'll rely on TTL expiration
        logger.info(f"[GraphCache] Node invalidation requested: {node_id} (using TTL expiration)")
        return True




