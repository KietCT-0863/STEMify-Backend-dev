from typing import Dict, Any, Optional
import logging
import json

from app.core.agent.reflection_agent import ReflectionContentAgent
from app.core.tools.registry import ToolRegistry
from app.core.tools.image_analysis_tool import ImageAnalysisTool
from app.core.tools.vision_tool import VisionTool
from app.core.tools.description_generator_tool import DescriptionGeneratorTool
from app.core.tools.terminology_tool import TerminologyTool
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class Image3DDescriptionAgent(ReflectionContentAgent):
    """
    3D Emulator Image Description Agent for Staff
    
    Reflection paradigm for quality description generation:
    - Uses ReflectionContentAgent base for iterative improvement
    - Integrates image analysis, vision, terminology, and description generation
    """

    def __init__(
        self,
        llm: LLMClient,
        vision_llm: Optional[LLMClient] = None,
        terminology_db: Optional[Dict[str, Any]] = None,
        use_remote: bool = False,
    ):
        system_prompt = """You are an expert at describing 3D educational models.
Generate clear, accurate, educational descriptions from 3D emulator images.

Requirements:
- Accurate technical descriptions
- Educational context
- Clear terminology
- Step-by-step if applicable

Use available tools to:
1. Analyze images
2. Understand visual content with vision model
3. Access STEM terminology
4. Generate high-quality descriptions"""

        super().__init__(
            name="Image3DDescriptionAgent",
            llm=llm,
            system_prompt=system_prompt,
            max_iterations=3,
            use_remote=use_remote,
        )

        # Setup tools
        tool_registry = ToolRegistry()
        tool_registry.register_tool(ImageAnalysisTool())
        tool_registry.register_tool(VisionTool(llm=vision_llm or llm))
        tool_registry.register_tool(DescriptionGeneratorTool(llm=llm))
        tool_registry.register_tool(TerminologyTool(terminology_db=terminology_db))

        self.tool_registry = tool_registry

    async def generate_description(
        self,
        image_path: Optional[str] = None,
        image_base64: Optional[str] = None,
        model_type: str = "unknown",
        context: Optional[str] = None,
    ) -> Dict[str, Any]:
        """
        Generate description from 3D emulator image
        
        Args:
            image_path: Path to image file
            image_base64: Base64 encoded image data
            model_type: Type of 3D model (e.g., microbit, arduino)
            context: Additional context (optional)
        
        Returns:
            Generated description with analysis results
        """
        if not image_path and not image_base64:
            return {"error": "Either image_path or image_base64 required"}

        query = f"""Generate description for 3D {model_type} model from image.
Image: {'path: ' + image_path if image_path else 'base64 data provided'}
Context: {context or 'None'}

Use tools to:
1. Analyze the image
2. Understand visual content
3. Access relevant terminology
4. Generate a clear, educational description

Ensure the description is:
- Technically accurate
- Educational and clear
- Uses proper STEM terminology
- Includes relevant context"""

        result = await self.run(query)
        return result

