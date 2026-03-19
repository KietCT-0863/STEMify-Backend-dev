"""
Ingestion Pipeline
End-to-end pipeline for ingesting classroom data into RAG system
"""

from typing import Dict, Any, List
import logging

from app.core.rag.document_processor import DocumentProcessor
from app.core.embedding.pipeline import EmbeddingPipeline
from app.core.graph.builder import GraphBuilder
from app.core.graph.client import GraphClient
from app.core.graph.monitor import GraphMonitor
from app.core.vector_store import VectorStoreClient
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class IngestionPipeline:
    """Complete ingestion pipeline: Process → Embed → Store → Graph"""
    
    def __init__(
        self,
        document_processor: DocumentProcessor,
        embedding_pipeline: EmbeddingPipeline,
        graph_builder: GraphBuilder,
        vector_store: VectorStoreClient,
        graph_client: GraphClient
    ):
        self.document_processor = document_processor
        self.embedding_pipeline = embedding_pipeline
        self.graph_builder = graph_builder
        self.vector_store = vector_store
        self.graph_client = graph_client
    
    async def ingest(self, classroom_data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Complete ingestion pipeline
        
        Steps:
        1. Process raw data into documents
        2. Generate embeddings
        3. Store in vector database
        4. Build knowledge graph
        5. Return summary with provenance
        """
        logger.info(f"Starting ingestion for classroom {classroom_data['classroom']['id']}")
        
        summary = {
            "classroom_id": classroom_data["classroom"]["id"],
            "documents_processed": 0,
            "documents_embedded": 0,
            "documents_stored": 0,
            "graph_nodes": 0,
            "graph_conflicts": 0,
            "errors": []
        }
        
        try:
            # Step 1: Process documents
            logger.info("Step 1: Processing documents...")
            documents = self.document_processor.process_classroom_data(classroom_data)
            summary["documents_processed"] = len(documents)
            logger.info(f"Processed {len(documents)} documents")
            
            # Step 2: Generate embeddings
            logger.info("Step 2: Generating embeddings...")
            documents_with_embeddings = self.embedding_pipeline.generate_embeddings(documents)
            summary["documents_embedded"] = len(documents_with_embeddings)
            logger.info(f"Generated {len(documents_with_embeddings)} embeddings")
            
            # Step 3: Store in vector database
            logger.info("Step 3: Storing in vector database...")
            stored_count = await self._store_documents(documents_with_embeddings)
            summary["documents_stored"] = stored_count
            logger.info(f"Stored {stored_count} documents in vector database")
            
            # Step 4: Build graph
            logger.info("Step 4: Building knowledge graph...")
            graph_summary = await self.graph_builder.build_graph(classroom_data)
            summary["graph_nodes"] = graph_summary["nodes_created"]
            summary["graph_conflicts"] = graph_summary["conflicts"]["total_conflicts"]
            summary["graph_conflicts_details"] = graph_summary["conflicts"]
            logger.info(f"Built graph with {graph_summary['nodes_created']} nodes, "
                       f"{graph_summary['conflicts']['total_conflicts']} conflicts")
            
            # Add provenance summary
            summary["provenance"] = {
                "ingestion_timestamp": self._get_timestamp(),
                "data_version": "1.0",
                "embedding_model": settings.EMBEDDING_MODEL,
                "vector_store": "Qdrant",
                "graph_store": "Neo4j",
                "confidence_scores": {
                    "min": min((d.get("confidence_score", 0) for d in documents_with_embeddings), default=0),
                    "max": max((d.get("confidence_score", 0) for d in documents_with_embeddings), default=0),
                    "avg": sum((d.get("confidence_score", 0) for d in documents_with_embeddings)) / len(documents_with_embeddings) if documents_with_embeddings else 0
                }
            }
            
            logger.info("Ingestion complete!")
            return summary
            
        except Exception as e:
            logger.error(f"Error during ingestion: {e}", exc_info=True)
            summary["errors"].append(str(e))
            return summary
    
    async def _store_documents(self, documents: List[Dict[str, Any]]) -> int:
        """Store documents in vector database"""
        stored = 0
        
        for doc in documents:
            try:
                # Extract embedding and metadata
                embedding = doc.pop("embedding")
                metadata = doc.get("metadata", {})
                
                # Add provenance to metadata
                if "provenance" in doc:
                    metadata["provenance"] = doc["provenance"]
                    metadata["confidence_score"] = doc.get("confidence_score", 0.5)
                
                doc_id = self._convert_to_qdrant_id(doc["document_id"])
                
                # Store in Qdrant
                await self.vector_store.upsert(
                    id=doc_id,
                    vector=embedding,
                    payload={
                        **metadata,
                        "content": doc["content"],
                        "document_id": doc["document_id"] 
                    }
                )
                stored += 1
            except Exception as e:
                logger.error(f"Error storing document {doc.get('document_id')}: {e}")
        
        return stored
    
    def _convert_to_qdrant_id(self, document_id: str) -> int:
        """
        Convert string document_id to integer for Qdrant
        Qdrant requires integer or UUID, not string
        """
        import hashlib
        
        hash_obj = hashlib.md5(document_id.encode())
        hash_bytes = hash_obj.digest()[:8]
        
        qdrant_id = int.from_bytes(hash_bytes, byteorder='big', signed=True)
        
        if qdrant_id < 0:
            qdrant_id = abs(qdrant_id)
        
        return qdrant_id
    
    def _get_timestamp(self) -> str:
        """Get current timestamp"""
        from datetime import datetime
        return datetime.utcnow().isoformat()

