import asyncio
from unittest.mock import MagicMock, AsyncMock

from app.features.student.learning_advisor_agent import LearningAdvisorAgent
from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import LLMResponse


class MockLLMClient(LLMClient):
    def __init__(self):
        super().__init__(local_provider=None, remote_provider=None)
    
    async def generate(self, messages, use_remote=False, **kwargs):
        # Mock response
        return LLMResponse(
            content="Thought: I need to check the student's progress.\nAction: learning_progress[{\"action\": \"get_progress\"}]\nObservation: Progress is 50%.\nThought: Based on progress, I recommend focusing on Python functions.\nAction: Finish[Based on your 50% completion rate, I recommend studying Python functions next.]",
            model="mock",
            usage={"prompt_tokens": 10, "completion_tokens": 20}
        )


def test_learning_advisor_agent_initialization():
    """Test agent initialization"""
    llm = MockLLMClient()
    agent = LearningAdvisorAgent(
        student_id="student_123",
        llm=llm,
        classroom_repository=None,
        graph_client=None,
        hybrid_retriever=None,
        memory_manager=None
    )
    
    assert agent.student_id == "student_123"
    assert agent.name == "LearningAdvisor_student_123"
    assert agent.tool_registry is not None


def test_learning_advisor_agent_advise():
    """Test advise method"""
    llm = MockLLMClient()
    agent = LearningAdvisorAgent(
        student_id="student_123",
        llm=llm,
        classroom_repository=None,
        graph_client=None,
        hybrid_retriever=None,
        memory_manager=None
    )
    
    async def run():
        result = await agent.advise("What should I study next?")
        assert "answer" in result
        assert result["agent_type"] == "learning_advisor"
        assert result["student_id"] == "student_123"
    
    asyncio.get_event_loop().run_until_complete(run())

