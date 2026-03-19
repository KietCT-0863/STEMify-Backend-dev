"""
Context Gatherer - GSSC
Collects candidate context from memory and retrieval.
"""

from typing import List, Dict, Any, Optional
import logging

from app.core.context.models import ContextItem
from app.core.memory.memory_manager import MemoryManager
from app.core.rag.hybrid_retriever import HybridRetriever

logger = logging.getLogger(__name__)


class ContextGatherer:
    """
    Gather context from memory layers and hybrid retriever.
    """

    def __init__(
        self,
        memory_manager: MemoryManager,
        hybrid_retriever: HybridRetriever,
    ):
        self.memory_manager = memory_manager
        self.hybrid_retriever = hybrid_retriever

    async def gather(
        self,
        query: str,
        user_id: Optional[str] = None,
        top_k: int = 10,
    ) -> List[ContextItem]:
        """
        Gather candidate context items.
        """
        candidates: List[ContextItem] = []

        # Memory search (multi-layer)
        try:
            mem_results = await self.memory_manager.retrieve_memories(
                query=query,
                memory_types=None,
                limit=top_k,
                user_id=user_id,
            )
            for layer, items in mem_results.items():
                for item in items:
                    metadata = item.get("metadata", {}) or {}
                    if metadata.get("type") == "teacher_student_analysis":
                        continue

                    candidates.append(
                        ContextItem(
                            content=item.get("content", ""),
                            score=item.get("relevance_score", 0.5),
                            source=f"memory:{layer}",
                            metadata=metadata,
                        )
                    )
        except Exception as e:
            logger.warning(f"[ContextGatherer] Memory gather failed: {e}")

        # Retrieval (vector + graph hybrid)
        try:
            retrieval_top_k = max(top_k * 3, 30)  # Request at least 30 documents
            
            # Query 1: General retrieval (may include teacher_student_analysis)
            general_results = await self.hybrid_retriever.retrieve(query, top_k=retrieval_top_k)
            
            # Query 2: Try to get documents with document_type (from ingestion)
            # Request many more documents to find ingestion documents that may have lower similarity scores
            ingestion_results = []
            try:
                # Request significantly more documents to increase chance of finding ingestion documents
                # that may rank lower due to embedding mismatch but are still relevant
                ingestion_results = await self.hybrid_retriever.retrieve_vector_only(
                    query=query,
                    top_k=retrieval_top_k * 5  # Request 5x more to find ingestion documents
                )
                
                logger.info(
                    f"[ContextGatherer] Vector-only retrieval found {len(ingestion_results)} documents "
                    f"(requested {retrieval_top_k * 5} to find ingestion documents)"
                )
            except Exception as e:
                logger.debug(f"[ContextGatherer] Vector-only retrieval failed: {e}")
            
            
            seen_docs = {}  # doc_id -> (doc, priority)
            
            for doc in ingestion_results:
                doc_id = doc.get("document_id") or doc.get("id")
                if doc_id:
                    inner_meta = doc.get("metadata", {}).get("metadata", {}) or {}
                    has_document_type = "document_type" in inner_meta
                    priority = 1 if has_document_type else 3  # Higher priority for ingestion docs
                    if doc_id not in seen_docs or seen_docs[doc_id][1] > priority:
                        seen_docs[doc_id] = (doc, priority)
            
            # Priority 2: General results that weren't already included
            for doc in general_results:
                doc_id = doc.get("document_id") or doc.get("id")
                if doc_id and doc_id not in seen_docs:
                    inner_meta = doc.get("metadata", {}).get("metadata", {}) or {}
                    has_document_type = "document_type" in inner_meta
                    priority = 2 if has_document_type else 4  # Lower priority for general docs
                    seen_docs[doc_id] = (doc, priority)
            
            # Sort by priority (lower number = higher priority), then by retrieval_score
            retrieval_results = sorted(
                [doc for doc, _ in seen_docs.values()],
                key=lambda d: (
                    seen_docs.get(d.get("document_id") or d.get("id"), (None, 999))[1],
                    -d.get("retrieval_score", 0)  # Negative for descending
                )
            )
            
            
            
            logger.info(
                f"[ContextGatherer] Combined retrieval: {len(retrieval_results)} unique documents "
                f"(from {len(general_results)} general + {len(ingestion_results)} ingestion-focused, "
                f"prioritized by document_type)"
            )
            
            # Log document types for debugging
            doc_types = {}
            filtered_count = 0
            added_count = 0
            
           
            
            for doc in retrieval_results:
                metadata = {k: v for k, v in doc.items() if k not in ("content", "retrieval_score", "score")}
                inner_metadata = metadata.get("metadata", {}) or {}
                
                doc_type = (
                    metadata.get("type") or  # Top level (episodic memory)
                    inner_metadata.get("type") or  # Inner metadata
                    inner_metadata.get("document_type") or  # Ingestion documents
                    "unknown"
                )
                doc_types[doc_type] = doc_types.get(doc_type, 0) + 1
                
                if doc_type == "teacher_student_analysis":
                    filtered_count += 1
                    continue
                
                score = doc.get("retrieval_score") or doc.get("score", 0.0)
                
                candidates.append(
                    ContextItem(
                        content=doc.get("content", ""),
                        score=float(score),
                        source="retrieval",
                        metadata=metadata,
                    )
                )
                added_count += 1
            
            # Log summary with document IDs for debugging
            sample_doc_ids = [doc.get("document_id", "unknown") for doc in retrieval_results[:5]]
            # Check if any documents have classroom_id pattern (from ingestion)
            ingestion_pattern_count = sum(
                1 for doc in retrieval_results 
                if doc.get("document_id", "").startswith("classroom_") 
                or (metadata.get("metadata", {}).get("classroom_id") is not None)
            )
            
            logger.info(
                f"[ContextGatherer] Document types retrieved: {doc_types} | "
                f"Filtered {filtered_count} teacher_student_analysis | "
                f"Added {added_count} classroom data documents | "
                f"Documents with ingestion pattern: {ingestion_pattern_count}/{len(retrieval_results)} | "
                f"Sample doc_ids: {sample_doc_ids}"
            )
            
            if ingestion_pattern_count == 0 and len(retrieval_results) > 0:
                logger.warning(
                    f"[ContextGatherer] No documents with ingestion pattern found. "
                    f"All {len(retrieval_results)} documents appear to be from memory, not ingestion. "
                    f"This suggests: 1) Ingestion documents not in vector store, "
                    f"2) Embeddings don't match query, 3) Documents overwritten by memory."
                )
            
            # Fallback: If no classroom data documents found, use a few teacher_student_analysis
            # documents with lower score to provide some context (better than empty context)
            if added_count == 0 and filtered_count > 0:
                logger.warning(
                    f"[ContextGatherer] WARNING: All {filtered_count} retrieved documents are teacher_student_analysis. "
                    f"No classroom data documents found. This may indicate: "
                    f"1) Classroom not ingested yet, 2) Only memory documents in vector store, "
                    f"3) Need to ingest classroom data first. "
                    f"Using {min(3, filtered_count)} teacher_student_analysis documents as fallback."
                )
                
                # Add a few teacher_student_analysis documents with reduced score as fallback
                fallback_count = 0
                for doc in retrieval_results:
                    if fallback_count >= 3:  # Limit to 3 fallback documents
                        break
                    
                    metadata = {k: v for k, v in doc.items() if k not in ("content", "retrieval_score", "score")}
                    inner_metadata = metadata.get("metadata", {}) or {}
                    doc_type = inner_metadata.get("type") or inner_metadata.get("document_type") or "unknown"
                    
                    if doc_type == "teacher_student_analysis":
                        # Use lower score for fallback documents
                        score = (doc.get("retrieval_score") or doc.get("score", 0.0)) * 0.3  # Reduce score by 70%
                        
                        candidates.append(
                            ContextItem(
                                content=doc.get("content", ""),
                                score=float(score),
                                source="retrieval:fallback",
                                metadata={**metadata, "fallback": True, "original_type": doc_type},
                            )
                        )
                        fallback_count += 1
                
                if fallback_count > 0:
                    logger.info(
                        f"[ContextGatherer] Added {fallback_count} fallback teacher_student_analysis documents "
                        f"with reduced scores (score * 0.3)"
                    )
        except Exception as e:
            logger.warning(f"[ContextGatherer] Retrieval gather failed: {e}")

        return candidates

