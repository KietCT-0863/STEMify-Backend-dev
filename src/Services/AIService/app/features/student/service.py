from typing import Dict, Any, Optional
import logging

from app.features.student.learning_advisor_agent import LearningAdvisorAgent
from app.core.agent.pool.manager import AgentPoolManager
from app.core.context.builder import JITContextBuilder
from app.core.memory.memory_manager import MemoryManager
from app.core.llm.client import LLMClient
from app.core.data.classroom_repository import ClassroomRepository
from app.core.graph.client import GraphClient
from app.core.rag.hybrid_retriever import HybridRetriever
from app.core.cache.agent_cache import AgentResponseCache
from app.core.tools.sentiment_analysis_tool import SentimentAnalysisTool
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class StudentService:
    
    def __init__(
        self,
        llm: LLMClient,
        context_builder: JITContextBuilder,
        memory_manager: MemoryManager,
        agent_pool_manager: AgentPoolManager,
        classroom_repository: Optional[ClassroomRepository] = None,
        graph_client: Optional[GraphClient] = None,
        hybrid_retriever: Optional[HybridRetriever] = None,
        agent_cache: Optional[AgentResponseCache] = None,
        sentiment_tool: Optional[SentimentAnalysisTool] = None
    ):
        
        self.llm = llm
        self.context_builder = context_builder
        self.memory_manager = memory_manager
        self.agent_pool_manager = agent_pool_manager
        self.classroom_repository = classroom_repository
        self.graph_client = graph_client
        self.hybrid_retriever = hybrid_retriever
        self.agent_cache = agent_cache
        self.sentiment_tool = sentiment_tool
        
        logger.info("StudentService initialized")
    
    async def get_learning_advice(
        self,
        student_id: str,
        query: str,
        session_id: Optional[str] = None
    ) -> Dict[str, Any]:
        try:
            agent = await self.agent_pool_manager.acquire(
                role="student",
                agent_type="react",
                llm=self.llm,
                tool_registry=None, 
                system_prompt=None  
            )
            
            learning_agent = LearningAdvisorAgent(
                student_id=student_id,
                llm=self.llm,
                classroom_repository=self.classroom_repository,
                graph_client=self.graph_client,
                hybrid_retriever=self.hybrid_retriever,
                memory_manager=self.memory_manager,
                sentiment_tool=self.sentiment_tool,
                use_remote=settings.STUDENT_AGENTS_USE_REMOTE
            )
            
            # Build context
            context_bundle = await self.context_builder.build(
                query=query,
                user_id=student_id,
                top_k=10,
                session_id=session_id
            )
            
            result = await learning_agent.advise(query)
            
            try:
                await self.memory_manager.add_memory(
                    content=f"Learning advice given: {result.get('answer', '')[:200]}",
                    memory_type="episodic",
                    metadata={
                        "type": "learning_advice",
                        "query": query,
                        "student_id": student_id,
                        "session_id": session_id,
                        "context_tokens": context_bundle.total_tokens
                    }
                )
            except Exception as e:
                logger.warning(f"[StudentService] Failed to store advice in memory: {e}")
            
            # Add context info to result
            result["metadata"]["context_bundle"] = {
                "total_tokens": context_bundle.total_tokens,
                "token_budget": context_bundle.token_budget,
                "items_count": len(context_bundle.items)
            }
            
            return result
        except Exception as e:
            logger.error(f"[StudentService] Error getting learning advice: {e}", exc_info=True)
            return {
                "answer": f"I encountered an error while processing your request: {str(e)}",
                "path": "error",
                "metadata": {"error": str(e)},
                "agent_type": "learning_advisor"
            }
  
