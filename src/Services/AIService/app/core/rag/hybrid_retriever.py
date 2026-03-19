"""
Hybrid Retriever
Orchestrator for hybrid retrieval (vector + graph)
"""

from typing import List, Dict, Any, Optional
import logging

from app.core.rag.vector_retriever import VectorRetriever
from app.core.graph.retriever import GraphRetriever
from app.core.rag.result_merger import ResultMerger
from app.core.reranker import BaseReranker, create_reranker, RelevanceFilter
from app.core.rag.streaming_hooks import StreamingRAGRouter
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class HybridRetriever:
    """
    Hybrid retriever combining vector and graph search
    
    Responsibilities:
    - Orchestrate vector and graph retrieval
    - Merge results
    - Return ranked documents
    """
    
    def __init__(
        self,
        vector_retriever: VectorRetriever,
        graph_retriever: GraphRetriever,
        result_merger: Optional[ResultMerger] = None,
        reranker: Optional[BaseReranker] = None,
        relevance_filter: Optional[RelevanceFilter] = None,
        streaming_router: Optional[StreamingRAGRouter] = None,
    ):
        self.vector_retriever = vector_retriever
        self.graph_retriever = graph_retriever
        self.result_merger = result_merger or ResultMerger(
            vector_weight=0.6,
            graph_weight=0.4,
        )
        self.reranker = reranker or create_reranker()
        self.relevance_filter = relevance_filter
        if relevance_filter is None and settings.ENABLE_RELEVANCE_FILTER:
            self.relevance_filter = RelevanceFilter(
                min_rerank_score=settings.MIN_RERANK_SCORE,
                min_combined_score=settings.MIN_COMBINED_SCORE,
                use_adaptive_threshold=settings.USE_ADAPTIVE_THRESHOLD,
            )
        self.streaming_router = streaming_router
    
    async def retrieve(
        self,
        query: str,
        top_k: Optional[int] = None,
        filters: Optional[Dict[str, Any]] = None,
        use_graph: bool = True,
        use_vector: bool = True,
        max_depth: int = None,
        context_bundle: Optional[Dict[str, Any]] = None,
    ) -> List[Dict[str, Any]]:
        """
        Hybrid retrieval from vector and graph
        
        Args:
            query: Natural language query
            top_k: Maximum number of results to return
            filters: Metadata filters for vector search
            use_graph: Whether to use graph retrieval
            use_vector: Whether to use vector retrieval
            max_depth: Maximum graph traversal depth
        
        Returns:
            Merged and ranked results
        """
        if top_k is None:
            top_k = settings.VECTOR_SEARCH_TOP_K
        
        logger.info("Hybrid retrieval: query='%s...', top_k=%s", query[:50], top_k)

        # Optional: log routing mode for Streaming RAG (no behavior change yet)
        if self.streaming_router:
            has_fresh_chunks = settings.STREAMING_RAG_PREFER_FRESH
            mode = self.streaming_router.route(has_fresh_chunks=has_fresh_chunks)
            logger.info(
                "[StreamingRAGRouter] mode=%s, has_fresh_chunks=%s",
                mode,
                has_fresh_chunks,
            )
        
        vector_results = []
        graph_results = []
        
        # Step 1: Vector retrieval
        if use_vector:
            try:
                vector_results = await self.vector_retriever.retrieve(
                    query=query,
                    top_k=top_k * 2,  # Get more for merging
                    filters=filters
                )
                logger.info(f"Vector retrieval: {len(vector_results)} results")
            except Exception as e:
                logger.error(f"Error in vector retrieval: {e}", exc_info=True)
        
        # Step 2: Graph retrieval
        if use_graph:
            try:
                graph_results = await self.graph_retriever.retrieve(
                    query=query,
                    max_depth=max_depth,
                    limit=top_k * 2  # Get more for merging
                )
                logger.info(f"Graph retrieval: {len(graph_results)} results")
            except Exception as e:
                logger.error(f"Error in graph retrieval: {e}", exc_info=True)
        
        # Step 3: Merge results
        if not vector_results and not graph_results:
            logger.warning("No results from either retrieval method")
            return []
        
        merged_results = self.result_merger.merge(
            vector_results=vector_results,
            graph_results=graph_results,
            top_k=top_k * 2 if self.reranker else top_k  # Get more for reranking
        )
        
        # Step 4: Rerank if reranker is available
        final_results: List[Dict[str, Any]] = []

        if self.reranker and merged_results:
            try:
                logger.info(f"Reranking {len(merged_results)} documents")
                reranked_results = await self.reranker.rerank(
                    query=query,
                    documents=merged_results,
                    top_k=top_k * 2  # Get more for filtering
                )
                logger.info(f"Reranked to {len(reranked_results)} documents")
                
                # Step 5: Filter by relevance if filter is enabled
                if self.relevance_filter and reranked_results:
                    try:
                        filtered_results = self.relevance_filter.filter(
                            documents=reranked_results,
                            query=query
                        )
                        logger.info(f"Filtered to {len(filtered_results)} relevant documents")
                        final_results = filtered_results[:top_k]
                    except Exception as e:
                        logger.error(f"Error in relevance filtering: {e}", exc_info=True)
                        logger.warning("Returning reranked results without filtering")
                        final_results = reranked_results[:top_k]
                else:
                    final_results = reranked_results[:top_k]
            except Exception as e:
                logger.error(f"Error in reranking: {e}", exc_info=True)
                logger.warning("Returning merged results without reranking")
                final_results = merged_results[:top_k]
        else:
            logger.info(f"Hybrid retrieval complete: {len(merged_results)} merged results")
            final_results = merged_results[:top_k] if len(merged_results) > top_k else merged_results

        # Attach context bundle reference (optional)
        if context_bundle is not None:
            for doc in final_results:
                doc["context_bundle"] = context_bundle

        return final_results
    
    async def retrieve_vector_only(
        self,
        query: str,
        top_k: Optional[int] = None,
        filters: Optional[Dict[str, Any]] = None
    ) -> List[Dict[str, Any]]:
        """Retrieve only from vector store"""
        return await self.retrieve(
            query=query,
            top_k=top_k,
            filters=filters,
            use_graph=False,
            use_vector=True
        )
    
    async def retrieve_graph_only(
        self,
        query: str,
        top_k: Optional[int] = None,
        max_depth: int = None
    ) -> List[Dict[str, Any]]:
        """Retrieve only from graph"""
        return await self.retrieve(
            query=query,
            top_k=top_k,
            use_graph=True,
            use_vector=False,
            max_depth=max_depth
        )

