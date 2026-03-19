"""
Base Vector Store Provider
Abstract base class for vector store providers
"""

from abc import ABC, abstractmethod
from typing import List, Dict, Any, Optional


class BaseVectorStoreProvider(ABC):
    """Abstract interface for vector store providers"""
    
    @abstractmethod
    async def upsert(
        self,
        id: str,
        vector: List[float],
        payload: Dict[str, Any]
    ) -> bool:
        """Upsert a vector with payload"""
        pass
    
    @abstractmethod
    async def search(
        self,
        query_vector: List[float],
        top_k: int = 10,
        filters: Optional[Dict[str, Any]] = None
    ) -> List[Dict[str, Any]]:
        """Search for similar vectors"""
        pass
    
    @abstractmethod
    async def delete(self, ids: List[str]) -> bool:
        """Delete vectors by IDs"""
        pass
    
    @abstractmethod
    async def create_collection(
        self,
        collection_name: str,
        vector_size: int
    ) -> bool:
        """Create a new collection"""
        pass
