from typing import Dict, Any, Optional
import logging
import json

from app.core.agent.plan_solve_agent import PlanAndSolveInsightsAgent
from app.core.tools.registry import ToolRegistry
from app.core.tools.model_analysis_tool import ModelAnalysisTool
from app.core.tools.step_generator_tool import StepGeneratorTool
from app.core.tools.visualization_tool import VisualizationTool
from app.core.tools.validation_tool import ValidationTool
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class StepDescriptionAgent(PlanAndSolveInsightsAgent):
    """
    3D Model Step Description Generator Agent for Staff
    
    Plan-and-Solve paradigm for structured step generation:
    - Plans the step sequence
    - Generates each step
    - Validates the sequence
    """

    def __init__(
        self,
        llm: LLMClient,
        use_remote: bool = False,
    ):
        system_prompt = """You are an expert at creating step-by-step instructions for 3D models.
Generate clear, sequential instructions for assembling or using 3D models.

Requirements:
- Logical sequence
- Clear instructions
- Safety considerations
- Visual references

Use available tools to:
1. Analyze model structure
2. Generate step-by-step instructions
3. Create visual aids
4. Validate the sequence"""

        super().__init__(
            name="StepDescriptionAgent",
            llm=llm,
            system_prompt=system_prompt,
            use_remote=use_remote,
        )

        # Setup tools
        tool_registry = ToolRegistry()
        tool_registry.register_tool(ModelAnalysisTool())
        tool_registry.register_tool(StepGeneratorTool(llm=llm))
        tool_registry.register_tool(VisualizationTool())
        tool_registry.register_tool(ValidationTool())

        self.tool_registry = tool_registry

    async def generate_steps(
        self,
        model_id: str,
        action_type: str = "assembly",  # assembly, usage, disassembly
        model_data: Optional[Dict[str, Any]] = None,
    ) -> Dict[str, Any]:
        """
        Generate step-by-step instructions
        
        Args:
            model_id: Model identifier
            action_type: Type of action (assembly, usage, disassembly)
            model_data: Model structure data (optional)
        
        Returns:
            Generated step-by-step instructions with validation
        """
        query = f"""Generate {action_type} steps for 3D model {model_id}.

Model data: {json.dumps(model_data, indent=2) if model_data else 'Not provided'}

Plan and execute:
1. Analyze the model structure
2. Generate clear step-by-step instructions
3. Create visual aid suggestions
4. Validate the sequence for logical flow and safety

Ensure steps are:
- In logical order
- Clear and easy to follow
- Include safety considerations
- Reference visual aids where helpful"""

        result = await self.run(query)
        return result

