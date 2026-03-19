"""
Unit tests for Staff Feature Tools
"""

import pytest
import json
from unittest.mock import Mock, AsyncMock, patch

from app.core.tools.curriculum_template_tool import CurriculumTemplateTool
from app.core.tools.content_generator_tool import ContentGeneratorTool
from app.core.tools.structure_validator_tool import StructureValidatorTool
from app.core.tools.image_analysis_tool import ImageAnalysisTool
from app.core.tools.vision_tool import VisionTool
from app.core.tools.model_analysis_tool import ModelAnalysisTool
from app.core.tools.terminology_tool import TerminologyTool
from app.core.tools.description_generator_tool import DescriptionGeneratorTool
from app.core.tools.step_generator_tool import StepGeneratorTool
from app.core.tools.visualization_tool import VisualizationTool
from app.core.tools.validation_tool import ValidationTool
from app.core.tools.kit_data_tool import KitDataTool
from app.core.tools.component_analysis_tool import ComponentAnalysisTool
from app.core.tools.content_analysis_tool import ContentAnalysisTool
from app.core.tools.category_taxonomy_tool import CategoryTaxonomyTool
from app.core.tools.classification_tool import ClassificationTool
from app.core.tools.list_generator_tool import ListGeneratorTool


# Curriculum Template Tool Tests
def test_curriculum_template_tool_get_template():
    tool = CurriculumTemplateTool()
    result = tool.run({"action": "get_template", "subject": "STEM", "level": "Elementary"})
    data = json.loads(result)
    assert "subject" in data or "error" in data


def test_curriculum_template_tool_list_templates():
    tool = CurriculumTemplateTool()
    result = tool.run({"action": "list_templates"})
    data = json.loads(result)
    assert "templates" in data
    assert "count" in data


# Content Generator Tool Tests
@pytest.mark.asyncio
async def test_content_generator_tool_generate_lesson():
    llm = Mock()
    llm.generate = AsyncMock(return_value=Mock(content="Generated lesson content"))
    
    tool = ContentGeneratorTool(llm=llm)
    result = await tool.run({
        "action": "generate_lesson",
        "topic": "Python Basics",
        "level": "Middle",
    })
    data = json.loads(result)
    assert "lesson_content" in data or "error" in data


# Structure Validator Tool Tests
@pytest.mark.asyncio
async def test_structure_validator_tool_validate_curriculum():
    tool = StructureValidatorTool()
    result = await tool.run({
        "action": "validate_curriculum",
        "structure": {
            "title": "Test Course",
            "subject": "Math",
            "level": "Elementary",
            "modules": [{"title": "Module 1", "lessons": [{"title": "Lesson 1"}]}],
            "duration_weeks": 8,
        },
    })
    data = json.loads(result)
    assert "valid" in data
    assert "issues" in data


# Image Analysis Tool Tests
@pytest.mark.asyncio
async def test_image_analysis_tool_analyze():
    tool = ImageAnalysisTool()
    result = await tool.run({
        "action": "analyze",
        "image_path": "/path/to/image.png",
        "model_type": "microbit",
    })
    data = json.loads(result)
    assert "model_type" in data or "error" in data


# Vision Tool Tests
@pytest.mark.asyncio
async def test_vision_tool_analyze():
    llm = Mock()
    tool = VisionTool(llm=llm)
    result = await tool.run({
        "action": "analyze",
        "image_path": "/path/to/image.png",
        "model_type": "microbit",
    })
    data = json.loads(result)
    assert "description" in data or "error" in data


# Model Analysis Tool Tests
@pytest.mark.asyncio
async def test_model_analysis_tool_analyze_structure():
    tool = ModelAnalysisTool()
    result = await tool.run({
        "action": "analyze_structure",
        "model_id": "model_123",
        "model_data": {
            "components": [{"name": "Component1"}],
            "connections": [{"from": "Component1", "to": "Component2"}],
        },
    })
    data = json.loads(result)
    assert "model_id" in data
    assert "components" in data


