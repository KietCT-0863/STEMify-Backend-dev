from typing import Dict, Any, Optional, List
import logging
import json
from datetime import datetime, timedelta

from app.core.tools.base import Tool
from app.core.memory.memory_manager import MemoryManager

logger = logging.getLogger(__name__)


class ReminderTool(Tool):
    """    
    Set study reminders (deadlines, scheduled sessions).
    Store reminders in Working Memory  and Episodic Memory.
    Query upcoming reminders.
    """
    
    def __init__(
        self,
        student_id: str,
        memory_manager: Optional[MemoryManager] = None
    ):
        super().__init__(
            name="reminder",
            description="Set, query, and manage study reminders and deadlines"
        )
        self.student_id = student_id
        self.memory_manager = memory_manager
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Execute tool
        
        Actions:
        - set_reminder: Set a new reminder
        - get_reminders: Get reminders (upcoming, all, or by type)
        - delete_reminder: Delete a reminder
        """
        action = parameters.get("action", "get_reminders")
        
        try:
            if action == "set_reminder":
                return await self._set_reminder(parameters)
            elif action == "get_reminders":
                return await self._get_reminders(parameters)
            elif action == "delete_reminder":
                return await self._delete_reminder(parameters)
            else:
                return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[ReminderTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})
    
    async def _set_reminder(self, parameters: Dict[str, Any]) -> str:
        """Set a new reminder"""
        if not self.memory_manager:
            return json.dumps({"error": "MemoryManager not available"})
        
        try:
            title = parameters.get("title", "")
            description = parameters.get("description", "")
            reminder_type = parameters.get("type", "study")  # study, deadline, session
            due_date = parameters.get("due_date") 
            priority = parameters.get("priority", "medium")  # low, medium, high
            
            if not title:
                return json.dumps({"error": "Reminder title is required"})
            
            if not due_date:
                return json.dumps({"error": "Due date is required"})
            
            reminder_data = {
                "title": title,
                "description": description,
                "type": reminder_type,
                "due_date": due_date,
                "priority": priority,
                "status": "active",
                "created_at": datetime.utcnow().isoformat(),
                "student_id": self.student_id
            }
            
            reminder_id = await self.memory_manager.add_memory(
                content=f"Reminder: {title}",
                memory_type="working",
                metadata={
                    "type": "reminder",
                    "reminder_data": reminder_data,
                    "user_id": self.student_id
                }
            )

            await self.memory_manager.add_memory(
                content=f"Study reminder: {title} - {description}",
                memory_type="episodic",
                metadata={
                    "type": "reminder",
                    "reminder_data": reminder_data,
                    "user_id": self.student_id
                }
            )
            
            return json.dumps({
                "reminder_id": reminder_id,
                "reminder": reminder_data,
                "message": "Reminder set successfully"
            })
        except Exception as e:
            logger.error(f"[ReminderTool] Error setting reminder: {e}")
            return json.dumps({"error": str(e)})
    
    async def _get_reminders(self, parameters: Dict[str, Any]) -> str:
        """Get reminders (upcoming, all, or by type)"""
        if not self.memory_manager:
            return json.dumps({"reminders": [], "note": "MemoryManager not available"})
        
        try:
            filter_type = parameters.get("filter", "upcoming")  # upcoming, all, by_type
            reminder_type = parameters.get("type")
            
            memories = await self.memory_manager.retrieve_memories(
                query="study reminders",
                memory_types=["working", "episodic"],
                limit=100,
                user_id=self.student_id
            )
            
            reminders = []
            now = datetime.utcnow()
            
            for memory_list in memories.values():
                for memory in memory_list:
                    metadata = memory.get("metadata", {})
                    if metadata.get("type") == "reminder":
                        reminder_data = metadata.get("reminder_data", {})
                        status = reminder_data.get("status", "active")
                        
                        if filter_type == "upcoming":
                            due_date_str = reminder_data.get("due_date")
                            if due_date_str:
                                try:
                                    due_date = datetime.fromisoformat(due_date_str.replace('Z', '+00:00'))
                                    if due_date < now or status != "active":
                                        continue
                                except:
                                    pass
                        
                        if filter_type == "by_type" and reminder_type:
                            if reminder_data.get("type") != reminder_type:
                                continue
                        
                        reminders.append({
                            "reminder_id": memory.get("id", ""),
                            **reminder_data
                        })
            
            reminders.sort(key=lambda r: r.get("due_date", ""))
            
            return json.dumps({
                "reminders": reminders,
                "count": len(reminders),
                "student_id": self.student_id
            })
        except Exception as e:
            logger.error(f"[ReminderTool] Error getting reminders: {e}")
            return json.dumps({"error": str(e)})
    
    async def _delete_reminder(self, parameters: Dict[str, Any]) -> str:
        """Delete a reminder (mark as completed)"""
        if not self.memory_manager:
            return json.dumps({"error": "MemoryManager not available"})
        
        try:
            reminder_id = parameters.get("reminder_id")
            if not reminder_id:
                return json.dumps({"error": "Reminder ID is required"})
            
            # Get reminder
            memories = await self.memory_manager.retrieve_memories(
                query=f"reminder {reminder_id}",
                memory_types=["working", "episodic"],
                limit=10,
                user_id=self.student_id
            )
            
            # Find and mark as completed
            found = False
            for memory_list in memories.values():
                for memory in memory_list:
                    if memory.get("id") == reminder_id:
                        metadata = memory.get("metadata", {})
                        reminder_data = metadata.get("reminder_data", {})
                        reminder_data["status"] = "completed"
                        reminder_data["completed_at"] = datetime.utcnow().isoformat()
                        
                        # Update in memory
                        await self.memory_manager.add_memory(
                            content=f"Completed reminder: {reminder_data.get('title', '')}",
                            memory_type="episodic",
                            metadata={
                                "type": "reminder_update",
                                "reminder_id": reminder_id,
                                "reminder_data": reminder_data,
                                "user_id": self.student_id
                            }
                        )
                        found = True
                        break
                if found:
                    break
            
            if not found:
                return json.dumps({"error": "Reminder not found"})
            
            return json.dumps({
                "reminder_id": reminder_id,
                "message": "Reminder marked as completed"
            })
        except Exception as e:
            logger.error(f"[ReminderTool] Error deleting reminder: {e}")
            return json.dumps({"error": str(e)})
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["set_reminder", "get_reminders", "delete_reminder"],
                    "description": "Action to perform",
                    "default": "get_reminders"
                },
                "title": {
                    "type": "string",
                    "description": "Reminder title (for set_reminder)"
                },
                "description": {
                    "type": "string",
                    "description": "Reminder description (for set_reminder)"
                },
                "type": {
                    "type": "string",
                    "enum": ["study", "deadline", "session"],
                    "description": "Reminder type (for set_reminder or filter)"
                },
                "due_date": {
                    "type": "string",
                    "description": "Due date in ISO format (for set_reminder)"
                },
                "priority": {
                    "type": "string",
                    "enum": ["low", "medium", "high"],
                    "description": "Reminder priority (for set_reminder)",
                    "default": "medium"
                },
                "filter": {
                    "type": "string",
                    "enum": ["upcoming", "all", "by_type"],
                    "description": "Filter for get_reminders",
                    "default": "upcoming"
                },
                "reminder_id": {
                    "type": "string",
                    "description": "Reminder ID (for delete_reminder)"
                }
            },
            "required": []
        }

