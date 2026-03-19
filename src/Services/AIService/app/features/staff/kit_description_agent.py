from typing import Dict, Any, Optional
import logging

from app.core.agent.reflection_agent import ReflectionContentAgent
from app.core.tools.registry import ToolRegistry
from app.core.tools.kit_data_tool import KitDataTool
from app.core.tools.component_analysis_tool import ComponentAnalysisTool
from app.core.tools.description_generator_tool import DescriptionGeneratorTool
from app.core.tools.rag_tool import RAGTool
from app.core.llm.client import LLMClient
from app.core.rag.hybrid_retriever import HybridRetriever

logger = logging.getLogger(__name__)


class KitDescriptionAgent(ReflectionContentAgent):
    """
    Kit Description Agent for Staff
    
    Reflection paradigm for quality kit description generation:
    - Uses ReflectionContentAgent base for iterative improvement
    - Integrates kit data, component analysis, and description generation
    - Similar pattern to Image3DDescriptionAgent
    """

    def __init__(
        self,
        llm: LLMClient,
        hybrid_retriever: HybridRetriever,
        kit_repository: Optional[Any] = None,
        use_remote: bool = False,
    ):
        system_prompt = """You are an expert at describing educational STEM kits.
Generate clear, accurate, educational descriptions for kits and their components.

Requirements:
- Accurate technical descriptions
- Educational context
- Component relationships
- Usage guidance

Use available tools to:
1. Get kit specifications
2. Analyze components
3. Search similar kit descriptions
4. Generate high-quality descriptions"""

        super().__init__(
            name="KitDescriptionAgent",
            llm=llm,
            system_prompt=system_prompt,
            max_iterations=3,
            use_remote=use_remote,
        )

        # Setup tools
        tool_registry = ToolRegistry()
        tool_registry.register_tool(KitDataTool(kit_repository=kit_repository))
        tool_registry.register_tool(ComponentAnalysisTool())
        tool_registry.register_tool(DescriptionGeneratorTool(llm=llm))
        tool_registry.register_tool(RAGTool(hybrid_retriever=hybrid_retriever))

        self.tool_registry = tool_registry

    async def generate_description(
        self,
        kit_id: str,
        context: Optional[str] = None,
    ) -> Dict[str, Any]:
        """
        Generate description for a kit
        
        Args:
            kit_id: Kit identifier
            context: Additional context (optional)
        
        Returns:
            Generated kit description with analysis
        """
        query = f"""Generate description for kit {kit_id}.
Context: {context or 'None'}

Use tools to:
1. Get kit specifications and components
2. Analyze component relationships and usage
3. Search for similar kit descriptions
4. Generate a comprehensive, educational description

Ensure the description includes:
- Kit overview and purpose
- Component list and descriptions
- Educational value
- Usage guidance
- Age range and difficulty level"""

        result = await self.run(query)
        return result

