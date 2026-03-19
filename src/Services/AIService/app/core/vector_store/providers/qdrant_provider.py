"""
Qdrant Provider
Qdrant implementation of vector store
"""

from typing import List, Dict, Any, Optional
from qdrant_client import QdrantClient
from qdrant_client.models import (
    Distance, VectorParams, PointStruct, Filter, FieldCondition, MatchValue, Range
)
import logging

from app.core.vector_store.providers.base_provider import BaseVectorStoreProvider
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class QdrantProvider(BaseVectorStoreProvider):
    """Qdrant vector store provider"""
    
    def __init__(self):

        qdrant_url = settings.get_qdrant_url()
        qdrant_api_key = settings.get_qdrant_api_key()
        
        if qdrant_api_key:
            self.client = QdrantClient(
                url=qdrant_url,
                api_key=qdrant_api_key
            )
        else:
            self.client = QdrantClient(url=qdrant_url)
        
        self.collection_name = settings.QDRANT_COLLECTION
        self._collection_initialized = False

    def _ensure_collection(self, vector_size: int) -> None:
        """
        Ensure the collection exists without dropping existing data.
        If missing, create it with the provided vector size.
        """
        if self._collection_initialized:
            return

        try:
            collections = self.client.get_collections()
            collection_names = [c.name for c in collections.collections]

            if self.collection_name not in collection_names:
                self.client.create_collection(
                    collection_name=self.collection_name,
                    vectors_config=VectorParams(
                        size=vector_size,
                        distance=Distance.COSINE
                    )
                )
                logger.info(f"[QdrantProvider] Created collection: {self.collection_name}")
            else:
                logger.info(f"[QdrantProvider] Collection {self.collection_name} already exists")

            self._collection_initialized = True
        except Exception as e:
            logger.error(f"[QdrantProvider] Failed to ensure collection {self.collection_name}: {e}")
    
    async def create_collection(
        self,
        collection_name: str,
        vector_size: int
    ) -> bool:
        """Create a new collection"""
        try:
            collections = self.client.get_collections()
            collection_names = [c.name for c in collections.collections]
            
            if collection_name not in collection_names:
                self.client.create_collection(
                    collection_name=collection_name,
                    vectors_config=VectorParams(
                        size=vector_size,
                        distance=Distance.COSINE
                    )
                )
                logger.info(f"Created collection: {collection_name}")
                return True
            else:
                logger.info(f"Collection {collection_name} already exists")
                return True
        except Exception as e:
            logger.error(f"Error creating collection: {e}")
            return False
    
    async def upsert(
        self,
        id: str,
        vector: List[float],
        payload: Dict[str, Any]
    ) -> bool:
        """Upsert a vector with payload"""
        try:
            # Ensure collection exists before upsert (idempotent, non-destructive)
            self._ensure_collection(vector_size=len(vector))

            point = PointStruct(
                id=id,
                vector=vector,
                payload=payload
            )
            
            self.client.upsert(
                collection_name=self.collection_name,
                points=[point]
            )
            return True
        except Exception as e:
            logger.error(f"Error upserting vector {id}: {e}")
            return False
    
    async def search(
        self,
        query_vector: List[float],
        top_k: int = 10,
        filters: Optional[Dict[str, Any]] = None
    ) -> List[Dict[str, Any]]:
        """Search for similar vectors"""
        try:
            # Ensure collection exists before search (idempotent, non-destructive)
            self._ensure_collection(vector_size=len(query_vector))

            # Build filter if provided
            qdrant_filter = None
            if filters:
                qdrant_filter = self._build_filter(filters)
            
            results = self.client.search(
                collection_name=self.collection_name,
                query_vector=query_vector,
                limit=top_k,
                query_filter=qdrant_filter
            )
            
            # Format results
            formatted_results = []
            for result in results:
                formatted_results.append({
                    "id": result.id,
                    "score": result.score,
                    "payload": result.payload,
                    "content": result.payload.get("content", ""),
                    "metadata": {k: v for k, v in result.payload.items() if k != "content"},
                    "confidence_score": result.payload.get("confidence_score", 0.5)
                })
            
            return formatted_results
        except Exception as e:
            logger.error(f"Error searching vectors: {e}")
            return []
    
    async def delete(self, ids: List[str]) -> bool:
        """Delete vectors by IDs"""
        try:
            self.client.delete(
                collection_name=self.collection_name,
                points_selector=ids
            )
            return True
        except Exception as e:
            logger.error(f"Error deleting vectors: {e}")
            return False
    
    def _build_filter(self, filters: Dict[str, Any]) -> Filter:
        """Build Qdrant filter from filter dict"""
        conditions = []
        
        # Handle "must" conditions
        if "must" in filters:
            for condition in filters["must"]:
                if "key" in condition:
                    key = condition["key"]
                    
                    if "match" in condition:
                        # Exact match
                        match_value = condition["match"].get("value")
                        conditions.append(
                            FieldCondition(key=key, match=MatchValue(value=match_value))
                        )
                    elif "range" in condition:
                        # Range query
                        range_params = condition["range"]
                        conditions.append(
                            FieldCondition(
                                key=key,
                                range=Range(
                                    gte=range_params.get("gte"),
                                    lte=range_params.get("lte"),
                                    gt=range_params.get("gt"),
                                    lt=range_params.get("lt")
                                )
                            )
                        )
        
        return Filter(must=conditions) if conditions else None
