"""
Tool Registry
Central registry for all MCP-compatible tools
"""

from typing import Dict, Optional, List
import logging

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class ToolRegistry:
    """
    Tool Registry
    
    Central registry for all MCP-compatible tools.
    Provides tool registration, retrieval, and description generation.
    """
    
    def __init__(self):
        """Initialize tool registry"""
        self._tools: Dict[str, Tool] = {}
        logger.info("ToolRegistry initialized")
    
    def register_tool(self, tool: Tool) -> bool:
        """
        Register a tool
        
        Args:
            tool: Tool instance to register
        
        Returns:
            True if successful, False if tool name already exists
        """
        if tool.name in self._tools:
            logger.warning(f"Tool '{tool.name}' already registered, overwriting")
        
        self._tools[tool.name] = tool
        logger.debug(f"Registered tool: {tool.name} - {tool.description}")
        return True
    
    def get_tool(self, tool_name: str) -> Optional[Tool]:
        """
        Get tool by name
        
        Args:
            tool_name: Name of the tool
        
        Returns:
            Tool instance or None if not found
        """
        return self._tools.get(tool_name)
    
    def list_tools(self) -> List[str]:
        """
        List all registered tool names
        
        Returns:
            List of tool names
        """
        return list(self._tools.keys())
    
    def get_tools_description(self) -> str:
        """
        Get formatted description of all tools for LLM
        
        Returns:
            Formatted string describing all tools
        """
        if not self._tools:
            return "No tools available."
        
        descriptions = []
        descriptions.append("Available Tools:")
        descriptions.append("=" * 50)
        
        for tool_name, tool in self._tools.items():
            descriptions.append(f"\nTool: {tool_name}")
            descriptions.append(f"Description: {tool.description}")
            
            # Get parameters schema
            schema = tool.get_parameters_schema()
            if schema.get("properties"):
                descriptions.append("Parameters:")
                for param_name, param_info in schema["properties"].items():
                    param_type = param_info.get("type", "unknown")
                    param_desc = param_info.get("description", "")
                    required = " (required)" if param_name in schema.get("required", []) else ""
                    descriptions.append(f"  - {param_name} ({param_type}){required}: {param_desc}")
        
        return "\n".join(descriptions)
    
    def get_tools_schema(self) -> Dict[str, Dict]:
        """
        Get JSON schema for all tools (MCP-compatible)
        
        Returns:
            Dict mapping tool names to their parameter schemas
        """
        schemas = {}
        for tool_name, tool in self._tools.items():
            schemas[tool_name] = {
                "name": tool.name,
                "description": tool.description,
                "parameters": tool.get_parameters_schema()
            }
        return schemas
    
    def unregister_tool(self, tool_name: str) -> bool:
        """
        Unregister a tool
        
        Args:
            tool_name: Name of the tool to unregister
        
        Returns:
            True if successful, False if tool not found
        """
        if tool_name in self._tools:
            del self._tools[tool_name]
            logger.debug(f"Unregistered tool: {tool_name}")
            return True
        return False
    
    def has_tool(self, tool_name: str) -> bool:
        """
        Check if tool is registered
        
        Args:
            tool_name: Name of the tool
        
        Returns:
            True if tool is registered
        """
        return tool_name in self._tools
    
    def get_tool_count(self) -> int:
        """Get number of registered tools"""
        return len(self._tools)




