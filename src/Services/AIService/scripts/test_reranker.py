"""
Test Reranker with Real-World Classroom Data
Test reranker implementations using mock classroom data
"""

import asyncio
import sys
import os
from pathlib import Path

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from app.core.reranker import create_reranker, RelevanceFilter
from app.core.reranker.cross_encoder_reranker import CrossEncoderReranker
from app.core.rag.document_processor import DocumentProcessor
from app.core.embedding.pipeline import EmbeddingPipeline
from tests.fixtures.mock_classroom_data import get_mock_classroom_data_test
from app.infrastructure.config.settings import settings
import numpy as np


def _cosine_similarity(vec1: np.ndarray, vec2: np.ndarray) -> float:
    """Calculate cosine similarity between two vectors"""
    dot_product = np.dot(vec1, vec2)
    norm1 = np.linalg.norm(vec1)
    norm2 = np.linalg.norm(vec2)
    if norm1 == 0 or norm2 == 0:
        return 0.0
    return float(dot_product / (norm1 * norm2))


def _format_topics(topics) -> str:
    """
    Format topics list to string
    Handles both list of strings and list of dicts
    """
    if not topics:
        return "N/A"
    
    topic_names = []
    for topic in topics:
        if isinstance(topic, dict):
            # Extract topic name from dict
            topic_name = topic.get("name") or topic.get("topic_name") or topic.get("id") or str(topic)
            topic_names.append(str(topic_name))
        elif isinstance(topic, str):
            topic_names.append(topic)
        else:
            topic_names.append(str(topic))
    
    return ", ".join(topic_names) if topic_names else "N/A"


async def create_retrieved_documents_with_real_scores(
    query: str,
    classroom_data: dict,
    top_k: int = 15
) -> list:
    """
    Create retrieved documents with REAL retrieval scores using embeddings
    
    This function:
    1. Processes classroom data into documents
    2. Generates embeddings for query and documents
    3. Calculates REAL cosine similarity scores
    4. Returns documents sorted by similarity (like real vector search)
    
    Args:
        query: Natural language query
        classroom_data: Classroom data dictionary
        top_k: Number of top documents to return
    
    Returns:
        List of documents with real retrieval scores
    """
    # Step 1: Process classroom data into documents
    processor = DocumentProcessor()
    processed_docs = processor.process_classroom_data(classroom_data)
    
    # Step 2: Generate embeddings using EmbeddingPipeline
    embedding_pipeline = EmbeddingPipeline()
    
    # Generate query embedding
    query_doc = {"content": query}
    query_embeddings = embedding_pipeline.generate_embeddings(
        [query_doc],
        update_confidence=False
    )
    if not query_embeddings or "embedding" not in query_embeddings[0]:
        raise ValueError("Failed to generate query embedding")
    query_embedding = np.array(query_embeddings[0]["embedding"])
    
    # Generate document embeddings
    doc_embeddings = embedding_pipeline.generate_embeddings(
        processed_docs,
        update_confidence=False
    )
    
    # Step 3: Calculate cosine similarity for each document
    scored_docs = []
    for doc in doc_embeddings:
        if "embedding" not in doc:
            continue
        
        doc_embedding = np.array(doc["embedding"])
        similarity_score = _cosine_similarity(query_embedding, doc_embedding)
        
        # Add retrieval metadata (like real vector retriever)
        doc["retrieval_score"] = float(similarity_score)
        doc["retrieval_source"] = "vector"  # Simulating vector search
        doc["retrieval_query"] = query
        
        # Update provenance
        if "provenance" not in doc:
            doc["provenance"] = {}
        doc["provenance"]["retrieval_method"] = "vector_search"
        doc["provenance"]["similarity_score"] = float(similarity_score)
        
        scored_docs.append(doc)
    
    # Step 4: Sort by similarity score (descending) and return top_k
    scored_docs.sort(key=lambda x: x["retrieval_score"], reverse=True)
    
    return scored_docs[:top_k]



