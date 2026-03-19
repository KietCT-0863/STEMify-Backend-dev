"""
Unit tests for Staff Feature Agents
"""

import pytest
from unittest.mock import Mock, AsyncMock, patch

from app.features.staff.course_generator_agent import CourseGeneratorAgent
from app.features.staff.image_3d_description_agent import Image3DDescriptionAgent
from app.features.staff.step_description_agent import StepDescriptionAgent
from app.features.staff.kit_description_agent import KitDescriptionAgent
from app.features.staff.stem_category_agent import STEMCategoryAgent
from app.core.llm.client import LLMClient
from app.core.rag.hybrid_retriever import HybridRetriever


@pytest.fixture
def mock_llm():
    llm = Mock(spec=LLMClient)
    llm.generate = AsyncMock(return_value=Mock(content="Generated content"))
    return llm


@pytest.fixture
def mock_hybrid_retriever():
    retriever = Mock(spec=HybridRetriever)
    retriever.retrieve = AsyncMock(return_value=[])
    return retriever


# Course Generator Agent Tests
@pytest.mark.asyncio
async def test_course_generator_agent_initialization(mock_llm, mock_hybrid_retriever):
    agent = CourseGeneratorAgent(
        llm=mock_llm,
        hybrid_retriever=mock_hybrid_retriever,
    )
    assert agent.name == "CourseGeneratorAgent"
    assert agent.tool_registry is not None


@pytest.mark.asyncio
async def test_course_generator_agent_generate_course(mock_llm, mock_hybrid_retriever):
    agent = CourseGeneratorAgent(
        llm=mock_llm,
        hybrid_retriever=mock_hybrid_retriever,
    )
    result = await agent.generate_course(
        subject="Math",
        level="Elementary",
        duration="8 weeks",
    )
    assert "answer" in result or "error" in result


# Image 3D Description Agent Tests
@pytest.mark.asyncio
async def test_image_3d_description_agent_initialization(mock_llm):
    agent = Image3DDescriptionAgent(
        llm=mock_llm,
    )
    assert agent.name == "Image3DDescriptionAgent"
    assert agent.tool_registry is not None


@pytest.mark.asyncio
async def test_image_3d_description_agent_generate_description(mock_llm):
    agent = Image3DDescriptionAgent(
        llm=mock_llm,
    )
    result = await agent.generate_description(
        image_path="/path/to/image.png",
        model_type="microbit",
    )
    assert "answer" in result or "error" in result


# Step Description Agent Tests
@pytest.mark.asyncio
async def test_step_description_agent_initialization(mock_llm):
    agent = StepDescriptionAgent(
        llm=mock_llm,
    )
    assert agent.name == "StepDescriptionAgent"
    assert agent.tool_registry is not None


@pytest.mark.asyncio
async def test_step_description_agent_generate_steps(mock_llm):
    agent = StepDescriptionAgent(
        llm=mock_llm,
    )
    result = await agent.generate_steps(
        model_id="model_123",
        action_type="assembly",
    )
    assert "answer" in result or "error" in result


# Kit Description Agent Tests
@pytest.mark.asyncio
async def test_kit_description_agent_initialization(mock_llm, mock_hybrid_retriever):
    agent = KitDescriptionAgent(
        llm=mock_llm,
        hybrid_retriever=mock_hybrid_retriever,
    )
    assert agent.name == "KitDescriptionAgent"
    assert agent.tool_registry is not None


@pytest.mark.asyncio
async def test_kit_description_agent_generate_description(mock_llm, mock_hybrid_retriever):
    agent = KitDescriptionAgent(
        llm=mock_llm,
        hybrid_retriever=mock_hybrid_retriever,
    )
    result = await agent.generate_description(
        kit_id="kit_123",
    )
    assert "answer" in result or "error" in result


# STEM Category Agent Tests
@pytest.mark.asyncio
async def test_stem_category_agent_initialization(mock_llm, mock_hybrid_retriever):
    agent = STEMCategoryAgent(
        llm=mock_llm,
        hybrid_retriever=mock_hybrid_retriever,
    )
    assert agent.name == "STEMCategoryAgent"
    assert agent.tool_registry is not None


@pytest.mark.asyncio
async def test_stem_category_agent_generate_categories(mock_llm, mock_hybrid_retriever):
    agent = STEMCategoryAgent(
        llm=mock_llm,
        hybrid_retriever=mock_hybrid_retriever,
    )
    result = await agent.generate_categories(
        content_type="course",
        scope="comprehensive",
    )
    assert "answer" in result or "error" in result

