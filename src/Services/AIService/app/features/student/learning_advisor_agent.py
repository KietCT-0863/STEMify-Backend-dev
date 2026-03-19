from typing import Dict, Any, Optional
import logging

from app.core.agent.react_agent import ReActTeachingAgent
from app.core.agent.intent_classifier import IntentClassifier
from app.core.tools.registry import ToolRegistry
from app.core.tools.memory_tool import MemoryTool
from app.core.tools.learning_progress_tool import LearningProgressTool
from app.core.tools.performance_analysis_tool import PerformanceAnalysisTool
from app.core.tools.goal_tracking_tool import GoalTrackingTool
from app.core.tools.recommendation_tool import RecommendationTool
from app.core.tools.sentiment_analysis_tool import SentimentAnalysisTool
from app.core.data.classroom_repository import ClassroomRepository
from app.core.graph.client import GraphClient
from app.core.rag.hybrid_retriever import HybridRetriever
from app.core.memory.memory_manager import MemoryManager
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class LearningAdvisorAgent(ReActTeachingAgent):
    """
    Learning Advisor Agent for Students
    
    ReAct paradigm for interactive learning guidance.
    Provides personalized advice based on:
    - Learning progress
    - Performance analysis
    - Learning goals
    - Recommended content
    """
    
    def __init__(
        self,
        student_id: str,
        llm: LLMClient,
        classroom_repository: Optional[ClassroomRepository] = None,
        graph_client: Optional[GraphClient] = None,
        hybrid_retriever: Optional[HybridRetriever] = None,
        memory_manager: Optional[MemoryManager] = None,
        sentiment_tool: Optional[SentimentAnalysisTool] = None,
        use_remote: bool = False
    ):
        system_prompt = f"""You are a personalized learning advisor for student {student_id}.

Your role:
- Understand student's learning progress, strengths, and weaknesses
- Track short-term and long-term learning goals
- Suggest personalized learning paths
- Explain difficult concepts
- Remind about study schedules
- Provide encouragement and motivation

Use tools to get real-time learning data before making recommendations.
Always be supportive, clear, and educational."""

        super().__init__(
            name=f"LearningAdvisor_{student_id}",
            llm=llm,
            tool_registry=ToolRegistry(),
            system_prompt=system_prompt,
            max_steps=8,  # More steps for complex learning advice
            use_remote=use_remote
        )
        
        self.student_id = student_id
        

        if classroom_repository:
            self.tool_registry.register_tool(
                LearningProgressTool(student_id=student_id, classroom_repository=classroom_repository)
            )
        
        if graph_client:
            self.tool_registry.register_tool(
                PerformanceAnalysisTool(student_id=student_id, graph_client=graph_client)
            )
        
        if memory_manager:
            self.tool_registry.register_tool(
                GoalTrackingTool(student_id=student_id, memory_manager=memory_manager)
            )
            self.tool_registry.register_tool(
                MemoryTool(memory_manager=memory_manager)
            )
        
        if hybrid_retriever and memory_manager:
            self.tool_registry.register_tool(
                RecommendationTool(
                    student_id=student_id,
                    hybrid_retriever=hybrid_retriever,
                    memory_manager=memory_manager
                )
            )
        
        if sentiment_tool:
            self.tool_registry.register_tool(sentiment_tool)
        
        self.intent_classifier = IntentClassifier(llm_client=llm)
        self.memory_manager = memory_manager
        
        logger.info(f"LearningAdvisorAgent initialized for student {student_id}")
    
    async def advise(self, query: str) -> Dict[str, Any]:
        """
        Provide learning advice with intent-based routing optimization.
        
        Simple queries (greetings, memory recall) bypass full ReAct loop.
        Complex queries use full ReAct with dynamic max_steps.
        """
        # Classify intent first
        intent_result = self.intent_classifier.classify(query)
        logger.info(f"[LearningAdvisor] Intent: {intent_result.intent} (confidence: {intent_result.confidence:.2f})")
        
        # Handle general_chat directly without ReAct
        if intent_result.skip_react:
            quick_response = self.intent_classifier.get_quick_response(query, intent_result)
            if quick_response:
                return {
                    "answer": quick_response,
                    "path": "direct",
                    "steps": 0,
                    "history": [],
                    "agent_type": "learning_advisor",
                    "student_id": self.student_id,
                    "intent": intent_result.intent,
                    "metadata": {"optimized": True, "skip_react": True}
                }
        
        # For memory_recall, try direct memory search first
        if intent_result.intent == "memory_recall" and self.memory_manager:
            try:
                # Extract key terms from query for memory search
                memories = await self.memory_manager.retrieve_memories(
                    query=query,
                    memory_types=["episodic", "semantic"],
                    limit=3,
                    user_id=self.student_id
                )
                
                # If we found relevant memories, format and return
                all_memories = []
                for mem_type, mems in memories.items():
                    all_memories.extend(mems)
                
                if all_memories and len(all_memories) > 0:
                    # Format memory results
                    memory_content = "\n".join([
                        f"- {m.get('content', '')[:200]}" 
                        for m in all_memories[:3]
                    ])
                    
                    # Generate response using LLM with memory context
                    response = await self.llm.generate([
                        {"role": "user", "content": f"""Based on these memories about the student:
{memory_content}

Answer this question naturally: "{query}"

Be helpful and reference the specific information from the memories."""}
                    ])
                    
                    answer = response.content if hasattr(response, 'content') else str(response)
                    
                    return {
                        "answer": answer,
                        "path": "memory_direct",
                        "steps": 1,
                        "history": [f"Direct memory search: found {len(all_memories)} relevant memories"],
                        "agent_type": "learning_advisor",
                        "student_id": self.student_id,
                        "intent": intent_result.intent,
                        "metadata": {"optimized": True, "memories_found": len(all_memories)}
                    }
            except Exception as e:
                logger.warning(f"[LearningAdvisor] Direct memory search failed: {e}, falling back to ReAct")
        
        # Run ReAct with dynamic max_steps based on intent
        result = await self.run(
            query, 
            max_steps_override=intent_result.suggested_max_steps
        )
        
        result["agent_type"] = "learning_advisor"
        result["student_id"] = self.student_id
        result["intent"] = intent_result.intent
        result["metadata"] = result.get("metadata", {})
        result["metadata"]["tools_used"] = list(self.tool_registry.list_tools())
        result["metadata"]["intent_confidence"] = intent_result.confidence
        result["metadata"]["max_steps_used"] = intent_result.suggested_max_steps
        
        return result

