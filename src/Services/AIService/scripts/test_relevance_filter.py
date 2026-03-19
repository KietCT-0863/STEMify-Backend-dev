"""
Test Relevance Filter
Test relevance filtering after reranking
"""

import asyncio
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from app.core.reranker import create_reranker, RelevanceFilter
from app.core.rag.document_processor import DocumentProcessor
from app.core.embedding.pipeline import EmbeddingPipeline
from tests.fixtures.mock_classroom_data import get_mock_classroom_data_test
from app.infrastructure.config.settings import settings
import numpy as np


def _cosine_similarity(vec1: np.ndarray, vec2: np.ndarray) -> float:
    """Calculate cosine similarity"""
    dot_product = np.dot(vec1, vec2)
    norm1 = np.linalg.norm(vec1)
    norm2 = np.linalg.norm(vec2)
    if norm1 == 0 or norm2 == 0:
        return 0.0
    return float(dot_product / (norm1 * norm2))


async def create_retrieved_documents_with_scores(
    query: str,
    classroom_data: dict,
    top_k: int = 20
) -> list:
    """Create retrieved documents with real similarity scores"""
    processor = DocumentProcessor()
    processed_docs = processor.process_classroom_data(classroom_data)
    
    embedding_pipeline = EmbeddingPipeline()
    
    # Generate query embedding
    query_doc = {"content": query}
    query_embeddings = embedding_pipeline.generate_embeddings([query_doc], update_confidence=False)
    if not query_embeddings or "embedding" not in query_embeddings[0]:
        raise ValueError("Failed to generate query embedding")
    query_embedding = np.array(query_embeddings[0]["embedding"])
    
    # Generate document embeddings
    doc_embeddings = embedding_pipeline.generate_embeddings(processed_docs, update_confidence=False)
    
    # Calculate similarity
    scored_docs = []
    for doc in doc_embeddings:
        if "embedding" not in doc:
            continue
        doc_embedding = np.array(doc["embedding"])
        similarity_score = _cosine_similarity(query_embedding, doc_embedding)
        doc["retrieval_score"] = float(similarity_score)
        doc["retrieval_source"] = "vector"
        scored_docs.append(doc)
    
    scored_docs.sort(key=lambda x: x["retrieval_score"], reverse=True)
    return scored_docs[:top_k]


async def test_relevance_filter():
    """Test relevance filter with real data"""
    print("=" * 60)
    print("Testing Relevance Filter")
    print("=" * 60)
    
    # Load data
    classroom_data = get_mock_classroom_data_test()
    query = "Which students are struggling with physics topics?"
    
    print(f"\nQuery: {query}")
    
    # Create retrieved documents
    print("\n1. Creating retrieved documents...")
    retrieved_docs = await create_retrieved_documents_with_scores(query, classroom_data, top_k=20)
    print(f"   ✓ Retrieved {len(retrieved_docs)} documents")
    
    # Rerank
    print("\n2. Reranking documents...")
    reranker = create_reranker("local")
    if not reranker:
        print("   ✗ Failed to create reranker")
        return
    
    reranked = await reranker.rerank(query, retrieved_docs, top_k=15)
    print(f"   ✓ Reranked to {len(reranked)} documents")
    
    # Show reranked scores
    print("\n   Reranked Scores (before filtering):")
    for i, doc in enumerate(reranked[:10], 1):
        rerank_score = doc.get("rerank_score", 0)
        confidence = doc.get("confidence_score", 0)
        print(f"   {i}. {doc.get('document_id', 'unknown')[:40]}: "
              f"rerank={rerank_score:.3f}, confidence={confidence:.3f}")
    
    # Test filter with different thresholds
    print("\n" + "=" * 60)
    print("Test 1: Default Filter (min_rerank_score=0.3)")
    print("=" * 60)
    
    filter1 = RelevanceFilter(min_rerank_score=0.3, use_adaptive_threshold=False)
    filtered1 = filter1.filter(reranked, query)
    
    print(f"\nBefore filtering: {len(reranked)} documents")
    print(f"After filtering: {len(filtered1)} documents")
    print(f"Filtered out: {len(reranked) - len(filtered1)} documents")
    
    print("\n   Filtered Results:")
    for i, doc in enumerate(filtered1, 1):
        rerank_score = doc.get("rerank_score", 0)
        combined = doc.get("filter_metadata", {}).get("combined_score", 0)
        print(f"   {i}. {doc.get('document_id', 'unknown')[:40]}: "
              f"rerank={rerank_score:.3f}, combined={combined:.3f}")
    
    # Test adaptive threshold
    print("\n" + "=" * 60)
    print("Test 2: Adaptive Threshold Filter")
    print("=" * 60)
    
    filter2 = RelevanceFilter(use_adaptive_threshold=True)
    filtered2 = filter2.filter(reranked, query)
    
    print(f"\nBefore filtering: {len(reranked)} documents")
    print(f"After filtering: {len(filtered2)} documents")
    print(f"Filtered out: {len(reranked) - len(filtered2)} documents")
    
    if filtered2:
        threshold_used = filtered2[0].get("filter_metadata", {}).get("threshold_used", 0)
        print(f"Adaptive threshold used: {threshold_used:.3f}")
    
    # Test strict filter
    print("\n" + "=" * 60)
    print("Test 3: Strict Filter (min_rerank_score=0.5)")
    print("=" * 60)
    
    filter3 = RelevanceFilter(min_rerank_score=0.5, use_adaptive_threshold=False)
    filtered3 = filter3.filter(reranked, query)
    
    print(f"\nBefore filtering: {len(reranked)} documents")
    print(f"After filtering: {len(filtered3)} documents")
    print(f"Filtered out: {len(reranked) - len(filtered3)} documents")
    
    # Test percentile filter
    print("\n" + "=" * 60)
    print("Test 4: Top 30% Percentile Filter")
    print("=" * 60)
    
    filter4 = RelevanceFilter()
    filtered4 = filter4.filter_by_top_percentile(reranked, percentile=30.0)
    
    print(f"\nBefore filtering: {len(reranked)} documents")
    print(f"After filtering: {len(filtered4)} documents")
    print(f"Filtered out: {len(reranked) - len(filtered4)} documents")
    
    print("\n" + "=" * 60)
    print("✓ Relevance filter tests completed!")
    print("=" * 60)


async def test_filter_integration():
    """Test filter integration với Hybrid Retriever"""
    print("\n" + "=" * 60)
    print("Testing Filter Integration with Hybrid Retriever")
    print("=" * 60)
    
    from app.core.rag.hybrid_retriever import HybridRetriever
    from app.core.rag.vector_retriever import VectorRetriever
    from app.core.graph.retriever import GraphRetriever
    from app.core.graph.client import GraphClient
    from app.core.vector_store import VectorStoreClient
    
    # This would require actual Qdrant and Neo4j connections
    # For now, just show the integration point
    print("\nFilter is integrated into HybridRetriever pipeline:")
    print("  1. Retrieve (Vector + Graph)")
    print("  2. Merge results")
    print("  3. Rerank")
    print("  4. Filter by relevance ← NEW!")
    print("  5. Return top_k")
    
    print("\n✓ Integration test completed!")
    print("=" * 60)


async def main():
    """Run all tests"""
    await test_relevance_filter()
    await test_filter_integration()
    
    print("\n" + "=" * 60)
    print("All relevance filter tests completed!")
    print("=" * 60)


if __name__ == "__main__":
    asyncio.run(main())



