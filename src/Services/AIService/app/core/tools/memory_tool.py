"""
Memory Tool
MCP-compatible tool for memory operations
"""

from typing import Dict, Any, List, Optional
import logging
import json

from app.core.tools.base import Tool
from app.core.memory.memory_manager import MemoryManager

logger = logging.getLogger(__name__)


class MemoryTool(Tool):
    """
    Memory Tool - MCP-compatible
    
    Provides access to the 4-layer memory system:
    - Working Memory: Session-based temporary memory
    - Episodic Memory: Events and experiences
    - Semantic Memory: Knowledge and concepts
    - Perceptual Memory: Multimodal data
    """
    
    def __init__(self, memory_manager: MemoryManager):
        """
        Initialize memory tool
        
        Args:
            memory_manager: MemoryManager instance
        """
        super().__init__(
            name="memory",
            description="Search and manage memories across working, episodic, semantic, and perceptual layers"
        )
        self.memory_manager = memory_manager
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Execute memory operation
        
        Parameters:
            - operation: "search", "add", "get_important", "consolidate"
            - query: Search query (for search operation)
            - content: Memory content (for add operation)
            - memory_type: "working", "episodic", "semantic", "perceptual"
            - metadata: Optional metadata dict
            - user_id: Optional user ID
            - limit: Maximum results (default: 5)
        
        Returns:
            JSON string with results
        """
        operation = parameters.get("operation", "search")
        
        try:
            if operation == "search":
                return await self._search(parameters)
            elif operation == "add":
                return await self._add(parameters)
            elif operation == "get_important":
                return await self._get_important(parameters)
            elif operation == "consolidate":
                return await self._consolidate(parameters)
            else:
                return json.dumps({
                    "error": f"Unknown operation: {operation}",
                    "available_operations": ["search", "add", "get_important", "consolidate"]
                })
        except Exception as e:
            logger.error(f"[MemoryTool] Operation failed: {e}")
            return json.dumps({"error": str(e)})
    
    async def _search(self, parameters: Dict[str, Any]) -> str:
        """Search memories"""
        query = parameters.get("query", "")
        memory_types = parameters.get("memory_types")
        if isinstance(memory_types, str):
            memory_types = [memory_types]
        limit = parameters.get("limit", 5)
        user_id = parameters.get("user_id")
        
        results = await self.memory_manager.retrieve_memories(
            query=query,
            memory_types=memory_types,
            limit=limit,
            user_id=user_id
        )
        
        return json.dumps({
            "operation": "search",
            "query": query,
            "results": results,
            "total_layers": len(results),
            "total_results": sum(len(v) for v in results.values())
        })
    
    async def _add(self, parameters: Dict[str, Any]) -> str:
        """Add memory"""
        content = parameters.get("content", "")
        memory_type = parameters.get("memory_type", "working")
        metadata = parameters.get("metadata", {})
        user_id = parameters.get("user_id")
        
        if user_id:
            metadata["user_id"] = user_id
        
        memory_id = await self.memory_manager.add_memory(
            content=content,
            memory_type=memory_type,
            metadata=metadata
        )
        
        return json.dumps({
            "operation": "add",
            "memory_id": memory_id,
            "memory_type": memory_type,
            "success": True
        })
    
    async def _get_important(self, parameters: Dict[str, Any]) -> str:
        """Get important memories"""
        user_id = parameters.get("user_id")
        
        if not user_id:
            return json.dumps({"error": "user_id is required for get_important operation"})
        
        # Get important from each layer
        results = {}
        
        # Working memory
        try:
            important_working = await self.memory_manager.working_memory.get_important(user_id=user_id)
            results["working"] = important_working
        except Exception as e:
            logger.warning(f"[MemoryTool] Failed to get important working memories: {e}")
            results["working"] = []
        
        # Episodic memory
        if self.memory_manager.episodic_memory:
            try:
                important_episodic = await self.memory_manager.episodic_memory.get_important(user_id=user_id)
                results["episodic"] = important_episodic
            except Exception as e:
                logger.warning(f"[MemoryTool] Failed to get important episodic memories: {e}")
                results["episodic"] = []
        
        return json.dumps({
            "operation": "get_important",
            "user_id": user_id,
            "results": results
        })
    
    async def _consolidate(self, parameters: Dict[str, Any]) -> str:
        """Consolidate memories"""
        user_id = parameters.get("user_id")
        importance_threshold = parameters.get("importance_threshold", 0.7)
        
        if not user_id:
            return json.dumps({"error": "user_id is required for consolidate operation"})
        
        consolidated = await self.memory_manager.consolidate_memories(
            user_id=user_id,
            importance_threshold=importance_threshold
        )
        
        return json.dumps({
            "operation": "consolidate",
            "user_id": user_id,
            "consolidated": consolidated
        })
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        """Get JSON schema for parameters"""
        return {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "enum": ["search", "add", "get_important", "consolidate"],
                    "description": "Memory operation to perform"
                },
                "query": {
                    "type": "string",
                    "description": "Search query (for search operation)"
                },
                "content": {
                    "type": "string",
                    "description": "Memory content (for add operation)"
                },
                "memory_type": {
                    "type": "string",
                    "enum": ["working", "episodic", "semantic", "perceptual"],
                    "description": "Type of memory layer"
                },
                "memory_types": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "List of memory types to search (for search operation)"
                },
                "metadata": {
                    "type": "object",
                    "description": "Optional metadata dictionary"
                },
                "user_id": {
                    "type": "string",
                    "description": "User ID for filtering"
                },
                "limit": {
                    "type": "integer",
                    "description": "Maximum results (default: 5)"
                },
                "importance_threshold": {
                    "type": "number",
                    "description": "Minimum importance for consolidation (default: 0.7)"
                }
            },
            "required": ["operation"]
        }




