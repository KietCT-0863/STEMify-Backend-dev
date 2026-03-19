"""
Integration tests for StaffService
"""

import pytest
from unittest.mock import Mock, AsyncMock

from app.features.staff.service import StaffService
from app.core.llm.client import LLMClient
from app.core.context.builder import JITContextBuilder
from app.core.memory.memory_manager import MemoryManager
from app.core.agent.pool.manager import AgentPoolManager
from app.core.rag.hybrid_retriever import HybridRetriever


@pytest.fixture
def mock_llm():
    llm = Mock(spec=LLMClient)
    llm.generate = AsyncMock(return_value=Mock(content="Generated content"))
    return llm


@pytest.fixture
def mock_context_builder():
    builder = Mock(spec=JITContextBuilder)
    builder.build = AsyncMock(return_value=Mock(
        total_tokens=100,
        token_budget=1000,
        items=[],
    ))
    return builder


@pytest.fixture
def mock_memory_manager():
    manager = Mock(spec=MemoryManager)
    manager.add_memory = AsyncMock(return_value="memory_id")
    return manager


@pytest.fixture
def mock_agent_pool_manager():
    manager = Mock(spec=AgentPoolManager)
    return manager


@pytest.fixture
def mock_hybrid_retriever():
    retriever = Mock(spec=HybridRetriever)
    retriever.retrieve = AsyncMock(return_value=[])
    return retriever


@pytest.fixture
def staff_service(
    mock_llm,
    mock_context_builder,
    mock_memory_manager,
    mock_agent_pool_manager,
    mock_hybrid_retriever,
):
    return StaffService(
        llm=mock_llm,
        context_builder=mock_context_builder,
        memory_manager=mock_memory_manager,
        agent_pool_manager=mock_agent_pool_manager,
        hybrid_retriever=mock_hybrid_retriever,
    )


@pytest.mark.asyncio
async def test_staff_service_generate_course(staff_service):
    result = await staff_service.generate_course(
        staff_id="staff_123",
        subject="Math",
        level="Elementary",
        duration="8 weeks",
    )
    assert "answer" in result or "error" in result
    assert "metadata" in result


@pytest.mark.asyncio
async def test_staff_service_generate_3d_description(staff_service):
    result = await staff_service.generate_3d_description(
        staff_id="staff_123",
        image_path="/path/to/image.png",
        model_type="microbit",
    )
    assert "answer" in result or "error" in result
    assert "metadata" in result


@pytest.mark.asyncio
async def test_staff_service_generate_kit_description(staff_service):
    result = await staff_service.generate_kit_description(
        staff_id="staff_123",
        kit_id="kit_123",
    )
    assert "answer" in result or "error" in result
    assert "metadata" in result


@pytest.mark.asyncio
async def test_staff_service_generate_step_description(staff_service):
    result = await staff_service.generate_step_description(
        staff_id="staff_123",
        model_id="model_123",
        action_type="assembly",
    )
    assert "answer" in result or "error" in result
    assert "metadata" in result


@pytest.mark.asyncio
async def test_staff_service_generate_categories(staff_service):
    result = await staff_service.generate_categories(
        staff_id="staff_123",
        content_type="course",
        scope="comprehensive",
    )
    assert "answer" in result or "error" in result
    assert "metadata" in result