async def test_reranker_with_classroom_data():
    """Test reranker with real classroom data"""
    print("=" * 60)
    print("Testing Reranker with Real Classroom Data")
    print("=" * 60)
    
    # Load mock classroom data
    print("\n1. Loading mock classroom data...")
    classroom_data = get_mock_classroom_data_test()
    print(f"   ✓ Loaded data for: {classroom_data['classroom']['name']}")
    print(f"   ✓ Students: {len(classroom_data['students'])}")
    print(f"   ✓ Quiz attempts: {len(classroom_data['quizzes']['quiz_attempts'])}")
    print(f"   ✓ Assignment attempts: {len(classroom_data['assignments']['assignment_attempts'])}")
    
    # Create reranker
    print("\n2. Creating reranker...")
    try:
        reranker = CrossEncoderReranker()
        print("   ✓ Cross-encoder reranker created")
    except Exception as e:
        print(f"   ✗ Error creating reranker: {e}")
        return
    
    # Test Case 1: Find struggling students
    print("\n" + "=" * 60)
    print("Test Case 1: Find struggling students")
    print("=" * 60)
    query1 = "Which students are struggling with physics topics?"
    
    try:
        # Create retrieved documents with REAL retrieval scores
        print(f"\n   Generating embeddings and calculating real similarity scores...")
        retrieved_docs = await create_retrieved_documents_with_real_scores(
            query=query1,
            classroom_data=classroom_data,
            top_k=15
        )
        print(f"   ✓ Created {len(retrieved_docs)} documents with REAL retrieval scores")
        
        reranked1 = await reranker.rerank(query1, retrieved_docs, top_k=10)
        
        # Apply relevance filter
        print(f"\n   Applying relevance filter...")
        relevance_filter = RelevanceFilter(use_adaptive_threshold=True)
        filtered1 = relevance_filter.filter(reranked1, query1)
        
        print(f"\nQuery: {query1}")
        print(f"Retrieved: {len(retrieved_docs)} documents")
        print(f"Reranked: {len(reranked1)} documents")
        print(f"Filtered: {len(filtered1)} relevant documents")
        print("\nTop 5 Filtered Results:")
        print("-" * 60)
        
        # Show top 5 after filtering
        for i, doc in enumerate(filtered1[:5], 1):
            doc_type = doc.get("metadata", {}).get("document_type", "unknown")
            doc_id = doc.get("document_id", "unknown")
            content_preview = doc.get("content", "")[:70].replace("\n", " ")
            
            print(f"\n{i}. [{doc_type}] {doc_id}")
            print(f"   Content: {content_preview}...")
            print(f"   Retrieval Score: {doc.get('retrieval_score', 0):.3f}")
            print(f"   Rerank Score: {doc.get('rerank_score', 0):.3f}")
            if "provenance" in doc and "rerank_raw_score" in doc["provenance"]:
                print(f"   Raw Score: {doc['provenance']['rerank_raw_score']:.3f}")
        
        print("\n✓ Test Case 1 completed!")
        
    except Exception as e:
        print(f"\n✗ Error: {e}")
        import traceback
        traceback.print_exc()
    
    # Test Case 2: Find students with low quiz scores
    print("\n" + "=" * 60)
    print("Test Case 2: Find students with low quiz scores")
    print("=" * 60)
    query2 = "Show me students who scored below 60% on quizzes"
    
    try:
        # Create retrieved documents with REAL retrieval scores for this query
        print(f"\n   Generating embeddings and calculating real similarity scores...")
        retrieved_docs2 = await create_retrieved_documents_with_real_scores(
            query=query2,
            classroom_data=classroom_data,
            top_k=15
        )
        print(f"   ✓ Created {len(retrieved_docs2)} documents with REAL retrieval scores")
        
        reranked2 = await reranker.rerank(query2, retrieved_docs2, top_k=10)
        
        # Apply relevance filter
        print(f"\n   Applying relevance filter...")
        relevance_filter = RelevanceFilter(use_adaptive_threshold=True)
        filtered2 = relevance_filter.filter(reranked2, query2)
        
        print(f"\nQuery: {query2}")
        print(f"Reranked: {len(reranked2)} documents")
        print(f"Filtered: {len(filtered2)} relevant documents")
        print("\nTop 5 Filtered Results:")
        print("-" * 60)
        
        for i, doc in enumerate(filtered2[:5], 1):
            doc_type = doc.get("metadata", {}).get("document_type", "unknown")
            metadata = doc.get("metadata", {})
            
            # Extract relevant info
            student_name = metadata.get("student_name", "Unknown")
            score = metadata.get("score", metadata.get("average_quiz_score", "N/A"))
            
            print(f"\n{i}. [{doc_type}] Student: {student_name}")
            if isinstance(score, (int, float)):
                print(f"   Score: {score:.1f}%")
            print(f"   Rerank Score: {doc.get('rerank_score', 0):.3f}")
        
        print("\n✓ Test Case 2 completed!")
        
    except Exception as e:
        print(f"\n✗ Error: {e}")
        import traceback
        traceback.print_exc()
    
    # Test Case 3: Find topics students are struggling with
    print("\n" + "=" * 60)
    print("Test Case 3: Find weak topics")
    print("=" * 60)
    query3 = "What topics are students struggling with?"
    
    try:
        # Create retrieved documents with REAL retrieval scores for this query
        print(f"\n   Generating embeddings and calculating real similarity scores...")
        retrieved_docs3 = await create_retrieved_documents_with_real_scores(
            query=query3,
            classroom_data=classroom_data,
            top_k=15
        )
        print(f"   ✓ Created {len(retrieved_docs3)} documents with REAL retrieval scores")
        
        reranked3 = await reranker.rerank(query3, retrieved_docs3, top_k=10)
        
        # Apply relevance filter
        print(f"\n   Applying relevance filter...")
        relevance_filter = RelevanceFilter(use_adaptive_threshold=True)
        filtered3 = relevance_filter.filter(reranked3, query3)
        
        print(f"\nQuery: {query3}")
        print(f"Reranked: {len(reranked3)} documents")
        print(f"Filtered: {len(filtered3)} relevant documents")
        print("\nTop 5 Filtered Results:")
        print("-" * 60)
        
        for i, doc in enumerate(filtered3[:5], 1):
            doc_type = doc.get("metadata", {}).get("document_type", "unknown")
            metadata = doc.get("metadata", {})
            topics = metadata.get("topics", [])
            
            print(f"\n{i}. [{doc_type}]")
            if topics:
                topics_str = _format_topics(topics)
                print(f"   Topics: {topics_str}")
            print(f"   Rerank Score: {doc.get('rerank_score', 0):.3f}")
        
        print("\n✓ Test Case 3 completed!")
        
    except Exception as e:
        print(f"\n✗ Error: {e}")
        import traceback
        traceback.print_exc()
    
    # Test Case 4: Class performance overview
    print("\n" + "=" * 60)
    print("Test Case 4: Class performance overview")
    print("=" * 60)
    query4 = "How is the overall class performance?"
    
    try:
        # Create retrieved documents with REAL retrieval scores for this query
        print(f"\n   Generating embeddings and calculating real similarity scores...")
        retrieved_docs4 = await create_retrieved_documents_with_real_scores(
            query=query4,
            classroom_data=classroom_data,
            top_k=15
        )
        print(f"   ✓ Created {len(retrieved_docs4)} documents with REAL retrieval scores")
        
        reranked4 = await reranker.rerank(query4, retrieved_docs4, top_k=10)
        
        # Apply relevance filter
        print(f"\n   Applying relevance filter...")
        relevance_filter = RelevanceFilter(use_adaptive_threshold=True)
        filtered4 = relevance_filter.filter(reranked4, query4)
        
        print(f"\nQuery: {query4}")
        print(f"Reranked: {len(reranked4)} documents")
        print(f"Filtered: {len(filtered4)} relevant documents")
        print("\nTop 3 Filtered Results:")
        print("-" * 60)
        
        for i, doc in enumerate(filtered4[:3], 1):
            doc_type = doc.get("metadata", {}).get("document_type", "unknown")
            content_preview = doc.get("content", "")[:80].replace("\n", " ")
            
            print(f"\n{i}. [{doc_type}]")
            print(f"   Content: {content_preview}...")
            print(f"   Rerank Score: {doc.get('rerank_score', 0):.3f}")
        
        print("\n✓ Test Case 4 completed!")
        
    except Exception as e:
        print(f"\n✗ Error: {e}")
        import traceback
        traceback.print_exc()


