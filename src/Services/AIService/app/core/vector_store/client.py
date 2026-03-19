"""
Vector Store Client
Abstract interface for vector stores
"""

from typing import List, Dict, Any, Optional

from app.core.vector_store.providers.qdrant_provider import QdrantProvider
from app.core.vector_store.providers.base_provider import BaseVectorStoreProvider
from app.infrastructure.config.settings import settings


class VectorStoreClient:
    """Vector store client wrapper"""
    
    def __init__(self):
        self.provider: BaseVectorStoreProvider = QdrantProvider()
        self._collection_created = False
    
    async def ensure_collection(self, vector_size: int = 384):
        """Ensure collection exists"""
        if not self._collection_created:
            await self.provider.create_collection(settings.QDRANT_COLLECTION, vector_size)
            self._collection_created = True
    
    async def upsert(
        self,
        id: str,
        vector: List[float],
        payload: Dict[str, Any]
    ) -> bool:
        """Upsert a vector"""
        await self.ensure_collection(len(vector))
        return await self.provider.upsert(id, vector, payload)
    
    async def search(
        self,
        query_vector: List[float],
        top_k: int = 10,
        filters: Optional[Dict[str, Any]] = None
    ) -> List[Dict[str, Any]]:
        """Search for similar vectors"""
        return await self.provider.search(query_vector, top_k, filters)
    
    async def delete(self, ids: List[str]) -> bool:
        """Delete vectors"""
        return await self.provider.delete(ids)
