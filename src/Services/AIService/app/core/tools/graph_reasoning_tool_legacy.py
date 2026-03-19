from typing import Dict, Any
import logging

from app.core.tools.base import Tool
from app.core.reasoning.orchestrator import GraphReasoningOrchestrator
from app.core.reasoning.models import ReasoningResult, CausalFinding

logger = logging.getLogger(__name__)


class LegacyGraphReasoningTool(Tool):
    """
    Legacy wrapper for existing GraphReasoningOrchestrator
    
    This wrapper adapts the existing GraphReasoningOrchestrator to the new Tool interface,
    enabling backward compatibility during migration to the new Agent Framework.
    
    Migration Path:
    - Phase 0: LegacyGraphReasoningTool wraps GraphReasoningOrchestrator 
    - Phase 1-2: New GraphReasoningTool implementation
    - Phase 8: Remove LegacyGraphReasoningTool and GraphReasoningOrchestrator
    """
    
    def __init__(self, orchestrator: GraphReasoningOrchestrator):
        """
        Initialize legacy graph reasoning tool wrapper
        
        Args:
            orchestrator: Existing GraphReasoningOrchestrator instance to wrap
        """
        if not orchestrator:
            raise ValueError("GraphReasoningOrchestrator instance is required")
        
        super().__init__(
            name="graph_reasoning",
            description="Graph-based reasoning using Neo4j and vector search (legacy wrapper)"
        )
        self.orchestrator = orchestrator
        logger.info("LegacyGraphReasoningTool initialized (wrapping GraphReasoningOrchestrator)")
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Execute graph reasoning using existing orchestrator
        
        This method:
        1. Extracts question from parameters
        2. Calls orchestrator.reason() to get ReasoningResult
        3. Formats result as string for LLM consumption
        
        Args:
            parameters: Dictionary containing:
                - question: str - The question to reason about (required)
        
        Returns:
            Formatted string containing:
                - Answer (from answer_teacher_friendly)
                - Findings (from causal_findings)
                - Next actions (if available)
        """
        question = parameters.get("question", "")
        
        if not question or not question.strip():
            logger.warning("Empty question received")
            return "Error: Please provide a valid question for graph reasoning."
        
        try:
            # Call orchestrator
            logger.debug(f"[LegacyGraphTool] Reasoning about: '{question[:50]}...'")
            result: ReasoningResult = await self.orchestrator.reason(question)
            
            # Format result as string
            output_parts = []
            
            # 1. Answer (primary output)
            answer = result.answer_teacher_friendly
            if answer:
                output_parts.append(f"Answer: {answer}")
            else:
                output_parts.append("Answer: (No answer generated)")
            
            # 2. Findings (causal findings)
            findings = result.causal_findings
            if findings:
                output_parts.append("\nFindings:")
                for i, finding in enumerate(findings, 1):
                    if isinstance(finding, CausalFinding):
                        hypothesis = finding.hypothesis
                        confidence = finding.confidence
                        output_parts.append(f"  {i}. {hypothesis} (confidence: {confidence:.2f})")
                    else:
                        # Fallback if finding is a string
                        output_parts.append(f"  {i}. {str(finding)}")
            
            # 3. Next actions (if available)
            next_actions = result.next_actions
            if next_actions:
                output_parts.append("\nNext Actions:")
                for i, action in enumerate(next_actions, 1):
                    output_parts.append(f"  {i}. {action}")
            
            # 4. Plan (if available and not empty)
            plan = result.plan
            if plan and plan.strip() and not plan.startswith("Error:"):
                output_parts.append(f"\nReasoning Plan: {plan}")
            
            # Combine all parts
            formatted_result = "\n".join(output_parts)
            
            logger.debug(f"[LegacyGraphTool] Reasoning completed, result length: {len(formatted_result)}")
            return formatted_result
        
        except Exception as e:
            logger.error(f"[LegacyGraphTool] Error during reasoning: {e}", exc_info=True)
            return f"Error: I encountered an error while performing graph reasoning: {str(e)}. Please try again."
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        """
        Get JSON schema for tool parameters
        
        Returns:
            JSON schema dict describing the expected parameters
        """
        return {
            "type": "object",
            "properties": {
                "question": {
                    "type": "string",
                    "description": "The question to reason about using graph and vector data"
                }
            },
            "required": ["question"]
        }

