"""
Graph Reasoning Tool
MCP-compatible tool wrapping GraphReasoningOrchestrator
"""

from typing import Dict, Any
import logging
import json

from app.core.tools.base import Tool
from app.core.reasoning.orchestrator import GraphReasoningOrchestrator

logger = logging.getLogger(__name__)


class GraphReasoningTool(Tool):
    """Graph Reasoning Tool - MCP-compatible"""
    
    def __init__(self, reasoning_orchestrator: GraphReasoningOrchestrator):
        super().__init__(
            name="graph_reasoning",
            description="Perform graph-based reasoning on educational data"
        )
        self.reasoning_orchestrator = reasoning_orchestrator
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """Execute graph reasoning"""
        question = parameters.get("question", "")
        
        try:
            result = await self.reasoning_orchestrator.reason(question)
            
            # Format result
            output = f"Answer: {result.answer_teacher_friendly}\n\n"
            
            if hasattr(result, 'findings') and result.findings:
                output += "Findings:\n" + "\n".join([f"- {f}" for f in result.findings])
            
            if hasattr(result, 'confidence') and result.confidence:
                output += f"\n\nConfidence: {result.confidence:.2f}"
            
            return output
        except Exception as e:
            logger.error(f"[GraphReasoningTool] Error: {e}")
            return json.dumps({"error": str(e)})
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "question": {
                    "type": "string",
                    "description": "Question for graph reasoning"
                }
            },
            "required": ["question"]
        }




