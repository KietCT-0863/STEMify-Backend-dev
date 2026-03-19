from typing import Dict, Any, Optional
import logging

from app.core.agent.plan_solve_agent import PlanAndSolveInsightsAgent
from app.core.tools.registry import ToolRegistry
from app.core.tools.content_analysis_tool import ContentAnalysisTool
from app.core.tools.category_taxonomy_tool import CategoryTaxonomyTool
from app.core.tools.classification_tool import ClassificationTool
from app.core.tools.list_generator_tool import ListGeneratorTool
from app.core.tools.rag_tool import RAGTool
from app.core.llm.client import LLMClient
from app.core.rag.hybrid_retriever import HybridRetriever

logger = logging.getLogger(__name__)


class STEMCategoryAgent(PlanAndSolveInsightsAgent):
    """
    STEM Category List Generator Agent for Staff
    
    Plan-and-Solve paradigm for structured categorization:
    - Plans categorization strategy
    - Analyzes content
    - Classifies into categories
    - Generates organized lists
    """

    def __init__(
        self,
        llm: LLMClient,
        hybrid_retriever: HybridRetriever,
        taxonomy: Optional[Dict[str, Any]] = None,
        use_remote: bool = False,
    ):
        system_prompt = """You are an expert at categorizing STEM content.
Create comprehensive, organized category lists for STEM educational materials.

Requirements:
- Logical categorization
- Hierarchical structure
- Comprehensive coverage
- Clear naming

Use available tools to:
1. Analyze content features
2. Access category taxonomy
3. Classify content
4. Generate organized lists"""

        super().__init__(
            name="STEMCategoryAgent",
            llm=llm,
            system_prompt=system_prompt,
            use_remote=use_remote,
        )

        # Setup tools
        tool_registry = ToolRegistry()
        tool_registry.register_tool(ContentAnalysisTool())
        tool_registry.register_tool(CategoryTaxonomyTool(taxonomy=taxonomy))
        tool_registry.register_tool(ClassificationTool(llm=llm))
        tool_registry.register_tool(ListGeneratorTool())
        tool_registry.register_tool(RAGTool(hybrid_retriever=hybrid_retriever))

        self.tool_registry = tool_registry

    async def generate_categories(
        self,
        content_type: str,  # course, lesson, kit, model
        scope: str = "comprehensive",  # basic, comprehensive, advanced
        content_items: Optional[list] = None,
    ) -> Dict[str, Any]:
        """
        Generate STEM category list
        
        Args:
            content_type: Type of content (course, lesson, kit, model)
            scope: Scope of categorization (basic, comprehensive, advanced)
            content_items: Optional list of content items to categorize
        
        Returns:
            Generated category list with organization
        """
        query = f"""Generate {scope} category list for {content_type} in STEM education.

Content items: {len(content_items) if content_items else 'Not provided'}

Plan and execute:
1. Analyze content features and characteristics
2. Access category taxonomy
3. Classify content into appropriate categories
4. Generate organized, hierarchical category list

Ensure the list is:
- Logically organized
- Hierarchical structure
- Comprehensive coverage
- Clear naming conventions"""

        result = await self.run(query)
        return result

