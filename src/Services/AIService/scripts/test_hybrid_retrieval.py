"""
Test Hybrid Retrieval
Test script for hybrid retrieval (vector + graph) with enhanced features:
- Entity extraction with synonym mapping (en↔vi)
- Intent detection (struggling, need_help, performing_poorly)
- Performance-based relationships (STRUGGLES_WITH, EXCELS_AT)
- Virtual/analytics documents
"""

import asyncio
import sys
import io
from pathlib import Path

if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

sys.path.insert(0, str(Path(__file__).parent.parent))

from app.core.rag import HybridRetriever, VectorRetriever, ResultMerger
from app.core.graph import GraphRetriever, GraphClient, EntityExtractor
from app.core.vector_store import VectorStoreClient
from app.core.embedding.pipeline import EmbeddingPipeline
from app.infrastructure.config.settings import settings


async def test_entity_extraction():
    """Test entity extraction and intent detection"""
    print("\n" + "=" * 60)
    print("Testing Entity Extraction & Intent Detection")
    print("=" * 60)
    
    extractor = EntityExtractor()
    
    test_cases = [
        ("Which students are struggling with electrical circuits?", ["Topic"], "struggling"),
        ("What topics are students performing poorly on?", [], "performing_poorly"),
        ("Show me classroom performance for class 7A", ["Classroom"], None),
        ("Which students need extra help?", [], "need_help"),
        ("Học sinh nào đang gặp khó khăn với mạch điện?", ["Topic"], "struggling"),
        ("Chủ đề nào học sinh đang học kém?", [], "performing_poorly"),
    ]
    
    for query, expected_entity_types, expected_intent in test_cases:
        print(f"\nQuery: {query}")
        entities = extractor.extract_entities(query)
        intent = extractor.detect_intent(query)
        
        print(f"  Entities: {entities}")
        print(f"  Intent: {intent}")
        
        # Verify entity types
        if expected_entity_types:
            found_types = [e[0] for e in entities]
            for expected_type in expected_entity_types:
                if expected_type in found_types:
                    print(f"  [OK] Found expected entity type: {expected_type}")
                else:
                    print(f"  [FAIL] Missing expected entity type: {expected_type}")
        
        # Verify intent
        if expected_intent:
            if intent == expected_intent:
                print(f"  [OK] Intent detected correctly: {intent}")
            else:
                print(f"  [FAIL] Intent mismatch. Expected: {expected_intent}, Got: {intent}")
        else:
            if intent is None:
                print(f"  [OK] No intent (as expected)")
            else:
                print(f"  [WARN] Unexpected intent: {intent}")


async def test_performance_relationships(graph_client: GraphClient):
    """Test if performance relationships exist in graph"""
    print("\n" + "=" * 60)
    print("Testing Performance Relationships")
    print("=" * 60)
    
    # Check for STRUGGLES_WITH relationships
    struggles_query = """
    MATCH (s:Student)-[r:STRUGGLES_WITH]->(t:Topic)
    RETURN s.id as student_id, t.name as topic_name, r.average_score as avg_score
    LIMIT 5
    """
    
    try:
        struggles_results = await graph_client.query_cypher(struggles_query)
        print(f"\n[OK] Found {len(struggles_results)} STRUGGLES_WITH relationships")
        for result in struggles_results[:3]:
            student_id = result.get("student_id", "unknown")
            topic_name = result.get("topic_name", "unknown")
            avg_score = result.get("avg_score", 0)
            print(f"  - Student {student_id} struggles with {topic_name} (avg: {avg_score:.2f})")
    except Exception as e:
        print(f"[ERROR] Error checking STRUGGLES_WITH: {e}")
    
    # Check for EXCELS_AT relationships
    excels_query = """
    MATCH (s:Student)-[r:EXCELS_AT]->(t:Topic)
    RETURN s.id as student_id, t.name as topic_name, r.average_score as avg_score
    LIMIT 5
    """
    
    try:
        excels_results = await graph_client.query_cypher(excels_query)
        print(f"\n[OK] Found {len(excels_results)} EXCELS_AT relationships")
        for result in excels_results[:3]:
            student_id = result.get("student_id", "unknown")
            topic_name = result.get("topic_name", "unknown")
            avg_score = result.get("avg_score", 0)
            print(f"  - Student {student_id} excels at {topic_name} (avg: {avg_score:.2f})")
    except Exception as e:
        print(f"[ERROR] Error checking EXCELS_AT: {e}")


