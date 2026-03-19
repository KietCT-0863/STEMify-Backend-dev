from abc import ABC, abstractmethod
from typing import Dict, Any, Optional
import logging

logger = logging.getLogger(__name__)


class AgentRouter(ABC):
    """
    Abstract base class for Agent Router
    
    Routes queries to appropriate agents based on complexity and task type.
    This is a minimal interface that will be fully implemented in Phase 1.
    """
    
    @abstractmethod
    async def route(self, query: str, task_type: Optional[str] = None) -> Dict[str, Any]:
        """
        Route query to appropriate agent
        
        Args:
            query: User query string
            task_type: Optional task type hint (e.g., "teaching", "insights", "content")
        
        Returns:
            Dict containing:
                - answer: str - The generated answer
                - path: str - The path taken (e.g., "simple", "complex")
                - metadata: Dict[str, Any] - Additional metadata about the routing/execution
        """
        pass

