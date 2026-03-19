from typing import Dict, Any, Optional
import logging
import json

from app.core.agent.reflection_agent import ReflectionContentAgent
from app.core.tools.registry import ToolRegistry
from app.core.tools.curriculum_template_tool import CurriculumTemplateTool
from app.core.tools.content_generator_tool import ContentGeneratorTool
from app.core.tools.structure_validator_tool import StructureValidatorTool
from app.core.tools.rag_tool import RAGTool
from app.core.llm.client import LLMClient
from app.core.rag.hybrid_retriever import HybridRetriever

logger = logging.getLogger(__name__)


class CourseGeneratorAgent(ReflectionContentAgent):
    """
    Course/Curriculum Generator Agent for Staff
    
    Reflection paradigm for quality content generation:
    - Uses ReflectionContentAgent base for iterative improvement
    - Integrates curriculum templates, content generation, and validation tools
    - Searches educational standards via RAG
    """

    def __init__(
        self,
        llm: LLMClient,
        hybrid_retriever: HybridRetriever,
        templates: Optional[list] = None,
        use_remote: bool = False,
    ):
        system_prompt = """You are an expert curriculum designer.
Create comprehensive courses and curricula following educational best practices.

Requirements:
- Align with educational standards
- Progressive difficulty
- Clear learning objectives
- Engaging content structure
- Assessment integration

Use available tools to:
1. Retrieve curriculum templates
2. Generate course content
3. Validate structure
4. Search educational standards"""

        super().__init__(
            name="CourseGeneratorAgent",
            llm=llm,
            system_prompt=system_prompt,
            max_iterations=3,
            use_remote=use_remote,
        )

        # Setup tools
        tool_registry = ToolRegistry()
        tool_registry.register_tool(CurriculumTemplateTool(templates=templates))
        tool_registry.register_tool(ContentGeneratorTool(llm=llm))
        tool_registry.register_tool(StructureValidatorTool())
        tool_registry.register_tool(RAGTool(hybrid_retriever=hybrid_retriever))

        self.tool_registry = tool_registry

    async def generate_course(
        self,
        subject: str,
        level: str,
        duration: str,
        requirements: Optional[Dict[str, Any]] = None,
    ) -> Dict[str, Any]:
        """
        Generate course/curriculum
        
        Args:
            subject: Subject name (e.g., "Math", "Science", "STEM")
            level: Education level (e.g., "Elementary", "Middle", "High")
            duration: Course duration (e.g., "8 weeks", "1 semester")
            requirements: Additional requirements dict
        
        Returns:
            Generated course structure with content
        """
        requirements_str = ""
        if requirements:
            requirements_str = f"\nAdditional requirements: {json.dumps(requirements, indent=2)}"

        query = f"""Create a {subject} course for {level} level, duration {duration}.
{requirements_str}

Generate a complete curriculum with:
- Course overview and objectives
- Module structure
- Lesson plans
- Assessment strategy
- Resources and references

Ensure alignment with educational standards and best practices."""

        result = await self.run(query)
        return result