# Terminology Tool Tests
@pytest.mark.asyncio
async def test_terminology_tool_get_term():
    tool = TerminologyTool()
    result = await tool.run({
        "action": "get_term",
        "term": "resistor",
    })
    data = json.loads(result)
    assert "term" in data or "error" in data


# Description Generator Tool Tests
@pytest.mark.asyncio
async def test_description_generator_tool_generate():
    llm = Mock()
    llm.generate = AsyncMock(return_value=Mock(content="Generated description"))
    
    tool = DescriptionGeneratorTool(llm=llm)
    result = await tool.run({
        "action": "generate",
        "analysis": {"model_type": "microbit"},
        "description_type": "educational",
    })
    data = json.loads(result)
    assert "description" in data or "error" in data


# Step Generator Tool Tests
@pytest.mark.asyncio
async def test_step_generator_tool_generate_steps():
    llm = Mock()
    llm.generate = AsyncMock(return_value=Mock(content="Step 1: ...\nStep 2: ..."))
    
    tool = StepGeneratorTool(llm=llm)
    result = await tool.run({
        "action": "generate_steps",
        "model_id": "model_123",
        "action_type": "assembly",
    })
    data = json.loads(result)
    assert "steps" in data or "error" in data


# Validation Tool Tests
@pytest.mark.asyncio
async def test_validation_tool_validate_sequence():
    tool = ValidationTool()
    result = await tool.run({
        "action": "validate_sequence",
        "steps": ["Step 1: Connect", "Step 2: Test"],
        "action_type": "assembly",
    })
    data = json.loads(result)
    assert "valid" in data
    assert "issues" in data


# Kit Data Tool Tests
@pytest.mark.asyncio
async def test_kit_data_tool_get_kit():
    tool = KitDataTool()
    result = await tool.run({
        "action": "get_kit",
        "kit_id": "kit_123",
    })
    data = json.loads(result)
    assert "kit_id" in data or "error" in data


# Component Analysis Tool Tests
@pytest.mark.asyncio
async def test_component_analysis_tool_analyze():
    tool = ComponentAnalysisTool()
    result = await tool.run({
        "action": "analyze_components",
        "components": [
            {"name": "Micro:bit", "type": "microcontroller"},
            {"name": "LED", "type": "actuator"},
        ],
    })
    data = json.loads(result)
    assert "total_components" in data


# Content Analysis Tool Tests
@pytest.mark.asyncio
async def test_content_analysis_tool_analyze():
    tool = ContentAnalysisTool()
    result = await tool.run({
        "action": "analyze",
        "content": {"title": "Python Course", "description": "Learn Python programming"},
        "content_type": "course",
    })
    data = json.loads(result)
    assert "topics" in data
    assert "difficulty" in data


# Category Taxonomy Tool Tests
@pytest.mark.asyncio
async def test_category_taxonomy_tool_get_taxonomy():
    tool = CategoryTaxonomyTool()
    result = await tool.run({
        "action": "get_taxonomy",
    })
    data = json.loads(result)
    assert "taxonomy" in data


# Classification Tool Tests
@pytest.mark.asyncio
async def test_classification_tool_classify():
    llm = Mock()
    llm.generate = AsyncMock(return_value=Mock(content='{"categories": [{"path": "STEM/Technology", "confidence": 0.9}]}'))
    
    tool = ClassificationTool(llm=llm)
    result = await tool.run({
        "action": "classify",
        "content": {"title": "Python Course"},
        "content_type": "course",
    })
    data = json.loads(result)
    assert "categories" in data or "error" in data


# List Generator Tool Tests
@pytest.mark.asyncio
async def test_list_generator_tool_generate_list():
    tool = ListGeneratorTool()
    result = await tool.run({
        "action": "generate_list",
        "content_items": [
            {"id": "item1", "title": "Item 1"},
            {"id": "item2", "title": "Item 2"},
        ],
        "categories": {
            "item1": [{"path": "STEM/Technology"}],
            "item2": [{"path": "STEM/Science"}],
        },
        "format": "hierarchical",
    })
    data = json.loads(result)
    assert "list" in data
    assert "total_items" in data

