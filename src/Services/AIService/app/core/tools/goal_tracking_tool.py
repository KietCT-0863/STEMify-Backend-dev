from typing import Dict, Any, Optional, List
import logging
import json
from datetime import datetime

from app.core.tools.base import Tool
from app.core.memory.memory_manager import MemoryManager

logger = logging.getLogger(__name__)


class GoalTrackingTool(Tool):
    """
    Goal Tracking Tool - MCP-compatible
    
    Track short-term and long-term learning goals.
    Store/retrieve goals from Memory (Episodic for history, Semantic for patterns).
    """
    
    def __init__(
        self,
        student_id: str,
        memory_manager: Optional[MemoryManager] = None
    ):
        super().__init__(
            name="goal_tracking",
            description="Track short-term and long-term learning goals, calculate progress, and suggest adjustments"
        )
        self.student_id = student_id
        self.memory_manager = memory_manager
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Execute tool
        
        Actions:
        - set_goal: Set a new learning goal
        - get_goals: Get all goals (active, completed, or all)
        - update_goal: Update goal progress or status
        - get_progress: Calculate goal progress
        - suggest_adjustments: Suggest goal adjustments based on performance
        """
        action = parameters.get("action", "get_goals")
        
        try:
            if action == "set_goal":
                return await self._set_goal(parameters)
            elif action == "get_goals":
                return await self._get_goals(parameters)
            elif action == "update_goal":
                return await self._update_goal(parameters)
            elif action == "get_progress":
                return await self._get_progress(parameters)
            elif action == "suggest_adjustments":
                return await self._suggest_adjustments(parameters)
            else:
                return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[GoalTrackingTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})
    
    async def _set_goal(self, parameters: Dict[str, Any]) -> str:
        """Set a new learning goal"""
        if not self.memory_manager:
            return json.dumps({"error": "MemoryManager not available"})
        
        try:
            goal_type = parameters.get("goal_type", "short_term")  # short_term or long_term
            description = parameters.get("description", "")
            target_date = parameters.get("target_date")
            target_value = parameters.get("target_value")
            
            if not description:
                return json.dumps({"error": "Goal description is required"})
            
            goal_data = {
                "goal_type": goal_type,
                "description": description,
                "target_date": target_date,
                "target_value": target_value,
                "status": "active",
                "progress": 0.0,
                "created_at": datetime.utcnow().isoformat(),
                "student_id": self.student_id
            }
            
            # Store in Episodic Memory and Semantic Memory 
            goal_id = await self.memory_manager.add_memory(
                content=f"Learning goal: {description}",
                memory_type="episodic",
                metadata={
                    "type": "goal",
                    "goal_data": goal_data,
                    "user_id": self.student_id
                }
            )
            
            # store goal pattern in Semantic Memory
            await self.memory_manager.add_memory(
                content=f"Goal pattern: {goal_type} goal for {description}",
                memory_type="semantic",
                metadata={
                    "type": "goal_pattern",
                    "goal_type": goal_type,
                    "user_id": self.student_id
                }
            )
            
            return json.dumps({
                "goal_id": goal_id,
                "goal": goal_data,
                "message": "Goal set successfully"
            })
        except Exception as e:
            logger.error(f"[GoalTrackingTool] Error setting goal: {e}")
            return json.dumps({"error": str(e)})
    
    async def _get_goals(self, parameters: Dict[str, Any]) -> str:
        if not self.memory_manager:
            return json.dumps({"goals": [], "note": "MemoryManager not available"})
        
        try:
            status_filter = parameters.get("status", "all")  # active, completed, all
            
            memories = await self.memory_manager.retrieve_memories(
                query="learning goals",
                memory_types=["episodic"],
                limit=100,
                user_id=self.student_id
            )
            
            goals = []
            for memory_list in memories.values():
                for memory in memory_list:
                    metadata = memory.get("metadata", {})
                    if metadata.get("type") == "goal":
                        goal_data = metadata.get("goal_data", {})
                        if status_filter == "all" or goal_data.get("status") == status_filter:
                            goals.append({
                                "goal_id": memory.get("id", ""),
                                **goal_data
                            })
            
            return json.dumps({
                "goals": goals,
                "count": len(goals),
                "student_id": self.student_id
            })
        except Exception as e:
            logger.error(f"[GoalTrackingTool] Error getting goals: {e}")
            return json.dumps({"error": str(e)})
    
    async def _update_goal(self, parameters: Dict[str, Any]) -> str:
        """Update goal progress or status"""
        if not self.memory_manager:
            return json.dumps({"error": "MemoryManager not available"})
        
        try:
            goal_id = parameters.get("goal_id")
            progress = parameters.get("progress")
            status = parameters.get("status")
            
            if not goal_id:
                return json.dumps({"error": "Goal ID is required"})
            
            memories = await self.memory_manager.retrieve_memories(
                query=f"goal {goal_id}",
                memory_types=["episodic"],
                limit=10,
                user_id=self.student_id
            )
            
            goal_memory = None
            for memory_list in memories.values():
                for memory in memory_list:
                    if memory.get("id") == goal_id:
                        goal_memory = memory
                        break
                if goal_memory:
                    break
            
            if not goal_memory:
                return json.dumps({"error": "Goal not found"})
            
            metadata = goal_memory.get("metadata", {})
            goal_data = metadata.get("goal_data", {})
            
            if progress is not None:
                goal_data["progress"] = float(progress)
            if status:
                goal_data["status"] = status
            goal_data["updated_at"] = datetime.utcnow().isoformat()
            
            await self.memory_manager.add_memory(
                content=f"Updated learning goal: {goal_data.get('description', '')}",
                memory_type="episodic",
                metadata={
                    "type": "goal_update",
                    "goal_id": goal_id,
                    "goal_data": goal_data,
                    "user_id": self.student_id
                }
            )
            
            return json.dumps({
                "goal_id": goal_id,
                "goal": goal_data,
                "message": "Goal updated successfully"
            })
        except Exception as e:
            logger.error(f"[GoalTrackingTool] Error updating goal: {e}")
            return json.dumps({"error": str(e)})
    
    async def _get_progress(self, parameters: Dict[str, Any]) -> str:
        goals_result = await self._get_goals({"status": "active"})
        goals_data = json.loads(goals_result)
        goals = goals_data.get("goals", [])
        
        if not goals:
            return json.dumps({
                "total_goals": 0,
                "average_progress": 0.0,
                "goals": []
            })
        
        total_progress = sum(g.get("progress", 0.0) for g in goals)
        average_progress = total_progress / len(goals) if goals else 0.0
        
        return json.dumps({
            "total_goals": len(goals),
            "average_progress": round(average_progress, 2),
            "goals": goals,
            "student_id": self.student_id
        })
    
    async def _suggest_adjustments(self, parameters: Dict[str, Any]) -> str:
        """Suggest goal adjustments based on performance"""
        # This is a simplified version - full implementation would analyze
        # performance data and suggest realistic adjustments
        goals_result = await self._get_goals({"status": "active"})
        goals_data = json.loads(goals_result)
        goals = goals_data.get("goals", [])
        
        suggestions = []
        for goal in goals:
            progress = goal.get("progress", 0.0)
            if progress < 30.0:
                suggestions.append({
                    "goal_id": goal.get("goal_id"),
                    "description": goal.get("description"),
                    "suggestion": "Consider breaking this goal into smaller milestones",
                    "reason": f"Current progress is {progress}%, which may indicate the goal is too ambitious"
                })
            elif progress > 90.0:
                suggestions.append({
                    "goal_id": goal.get("goal_id"),
                    "description": goal.get("description"),
                    "suggestion": "Consider setting a more challenging goal",
                    "reason": f"Current progress is {progress}%, indicating you're ahead of schedule"
                })
        
        return json.dumps({
            "suggestions": suggestions,
            "count": len(suggestions),
            "student_id": self.student_id
        })
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["set_goal", "get_goals", "update_goal", "get_progress", "suggest_adjustments"],
                    "description": "Action to perform",
                    "default": "get_goals"
                },
                "goal_type": {
                    "type": "string",
                    "enum": ["short_term", "long_term"],
                    "description": "Type of goal (for set_goal)"
                },
                "description": {
                    "type": "string",
                    "description": "Goal description (for set_goal)"
                },
                "target_date": {
                    "type": "string",
                    "description": "Target date in ISO format (for set_goal)"
                },
                "target_value": {
                    "type": "number",
                    "description": "Target value/metric (for set_goal)"
                },
                "goal_id": {
                    "type": "string",
                    "description": "Goal ID (for update_goal)"
                },
                "progress": {
                    "type": "number",
                    "description": "Progress percentage 0-100 (for update_goal)"
                },
                "status": {
                    "type": "string",
                    "enum": ["active", "completed", "paused"],
                    "description": "Goal status (for update_goal or get_goals filter)"
                }
            },
            "required": []
        }

