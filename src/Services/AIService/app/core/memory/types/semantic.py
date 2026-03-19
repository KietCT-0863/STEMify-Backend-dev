"""
Semantic Memory
Qdrant (vectors) + Neo4j (relationships) for knowledge and concepts
"""

from typing import Dict, Any, List, Optional
from datetime import datetime
import logging
import uuid
import json

from app.core.vector_store.client import VectorStoreClient
from app.core.graph.client import GraphClient

logger = logging.getLogger(__name__)


class SemanticMemory:
    """
    Semantic Memory (Layer 3)
    
    Stores knowledge and concepts.
    - Qdrant: Vector storage for semantic search
    - Neo4j: Graph relationships for knowledge connections
    """
    
    def __init__(
        self,
        vector_store: Optional[VectorStoreClient] = None,
        graph_client: Optional[GraphClient] = None,
        collection_name: str = "semantic_memory"
    ):
        """
        Initialize semantic memory
        
        Args:
            vector_store: Vector store client for embeddings
            graph_client: Graph client for relationships
            collection_name: Qdrant collection name
        """
        self.vector_store = vector_store
        self.graph_client = graph_client
        self.collection_name = collection_name
    
    async def add(self, content: str, metadata: Dict[str, Any]) -> str:
        """
        Add semantic memory
        
        Args:
            content: Memory content (knowledge/concept)
            metadata: Memory metadata
        
        Returns:
            Memory ID
        """
        memory_id = str(uuid.uuid4())
        importance = metadata.get("importance", 0.5)
        user_id = metadata.get("user_id")
        
        # Store embedding in Qdrant
        if self.vector_store:
            try:
                from app.core.embedding.pipeline import get_embedding_pipeline
                embedding_pipeline = get_embedding_pipeline()
                embedding = embedding_pipeline.encode([content])[0].tolist()
                
                await self.vector_store.upsert(
                    id=memory_id,
                    vector=embedding,
                    payload={
                        "content": content,
                        "memory_type": "semantic",
                        "importance": importance,
                        "user_id": user_id,
                        **metadata
                    }
                )
            except Exception as e:
                logger.warning(f"[SemanticMemory] Failed to store embedding: {e}")
        
        # Store node in Neo4j (if available)
        if self.graph_client:
            try:
                await self.graph_client.create_node(
                    node_type="SemanticMemory",
                    node_id=memory_id,
                    properties={
                        "content": content,
                        "importance": importance,
                        "user_id": user_id,
                        "created_at": datetime.now().isoformat(),
                        **metadata
                    }
                )
                
                # Create relationships if specified
                if "related_concepts" in metadata:
                    for related_id in metadata["related_concepts"]:
                        await self.graph_client.create_relationship(
                            from_node_id=memory_id,
                            to_node_id=related_id,
                            relationship_type="RELATED_TO",
                            properties={}
                        )
            except Exception as e:
                logger.warning(f"[SemanticMemory] Failed to store in graph: {e}")
        
        logger.debug(f"[SemanticMemory] Added memory: {memory_id}")
        return memory_id
    
    async def search(
        self,
        query: str,
        limit: int = 5,
        user_id: Optional[str] = None,
        min_importance: float = 0.3
    ) -> List[Dict[str, Any]]:
        """
        Search semantic memories
        
        Args:
            query: Search query
            limit: Maximum results
            user_id: Optional user ID filter
            min_importance: Minimum importance threshold
        
        Returns:
            List of matching memories
        """
        if not self.vector_store:
            return []
        
        try:
            from app.core.embedding.pipeline import get_embedding_pipeline
            embedding_pipeline = get_embedding_pipeline()
            query_embedding = embedding_pipeline.encode([query])[0].tolist()
            
            filters = {}
            if user_id:
                filters["user_id"] = user_id
            filters["memory_type"] = "semantic"
            
            vector_results = await self.vector_store.search(
                query_vector=query_embedding,
                top_k=limit * 2,
                filters=filters
            )
            
            # Filter by importance
            results = []
            for result in vector_results:
                importance = result.get("payload", {}).get("importance", 0.5)
                if importance >= min_importance:
                    results.append({
                        "memory_id": result["id"],
                        "content": result.get("content", ""),
                        "metadata": result.get("payload", {}),
                        "relevance_score": result.get("score", 0.5),
                        "importance": importance
                    })
            
            # Optionally enrich with graph relationships
            if self.graph_client and results:
                for result in results:
                    try:
                        # Get related concepts from graph
                        relationships = await self.graph_client.get_relationships(
                            node_id=result["memory_id"],
                            relationship_type="RELATED_TO"
                        )
                        result["related_concepts"] = [rel["to_node_id"] for rel in relationships]
                    except Exception as e:
                        logger.debug(f"[SemanticMemory] Failed to get relationships: {e}")
            
            return results[:limit]
        except Exception as e:
            logger.warning(f"[SemanticMemory] Search failed: {e}")
            return []