async def test_virtual_documents(vector_retriever: VectorRetriever):
    """Test if virtual/analytics documents are in vector store"""
    print("\n" + "=" * 60)
    print("Testing Virtual/Analytics Documents")
    print("=" * 60)
    
    test_queries = [
        "students need help",
        "poor performing topics",
        "struggling students"
    ]
    
    for query in test_queries:
        print(f"\nQuery: '{query}'")
        try:
            results = await vector_retriever.retrieve(query, top_k=5, min_score=0.0)
            
            # Check for analytics documents
            analytics_docs = [
                r for r in results 
                if r.get("metadata", {}).get("document_type", "").startswith("analytics_")
            ]
            
            if analytics_docs:
                print(f"  [OK] Found {len(analytics_docs)} analytics documents")
                for doc in analytics_docs[:2]:
                    doc_type = doc.get("metadata", {}).get("document_type", "unknown")
                    print(f"    - {doc_type}: {doc.get('content', '')[:80]}...")
            else:
                print(f"  [WARN] No analytics documents found (may need to re-run ingestion)")
        except Exception as e:
            print(f"  [ERROR] Error: {e}")


async def test_hybrid_retrieval():
    """Test hybrid retrieval pipeline with enhanced features"""
    
    print("=" * 60)
    print("Testing Hybrid Retrieval (Enhanced)")
    print("=" * 60)
    
    # Initialize components
    print("\n1. Initializing components...")
    vector_store = VectorStoreClient()
    embedding_pipeline = EmbeddingPipeline()
    graph_client = GraphClient()
    
    # Create retrievers
    vector_retriever = VectorRetriever(
        vector_store=vector_store,
        embedding_pipeline=embedding_pipeline
    )
    
    graph_retriever = GraphRetriever(
        graph_client=graph_client
    )
    
    result_merger = ResultMerger(
        vector_weight=0.6,
        graph_weight=0.4
    )
    
    hybrid_retriever = HybridRetriever(
        vector_retriever=vector_retriever,
        graph_retriever=graph_retriever,
        result_merger=result_merger
    )
    
    print("[OK] Components initialized")
    
    # Test entity extraction and intent detection
    await test_entity_extraction()
    
    # Test performance relationships
    await test_performance_relationships(graph_client)
    
    # Test virtual documents
    await test_virtual_documents(vector_retriever)
    
    # Test queries with detailed analysis
    test_queries = [
        {
            "query": "Which students are struggling with electrical circuits?",
            "expected_entities": ["Topic"],
            "expected_intent": "struggling",
            "expected_sources": ["graph", "vector"]
        },
        {
            "query": "What topics are students performing poorly on?",
            "expected_entities": [],
            "expected_intent": "performing_poorly",
            "expected_sources": ["graph", "vector"]
        },
        {
            "query": "Show me classroom performance for class 7A",
            "expected_entities": ["Classroom"],
            "expected_intent": None,
            "expected_sources": ["graph", "vector"]
        },
        {
            "query": "Which students need extra help?",
            "expected_entities": [],
            "expected_intent": "need_help",
            "expected_sources": ["graph", "vector"]
        },
        {
            "query": "Học sinh nào đang gặp khó khăn với mạch điện?",
            "expected_entities": ["Topic"],
            "expected_intent": "struggling",
            "expected_sources": ["graph", "vector"]
        }
    ]
    
    print("\n" + "=" * 60)
    print("Testing Hybrid Retrieval Queries")
    print("=" * 60)
    
    for i, test_case in enumerate(test_queries, 1):
        query = test_case["query"]
        expected_entities = test_case["expected_entities"]
        expected_intent = test_case["expected_intent"]
        expected_sources = test_case["expected_sources"]
        
        print(f"\n{'=' * 60}")
        print(f"Test Query {i}: {query}")
        print('=' * 60)
        
        # Analyze query first
        extractor = EntityExtractor()
        entities = extractor.extract_entities(query)
        intent = extractor.detect_intent(query)
        
        print(f"\n[Query Analysis]")
        print(f"  Entities extracted: {entities}")
        print(f"  Intent detected: {intent}")
        
        # Verify expectations
        if expected_entities:
            found_types = [e[0] for e in entities]
            for expected_type in expected_entities:
                if expected_type in found_types:
                    print(f"  [OK] Found expected entity: {expected_type}")
                else:
                    print(f"  [FAIL] Missing expected entity: {expected_type}")
        
        if expected_intent:
            if intent == expected_intent:
                print(f"  [OK] Intent detected correctly: {intent}")
            else:
                print(f"  [FAIL] Intent mismatch. Expected: {expected_intent}, Got: {intent}")
        
        try:
            # Test hybrid retrieval
            print(f"\n[Hybrid Retrieval]")
            results = await hybrid_retriever.retrieve(
                query=query,
                top_k=5
            )
            
            print(f"[OK] Retrieved {len(results)} results")
            
            # Analyze sources
            sources_found = set()
            vector_count = 0
            graph_count = 0
            
            # Display results
            for j, result in enumerate(results[:3], 1):  # Show top 3
                sources = result.get('retrieval_sources', [])
                sources_found.update(sources)
                
                if 'vector' in sources:
                    vector_count += 1
                if 'graph' in sources:
                    graph_count += 1
                
                print(f"\n--- Result {j} ---")
                print(f"Document ID: {result.get('document_id')}")
                print(f"Retrieval Score: {result.get('retrieval_score', 0):.3f}")
                print(f"Confidence Score: {result.get('confidence_score', 0):.3f}")
                print(f"Sources: {sources}")
                
                # Show document type
                doc_type = result.get('metadata', {}).get('document_type', 'unknown')
                intent_meta = result.get('metadata', {}).get('intent', None)
                if doc_type.startswith('analytics_'):
                    print(f"Type: {doc_type} (Virtual Document)")
                elif intent_meta:
                    print(f"Type: {doc_type} (Intent-based: {intent_meta})")
                else:
                    print(f"Type: {doc_type}")
                
                content = result.get('content', '')
                print(f"Content: {content[:120]}...")
            
            # Verify sources
            print(f"\n[Source Analysis]")
            print(f"  Vector results: {vector_count}")
            print(f"  Graph results: {graph_count}")
            print(f"  Sources found: {sources_found}")
            
            for expected_source in expected_sources:
                if expected_source in sources_found:
                    print(f"  [OK] Found expected source: {expected_source}")
                else:
                    print(f"  [WARN] Missing expected source: {expected_source} (may be OK if no data)")
            
            # Test vector-only retrieval
            print(f"\n[Vector-Only Retrieval]")
            vector_results = await hybrid_retriever.retrieve_vector_only(
                query=query,
                top_k=3
            )
            print(f"[OK] Retrieved {len(vector_results)} vector results")
            
            # Check for analytics documents
            analytics_in_vector = [
                r for r in vector_results 
                if r.get("metadata", {}).get("document_type", "").startswith("analytics_")
            ]
            if analytics_in_vector:
                print(f"  [OK] Found {len(analytics_in_vector)} analytics documents in vector results")
            
            # Test graph-only retrieval
            print(f"\n[Graph-Only Retrieval]")
            graph_results = await hybrid_retriever.retrieve_graph_only(
                query=query,
                top_k=3
            )
            print(f"[OK] Retrieved {len(graph_results)} graph results")
            
            # Check for intent-based results
            intent_based = [
                r for r in graph_results 
                if r.get("metadata", {}).get("intent")
            ]
            if intent_based:
                print(f"  [OK] Found {len(intent_based)} intent-based results")
                for r in intent_based[:2]:
                    intent_type = r.get("metadata", {}).get("intent")
                    print(f"    - Intent: {intent_type}")
            
        except Exception as e:
            print(f"[ERROR] Error: {e}")
            import traceback
            traceback.print_exc()
    
    # Cleanup
    print("\n" + "=" * 60)
    print("Cleaning up...")
    await graph_client.close()
    print("[OK] Cleanup complete")
    
    print("\n" + "=" * 60)
    print("Test Complete!")
    print("=" * 60)


if __name__ == "__main__":
    asyncio.run(test_hybrid_retrieval())

