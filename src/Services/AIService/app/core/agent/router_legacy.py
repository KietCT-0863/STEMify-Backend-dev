"""
Legacy Agent Router Wrapper
Wraps existing DecisionRouter to provide AgentRouter interface for migration
"""

from typing import Dict, Any, Optional
import logging

from app.core.agent.base_router import AgentRouter
from app.core.pipeline.decision_router import DecisionRouter, QueryPath, RoutingDecision

logger = logging.getLogger(__name__)


class LegacyAgentRouter(AgentRouter):
    """
    Legacy wrapper for existing DecisionRouter
    
    This wrapper adapts the existing DecisionRouter to the new AgentRouter interface,
    enabling backward compatibility during migration to the new Agent Framework.
    
    Migration Path:
    - Phase 0: LegacyAgentRouter wraps DecisionRouter (this file)
    - Phase 1: New AgentRouter implementation
    - Phase 8: Remove LegacyAgentRouter and DecisionRouter
    """
    
    def __init__(self, decision_router: DecisionRouter):
        """
        Initialize legacy router wrapper
        
        Args:
            decision_router: Existing DecisionRouter instance to wrap
        """
        if not decision_router:
            raise ValueError("DecisionRouter instance is required")
        
        self.decision_router = decision_router
        logger.info("LegacyAgentRouter initialized (wrapping DecisionRouter)")
    
    async def route(self, query: str, task_type: Optional[str] = None) -> Dict[str, Any]:
        """
        Route query using existing DecisionRouter logic
        
        This method:
        1. Uses DecisionRouter.route() to determine path (SIMPLE/COMPLEX)
        2. Calls appropriate processing method (process_simple_query/process_complex_query)
        3. Returns result in AgentRouter format
        
        Args:
            query: User query string
            task_type: Optional task type hint (ignored in legacy implementation)
        
        Returns:
            Dict containing:
                - answer: str - The generated answer
                - path: str - The path taken ("simple" or "complex")
                - metadata: Dict[str, Any] - Additional metadata
        """
        if not query or not query.strip():
            logger.warning("Empty query received")
            return {
                "answer": "Please provide a valid query.",
                "path": "unknown",
                "metadata": {"error": "empty_query"}
            }
        
        try:
            # Step 1: Use DecisionRouter to determine path
            routing_decision: RoutingDecision = self.decision_router.route(query)
            
            # Step 2: Process query based on path
            if routing_decision.path == QueryPath.SIMPLE:
                logger.debug(f"[LegacyRouter] Processing simple query: '{query[:50]}...'")
                result = await self.decision_router.process_simple_query(
                    query=query,
                    top_k=5,
                    rerank_top_k=3
                )
                
                # Ensure result has required fields
                return {
                    "answer": result.get("answer", "No answer generated"),
                    "path": "simple",
                    "metadata": {
                        **result.get("metadata", {}),
                        "routing_decision": {
                            "path": routing_decision.path.value,
                            "complexity_score": routing_decision.complexity.score,
                            "reasoning": routing_decision.reasoning
                        },
                        "legacy_wrapper": True
                    }
                }
            
            elif routing_decision.path == QueryPath.COMPLEX:
                logger.debug(f"[LegacyRouter] Processing complex query: '{query[:50]}...'")
                result = await self.decision_router.process_complex_query(
                    query=query,
                    use_agent_layer=False
                )
                
                # Extract answer and format metadata
                answer = result.get("answer", "No answer generated")
                reasoning_result = result.get("reasoning_result", {})
                
                return {
                    "answer": answer,
                    "path": "complex",
                    "metadata": {
                        **result.get("metadata", {}),
                        "routing_decision": {
                            "path": routing_decision.path.value,
                            "complexity_score": routing_decision.complexity.score,
                            "reasoning": routing_decision.reasoning
                        },
                        "reasoning_result": {
                            "confidence": reasoning_result.get("confidence", 0.0),
                            "findings": reasoning_result.get("findings", []),
                            "evidence_count": reasoning_result.get("evidence_count", 0)
                        },
                        "legacy_wrapper": True
                    }
                }
            
            else:
                # Unknown path - default to simple
                logger.warning(f"[LegacyRouter] Unknown path '{routing_decision.path}', defaulting to simple")
                result = await self.decision_router.process_simple_query(
                    query=query,
                    top_k=5,
                    rerank_top_k=3
                )
                
                return {
                    "answer": result.get("answer", "No answer generated"),
                    "path": "simple",
                    "metadata": {
                        **result.get("metadata", {}),
                        "routing_decision": {
                            "path": "simple",
                            "complexity_score": routing_decision.complexity.score,
                            "reasoning": f"Unknown path, defaulted to simple: {routing_decision.reasoning}"
                        },
                        "legacy_wrapper": True,
                        "defaulted": True
                    }
                }
        
        except Exception as e:
            logger.error(f"[LegacyRouter] Error processing query: {e}", exc_info=True)
            return {
                "answer": f"I encountered an error while processing your query: {str(e)}. Please try again.",
                "path": "error",
                "metadata": {
                    "error": str(e),
                    "legacy_wrapper": True
                }
            }

