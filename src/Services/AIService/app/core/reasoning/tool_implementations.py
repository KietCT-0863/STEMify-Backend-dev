"""
Reasoning Engine Tool Implementations
Concrete implementations of tool interfaces using existing services
"""

from typing import List, Dict, Any, Optional
from datetime import datetime
import statistics
import logging

from app.core.reasoning.tools import (
    GraphTool,
    VectorTool,
    RerankTool,
    MathTool,
    ClockTool
)
from app.core.graph.client import GraphClient
from app.core.vector_store.providers.qdrant_provider import QdrantProvider
from app.core.embedding.pipeline import EmbeddingPipeline
from app.core.reranker import BaseReranker, create_reranker

logger = logging.getLogger(__name__)


class GraphToolImpl(GraphTool):
    """Graph tool implementation using GraphClient"""
    
    def __init__(self, graph_client: GraphClient):
        self.graph_client = graph_client
    
    async def query(self, cypher: str, parameters: Optional[Dict[str, Any]] = None) -> List[Dict[str, Any]]:
        """Execute Cypher query"""
        try:
            return await self.graph_client.query_cypher(cypher, parameters or {})
        except Exception as e:
            logger.error(f"Graph query error: {e}", exc_info=True)
            return []


class VectorToolImpl(VectorTool):
    """Vector tool implementation using QdrantProvider"""
    
    def __init__(self, qdrant_provider: QdrantProvider, embedding_pipeline: EmbeddingPipeline):
        self.qdrant_provider = qdrant_provider
        self.embedding_pipeline = embedding_pipeline
    
    async def search(
        self,
        query: str,
        top_k: int,
        filters: Optional[Dict[str, Any]] = None
    ) -> List[Dict[str, Any]]:
        """Search for similar vectors"""
        try:
            # Generate embedding for query
            query_doc = {"content": query}
            query_docs = self.embedding_pipeline.generate_embeddings(
                [query_doc],
                update_confidence=False
            )
            
            if not query_docs or "embedding" not in query_docs[0]:
                logger.error("Failed to generate query embedding")
                return []
            
            embedding = query_docs[0]["embedding"]
            
            # Search in Qdrant
            results = await self.qdrant_provider.search(
                query_vector=embedding,
                top_k=top_k,
                filters=filters
            )
            
            return results
        except Exception as e:
            logger.error(f"Vector search error: {e}", exc_info=True)
            return []


class RerankToolImpl(RerankTool):
    """Rerank tool implementation using BaseReranker"""
    
    def __init__(self, reranker: Optional[BaseReranker] = None):
        self.reranker = reranker or create_reranker()
    
    async def rerank(
        self,
        entries: List[Dict[str, Any]],
        query: str,
        top_k: int
    ) -> List[Dict[str, Any]]:
        """Rerank entries by relevance"""
        try:
            # Convert entries to document format
            documents = []
            for entry in entries:
                doc = {
                    "content": entry.get("text", ""),
                    "metadata": entry.get("meta", {}),
                    "retrieval_score": entry.get("score", 0.0)
                }
                documents.append(doc)
            
            # Rerank
            reranked = await self.reranker.rerank(
                query=query,
                documents=documents,
                top_k=top_k
            )
            
            # Convert back to entry format
            results = []
            for doc in reranked:
                result = {
                    "text": doc.get("content", ""),
                    "meta": doc.get("metadata", {}),
                    "score": doc.get("rerank_score", doc.get("retrieval_score", 0.0))
                }
                results.append(result)
            
            return results
        except Exception as e:
            logger.error(f"Rerank error: {e}", exc_info=True)
            return entries[:top_k]  # Return original entries if reranking fails


class MathToolImpl(MathTool):
    """Math tool implementation for statistics"""
    
    def stats(self, series: List[float]) -> Dict[str, float]:
        """Calculate statistics on a series"""
        try:
            if not series:
                return {"mean": 0.0, "trend": 0.0, "std": 0.0}
            
            mean = statistics.mean(series)
            std = statistics.stdev(series) if len(series) > 1 else 0.0
            
            # Calculate trend (simple linear regression slope)
            trend = 0.0
            if len(series) > 1:
                n = len(series)
                x_mean = (n - 1) / 2
                y_mean = mean
                
                numerator = sum((i - x_mean) * (series[i] - y_mean) for i in range(n))
                denominator = sum((i - x_mean) ** 2 for i in range(n))
                
                if denominator != 0:
                    trend = numerator / denominator
            
            return {
                "mean": float(mean),
                "std": float(std),
                "trend": float(trend),
                "min": float(min(series)),
                "max": float(max(series)),
                "median": float(statistics.median(series))
            }
        except Exception as e:
            logger.error(f"Stats calculation error: {e}", exc_info=True)
            return {"mean": 0.0, "trend": 0.0, "std": 0.0}
    
    def correlation(self, series1: List[float], series2: List[float]) -> float:
        """Calculate correlation between two series"""
        try:
            if len(series1) != len(series2) or len(series1) < 2:
                return 0.0
            
            return float(statistics.correlation(series1, series2))
        except Exception as e:
            logger.error(f"Correlation calculation error: {e}", exc_info=True)
            return 0.0


class ClockToolImpl(ClockTool):
    """Clock tool implementation"""
    
    def now(self) -> str:
        """Get current timestamp in ISO8601 format"""
        return datetime.utcnow().isoformat()

