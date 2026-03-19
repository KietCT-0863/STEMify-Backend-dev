"""
Graph Reasoning Orchestrator
Coordinates the two-phase reasoning pipeline with minions
"""

from typing import Dict, Any, Optional
import logging

from app.core.reasoning.models import ReasoningResult, ReasoningPlan
from app.core.reasoning.minions import (
    PlannerMinion,
    SubgraphMinion,
    CausalMinion,
    EvidenceMinion,
    VerifierMinion,
    SynthesizerMinion
)
from app.core.reasoning.tools import (
    GraphTool,
    VectorTool,
    RerankTool,
    MathTool,
    ClockTool
)
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class GraphReasoningOrchestrator:
    """
    Orchestrator for Graph Reasoning Engine
    
    Follows a TWO-PHASE pipeline:
    1. PHASE 1: SUBGRAPH EXPANSION
       - Parse question into entities and constraints
       - Generate Cypher queries
       - Retrieve subgraph
    
    2. PHASE 2: CAUSE-EFFECT REASONING
       - Detect causal patterns
       - Triangulate with vector evidence
       - Produce grounded explanation
    """
    
    def __init__(
        self,
        graph_tool: GraphTool,
        vector_tool: VectorTool,
        rerank_tool: RerankTool,
        math_tool: MathTool,
        clock_tool: ClockTool,
        llm_client: Optional[LLMClient] = None
    ):
        """
        Initialize orchestrator with tools
        
        Args:
            graph_tool: Graph database tool
            vector_tool: Vector search tool
            rerank_tool: Reranking tool
            math_tool: Statistical operations tool
            clock_tool: Time operations tool
            llm_client: LLM client for Local/Remote LLM access
        """
        self.graph_tool = graph_tool
        self.vector_tool = vector_tool
        self.rerank_tool = rerank_tool
        self.math_tool = math_tool
        self.clock_tool = clock_tool
        self.llm_client = llm_client
        
        # Initialize minions with LLM client
        # Local LLM minions: Planner, Verifier
        # Remote LLM minions: Causal, Synthesizer
        self.planner = PlannerMinion(
            graph_tool, vector_tool, rerank_tool, math_tool, clock_tool, llm_client
        )
        self.subgraph = SubgraphMinion(
            graph_tool, vector_tool, rerank_tool, math_tool, clock_tool, llm_client
        )
        self.causal = CausalMinion(
            graph_tool, vector_tool, rerank_tool, math_tool, clock_tool, llm_client
        )
        self.evidence = EvidenceMinion(
            graph_tool, vector_tool, rerank_tool, math_tool, clock_tool, llm_client
        )
        self.verifier = VerifierMinion(
            graph_tool, vector_tool, rerank_tool, math_tool, clock_tool, llm_client
        )
        self.synthesizer = SynthesizerMinion(
            graph_tool, vector_tool, rerank_tool, math_tool, clock_tool, llm_client
        )
        
        logger.info("Graph Reasoning Orchestrator initialized")
    
    async def reason(self, question: str) -> ReasoningResult:
        """
        Execute reasoning pipeline
        
        Args:
            question: Natural language question from teacher
        
        Returns:
            ReasoningResult with grounded insights
        """
        logger.info(f"Starting reasoning for question: {question[:100]}...")
        
        # Shared context for minions
        context: Dict[str, Any] = {
            "question": question
        }
        
        executed_minions = []
        
        try:
            # ============================================================
            # PHASE 1: SUBGRAPH EXPANSION
            # ============================================================
            
            # Step 1: Planner - Decompose question
            logger.info("Phase 1: Subgraph Expansion")
            logger.info("Step 1: Planning...")
            planner_result = await self.planner.execute(context)
            context.update(planner_result)
            executed_minions.append("Planner")
            
            plan: ReasoningPlan = planner_result.get("plan")
            if not plan:
                logger.warning("Planner did not create a plan")
                return self._create_error_result(question, "Failed to create reasoning plan")
            
            # Step 2: Subgraph - Expand subgraph
            logger.info("Step 2: Expanding subgraph...")
            subgraph_result = await self.subgraph.execute(context)
            context.update(subgraph_result)
            executed_minions.append("Subgraph")
            
            # ============================================================
            # PHASE 2: CAUSE-EFFECT REASONING
            # ============================================================
            
            logger.info("Phase 2: Cause-Effect Reasoning")
            
            # Step 3: Causal - Test hypotheses
            logger.info("Step 3: Testing causal hypotheses...")
            causal_result = await self.causal.execute(context)
            context.update(causal_result)
            executed_minions.append("Causal")
            
            # Step 4: Evidence - Assemble evidence pack
            logger.info("Step 4: Assembling evidence pack...")
            evidence_result = await self.evidence.execute(context)
            context.update(evidence_result)
            executed_minions.append("Evidence")
            
            # Step 5: Verifier - Check for gaps (before synthesizer)
            logger.info("Step 5: Verifying results...")
            # Create temporary result for verifier (before final synthesis)
            from app.core.reasoning.models import EvidencePack
            temp_result = ReasoningResult(
                plan=planner_result.get("plan_description", ""),
                cypher=subgraph_result.get("cypher", []),
                graph_sample=subgraph_result.get("graph_sample", {"nodes": [], "edges": []}),
                causal_findings=causal_result.get("causal_findings", []),
                evidence_pack=evidence_result.get("evidence_pack", EvidencePack()),
                answer_teacher_friendly="",  # Not yet generated
                next_actions=[],
                audit={}
            )
            context["result"] = temp_result
            verifier_result = await self.verifier.execute(context)
            context.update(verifier_result)
            executed_minions.append("Verifier")
            
            # Step 6: Synthesizer - Generate final answer
            logger.info("Step 6: Synthesizing answer...")
            synthesizer_result = await self.synthesizer.execute(context)
            context.update(synthesizer_result)
            executed_minions.append("Synthesizer")
            
            # ============================================================
            # ASSEMBLE RESULT
            # ============================================================
            
            result = ReasoningResult(
                plan=planner_result.get("plan_description", ""),
                cypher=subgraph_result.get("cypher", []),
                graph_sample=subgraph_result.get("graph_sample", {"nodes": [], "edges": []}),
                causal_findings=causal_result.get("causal_findings", []),
                evidence_pack=evidence_result.get("evidence_pack"),
                answer_teacher_friendly=synthesizer_result.get("answer_teacher_friendly", ""),
                next_actions=synthesizer_result.get("next_actions", []),
                audit={
                    "time": self.clock_tool.now(),
                    "minions": executed_minions,
                    "verification": verifier_result.get("verification", {})
                }
            )
            
            logger.info("Reasoning pipeline complete")
            return result
            
        except Exception as e:
            logger.error(f"Error in reasoning pipeline: {e}", exc_info=True)
            return self._create_error_result(question, str(e), executed_minions)
    
    def _create_error_result(
        self,
        question: str,
        error_message: str,
        executed_minions: Optional[list] = None
    ) -> ReasoningResult:
        """Create error result when pipeline fails"""
        from app.core.reasoning.models import EvidencePack
        
        return ReasoningResult(
            plan=f"Error: {error_message}",
            cypher=[],
            graph_sample={"nodes": [], "edges": []},
            causal_findings=[],
            evidence_pack=EvidencePack(),
            answer_teacher_friendly=(
                f"I encountered an error while analyzing your question: {error_message}. "
                "Please try rephrasing your question or contact support if the issue persists."
            ),
            next_actions=[
                "Try rephrasing your question",
                "Check if the required data is available in the system"
            ],
            audit={
                "time": self.clock_tool.now(),
                "minions": executed_minions or [],
                "error": error_message
            }
        )

