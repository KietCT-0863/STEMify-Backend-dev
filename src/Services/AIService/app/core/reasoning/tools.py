"""
Reasoning Engine Tools Interface
Abstract interfaces for tools used by minions
"""

from abc import ABC, abstractmethod
from typing import List, Dict, Any, Optional


class GraphTool(ABC):
    """Interface for graph database operations"""
    
    @abstractmethod
    async def query(self, cypher: str, parameters: Optional[Dict[str, Any]] = None) -> List[Dict[str, Any]]:
        """
        Execute a Cypher query
        
        Args:
            cypher: Cypher query string
            parameters: Query parameters
            
        Returns:
            List of records (each record is a dict)
        """
        pass


class VectorTool(ABC):
    """Interface for vector database operations"""
    
    @abstractmethod
    async def search(
        self,
        query: str,
        top_k: int,
        filters: Optional[Dict[str, Any]] = None
    ) -> List[Dict[str, Any]]:
        """
        Search for similar vectors
        
        Args:
            query: Natural language query
            top_k: Number of results to return
            filters: Optional metadata filters
            
        Returns:
            List of results with content, score, payload
        """
        pass


class RerankTool(ABC):
    """Interface for reranking operations"""
    
    @abstractmethod
    async def rerank(
        self,
        entries: List[Dict[str, Any]],
        query: str,
        top_k: int
    ) -> List[Dict[str, Any]]:
        """
        Rerank entries by relevance to query
        
        Args:
            entries: List of entries with 'text' and 'meta' keys
            query: Query string
            top_k: Number of top results to return
            
        Returns:
            Reranked entries with scores
        """
        pass


class MathTool(ABC):
    """Interface for statistical operations"""
    
    @abstractmethod
    def stats(self, series: List[float]) -> Dict[str, float]:
        """
        Calculate statistics on a series
        
        Args:
            series: List of numeric values
            
        Returns:
            Dict with 'corr' (if multiple series), 'mean', 'trend', etc.
        """
        pass


class ClockTool(ABC):
    """Interface for time operations"""
    
    @abstractmethod
    def now(self) -> str:
        """
        Get current timestamp
        
        Returns:
            ISO8601 formatted timestamp string
        """
        pass





