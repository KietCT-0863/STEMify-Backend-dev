"""
Minions Coordinator - HazyResearch Minions Protocol (Decompose → Execute → Aggregate)
Integrates existing minions with optional local-first execution.
"""

from typing import Dict, Any, List, Optional
import logging
import time

from app.core.reasoning.minions.type.planner import PlannerMinion
from app.core.reasoning.minions.type.subgraph import SubgraphMinion
from app.core.reasoning.minions.type.causal import CausalMinion
from app.core.reasoning.minions.type.evidence import EvidenceMinion
from app.core.reasoning.minions.type.verifier import VerifierMinion
from app.core.reasoning.minions.type.synthesizer import SynthesizerMinion
from app.core.reasoning.tools import GraphTool, VectorTool, RerankTool, MathTool, ClockTool
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class MinionsCoordinator:
    """
    Coordinates minions execution:
    - Decompose (Planner)
    - Execute (Subgraph, Causal, Evidence, Verifier)
    - Aggregate (Synthesizer)
    """

    def __init__(
        self,
        graph_tool: GraphTool,
        vector_tool: VectorTool,
        rerank_tool: RerankTool,
        math_tool: MathTool,
        clock_tool: ClockTool,
        llm_client: Optional[LLMClient] = None,
        enable_local_first: bool = True,
    ):
        self.graph_tool = graph_tool
        self.vector_tool = vector_tool
        self.rerank_tool = rerank_tool
        self.math_tool = math_tool
        self.clock_tool = clock_tool
        self.llm_client = llm_client
        self.enable_local_first = enable_local_first

        # Instantiate minions
        common_kwargs = dict(
            graph_tool=graph_tool,
            vector_tool=vector_tool,
            rerank_tool=rerank_tool,
            math_tool=math_tool,
            clock_tool=clock_tool,
            llm_client=llm_client,
        )
        self.planner = PlannerMinion(**common_kwargs)
        self.subgraph = SubgraphMinion(**common_kwargs)
        self.causal = CausalMinion(**common_kwargs)
        self.evidence = EvidenceMinion(**common_kwargs)
        self.verifier = VerifierMinion(**common_kwargs)
        self.synthesizer = SynthesizerMinion(**common_kwargs)

    async def run(self, question: str) -> Dict[str, Any]:
        """
        Run the Minions protocol and return synthesized answer with metrics.
        """
        context: Dict[str, Any] = {"question": question, "previous_results": []}
        metrics: List[Dict[str, Any]] = []

        async def _exec(minion, ctx: Dict[str, Any]) -> Dict[str, Any]:
            start = time.time()
            result = await minion.execute(ctx)
            elapsed = time.time() - start
            metrics.append({"minion": minion.name, "duration_sec": elapsed})
            ctx["previous_results"].append({minion.name: result})
            ctx.update(result)
            return result

        # Decompose
        await _exec(self.planner, context)

        # Execute sequence (can be parallelized later)
        for minion in [self.subgraph, self.causal, self.evidence, self.verifier]:
            await _exec(minion, context)

        # Aggregate
        synth_result = await _exec(self.synthesizer, context)

        return {
            "answer": synth_result.get("answer"),
            "context": context,
            "metrics": metrics,
            "path": "minions",
        }

