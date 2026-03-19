from typing import Optional

from app.core.tools.registry import ToolRegistry
from app.core.tools.rag_tool import RAGTool
from app.core.tools.graph_reasoning_tool import GraphReasoningTool
from app.core.tools.llm_generation_tool import LLMGenerationTool
from app.core.tools.memory_tool import MemoryTool
from app.core.tools.context_builder_tool import ContextBuilderTool
from app.core.tools.minions_tool import MinionsTool

from app.core.rag.hybrid_retriever import HybridRetriever
from app.core.reasoning.orchestrator import GraphReasoningOrchestrator
from app.core.llm.client import LLMClient
from app.core.memory.memory_manager import MemoryManager
from app.core.context.builder import JITContextBuilder
from app.core.reasoning.minions.coordinator import MinionsCoordinator


def create_default_registry(
    hybrid_retriever: HybridRetriever,
    reasoning_orchestrator: GraphReasoningOrchestrator,
    llm_client: LLMClient,
    memory_manager: MemoryManager,
    context_builder: Optional[JITContextBuilder] = None,
    minions_coordinator: Optional[MinionsCoordinator] = None,
) -> ToolRegistry:
    registry = ToolRegistry()

    registry.register_tool(RAGTool(hybrid_retriever))
    registry.register_tool(GraphReasoningTool(reasoning_orchestrator))
    registry.register_tool(LLMGenerationTool(llm_client))
    registry.register_tool(MemoryTool(memory_manager))

    if context_builder:
        registry.register_tool(ContextBuilderTool(context_builder))

    if minions_coordinator:
        registry.register_tool(MinionsTool(minions_coordinator))

    return registry

