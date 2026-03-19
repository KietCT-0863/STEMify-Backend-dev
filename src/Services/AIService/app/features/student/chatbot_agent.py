from typing import Dict, Any, Optional
import logging
import json

from app.core.agent.react_agent import ReActTeachingAgent
from app.core.tools.registry import ToolRegistry
from app.core.tools.rag_tool import RAGTool
from app.core.tools.explanation_tool import ExplanationTool
from app.core.tools.reminder_tool import ReminderTool
from app.core.tools.memory_tool import MemoryTool
from app.core.tools.sentiment_analysis_tool import SentimentAnalysisTool
from app.core.rag.hybrid_retriever import HybridRetriever
from app.core.llm.client import LLMClient
from app.core.memory.memory_manager import MemoryManager
from app.core.cache.agent_cache import AgentResponseCache

logger = logging.getLogger(__name__)


class StudentChatbotAgent(ReActTeachingAgent):
    
    def __init__(
        self,
        student_id: str,
        llm: LLMClient,
        hybrid_retriever: HybridRetriever,
        memory_manager: Optional[MemoryManager] = None,
        agent_cache: Optional[AgentResponseCache] = None,
        sentiment_tool: Optional[SentimentAnalysisTool] = None,
        use_remote: bool = False
    ):
        
        system_prompt = f"""You are a friendly learning assistant for student {student_id}.

Your capabilities:
- Answer questions about lessons and homework
- Explain difficult concepts step-by-step
- Help with problem-solving
- Remind about study schedules
- Provide encouragement and motivation

Always be patient, clear, and educational.
Adjust your tone based on the student's emotional state (if detected)."""

        super().__init__(
            name=f"StudentChatbot_{student_id}",
            llm=llm,
            tool_registry=ToolRegistry(),
            system_prompt=system_prompt,
            max_steps=6,
            use_remote=use_remote
        )
        
        self.student_id = student_id
        self.memory_manager = memory_manager
        self.sentiment_tool = sentiment_tool
        
        # Setup tools
        self.tool_registry.register_tool(RAGTool(hybrid_retriever=hybrid_retriever))
        
        if agent_cache:
            self.tool_registry.register_tool(
                ExplanationTool(llm_client=llm, agent_cache=agent_cache)
            )
        else:
            self.tool_registry.register_tool(ExplanationTool(llm_client=llm))
        
        if memory_manager:
            self.tool_registry.register_tool(
                ReminderTool(student_id=student_id, memory_manager=memory_manager)
            )
            self.tool_registry.register_tool(
                MemoryTool(memory_manager=memory_manager)
            )
        
        if sentiment_tool:
            self.tool_registry.register_tool(sentiment_tool)
        
        logger.info(f"StudentChatbotAgent initialized for student {student_id}")
    
    async def chat(self, query: str, session_id: Optional[str] = None) -> Dict[str, Any]:
        sentiment_result = None
        adjusted_query = query
        
        if self.sentiment_tool:
            try:
                sentiment_response = await self.sentiment_tool.run({
                    "text": query,
                    "type": "full"
                })
                sentiment_result = json.loads(sentiment_response)
                
                # Step 2: Adjust response based on sentiment
                emotion = sentiment_result.get("emotion", {}).get("label", "neutral")
                
                if emotion == "frustration":
                    adjusted_query = f"Student seems frustrated. Provide extra encouragement and step-by-step guidance.\n\nOriginal question: {query}"
                elif emotion == "confusion":
                    adjusted_query = f"Student seems confused. Break down concepts into smaller parts.\n\nOriginal question: {query}"
                elif emotion == "anxiety":
                    adjusted_query = f"Student seems anxious. Be reassuring and supportive.\n\nOriginal question: {query}"
                
                logger.info(f"[StudentChatbot] Detected emotion: {emotion}")
            except Exception as e:
                logger.warning(f"[StudentChatbot] Sentiment analysis failed: {e}")
        
        # Step 3: Execute agent
        result = await self.run(adjusted_query)
        
        # Step 4: Store emotional state in memory
        if self.memory_manager and sentiment_result:
            try:
                await self.memory_manager.add_memory(
                    content=f"Emotional state: {sentiment_result.get('emotion', {}).get('label', 'neutral')}",
                    memory_type="episodic",
                    metadata={
                        "type": "emotional_state",
                        "emotion": sentiment_result.get("emotion", {}).get("label", "neutral"),
                        "sentiment": sentiment_result.get("sentiment", {}).get("label", "neutral"),
                        "query": query,
                        "session_id": session_id,
                        "user_id": self.student_id
                    }
                )
            except Exception as e:
                logger.warning(f"[StudentChatbot] Failed to store emotional state: {e}")
        
        # Add metadata
        result["agent_type"] = "student_chatbot"
        result["student_id"] = self.student_id
        result["metadata"] = result.get("metadata", {})
        result["metadata"]["tools_used"] = list(self.tool_registry.list_tools())
        
        if sentiment_result:
            result["sentiment"] = sentiment_result.get("sentiment", {})
            result["emotion"] = sentiment_result.get("emotion", {})
        
        return result

