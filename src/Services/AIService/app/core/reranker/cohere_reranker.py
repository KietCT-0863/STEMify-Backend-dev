"""
Cohere Reranker
Rerank using Cohere Rerank API
"""

from typing import List, Dict, Any
import logging

try:
    import cohere
except ImportError:
    cohere = None

from app.core.reranker.base_reranker import BaseReranker
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class CohereReranker(BaseReranker):
    """Cohere API-based reranker"""
    
    def __init__(self, api_key: str = None, model: str = None):
        if cohere is None:
            raise ImportError(
                "cohere package not installed. Install with: pip install cohere"
            )
        
        self.api_key = api_key or settings.COHERE_API_KEY
        if not self.api_key:
            raise ValueError("Cohere API key is required")
        
        self.model = model or settings.RERANKER_MODEL
        self.client = cohere.Client(api_key=self.api_key)
        logger.info(f"Initialized Cohere reranker with model: {self.model}")
    
    async def rerank(
        self,
        query: str,
        documents: List[Dict[str, Any]],
        top_k: int = 5
    ) -> List[Dict[str, Any]]:
        """
        Re-rank documents using Cohere API
        
        Args:
            query: Natural language query
            documents: List of documents to rerank
            top_k: Number of top documents to return
        
        Returns:
            Re-ranked documents
        """
        if not documents:
            return []
        
        logger.info(f"Reranking {len(documents)} documents with Cohere")
        
        try:
            # Extract content from documents
            doc_texts = [self._extract_content(doc) for doc in documents]
            
            # Call Cohere rerank API
            # Note: Cohere client is synchronous, so we run it in executor
            import asyncio
            loop = asyncio.get_event_loop()
            response = await loop.run_in_executor(
                None,
                lambda: self.client.rerank(
                    model=self.model,
                    query=query,
                    documents=doc_texts,
                    top_n=min(top_k, len(documents))
                )
            )
            
            # Map results back to documents
            reranked_docs = []
            for i, result in enumerate(response.results):
                original_index = result.index
                original_doc = documents[original_index]
                
                # Cohere returns relevance score (0-1)
                rerank_score = result.relevance_score
                
                # Add rerank metadata
                reranked_doc = self._add_rerank_metadata(
                    original_doc,
                    rerank_score,
                    i + 1
                )
                
                reranked_docs.append(reranked_doc)
            
            logger.info(f"Reranked to {len(reranked_docs)} documents")
            return reranked_docs
            
        except Exception as e:
            logger.error(f"Error in Cohere reranking: {e}", exc_info=True)
            # Fallback: return original documents sorted by retrieval score
            logger.warning("Falling back to original order")
            sorted_docs = sorted(
                documents,
                key=lambda x: x.get("retrieval_score", 0),
                reverse=True
            )
            return sorted_docs[:top_k]

