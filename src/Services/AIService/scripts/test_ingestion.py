"""
Test Ingestion Pipeline
Test document ingestion with mock data
"""

import asyncio
import sys
import os
import io
from pathlib import Path

# Fix encoding for Windows console
if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from app.core.rag.document_processor import DocumentProcessor
from app.core.embedding.pipeline import EmbeddingPipeline
from app.core.graph.client import GraphClient
from app.core.graph.monitor import GraphMonitor
from app.core.graph.builder import GraphBuilder
from app.core.vector_store.client import VectorStoreClient
from app.core.rag.ingestion_pipeline import IngestionPipeline
from app.infrastructure.config.settings import settings
from tests.fixtures.mock_classroom_data import get_mock_classroom_data


async def test_ingestion():
    """Test complete ingestion pipeline"""
    print("=" * 60)
    print("Testing Ingestion Pipeline")
    print("=" * 60)
    
    # Load mock data
    print("\n1. Loading mock data...")
    data = get_mock_classroom_data()
    
    # Initialize components
    print("\n2. Initializing components...")
    document_processor = DocumentProcessor()
    embedding_pipeline = EmbeddingPipeline()
    graph_client = GraphClient()
    monitor = GraphMonitor(
        log_level=settings.GRAPH_MONITOR_LOG_LEVEL,
        enable_detection=settings.GRAPH_CONFLICT_DETECTION
    )
    graph_builder = GraphBuilder(graph_client, monitor)
    vector_store = VectorStoreClient()
    
    # Create ingestion pipeline
    pipeline = IngestionPipeline(
        document_processor=document_processor,
        embedding_pipeline=embedding_pipeline,
        graph_builder=graph_builder,
        vector_store=vector_store,
        graph_client=graph_client
    )
    print("   [OK] All components initialized")
    
    # Run ingestion
    print("\n3. Running ingestion pipeline...")
    try:
        summary = await pipeline.ingest(data)
        
        # Print summary
        print("\n" + "=" * 60)
        print("Ingestion Summary")
        print("=" * 60)
        print(f"Classroom ID: {summary['classroom_id']}")
        print(f"Documents Processed: {summary['documents_processed']}")
        print(f"Documents Embedded: {summary['documents_embedded']}")
        print(f"Documents Stored: {summary['documents_stored']}")
        print(f"Graph Nodes Created: {summary['graph_nodes']}")
        print(f"Graph Conflicts: {summary['graph_conflicts']}")
        
        if summary.get('graph_conflicts_details'):
            conflicts = summary['graph_conflicts_details']
            print(f"\nConflict Details:")
            print(f"  Total: {conflicts['total_conflicts']}")
            print(f"  By Type: {conflicts['by_type']}")
            print(f"  By Severity: {conflicts['by_severity']}")
        
        if summary.get('provenance'):
            prov = summary['provenance']
            print(f"\nProvenance:")
            print(f"  Timestamp: {prov['ingestion_timestamp']}")
            print(f"  Embedding Model: {prov['embedding_model']}")
            print(f"  Confidence Scores:")
            print(f"    Min: {prov['confidence_scores']['min']:.2f}")
            print(f"    Max: {prov['confidence_scores']['max']:.2f}")
            print(f"    Avg: {prov['confidence_scores']['avg']:.2f}")
        
        if summary.get('errors'):
            print(f"\nErrors: {summary['errors']}")
        
        print("\n" + "=" * 60)
        print("[OK] Ingestion test completed successfully!")
        print("=" * 60)
        
    except Exception as e:
        print(f"\n[ERROR] Error during ingestion: {e}")
        import traceback
        traceback.print_exc()
    finally:
        # Cleanup
        await graph_client.close()


if __name__ == "__main__":
    asyncio.run(test_ingestion())

