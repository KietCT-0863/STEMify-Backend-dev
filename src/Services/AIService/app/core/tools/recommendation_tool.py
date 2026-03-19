from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool
from app.core.rag.hybrid_retriever import HybridRetriever
from app.core.memory.memory_manager import MemoryManager

logger = logging.getLogger(__name__)


class RecommendationTool(Tool):
    """
    Recommendation Tool - MCP-compatible
    
    Suggest learning content based on progress and goals.
    Uses RAG (HybridRetriever) to find relevant lessons.
    Ranks recommendations by relevance and difficulty match.
    Considers student preferences from Semantic Memory.
    """
    
    def __init__(
        self,
        student_id: str,
        hybrid_retriever: Optional[HybridRetriever] = None,
        memory_manager: Optional[MemoryManager] = None
    ):
        super().__init__(
            name="recommendation",
            description="Suggest learning content based on student progress, goals, and preferences"
        )
        self.student_id = student_id
        self.hybrid_retriever = hybrid_retriever
        self.memory_manager = memory_manager
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Execute tool
        
        Actions:
        - recommend_lessons: Recommend lessons based on progress/goals
        - recommend_by_topic: Recommend lessons for a specific topic
        - recommend_by_difficulty: Recommend lessons by difficulty level
        """
        action = parameters.get("action", "recommend_lessons")
        
        try:
            if action == "recommend_lessons":
                return await self._recommend_lessons(parameters)
            elif action == "recommend_by_topic":
                return await self._recommend_by_topic(parameters)
            elif action == "recommend_by_difficulty":
                return await self._recommend_by_difficulty(parameters)
            else:
                return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[RecommendationTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})
    
    async def _get_student_preferences(self) -> Dict[str, Any]:
        """Get student preferences from Semantic Memory"""
        if not self.memory_manager:
            return {}
        
        try:
            memories = await self.memory_manager.retrieve_memories(
                query="learning preferences",
                memory_types=["semantic"],
                limit=5,
                user_id=self.student_id
            )
            
            preferences = {}
            for memory_list in memories.values():
                for memory in memory_list:
                    metadata = memory.get("metadata", {})
                    if metadata.get("type") == "preference":
                        preferences.update(metadata.get("preferences", {}))
            
            return preferences
        except Exception as e:
            logger.warning(f"[RecommendationTool] Error getting preferences: {e}")
            return {}
    
    async def _recommend_lessons(self, parameters: Dict[str, Any]) -> str:
        """Recommend lessons based on progress/goals"""
        if not self.hybrid_retriever:
            return json.dumps({
                "recommendations": [],
                "note": "HybridRetriever not available"
            })
        
        try:
            preferences = await self._get_student_preferences()
            
            query_parts = []
            if preferences.get("preferred_topics"):
                query_parts.append(f"topics: {', '.join(preferences['preferred_topics'])}")
            if preferences.get("learning_style"):
                query_parts.append(f"learning style: {preferences['learning_style']}")
            
            query = " ".join(query_parts) if query_parts else "educational content"
            top_k = parameters.get("top_k", 5)
            
            results = await self.hybrid_retriever.retrieve(query, top_k=top_k * 2)
            
            recommendations = []
            for doc in results[:top_k]:
                recommendations.append({
                    "lesson_id": doc.get("lesson_id", ""),
                    "title": doc.get("title", ""),
                    "content": doc.get("content", "")[:200],
                    "score": doc.get("score", 0.0),
                    "topic": doc.get("topic", ""),
                    "difficulty": doc.get("difficulty", "medium")
                })
            
            return json.dumps({
                "recommendations": recommendations,
                "count": len(recommendations),
                "student_id": self.student_id,
                "query_used": query
            })
        except Exception as e:
            logger.error(f"[RecommendationTool] Error recommending lessons: {e}")
            return json.dumps({"error": str(e)})
    
    async def _recommend_by_topic(self, parameters: Dict[str, Any]) -> str:
        """Recommend lessons for a specific topic"""
        if not self.hybrid_retriever:
            return json.dumps({
                "recommendations": [],
                "note": "HybridRetriever not available"
            })
        
        try:
            topic = parameters.get("topic", "")
            if not topic:
                return json.dumps({"error": "Topic is required"})
            
            top_k = parameters.get("top_k", 5)
            
            results = await self.hybrid_retriever.retrieve(
                f"lessons about {topic}",
                top_k=top_k
            )
            
            recommendations = []
            for doc in results:
                recommendations.append({
                    "lesson_id": doc.get("lesson_id", ""),
                    "title": doc.get("title", ""),
                    "content": doc.get("content", "")[:200],
                    "score": doc.get("score", 0.0),
                    "topic": doc.get("topic", topic)
                })
            
            return json.dumps({
                "recommendations": recommendations,
                "count": len(recommendations),
                "topic": topic,
                "student_id": self.student_id
            })
        except Exception as e:
            logger.error(f"[RecommendationTool] Error recommending by topic: {e}")
            return json.dumps({"error": str(e)})
    
    async def _recommend_by_difficulty(self, parameters: Dict[str, Any]) -> str:
        """Recommend lessons by difficulty level"""
        if not self.hybrid_retriever:
            return json.dumps({
                "recommendations": [],
                "note": "HybridRetriever not available"
            })
        
        try:
            difficulty = parameters.get("difficulty", "medium")  # easy, medium, hard
            top_k = parameters.get("top_k", 5)
            
            # Retrieve content for difficulty level
            results = await self.hybrid_retriever.retrieve(
                f"{difficulty} level educational content",
                top_k=top_k * 2
            )
            
            # Filter by difficulty
            recommendations = []
            for doc in results:
                doc_difficulty = doc.get("difficulty", "medium")
                if doc_difficulty == difficulty:
                    recommendations.append({
                        "lesson_id": doc.get("lesson_id", ""),
                        "title": doc.get("title", ""),
                        "content": doc.get("content", "")[:200],
                        "score": doc.get("score", 0.0),
                        "difficulty": doc_difficulty
                    })
                    if len(recommendations) >= top_k:
                        break
            
            return json.dumps({
                "recommendations": recommendations,
                "count": len(recommendations),
                "difficulty": difficulty,
                "student_id": self.student_id
            })
        except Exception as e:
            logger.error(f"[RecommendationTool] Error recommending by difficulty: {e}")
            return json.dumps({"error": str(e)})
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["recommend_lessons", "recommend_by_topic", "recommend_by_difficulty"],
                    "description": "Action to perform",
                    "default": "recommend_lessons"
                },
                "topic": {
                    "type": "string",
                    "description": "Topic for recommendation (for recommend_by_topic)"
                },
                "difficulty": {
                    "type": "string",
                    "enum": ["easy", "medium", "hard"],
                    "description": "Difficulty level (for recommend_by_difficulty)",
                    "default": "medium"
                },
                "top_k": {
                    "type": "integer",
                    "description": "Number of recommendations",
                    "default": 5
                }
            },
            "required": []
        }

