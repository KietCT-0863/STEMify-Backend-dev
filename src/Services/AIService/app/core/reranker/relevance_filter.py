"""
Relevance Filter
Filter documents by relevance score after reranking
"""

from typing import List, Dict, Any, Optional
import logging

from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class RelevanceFilter:
    """
    Filter documents by relevance after reranking
    
    Filters out documents that don't meet relevance thresholds
    """
    
    def __init__(
        self,
        min_rerank_score: float = None,
        min_confidence_score: float = None,
        min_combined_score: float = None,
        use_adaptive_threshold: bool = True
    ):
        """
        Initialize relevance filter
        
        Args:
            min_rerank_score: Minimum rerank score threshold (0-1)
            min_confidence_score: Minimum confidence score threshold (0-1)
            min_combined_score: Minimum combined score threshold (0-1)
            use_adaptive_threshold: If True, adapt threshold based on score distribution
        """
        self.min_rerank_score = min_rerank_score or 0.3  # Default: filter out bottom 70%
        self.min_confidence_score = min_confidence_score or settings.MIN_CONFIDENCE_SCORE
        self.min_combined_score = min_combined_score or 0.4
        self.use_adaptive_threshold = use_adaptive_threshold
    
    def filter(
        self,
        documents: List[Dict[str, Any]],
        query: str = None
    ) -> List[Dict[str, Any]]:
        """
        Filter documents by relevance
        
        Args:
            documents: List of reranked documents
            query: Original query (for logging)
        
        Returns:
            Filtered documents that meet relevance thresholds
        """
        if not documents:
            return []
        
        logger.info(f"Filtering {len(documents)} documents by relevance")
        
        # Calculate adaptive threshold if enabled
        if self.use_adaptive_threshold and len(documents) > 1:
            threshold = self._calculate_adaptive_threshold(documents)
            logger.info(f"Adaptive threshold: {threshold:.3f}")
        else:
            threshold = self.min_rerank_score
        
        # Filter documents
        filtered = []
        for doc in documents:
            # Get scores
            rerank_score = doc.get("rerank_score", 0)
            confidence_score = doc.get("confidence_score", 0)
            retrieval_score = doc.get("retrieval_score", 0)
            
            # Calculate combined score (weighted)
            combined_score = (
                rerank_score * 0.5 +
                confidence_score * 0.3 +
                retrieval_score * 0.2
            )
            
            # Apply filters
            if rerank_score < threshold:
                logger.debug(
                    f"Filtered out {doc.get('document_id', 'unknown')}: "
                    f"rerank_score={rerank_score:.3f} < {threshold:.3f}"
                )
                continue
            
            if confidence_score < self.min_confidence_score:
                logger.debug(
                    f"Filtered out {doc.get('document_id', 'unknown')}: "
                    f"confidence_score={confidence_score:.3f} < {self.min_confidence_score:.3f}"
                )
                continue
            
            if combined_score < self.min_combined_score:
                logger.debug(
                    f"Filtered out {doc.get('document_id', 'unknown')}: "
                    f"combined_score={combined_score:.3f} < {self.min_combined_score:.3f}"
                )
                continue
            
            # Add filter metadata
            doc["filtered"] = True
            doc["filter_metadata"] = {
                "rerank_score": rerank_score,
                "confidence_score": confidence_score,
                "combined_score": combined_score,
                "threshold_used": threshold
            }
            
            filtered.append(doc)
        
        logger.info(f"Filtered to {len(filtered)} relevant documents")
        return filtered
    
    def _calculate_adaptive_threshold(
        self,
        documents: List[Dict[str, Any]]
    ) -> float:
        """
        Calculate adaptive threshold based on score distribution
        
        Uses statistical methods to determine threshold:
        - If scores are well-distributed: use percentile (e.g., 30th percentile)
        - If scores are clustered: use mean - std_dev
        - If scores are very low: use fixed minimum
        """
        rerank_scores = [doc.get("rerank_score", 0) for doc in documents]
        
        if not rerank_scores:
            return self.min_rerank_score
        
        import numpy as np
        scores_array = np.array(rerank_scores)
        
        mean_score = np.mean(scores_array)
        std_score = np.std(scores_array)
        median_score = np.median(scores_array)
        
        # Strategy 1: If scores are well-distributed (std > 0.1), use percentile
        if std_score > 0.1:
            # Use 30th percentile (keep top 70%)
            threshold = np.percentile(scores_array, 30)
            logger.debug(f"Using percentile threshold: {threshold:.3f}")
        # Strategy 2: If scores are clustered, use mean - std
        elif std_score > 0.05:
            threshold = max(mean_score - std_score, self.min_rerank_score)
            logger.debug(f"Using mean-std threshold: {threshold:.3f}")
        # Strategy 3: If scores are very low/clustered, use median or fixed
        else:
            threshold = max(median_score * 0.8, self.min_rerank_score)
            logger.debug(f"Using median-based threshold: {threshold:.3f}")
        
        # Ensure threshold is reasonable
        threshold = max(self.min_rerank_score, min(threshold, 0.9))
        
        return float(threshold)
    
    def filter_by_top_percentile(
        self,
        documents: List[Dict[str, Any]],
        percentile: float = 70.0
    ) -> List[Dict[str, Any]]:
        """
        Filter to keep only top percentile of documents
        
        Args:
            documents: List of documents
            percentile: Percentile to keep (e.g., 70.0 = keep top 30%)
        
        Returns:
            Top percentile documents
        """
        if not documents:
            return []
        
        import numpy as np
        rerank_scores = [doc.get("rerank_score", 0) for doc in documents]
        
        if not rerank_scores:
            return documents
        
        threshold = np.percentile(rerank_scores, 100 - percentile)
        
        filtered = [
            doc for doc in documents
            if doc.get("rerank_score", 0) >= threshold
        ]
        
        logger.info(
            f"Filtered to top {percentile}%: {len(filtered)}/{len(documents)} documents "
            f"(threshold: {threshold:.3f})"
        )
        
        return filtered



