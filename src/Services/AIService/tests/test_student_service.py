import asyncio
from unittest.mock import MagicMock, AsyncMock

from app.features.student.service import StudentService
from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import LLMResponse
from app.core.context.builder import JITContextBuilder
from app.core.memory.memory_manager import MemoryManager
from app.core.agent.pool.manager import AgentPoolManager
from app.core.context.gather import ContextGatherer
from app.core.context.selector import ContextSelector
from app.core.context.structurer import ContextStructurer
from app.core.context.compressor import ContextCompressor
from app.core.context.models import ContextBundle, ContextItem


class MockLLMClient(LLMClient):
    def __init__(self):
        super().__init__(local_provider=None, remote_provider=None)
    
    async def generate(self, messages, use_remote=False, **kwargs):
        return LLMResponse(
            content="Thought: Analyzing student query.\nAction: Finish[Here's my advice: Focus on Python functions.]",
            model="mock",
            usage={"prompt_tokens": 10, "completion_tokens": 20}
        )


class MockContextGatherer:
    async def gather(self, query, user_id=None, top_k=10):
        return [
            ContextItem(content="Student progress: 50%", score=0.9, source="memory:working"),
            ContextItem(content="Python lesson content", score=0.8, source="retrieval")
        ]


def test_student_service_get_learning_advice():
    """Test end-to-end learning advice flow"""
    llm = MockLLMClient()
    gatherer = MockContextGatherer()
    selector = ContextSelector(max_items=5)
    structurer = ContextStructurer()
    compressor = ContextCompressor()
    context_builder = JITContextBuilder(
        gatherer=gatherer,
        selector=selector,
        structurer=structurer,
        compressor=compressor,
        token_budget=2000
    )
    memory_manager = MemoryManager()
    agent_pool_manager = AgentPoolManager()
    
    service = StudentService(
        llm=llm,
        context_builder=context_builder,
        memory_manager=memory_manager,
        agent_pool_manager=agent_pool_manager,
        classroom_repository=None,
        graph_client=None,
        hybrid_retriever=None,
        agent_cache=None,
        sentiment_tool=None
    )
    
    async def run():
        result = await service.get_learning_advice(
            student_id="student_123",
            query="What should I study next?",
            session_id="session_1"
        )
        assert "answer" in result
        assert result["agent_type"] == "learning_advisor"
        assert "metadata" in result
        assert "context_bundle" in result["metadata"]
    
    asyncio.get_event_loop().run_until_complete(run())


def test_student_service_chat():
    """Test end-to-end chat flow"""
    llm = MockLLMClient()
    gatherer = MockContextGatherer()
    selector = ContextSelector(max_items=5)
    structurer = ContextStructurer()
    compressor = ContextCompressor()
    context_builder = JITContextBuilder(
        gatherer=gatherer,
        selector=selector,
        structurer=structurer,
        compressor=compressor,
        token_budget=2000
    )
    memory_manager = MemoryManager()
    agent_pool_manager = AgentPoolManager()
    
    # Mock hybrid retriever
    class MockHybridRetriever:
        async def retrieve(self, query, top_k=5, **kwargs):
            return [{"content": "Python tutorial", "score": 0.9}]
    
    service = StudentService(
        llm=llm,
        context_builder=context_builder,
        memory_manager=memory_manager,
        agent_pool_manager=agent_pool_manager,
        classroom_repository=None,
        graph_client=None,
        hybrid_retriever=MockHybridRetriever(),
        agent_cache=None,
        sentiment_tool=None
    )
    
    async def run():
        result = await service.chat(
            student_id="student_123",
            query="What are Python functions?",
            session_id="session_1"
        )
        assert "answer" in result
        assert result["agent_type"] == "student_chatbot"
        assert "metadata" in result
        assert "context_bundle" in result["metadata"]
    
    asyncio.get_event_loop().run_until_complete(run())


def test_student_service_context_reuse():
    """Test context reuse with session_id"""
    llm = MockLLMClient()
    gatherer = MockContextGatherer()
    selector = ContextSelector(max_items=5)
    structurer = ContextStructurer()
    compressor = ContextCompressor()
    context_builder = JITContextBuilder(
        gatherer=gatherer,
        selector=selector,
        structurer=structurer,
        compressor=compressor,
        token_budget=2000
    )
    memory_manager = MemoryManager()
    agent_pool_manager = AgentPoolManager()
    
    service = StudentService(
        llm=llm,
        context_builder=context_builder,
        memory_manager=memory_manager,
        agent_pool_manager=agent_pool_manager,
        classroom_repository=None,
        graph_client=None,
        hybrid_retriever=None,
        agent_cache=None,
        sentiment_tool=None
    )
    
    async def run():
        # First call - should build context
        result1 = await service.chat(
            student_id="student_123",
            query="What are Python functions?",
            session_id="session_1"
        )
        
        # Second call with same session_id and query - should reuse context
        result2 = await service.chat(
            student_id="student_123",
            query="What are Python functions?",
            session_id="session_1"
        )
        
        # Both should succeed
        assert "answer" in result1
        assert "answer" in result2
    
    asyncio.get_event_loop().run_until_complete(run())

