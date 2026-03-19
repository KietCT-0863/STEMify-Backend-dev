from typing import Dict, Any, Optional
import logging
import json

from app.core.tools.base import Tool
from app.core.llm.client import LLMClient

logger = logging.getLogger(__name__)


class ContentGeneratorTool(Tool):
    """
    Content Generator Tool - MCP-compatible
    
    Generates course content using LLM based on requirements and templates.
    """

    def __init__(
        self,
        llm: LLMClient,
    ):
        super().__init__(
            name="content_generator",
            description="Generate course content, lesson plans, and educational materials using LLM. Takes requirements and generates structured content.",
        )
        self.llm = llm

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - generate_lesson: Generate a single lesson
        - generate_module: Generate a module with multiple lessons
        - generate_assessment: Generate assessment questions

        Parameters:
        - content_type: Type of content (lesson, module, assessment)
        - topic: Topic or subject
        - level: Education level
        - requirements: Specific requirements for the content
        - template: Template structure to follow
        """
        action = parameters.get("action", "generate_lesson")
        try:
            if action == "generate_lesson":
                return await self._generate_lesson(parameters)
            elif action == "generate_module":
                return await self._generate_module(parameters)
            elif action == "generate_assessment":
                return await self._generate_assessment(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[ContentGeneratorTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _generate_lesson(self, parameters: Dict[str, Any]) -> str:
        """Generate a single lesson"""
        topic = parameters.get("topic", "")
        level = parameters.get("level", "")
        requirements = parameters.get("requirements", "")
        template = parameters.get("template", {})

        prompt = f"""Generate a comprehensive lesson plan for the following:

Topic: {topic}
Level: {level}
Requirements: {requirements}

Template structure:
{json.dumps(template, indent=2) if template else "Standard lesson structure"}

Generate a lesson plan with:
1. Learning objectives
2. Introduction
3. Main content/activities
4. Practice exercises
5. Summary
6. Assessment questions

Make it engaging, educational, and aligned with best practices."""

        try:
            response = await self.llm.generate(
                messages=[{"role": "user", "content": prompt}]
            )
            return json.dumps({"lesson_content": response, "topic": topic, "level": level})
        except Exception as e:
            logger.error(f"Error generating lesson: {e}", exc_info=True)
            return json.dumps({"error": f"Failed to generate lesson: {str(e)}"})

    async def _generate_module(self, parameters: Dict[str, Any]) -> str:
        """Generate a module with multiple lessons"""
        topic = parameters.get("topic", "")
        level = parameters.get("level", "")
        num_lessons = parameters.get("num_lessons", 5)
        requirements = parameters.get("requirements", "")
        template = parameters.get("template", {})

        prompt = f"""Generate a comprehensive module for the following:

Topic: {topic}
Level: {level}
Number of lessons: {num_lessons}
Requirements: {requirements}

Template structure:
{json.dumps(template, indent=2) if template else "Standard module structure"}

Generate a module with:
1. Module overview
2. Learning objectives for the entire module
3. {num_lessons} detailed lesson plans
4. Module assessment
5. Resources and references

Ensure progressive difficulty and logical flow between lessons."""

        try:
            response = await self.llm.generate(
                messages=[{"role": "user", "content": prompt}]
            )
            return json.dumps(
                {
                    "module_content": response,
                    "topic": topic,
                    "level": level,
                    "num_lessons": num_lessons,
                }
            )
        except Exception as e:
            logger.error(f"Error generating module: {e}", exc_info=True)
            return json.dumps({"error": f"Failed to generate module: {str(e)}"})

    async def _generate_assessment(self, parameters: Dict[str, Any]) -> str:
        """Generate assessment questions"""
        topic = parameters.get("topic", "")
        level = parameters.get("level", "")
        num_questions = parameters.get("num_questions", 10)
        question_types = parameters.get("question_types", ["multiple_choice", "short_answer"])

        prompt = f"""Generate assessment questions for the following:

Topic: {topic}
Level: {level}
Number of questions: {num_questions}
Question types: {', '.join(question_types)}

Generate a mix of:
- Multiple choice questions with 4 options each
- Short answer questions
- Problem-solving questions (if applicable)

Include:
- Questions covering different difficulty levels
- Clear, unambiguous questions
- Correct answers and explanations
- Points/weights for each question"""

        try:
            response = await self.llm.generate(
                messages=[{"role": "user", "content": prompt}]
            )
            return json.dumps(
                {
                    "assessment_content": response,
                    "topic": topic,
                    "level": level,
                    "num_questions": num_questions,
                }
            )
        except Exception as e:
            logger.error(f"Error generating assessment: {e}", exc_info=True)
            return json.dumps({"error": f"Failed to generate assessment: {str(e)}"})

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["generate_lesson", "generate_module", "generate_assessment"],
                    "description": "Action to perform",
                },
                "topic": {
                    "type": "string",
                    "description": "Topic or subject for the content",
                },
                "level": {
                    "type": "string",
                    "description": "Education level (e.g., Elementary, Middle, High)",
                },
                "requirements": {
                    "type": "string",
                    "description": "Specific requirements for the content",
                },
                "template": {
                    "type": "object",
                    "description": "Template structure to follow",
                },
                "num_lessons": {
                    "type": "integer",
                    "description": "Number of lessons (for module generation)",
                    "default": 5,
                },
                "num_questions": {
                    "type": "integer",
                    "description": "Number of questions (for assessment generation)",
                    "default": 10,
                },
                "question_types": {
                    "type": "array",
                    "items": {"type": "string"},
                    "description": "Types of questions to generate",
                    "default": ["multiple_choice", "short_answer"],
                },
            },
            "required": ["action", "topic", "level"],
        }

