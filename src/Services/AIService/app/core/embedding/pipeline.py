

from typing import List, Dict, Any, Optional
import numpy as np
from sentence_transformers import SentenceTransformer
import logging
import threading

from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)

# Singleton instance 
_embedding_pipeline_instance: Optional["EmbeddingPipeline"] = None
_embedding_pipeline_lock = threading.Lock()


def get_embedding_pipeline() -> "EmbeddingPipeline":
   
    global _embedding_pipeline_instance
    if _embedding_pipeline_instance is None:
        with _embedding_pipeline_lock:
            # Double-check locking pattern
            if _embedding_pipeline_instance is None:
                _embedding_pipeline_instance = EmbeddingPipeline()
                logger.info("Created singleton EmbeddingPipeline instance")
    return _embedding_pipeline_instance


class EmbeddingPipeline:
    """Generate embeddings with confidence scores"""
    
    def __init__(self):
        logger.info(f"Loading embedding model: {settings.EMBEDDING_MODEL}...")
        self.model = SentenceTransformer(settings.EMBEDDING_MODEL, device=settings.EMBEDDING_DEVICE)
        logger.info(f"Loaded embedding model: {settings.EMBEDDING_MODEL}")
    
    def encode(self, texts: List[str], **kwargs) -> np.ndarray:
       
        return self.model.encode(
            texts,
            batch_size=kwargs.get("batch_size", settings.EMBEDDING_BATCH_SIZE),
            show_progress_bar=kwargs.get("show_progress_bar", False),
            convert_to_numpy=True
        )
    
    def generate_embeddings(
        self,
        documents: List[Dict[str, Any]],
        update_confidence: bool = True
    ) -> List[Dict[str, Any]]:
        """
        Generate embeddings for documents
        
        Args:
            documents: List of documents with content
            update_confidence: Whether to update confidence scores based on embedding quality
        
        Returns:
            Documents with embeddings and updated confidence scores
        """
        if not documents:
            return []
        
        # Extract content
        contents = [doc["content"] for doc in documents]

        # Generate embeddings in batches
        embeddings = self.model.encode(
            contents,
            batch_size=settings.EMBEDDING_BATCH_SIZE,
            show_progress_bar=False,
            convert_to_numpy=True
        )
        
        # Add embeddings and update confidence
        result = []
        for doc, embedding in zip(documents, embeddings):
            # Calculate embedding quality score
            quality_score = self._calculate_embedding_quality(embedding, doc)
            
            # Update confidence if requested
            if update_confidence:
                original_confidence = doc.get("confidence_score", 0.5)
                # Combine original confidence with embedding quality
                doc["confidence_score"] = (original_confidence * 0.7) + (quality_score * 0.3)
            
            # Add embedding
            doc["embedding"] = embedding.tolist()
            doc["embedding_dimension"] = len(embedding)
            doc["embedding_model"] = settings.EMBEDDING_MODEL
            
            # Update provenance
            if "provenance" in doc:
                doc["provenance"]["embedding_generated_at"] = self._get_timestamp()
                doc["provenance"]["embedding_model"] = settings.EMBEDDING_MODEL
                doc["provenance"]["embedding_quality_score"] = float(quality_score)
            
            result.append(doc)
        
        logger.info(f"Generated {len(result)} embeddings") 
        return result
    
    def _calculate_embedding_quality(self, embedding: np.ndarray, doc: Dict[str, Any]) -> float:
        """
        Calculate embedding quality score
        
        Factors:
        - Norm (should be reasonable, not too small/large)
        - Content length (longer content might have better embeddings)
        - Metadata completeness
        """
        # Check embedding norm
        norm = np.linalg.norm(embedding)
        norm_score = 1.0 if 0.5 < norm < 2.0 else 0.7
        
        # Check content quality
        content = doc.get("content", "")
        content_length = len(content.split())
        content_score = min(1.0, content_length / 50)  # Prefer longer content
        
        # Check metadata completeness
        metadata = doc.get("metadata", {})
        required_fields = ["document_type", "document_id"]
        metadata_score = sum(1 for field in required_fields if field in metadata) / len(required_fields)
        
        # Weighted average
        quality = (norm_score * 0.4) + (content_score * 0.4) + (metadata_score * 0.2)
        return min(1.0, max(0.0, quality))
    
    def _get_timestamp(self) -> str:
        """Get current timestamp in ISO format"""
        from datetime import datetime
        return datetime.utcnow().isoformat()

