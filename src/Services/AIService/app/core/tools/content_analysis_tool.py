from typing import Dict, Any, Optional, List
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class ContentAnalysisTool(Tool):
    """
    Content Analysis Tool - MCP-compatible
    
    Analyzes STEM content to extract features, topics, and characteristics for categorization.
    """

    def __init__(self):
        super().__init__(
            name="content_analysis",
            description="Analyze STEM content to extract features, topics, difficulty level, and characteristics. Helps determine appropriate categories for content.",
        )

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Actions:
        - analyze: Analyze content and extract features

        Parameters:
        - content: Content to analyze (text, metadata, etc.)
        - content_type: Type of content (course, lesson, kit, model)
        """
        action = parameters.get("action", "analyze")
        try:
            if action == "analyze":
                return await self._analyze_content(parameters)
            return json.dumps({"error": f"Unknown action: {action}"})
        except Exception as e:
            logger.error(f"[ContentAnalysisTool] Error: {e}", exc_info=True)
            return json.dumps({"error": str(e)})

    async def _analyze_content(self, parameters: Dict[str, Any]) -> str:
        """Analyze content and extract features"""
        content = parameters.get("content", {})
        content_type = parameters.get("content_type", "unknown")

        # Extract features (in production, would use NLP/ML)
        analysis = {
            "content_type": content_type,
            "topics": self._extract_topics(content),
            "difficulty": self._estimate_difficulty(content),
            "subject_areas": self._identify_subjects(content),
            "age_range": self._estimate_age_range(content),
            "keywords": self._extract_keywords(content),
        }

        return json.dumps(analysis)

    def _extract_topics(self, content: Dict[str, Any]) -> List[str]:
        """Extract topics from content"""
        # Simple keyword-based extraction
        text = str(content).lower()
        topics = []

        topic_keywords = {
            "programming": ["code", "program", "function", "variable", "loop"],
            "electronics": ["circuit", "resistor", "led", "voltage", "current"],
            "robotics": ["robot", "motor", "sensor", "actuator"],
            "math": ["equation", "calculate", "formula", "number"],
            "science": ["experiment", "hypothesis", "observation", "data"],
        }

        for topic, keywords in topic_keywords.items():
            if any(keyword in text for keyword in keywords):
                topics.append(topic)

        return topics if topics else ["general"]

    def _estimate_difficulty(self, content: Dict[str, Any]) -> str:
        """Estimate difficulty level"""
        text = str(content).lower()

        if any(word in text for word in ["beginner", "basic", "intro", "simple"]):
            return "beginner"
        elif any(word in text for word in ["advanced", "complex", "expert"]):
            return "advanced"
        else:
            return "intermediate"

    def _identify_subjects(self, content: Dict[str, Any]) -> List[str]:
        """Identify subject areas"""
        text = str(content).lower()
        subjects = []

        subject_keywords = {
            "STEM": ["stem", "science", "technology", "engineering", "math"],
            "Science": ["science", "physics", "chemistry", "biology"],
            "Technology": ["technology", "computer", "digital", "software"],
            "Engineering": ["engineering", "design", "build", "construct"],
            "Mathematics": ["math", "mathematics", "calculate", "equation"],
        }

        for subject, keywords in subject_keywords.items():
            if any(keyword in text for keyword in keywords):
                subjects.append(subject)

        return subjects if subjects else ["STEM"]

    def _estimate_age_range(self, content: Dict[str, Any]) -> str:
        """Estimate age range"""
        text = str(content).lower()

        if any(word in text for word in ["elementary", "primary", "young"]):
            return "6-10"
        elif any(word in text for word in ["middle", "teen", "adolescent"]):
            return "11-14"
        elif any(word in text for word in ["high", "secondary", "adult"]):
            return "15-18"
        else:
            return "8-14"

    def _extract_keywords(self, content: Dict[str, Any]) -> List[str]:
        """Extract keywords"""
        text = str(content).lower()

        # Simple keyword extraction (in production, would use NLP)
        common_stem_keywords = [
            "microbit",
            "arduino",
            "programming",
            "circuit",
            "sensor",
            "led",
            "robot",
            "code",
            "experiment",
        ]

        found_keywords = [kw for kw in common_stem_keywords if kw in text]
        return found_keywords[:10]  # Limit to top 10

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "enum": ["analyze"],
                    "description": "Action to perform",
                    "default": "analyze",
                },
                "content": {
                    "type": ["object", "string"],
                    "description": "Content to analyze (text, metadata, etc.)",
                },
                "content_type": {
                    "type": "string",
                    "enum": ["course", "lesson", "kit", "model"],
                    "description": "Type of content",
                },
            },
            "required": ["action", "content"],
        }

