"""
Result Merger
Merge and deduplicate results from vector and graph retrievers
"""

from typing import List, Dict, Any, Optional
import logging
from collections import defaultdict

logger = logging.getLogger(__name__)


class ResultMerger:
    """
    Merge results from multiple retrieval sources
    
    Responsibilities:
    - Merge vector and graph results
    - Deduplicate by document_id
    - Normalize scores
    - Rank final results
    """
    
    def __init__(
        self,
        vector_weight: float = 0.6,
        graph_weight: float = 0.4,
        deduplication_threshold: float = 0.95
    ):
        """
        Initialize result merger
        
        Args:
            vector_weight: Weight for vector search scores (0-1)
            graph_weight: Weight for graph search scores (0-1)
            deduplication_threshold: Similarity threshold for deduplication
        """
        self.vector_weight = vector_weight
        self.graph_weight = graph_weight
        self.deduplication_threshold = deduplication_threshold
        
        # Ensure weights sum to 1
        total_weight = vector_weight + graph_weight
        if total_weight != 1.0:
            logger.warning(f"Weights sum to {total_weight}, normalizing...")
            self.vector_weight = vector_weight / total_weight
            self.graph_weight = graph_weight / total_weight
    
    def merge(
        self,
        vector_results: List[Dict[str, Any]],
        graph_results: List[Dict[str, Any]],
        top_k: Optional[int] = None
    ) -> List[Dict[str, Any]]:
        """
        Merge vector and graph results
        
        Args:
            vector_results: Results from vector retriever
            graph_results: Results from graph retriever
            top_k: Maximum number of results to return
        
        Returns:
            Merged and ranked results
        """
        logger.info(f"Merging {len(vector_results)} vector + {len(graph_results)} graph results")
        
        # Step 1: Normalize scores
        normalized_vector = self._normalize_scores(vector_results, "vector")
        normalized_graph = self._normalize_scores(graph_results, "graph")
        
        # Step 2: Create document map for deduplication
        document_map: Dict[str, Dict[str, Any]] = {}
        
        # Add vector results
        for result in normalized_vector:
            doc_id = result.get("document_id", "unknown")
            if doc_id not in document_map:
                document_map[doc_id] = {
                    "document_id": doc_id,
                    "content": result.get("content", ""),
                    "metadata": result.get("metadata", {}),
                    "sources": [],
                    "scores": {},
                    "provenance": {}
                }
            
            # Add vector source
            document_map[doc_id]["sources"].append("vector")
            document_map[doc_id]["scores"]["vector"] = result.get("retrieval_score", 0)
            document_map[doc_id]["provenance"]["vector"] = result.get("provenance", {})
            
            # Merge content if different
            if result.get("content") and result["content"] not in document_map[doc_id]["content"]:
                document_map[doc_id]["content"] += f"\n{result['content']}"
        
        # Add graph results
        for result in normalized_graph:
            doc_id = result.get("document_id", "unknown")
            
            if doc_id not in document_map:
                # New document from graph
                document_map[doc_id] = {
                    "document_id": doc_id,
                    "content": result.get("content", ""),
                    "metadata": result.get("metadata", {}),
                    "sources": [],
                    "scores": {},
                    "provenance": {}
                }
            
            # Add graph source
            document_map[doc_id]["sources"].append("graph")
            document_map[doc_id]["scores"]["graph"] = result.get("retrieval_score", 0)
            document_map[doc_id]["provenance"]["graph"] = result.get("provenance", {})
            
            # Merge content if different
            if result.get("content") and result["content"] not in document_map[doc_id]["content"]:
                document_map[doc_id]["content"] += f"\n{result['content']}"
        
        # Step 3: Calculate combined scores
        merged_results = []
        for doc_id, doc_data in document_map.items():
            # Calculate weighted score
            combined_score = self._calculate_combined_score(
                doc_data["scores"],
                doc_data["sources"]
            )
            
            # Calculate confidence (average of all confidence scores)
            confidence_scores = []
            if "vector" in doc_data["provenance"]:
                vector_conf = doc_data["provenance"]["vector"].get("confidence_score")
                if vector_conf:
                    confidence_scores.append(vector_conf)
            if "graph" in doc_data["provenance"]:
                graph_conf = doc_data["provenance"]["graph"].get("confidence_score")
                if graph_conf:
                    confidence_scores.append(graph_conf)
            
            avg_confidence = (
                sum(confidence_scores) / len(confidence_scores)
                if confidence_scores
                else combined_score
            )
            
            # Build merged result
            merged_result = {
                "document_id": doc_id,
                "content": doc_data["content"],
                "metadata": doc_data["metadata"],
                "retrieval_sources": doc_data["sources"],
                "retrieval_score": combined_score,
                "confidence_score": avg_confidence,
                "provenance": {
                    "merged_at": self._get_timestamp(),
                    "sources": doc_data["sources"],
                    "vector_provenance": doc_data["provenance"].get("vector", {}),
                    "graph_provenance": doc_data["provenance"].get("graph", {}),
                    "score_breakdown": doc_data["scores"]
                }
            }
            
            merged_results.append(merged_result)
        
        # Step 4: Sort by combined score
        merged_results.sort(key=lambda x: x["retrieval_score"], reverse=True)
        
        # Step 5: Apply top_k if specified
        if top_k is not None:
            merged_results = merged_results[:top_k]
        
        logger.info(f"Merged to {len(merged_results)} unique documents")
        return merged_results
    
    def _normalize_scores(
        self,
        results: List[Dict[str, Any]],
        source: str
    ) -> List[Dict[str, Any]]:
        """
        Normalize scores to [0, 1] range
        
        Uses min-max normalization
        """
        if not results:
            return []
        
        scores = [r.get("retrieval_score", 0) for r in results]
        
        if not scores:
            return results
        
        min_score = min(scores)
        max_score = max(scores)
        
        # Avoid division by zero
        if max_score == min_score:
            normalized = [1.0] * len(scores)
        else:
            normalized = [
                (score - min_score) / (max_score - min_score)
                for score in scores
            ]
        
        # Update results with normalized scores
        normalized_results = []
        for i, result in enumerate(results):
            result_copy = result.copy()
            result_copy["retrieval_score"] = normalized[i]
            result_copy["original_score"] = scores[i]  # Keep original for reference
            normalized_results.append(result_copy)
        
        return normalized_results
    
    def _calculate_combined_score(
        self,
        scores: Dict[str, float],
        sources: List[str]
    ) -> float:
        """
        Calculate weighted combined score
        
        Args:
            scores: Dictionary of source -> score
            sources: List of sources for this document
        
        Returns:
            Combined score [0, 1]
        """
        combined = 0.0
        
        if "vector" in sources and "vector" in scores:
            combined += self.vector_weight * scores["vector"]
        
        if "graph" in sources and "graph" in scores:
            combined += self.graph_weight * scores["graph"]
        
        # If document appears in both sources, boost score slightly
        if len(sources) > 1:
            combined *= 1.1  # 10% boost for multi-source matches
            combined = min(1.0, combined)  # Cap at 1.0
        
        return combined
    
    def _get_timestamp(self) -> str:
        """Get current timestamp"""
        from datetime import datetime
        return datetime.utcnow().isoformat()

