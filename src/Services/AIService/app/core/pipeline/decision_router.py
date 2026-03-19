"""
Decision Router
Routes queries to simple (direct) or complex (reasoning) paths based on complexity analysis
"""

from typing import Dict, Any, Optional, List
from dataclasses import dataclass
from enum import Enum
import logging

from app.core.query.complexity_analyzer import QueryComplexityAnalyzer, QueryComplexity, ComplexityClassification
from app.core.rag.hybrid_retriever import HybridRetriever
from app.core.reasoning.orchestrator import GraphReasoningOrchestrator
from app.core.llm.client import LLMClient
from app.core.reranker import BaseReranker

logger = logging.getLogger(__name__)


class QueryPath(str, Enum):
    """Query processing path"""
    SIMPLE = "simple"  # Fast path: Direct LLM generation
    COMPLEX = "complex"  # Complex path: Reasoning engine → LLM generation
    UNKNOWN = "unknown"  # Default to simple path


@dataclass
class RoutingDecision:
    """Decision point routing result"""
    path: QueryPath
    complexity: QueryComplexity
    reasoning: str
    metadata: Dict[str, Any] = None


class DecisionRouter:
    """
    Decision router for query processing
    
    Routes queries based on complexity:
    - Simple queries → Fast path (Hybrid Retrieval → Reranker → LLM Generation)
    - Complex queries → Complex path (Reasoning Engine → LLM Generation)
    """
    
    def __init__(
        self,
        complexity_analyzer: Optional[QueryComplexityAnalyzer] = None,
        hybrid_retriever: Optional[HybridRetriever] = None,
        reasoning_orchestrator: Optional[GraphReasoningOrchestrator] = None,
        llm_client: Optional[LLMClient] = None,
        default_to_simple: bool = True
    ):
        """
        Initialize decision router
        
        Args:
            complexity_analyzer: Query complexity analyzer (created if None)
            hybrid_retriever: Hybrid retriever for simple path (optional, can be set later)
            reasoning_orchestrator: Reasoning orchestrator for complex path (optional, can be set later)
            llm_client: LLM client for generation (optional, can be set later)
            default_to_simple: If True, unknown complexity defaults to simple path
        """
        self.complexity_analyzer = complexity_analyzer or QueryComplexityAnalyzer()
        self.hybrid_retriever = hybrid_retriever
        self.reasoning_orchestrator = reasoning_orchestrator
        self.llm_client = llm_client
        self.default_to_simple = default_to_simple
        
        logger.info("DecisionRouter initialized")
    
    def route(self, query: str) -> RoutingDecision:
        """
        Determine routing path for query
        
        Args:
            query: User query string
            
        Returns:
            RoutingDecision with path and reasoning
        """
        # Analyze complexity
        complexity = self.complexity_analyzer.analyze(query)
        
        # Determine path
        if complexity.classification == ComplexityClassification.SIMPLE:
            path = QueryPath.SIMPLE
            reasoning = f"Simple query detected: {complexity.reasoning}"
        elif complexity.classification == ComplexityClassification.COMPLEX:
            path = QueryPath.COMPLEX
            reasoning = f"Complex query detected: {complexity.reasoning}"
        else:
            # Unknown complexity - use default
            path = QueryPath.SIMPLE if self.default_to_simple else QueryPath.UNKNOWN
            reasoning = f"Unknown complexity, defaulting to {path.value}: {complexity.reasoning}"
        
        metadata = {
            "complexity_score": complexity.score,
            "factors": complexity.factors,
            "default_used": complexity.classification == ComplexityClassification.UNKNOWN
        }
        
        logger.info(f"Query routed to {path.value} path: '{query[:50]}...' (score: {complexity.score:.2f})")
        
        return RoutingDecision(
            path=path,
            complexity=complexity,
            reasoning=reasoning,
            metadata=metadata
        )
    
    async def process_simple_query(
        self,
        query: str,
        top_k: int = 5,
        rerank_top_k: int = 3,
        filters: Optional[Dict[str, Any]] = None
    ) -> Dict[str, Any]:
        """
        Process simple query through fast path
        
        Flow:
        1. Hybrid Retrieval (vector + graph)
        2. Reranker
        3. LLM Generation (direct)
        
        Args:
            query: User query
            top_k: Number of documents to retrieve
            rerank_top_k: Number of documents after reranking
            filters: Optional metadata filters
            
        Returns:
            Response with answer and metadata
        """
        if not self.hybrid_retriever:
            raise ValueError("HybridRetriever not set. Cannot process simple query.")
        
        if not self.llm_client:
            raise ValueError("LLMClient not set. Cannot generate answer.")
        
        logger.info(f"[SIMPLE PATH] Processing query: '{query[:50]}...'")
        
        try:
            # Step 1: Hybrid Retrieval
            logger.debug("[SIMPLE PATH] Step 1: Hybrid retrieval...")
            retrieved_docs = await self.hybrid_retriever.retrieve(
                query=query,
                top_k=top_k * 2,  # Retrieve more for reranking
                filters=filters
            )
            logger.debug(f"[SIMPLE PATH] Retrieved {len(retrieved_docs)} documents")
            
            # Step 2: Rerank (already done in hybrid_retriever, but we can rerank again if needed)
            # The hybrid_retriever already includes reranking, so we just take top_k
            reranked_docs = retrieved_docs[:rerank_top_k]
            logger.debug(f"[SIMPLE PATH] Selected top {len(reranked_docs)} documents after reranking")
            
            # Step 3: LLM Generation
            logger.debug("[SIMPLE PATH] Step 3: LLM generation...")
            answer = await self._generate_answer(query, reranked_docs)
            
            return {
                "answer": answer,
                "path": "simple",
                "documents_used": len(reranked_docs),
                "metadata": {
                    "retrieval_count": len(retrieved_docs),
                    "rerank_count": len(reranked_docs),
                    "generation_method": "direct"
                }
            }
            
        except Exception as e:
            logger.error(f"[SIMPLE PATH] Error processing query: {e}")
            raise
    
    async def process_complex_query(
        self,
        query: str,
        use_agent_layer: bool = False
    ) -> Dict[str, Any]:
        """
        Process complex query through reasoning path
        
        Flow:
        1. Graph Reasoning Engine (with minions)
        2. Optional: Agent Layer (planning, self-retrieve, verification)
        3. LLM Generation (with reasoning context)
        
        Args:
            query: User query
            use_agent_layer: Whether to use agent layer (optional, not yet implemented)
            
        Returns:
            Response with answer and reasoning metadata
        """
        if not self.reasoning_orchestrator:
            raise ValueError("GraphReasoningOrchestrator not set. Cannot process complex query.")
        
        if not self.llm_client:
            raise ValueError("LLMClient not set. Cannot generate answer.")
        
        logger.info(f"[COMPLEX PATH] Processing query: '{query[:50]}...'")
        
        try:
            # Step 1: Graph Reasoning
            logger.debug("[COMPLEX PATH] Step 1: Graph reasoning...")
            reasoning_result = await self.reasoning_orchestrator.reason(query)
            logger.debug(f"[COMPLEX PATH] Reasoning completed: {reasoning_result.answer[:100] if reasoning_result.answer else 'No answer'}...")
            
            # Step 2: Optional Agent Layer (not yet implemented)
            if use_agent_layer:
                logger.warning("[COMPLEX PATH] Agent layer not yet implemented, skipping...")
                # TODO: Implement agent layer
            
            # Step 3: LLM Generation with reasoning context
            logger.debug("[COMPLEX PATH] Step 3: LLM generation with reasoning context...")
            
            # Use reasoning result as context for LLM generation
            context = self._format_reasoning_context(reasoning_result)
            answer = await self._generate_answer(query, context_documents=[context])
            
            return {
                "answer": answer,
                "path": "complex",
                "reasoning_result": {
                    "answer": reasoning_result.answer,
                    "confidence": reasoning_result.confidence,
                    "findings": reasoning_result.findings,
                    "evidence_count": len(reasoning_result.evidence_packs) if hasattr(reasoning_result, 'evidence_packs') else 0
                },
                "metadata": {
                    "reasoning_used": True,
                    "agent_layer_used": use_agent_layer,
                    "generation_method": "reasoning_enhanced"
                }
            }
            
        except Exception as e:
            logger.error(f"[COMPLEX PATH] Error processing query: {e}")
            raise
    
    async def process_query(
        self,
        query: str,
        top_k: int = 5,
        rerank_top_k: int = 3,
        filters: Optional[Dict[str, Any]] = None,
        use_agent_layer: bool = False
    ) -> Dict[str, Any]:
        """
        Process query through appropriate path based on complexity
        
        Args:
            query: User query
            top_k: Number of documents to retrieve (for simple path)
            rerank_top_k: Number of documents after reranking (for simple path)
            filters: Optional metadata filters (for simple path)
            use_agent_layer: Whether to use agent layer (for complex path)
            
        Returns:
            Response with answer and metadata
        """
        # Route query
        decision = self.route(query)
        
        # Process based on path
        if decision.path == QueryPath.SIMPLE:
            result = await self.process_simple_query(
                query=query,
                top_k=top_k,
                rerank_top_k=rerank_top_k,
                filters=filters
            )
            result["routing_decision"] = decision
            return result
        
        elif decision.path == QueryPath.COMPLEX:
            result = await self.process_complex_query(
                query=query,
                use_agent_layer=use_agent_layer
            )
            result["routing_decision"] = decision
            return result
        
        else:
            # Unknown path - default to simple
            logger.warning(f"Unknown path, defaulting to simple: {decision.path}")
            result = await self.process_simple_query(
                query=query,
                top_k=top_k,
                rerank_top_k=rerank_top_k,
                filters=filters
            )
            result["routing_decision"] = decision
            return result
    
    async def _generate_answer(
        self,
        query: str,
        context_documents: Optional[List[Dict[str, Any]]] = None
    ) -> str:
        """
        Generate answer using LLM
        
        Args:
            query: User query
            context_documents: Context documents for generation
            
        Returns:
            Generated answer string
        """
        if not self.llm_client:
            raise ValueError("LLMClient not set")
        
        from app.core.llm.providers.base_provider import LLMMessage
        
        # Format context
        context_text = ""
        if context_documents:
            if isinstance(context_documents[0], dict):
                # Format documents
                context_parts = []
                for i, doc in enumerate(context_documents, 1):
                    content = doc.get("content", "")
                    metadata = doc.get("metadata", {})
                    doc_type = metadata.get("document_type", "document")
                    context_parts.append(f"[{i}] {doc_type}: {content[:500]}...")
                context_text = "\n\n".join(context_parts)
            else:
                # Already formatted text
                context_text = "\n\n".join(str(doc) for doc in context_documents)
        
        # Create messages
        messages = []
        
        if context_text:
            system_message = "You are a helpful teaching assistant. Answer questions based on the provided context."
            messages.append(LLMMessage(role="system", content=system_message))
            
            user_content = f"""Based on the following context, answer the question.

Context:
{context_text}

Question: {query}

Answer:"""
            messages.append(LLMMessage(role="user", content=user_content))
        else:
            user_content = f"Answer the following question: {query}"
            messages.append(LLMMessage(role="user", content=user_content))
        
        # Generate
        response = await self.llm_client.generate(
            messages=messages,
            use_remote=True,  # Use remote for better quality
            max_tokens=500,
            temperature=0.7
        )
        
        return response.content if hasattr(response, 'content') else str(response)
    
    def _format_reasoning_context(self, reasoning_result) -> Dict[str, Any]:
        """Format reasoning result as context document"""
        context_parts = []
        
        if hasattr(reasoning_result, 'answer') and reasoning_result.answer:
            context_parts.append(f"Reasoning Answer: {reasoning_result.answer}")
        
        if hasattr(reasoning_result, 'findings') and reasoning_result.findings:
            findings_text = "\n".join([f"- {f}" for f in reasoning_result.findings])
            context_parts.append(f"Findings:\n{findings_text}")
        
        if hasattr(reasoning_result, 'evidence_packs') and reasoning_result.evidence_packs:
            evidence_text = "\n".join([f"- {ep}" for ep in reasoning_result.evidence_packs[:3]])
            context_parts.append(f"Evidence:\n{evidence_text}")
        
        context_text = "\n\n".join(context_parts)
        
        return {
            "content": context_text,
            "metadata": {
                "document_type": "reasoning_result",
                "confidence": getattr(reasoning_result, 'confidence', 0.0),
                "source": "graph_reasoning"
            }
        }

