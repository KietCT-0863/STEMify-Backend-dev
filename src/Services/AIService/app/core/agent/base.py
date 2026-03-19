"""
Agent Base Class
Abstract base class for all agents
"""

from abc import ABC, abstractmethod
from typing import Optional, List, Dict, Any
import logging

from app.core.llm.client import LLMClient
from app.core.tools.registry import ToolRegistry

logger = logging.getLogger(__name__)


class Agent(ABC):
    """
    Agent Base Class
    
    Abstract base class for all agents.
    Provides common functionality: tool management, history, LLM access.
    """
    
    def __init__(
        self,
        name: str,
        llm: LLMClient,
        tool_registry: Optional[ToolRegistry] = None,
        system_prompt: Optional[str] = None,
        use_remote: bool = False
    ):
        """
        Initialize agent
        
        Args:
            name: Agent name
            llm: LLM client instance
            tool_registry: Tool registry (optional, creates new if None)
            system_prompt: System prompt for the agent
            use_remote: Whether to use remote LLM provider (default: False for local)
        """
        self.name = name
        self.llm = llm
        self.tool_registry = tool_registry or ToolRegistry()
        self.system_prompt = system_prompt
        self.use_remote = use_remote
        self.history: List[Dict[str, str]] = []
        
        logger.info(f"Agent '{self.name}' initialized (remote={use_remote})")
    
    @abstractmethod
    async def run(self, query: str, **kwargs) -> Dict[str, Any]:
        """
        Run agent - must be implemented by subclasses
        
        Args:
            query: User query
            **kwargs: Additional parameters
        
        Returns:
            Dict with agent response
        """
        pass
    
    def add_tool(self, tool):
        """
        Add tool to agent's tool registry
        
        Args:
            tool: Tool instance to add
        """
        self.tool_registry.register_tool(tool)
        logger.debug(f"Agent '{self.name}' added tool: {tool.name}")
    
    def get_history(self) -> List[Dict[str, str]]:
        """
        Get conversation history
        
        Returns:
            Copy of conversation history
        """
        return self.history.copy()
    
    def add_to_history(self, role: str, content: str):
        """
        Add message to conversation history
        
        Args:
            role: Message role ("user", "assistant", "system")
            content: Message content
        """
        self.history.append({"role": role, "content": content})
    
    def clear_history(self):
        """Clear conversation history"""
        self.history.clear()
        logger.debug(f"Agent '{self.name}' history cleared")
    
    def get_system_prompt(self) -> str:
        """
        Get system prompt (with tool descriptions if available)
        
        Returns:
            System prompt string
        """
        base_prompt = self.system_prompt or f"You are {self.name}, an AI assistant."
        
        if self.tool_registry and self.tool_registry.get_tool_count() > 0:
            tools_description = self.tool_registry.get_tools_description()
            return f"{base_prompt}\n\n{tools_description}"
        
        return base_prompt




