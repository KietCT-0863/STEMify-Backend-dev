"""
Reasoning Engine Factory
Factory function to create orchestrator with existing services
"""

from app.core.reasoning.orchestrator import GraphReasoningOrchestrator
from app.core.reasoning.tool_implementations import (
    GraphToolImpl,
    VectorToolImpl,
    RerankToolImpl,
    MathToolImpl,
    ClockToolImpl
)
from app.core.graph.client import GraphClient
from app.core.vector_store.providers.qdrant_provider import QdrantProvider
from app.core.embedding.pipeline import EmbeddingPipeline
from app.core.reranker import create_reranker

import logging

logger = logging.getLogger(__name__)


def create_reasoning_orchestrator(
    graph_client: GraphClient = None,
    qdrant_provider: QdrantProvider = None,
    embedding_pipeline: EmbeddingPipeline = None
) -> GraphReasoningOrchestrator:
    """
    Create a Graph Reasoning Orchestrator with default or provided services
    
    Args:
        graph_client: Optional GraphClient instance (creates new if None)
        qdrant_provider: Optional QdrantProvider instance (creates new if None)
        embedding_pipeline: Optional EmbeddingPipeline instance (creates new if None)
    
    Returns:
        Configured GraphReasoningOrchestrator instance
    """
    # Create services if not provided
    if graph_client is None:
        graph_client = GraphClient()
    
    if qdrant_provider is None:
        qdrant_provider = QdrantProvider()
    
    if embedding_pipeline is None:
        from app.core.embedding.pipeline import get_embedding_pipeline
        embedding_pipeline = get_embedding_pipeline()
    
    # Create tool implementations
    graph_tool = GraphToolImpl(graph_client)
    vector_tool = VectorToolImpl(qdrant_provider, embedding_pipeline)
    rerank_tool = RerankToolImpl(create_reranker())
    math_tool = MathToolImpl()
    clock_tool = ClockToolImpl()
    
    # Create orchestrator
    orchestrator = GraphReasoningOrchestrator(
        graph_tool=graph_tool,
        vector_tool=vector_tool,
        rerank_tool=rerank_tool,
        math_tool=math_tool,
        clock_tool=clock_tool
    )
    
    logger.info("Created Graph Reasoning Orchestrator")
    return orchestrator

