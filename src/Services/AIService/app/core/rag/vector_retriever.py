"""
Vector Retriever
Retrieval from vector store (Qdrant) with query embedding
"""

from typing import List, Dict, Any, Optional
import logging

from app.core.vector_store import VectorStoreClient
from app.core.embedding.pipeline import EmbeddingPipeline
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class VectorRetriever:
    """
    Vector-based retriever using Qdrant
    
    Responsibilities:
    - Generate query embeddings
    - Search in vector store
    - Format results with provenance
    """
    
    def __init__(
        self,
        vector_store: VectorStoreClient,
        embedding_pipeline: EmbeddingPipeline
    ):
        self.vector_store = vector_store
        self.embedding_pipeline = embedding_pipeline
    
    async def retrieve(
        self,
        query: str,
        top_k: int = None,
        filters: Optional[Dict[str, Any]] = None,
        min_score: float = None
    ) -> List[Dict[str, Any]]:
        """
        Retrieve documents from vector store
        
        Args:
            query: Natural language query
            top_k: Number of results to return (default from settings)
            filters: Metadata filters (e.g., {"classroom_id": "123"})
            min_score: Minimum similarity score threshold
        
        Returns:
            List of retrieved documents with scores and provenance
        """
        if top_k is None:
            top_k = settings.VECTOR_SEARCH_TOP_K
        
        if min_score is None:
            min_score = settings.MIN_CONFIDENCE_SCORE
        
        logger.info(f"Vector retrieval: query='{query[:50]}...', top_k={top_k}, filters={filters}")
        
        try:
            # Step 1: Generate query embedding
            query_embedding = self._generate_query_embedding(query)
            
            # Step 2: Search in vector store
            results = await self.vector_store.search(
                query_vector=query_embedding,
                top_k=top_k,
                filters=filters
            )
            
            # Step 3: Format results with provenance
            formatted_results = self._format_results(results, query, min_score)
            
            logger.info(f"Retrieved {len(formatted_results)} documents from vector store")
            return formatted_results
            
        except Exception as e:
            logger.error(f"Error in vector retrieval: {e}", exc_info=True)
            return []
    
    def _generate_query_embedding(self, query: str) -> List[float]:
        """Generate embedding for query text"""
        # Use embedding pipeline to generate query embedding
        query_doc = {"content": query}
        query_docs = self.embedding_pipeline.generate_embeddings(
            [query_doc],
            update_confidence=False  # Don't update confidence for queries
        )
        
        if not query_docs or "embedding" not in query_docs[0]:
            raise ValueError("Failed to generate query embedding")
        
        return query_docs[0]["embedding"]
    
    def _format_results(
        self,
        results: List[Dict[str, Any]],
        query: str,
        min_score: float
    ) -> List[Dict[str, Any]]:
        """
        Format vector search results with provenance
        
        Adds:
        - retrieval_source: "vector"
        - retrieval_score: similarity score
        - retrieval_query: original query
        - confidence_score: from document metadata
        """
        formatted = []
        
        for result in results:
            # Extract score and payload
            score = result.get("score", 0.0)
            payload = result.get("payload", {})
            
            # Filter by minimum score
            if score < min_score:
                continue
            
            # Extract document info
            document_id = payload.get("document_id", result.get("id", "unknown"))
            content = payload.get("content", "")
            metadata = {k: v for k, v in payload.items() if k != "content"}
            
            # Get confidence score from metadata or use similarity score
            confidence_score = metadata.get("confidence_score", score)
            
            # Build formatted result
            formatted_result = {
                "document_id": document_id,
                "content": content,
                "metadata": metadata,
                "retrieval_source": "vector",
                "retrieval_score": float(score),
                "retrieval_query": query,
                "confidence_score": float(confidence_score),
                "provenance": {
                    **metadata.get("provenance", {}),
                    "retrieval_method": "vector_search",
                    "retrieval_timestamp": self._get_timestamp(),
                    "similarity_score": float(score)
                }
            }
            
            formatted.append(formatted_result)
        
        # Sort by retrieval score (descending)
        formatted.sort(key=lambda x: x["retrieval_score"], reverse=True)
        
        return formatted
    
    def _get_timestamp(self) -> str:
        """Get current timestamp"""
        from datetime import datetime
        return datetime.utcnow().isoformat()

