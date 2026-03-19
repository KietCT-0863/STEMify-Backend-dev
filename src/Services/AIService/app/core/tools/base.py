from abc import ABC, abstractmethod
from typing import Dict, Any, List, Tuple
import logging
import asyncio

logger = logging.getLogger(__name__)


class Tool(ABC):
    """
    Base tool class
    
    Tools are used by agents to perform specific actions (e.g., graph reasoning, vector search).
    """
    
    def __init__(self, name: str, description: str):
        """
        Initialize tool
        
        Args:
            name: Tool name (must be unique)
            description: Tool description for LLM to understand when to use it
        """
        self.name = name
        self.description = description
        logger.debug(f"Tool '{self.name}' initialized: {self.description}")
    
    @abstractmethod
    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Execute tool with given parameters
        
        Args:
            parameters: Dictionary of parameters for the tool
        
        Returns:
            String result of tool execution (for LLM consumption)
        """
        pass
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        """
        Get JSON schema for tool parameters
        
        Returns:
            JSON schema dict describing the expected parameters
        """
        return {
            "type": "object",
            "properties": {},
            "required": []
        }
    
    def can_run_parallel(self) -> bool:
        return False
    
    def __repr__(self) -> str:
        return f"Tool(name='{self.name}', description='{self.description}')"


async def execute_tools_parallel(
    tool_calls: List[Tuple[Any, Dict[str, Any]]]
) -> List[str]:
    async def execute_single_tool(tool, params: Dict[str, Any]) -> str:
        """Execute a single tool with error handling"""
        try:
            return await tool.run(params)
        except Exception as e:
            logger.error(
                f"Error executing tool {tool.name}",
                extra={"tool": tool.name, "error": str(e)},
                exc_info=True
            )
            return f'{{"error": "Tool execution failed: {str(e)}"}}'
    
    tasks = [execute_single_tool(tool, params) for tool, params in tool_calls]
    results = await asyncio.gather(*tasks, return_exceptions=True)
    
    processed_results = []
    for i, result in enumerate(results):
        if isinstance(result, Exception):
            tool_name = tool_calls[i][0].name if tool_calls[i] else "unknown"
            logger.error(
                f"Exception in parallel tool execution",
                extra={"tool": tool_name, "error": str(result)},
                exc_info=True
            )
            processed_results.append(f'{{"error": "Tool execution exception: {str(result)}"}}')
        else:
            processed_results.append(result)
    
    return processed_results

