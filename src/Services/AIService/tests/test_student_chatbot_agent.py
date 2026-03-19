import asyncio
import json
from unittest.mock import MagicMock, AsyncMock

from app.features.student.chatbot_agent import StudentChatbotAgent
from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import LLMResponse
from app.core.rag.hybrid_retriever import HybridRetriever
from app.core.tools.sentiment_analysis_tool import SentimentAnalysisTool


class MockLLMClient(LLMClient):
    def __init__(self):
        super().__init__(local_provider=None, remote_provider=None)
    
    async def generate(self, messages, use_remote=False, **kwargs):
        return LLMResponse(
            content="Thought: Student is asking about Python.\nAction: rag_search[{\"query\": \"Python functions\", \"top_k\": 5}]\nObservation: Found 3 relevant lessons.\nAction: Finish[Here are some resources about Python functions: ...]",
            model="mock",
            usage={"prompt_tokens": 10, "completion_tokens": 20}
        )


class MockHybridRetriever:
    """Mock hybrid retriever for testing"""
    async def retrieve(self, query, top_k=5, **kwargs):
        return [
            {"content": "Python functions tutorial", "score": 0.9, "lesson_id": "lesson_1"},
            {"content": "Advanced Python", "score": 0.8, "lesson_id": "lesson_2"}
        ]


def test_student_chatbot_agent_initialization():
    """Test agent initialization"""
    llm = MockLLMClient()
    hybrid_retriever = MockHybridRetriever()
    
    agent = StudentChatbotAgent(
        student_id="student_123",
        llm=llm,
        hybrid_retriever=hybrid_retriever,
        memory_manager=None,
        agent_cache=None,
        sentiment_tool=None
    )
    
    assert agent.student_id == "student_123"
    assert agent.name == "StudentChatbot_student_123"
    assert agent.tool_registry is not None


def test_student_chatbot_agent_chat():
    """Test chat method"""
    llm = MockLLMClient()
    hybrid_retriever = MockHybridRetriever()
    
    agent = StudentChatbotAgent(
        student_id="student_123",
        llm=llm,
        hybrid_retriever=hybrid_retriever,
        memory_manager=None,
        agent_cache=None,
        sentiment_tool=None
    )
    
    async def run():
        result = await agent.chat("What are Python functions?")
        assert "answer" in result
        assert result["agent_type"] == "student_chatbot"
        assert result["student_id"] == "student_123"
    
    asyncio.get_event_loop().run_until_complete(run())


def test_student_chatbot_agent_chat_with_sentiment():
    """Test chat method with sentiment analysis"""
    llm = MockLLMClient()
    hybrid_retriever = MockHybridRetriever()
    sentiment_tool = SentimentAnalysisTool()
    
    agent = StudentChatbotAgent(
        student_id="student_123",
        llm=llm,
        hybrid_retriever=hybrid_retriever,
        memory_manager=None,
        agent_cache=None,
        sentiment_tool=sentiment_tool
    )
    
    async def run():
        result = await agent.chat("I'm frustrated with this topic", session_id="session_1")
        assert "answer" in result
        assert "sentiment" in result or "emotion" in result  # May or may not be present depending on tool
        assert result["agent_type"] == "student_chatbot"
    
    asyncio.get_event_loop().run_until_complete(run())

