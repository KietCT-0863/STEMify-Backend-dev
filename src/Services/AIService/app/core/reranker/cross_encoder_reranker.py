"""
Cross-Encoder Reranker
Local reranker using cross-encoder model
"""

from typing import List, Dict, Any
import logging
import numpy as np

try:
    from sentence_transformers import CrossEncoder
except ImportError:
    CrossEncoder = None

from app.core.reranker.base_reranker import BaseReranker
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class CrossEncoderReranker(BaseReranker):
    """Local cross-encoder reranker"""
    
    def __init__(self, model_name: str = None, device: str = None):
        if CrossEncoder is None:
            raise ImportError(
                "sentence-transformers package not installed. "
                "Install with: pip install sentence-transformers"
            )
        
        # Default model: ms-marco-MiniLM for English, or multilingual
        self.model_name = model_name or settings.RERANKER_CROSS_ENCODER_MODEL
        self.device = device or settings.EMBEDDING_DEVICE
        
        logger.info(f"Loading cross-encoder model: {self.model_name}")
        try:
            self.model = CrossEncoder(self.model_name, device=self.device)
            logger.info(f"Loaded cross-encoder model on {self.device}")
        except Exception as e:
            logger.error(f"Error loading cross-encoder model: {e}")
            raise
    
    async def rerank(
        self,
        query: str,
        documents: List[Dict[str, Any]],
        top_k: int = 5
    ) -> List[Dict[str, Any]]:
        """
        Re-rank documents using cross-encoder model
        
        Args:
            query: Natural language query
            documents: List of documents to rerank
            top_k: Number of top documents to return
        
        Returns:
            Re-ranked documents
        """
        if not documents:
            return []
        
        logger.info(f"Reranking {len(documents)} documents with cross-encoder")
        
        try:
            # Prepare query-document pairs
            doc_texts = [self._extract_content(doc) for doc in documents]
            pairs = [[query, doc_text] for doc_text in doc_texts]
            
            # Run inference (cross-encoder is synchronous)
            import asyncio
            loop = asyncio.get_event_loop()
            scores = await loop.run_in_executor(
                None,
                lambda: self.model.predict(pairs)
            )
            
            # Convert to list if numpy array
            if isinstance(scores, np.ndarray):
                scores = scores.tolist()
            
            # Create list of (index, score, document)
            scored_docs = list(zip(range(len(documents)), scores, documents))
            
            # Sort by score (descending)
            scored_docs.sort(key=lambda x: x[1], reverse=True)
            
            # Normalize scores to [0, 1] range using min-max normalization
            # This gives better interpretability than sigmoid
            raw_scores = [s[1] for s in scored_docs[:top_k]]
            if raw_scores:
                min_score = min(raw_scores)
                max_score = max(raw_scores)
                score_range = max_score - min_score
                
                # Avoid division by zero
                if score_range == 0:
                    normalized_scores = [1.0] * len(raw_scores)
                else:
                    normalized_scores = [
                        (score - min_score) / score_range
                        for score in raw_scores
                    ]
            else:
                normalized_scores = []
            
            # Build reranked results
            reranked_docs = []
            for rank, ((original_index, raw_score, original_doc), normalized_score) in enumerate(
                zip(scored_docs[:top_k], normalized_scores)
            ):
                # Add rerank metadata
                reranked_doc = self._add_rerank_metadata(
                    original_doc,
                    normalized_score,
                    rank + 1
                )
                
                # Add raw score to provenance for debugging
                if "provenance" in reranked_doc:
                    reranked_doc["provenance"]["rerank_raw_score"] = float(raw_score)
                
                reranked_docs.append(reranked_doc)
            
            logger.info(f"Reranked to {len(reranked_docs)} documents")
            return reranked_docs
            
        except Exception as e:
            logger.error(f"Error in cross-encoder reranking: {e}", exc_info=True)
            # Fallback: return original documents sorted by retrieval score
            logger.warning("Falling back to original order")
            sorted_docs = sorted(
                documents,
                key=lambda x: x.get("retrieval_score", 0),
                reverse=True
            )
            return sorted_docs[:top_k]

