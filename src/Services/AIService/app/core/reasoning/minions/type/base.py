"""
Base Minion
Abstract base class for all reasoning minions
"""

from abc import ABC, abstractmethod
from typing import Dict, Any, Optional
import logging

from app.core.reasoning.tools import (
    GraphTool,
    VectorTool,
    RerankTool,
    MathTool,
    ClockTool
)
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class BaseMinion(ABC):
    """
    Base class for all reasoning minions
    
    Each minion has a specific responsibility in the reasoning pipeline.
    Minions are stateless and can be reused across multiple reasoning tasks.
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
        self.graph_tool = graph_tool
        self.vector_tool = vector_tool
        self.rerank_tool = rerank_tool
        self.math_tool = math_tool
        self.clock_tool = clock_tool
        self.llm_client = llm_client
    
    @property
    @abstractmethod
    def name(self) -> str:
        """Minion name for audit trail"""
        pass
    
    @abstractmethod
    async def execute(self, context: Dict[str, Any]) -> Dict[str, Any]:
        """
        Execute minion's task
        
        Args:
            context: Shared context from orchestrator containing:
                - question: Original question
                - plan: Reasoning plan
                - previous_results: Results from previous minions
                - etc.
        
        Returns:
            Dict with minion's output to be added to context
        """
        pass
    
    def _log(self, message: str, level: str = "INFO"):
        """Log message with minion name"""
        log_func = getattr(logger, level.lower(), logger.info)
        log_func(f"[{self.name}] {message}")

