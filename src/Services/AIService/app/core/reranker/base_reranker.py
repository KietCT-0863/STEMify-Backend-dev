"""
Base Reranker
Abstract base class for rerankers
"""

from abc import ABC, abstractmethod
from typing import List, Dict, Any


class BaseReranker(ABC):
    """Abstract interface for rerankers"""
    
    @abstractmethod
    async def rerank(
        self,
        query: str,
        documents: List[Dict[str, Any]],
        top_k: int = 5
    ) -> List[Dict[str, Any]]:
        """
        Re-rank documents by relevance to query
        
        Args:
            query: Natural language query
            documents: List of documents to rerank
            top_k: Number of top documents to return
        
        Returns:
            Re-ranked documents with relevance scores
        """
        pass
    
    def _extract_content(self, document: Dict[str, Any]) -> str:
        """Extract content from document for reranking"""
        return document.get("content", "")
    
    def _add_rerank_metadata(
        self,
        document: Dict[str, Any],
        rerank_score: float,
        rerank_rank: int
    ) -> Dict[str, Any]:
        """Add reranking metadata to document"""
        result = document.copy()
        
        # Add rerank metadata
        if "provenance" not in result:
            result["provenance"] = {}
        
        result["provenance"]["reranked"] = True
        result["provenance"]["rerank_score"] = float(rerank_score)
        result["provenance"]["rerank_rank"] = rerank_rank
        result["provenance"]["rerank_timestamp"] = self._get_timestamp()
        
        # Update retrieval score with rerank score
        # Combine original retrieval score with rerank score
        original_score = result.get("retrieval_score", 0.0)
        result["retrieval_score"] = (original_score * 0.3) + (rerank_score * 0.7)
        result["rerank_score"] = float(rerank_score)
        
        return result
    
    def _get_timestamp(self) -> str:
        """Get current timestamp"""
        from datetime import datetime
        return datetime.utcnow().isoformat()