async def test_reranker_factory():
    """Test reranker factory"""
    print("\n" + "=" * 60)
    print("Testing Reranker Factory")
    print("=" * 60)
    
    # Test local reranker
    print("\n1. Testing local reranker...")
    reranker = create_reranker("local")
    if reranker:
        print("   ✓ Local reranker created")
    else:
        print("   ✗ Failed to create local reranker")
    
    # Test none provider
    print("\n2. Testing 'none' provider...")
    reranker = create_reranker("none")
    if reranker is None:
        print("   ✓ 'none' provider returns None (expected)")
    else:
        print("   ✗ 'none' provider should return None")
    
    # Test cohere (if API key available)
    if settings.COHERE_API_KEY:
        print("\n3. Testing Cohere reranker...")
        try:
            reranker = create_reranker("cohere")
            if reranker:
                print("   ✓ Cohere reranker created")
            else:
                print("   ✗ Failed to create Cohere reranker")
        except Exception as e:
            print(f"   ✗ Error: {e}")
    else:
        print("\n3. Skipping Cohere reranker (no API key)")
    
    print("\n" + "=" * 60)
    print("✓ Reranker factory test completed!")
    print("=" * 60)


async def test_reranker_comparison():
    """Compare reranker performance with different queries"""
    print("\n" + "=" * 60)
    print("Testing Reranker Performance Comparison")
    print("=" * 60)
    
    # Load data
    classroom_data = get_mock_classroom_data_test()
    
    # Create reranker
    reranker = create_reranker("local")
    if not reranker:
        print("✗ Failed to create reranker")
        return
    
    # Test queries
    test_queries = [
        "Which students are struggling?",
        "Find students with low quiz scores",
        "What topics need more attention?",
        "Show me class performance summary"
    ]
    
    print(f"\nTesting {len(test_queries)} queries with REAL retrieval scores")
    print("-" * 60)
    
    for query in test_queries:
        try:
            # Create retrieved documents with REAL retrieval scores for this query
            retrieved_docs = await create_retrieved_documents_with_real_scores(
                query=query,
                classroom_data=classroom_data,
                top_k=15
            )
            
            reranked = await reranker.rerank(query, retrieved_docs, top_k=3)
            
            print(f"\nQuery: {query}")
            print(f"  Retrieved: {len(retrieved_docs)} documents with real scores")
            if reranked:
                top_result = reranked[0].get('document_id', 'unknown')
                retrieval_score = reranked[0].get('retrieval_score', 0)
                rerank_score = reranked[0].get('rerank_score', 0)
                print(f"  Top result: {top_result}")
                print(f"  Retrieval score: {retrieval_score:.3f}")
                print(f"  Rerank score: {rerank_score:.3f}")
            else:
                print("  No results")
            
        except Exception as e:
            print(f"\nQuery: {query}")
            print(f"  Error: {e}")
            import traceback
            traceback.print_exc()
    
    print("\n" + "=" * 60)
    print("✓ Performance comparison completed!")
    print("=" * 60)


async def main():
    """Run all tests"""
    print("\n" + "=" * 60)
    print("Reranker Test Suite - Real-World Classroom Data")
    print("=" * 60)
    
    # Test 1: Real classroom data
    await test_reranker_with_classroom_data()
    
    # Test 2: Factory
    await test_reranker_factory()
    
    # Test 3: Performance comparison
    await test_reranker_comparison()
    
    print("\n" + "=" * 60)
    print("All tests completed!")
    print("=" * 60)


if __name__ == "__main__":
    asyncio.run(main())
