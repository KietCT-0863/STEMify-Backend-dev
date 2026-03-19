"""
Mock Lesson Data for Development
Provides mock lesson and section data for use in MockLessonRepository
"""

from typing import Dict, Any


def get_mock_lec_sec_data() -> Dict[str, Any]:
    """
    Generate mock lesson and section data
    """
    return {
        "title": "Urban Planning",
        "publisher": "STEMify",
        "ageRange": "4-7",
        "durationMinutes": 130,
        "description": "Explore the fundamentals of sustainable urban planning—from housing and transportation to energy production, natural-hazard readiness, and eco-friendly city design. Students will prototype a simple solar panel model and integrate it into their own city layouts. By the end of the lesson, learners will extend their designs while aligning with key Sustainable Development Goals (SDGs).",
        "learningOutcomes": [
            "Understand key principles of sustainable urban development including infrastructure, housing, and clean energy.",
            "Apply design-thinking skills by sketching, building, and prototyping components of a solar-powered city."
        ],
        "requirements": [],
        "skills": ["Creativity", "Teamwork"],
        "topics": ["Storytelling"],
        "standards": ["Engineering Design"],
        "sections": [
            {
                "title": "Overview and Objectives",
                "durationMinutes": 10,
                "description": "Introduce the lesson's theme, goals, and expected outcomes. This segment builds curiosity and sets the stage for learning, connecting the activities to STEM standards and creative problem-solving."
            },
            {
                "title": "Materials",
                "durationMinutes": 40,
                "description": "Walk through all materials needed for hands-on exploration. STEMify Kits and additional tools ensure accessibility for every learner and support a proven, tactile learning approach."
            },
            {
                "title": "Facilitation and Collaboration Work",
                "durationMinutes": 10,
                "description": "Guide students through teamwork-oriented activities. Facilitation prompts help teachers foster collaboration, inquiry, and shared problem-solving to deepen understanding."
            },
            {
                "title": "Preparation",
                "durationMinutes": 10,
                "description": "Teachers prepare for the session using onboarding guidance and expert tips. STEMify Classroom resources support smooth facilitation and build educator confidence."
            },
            {
                "title": "Build",
                "durationMinutes": 30,
                "description": "Students create, test, and refine ideas using STEMify building systems. This hands-on experience encourages inventiveness, resilience, and creative confidence in STEM learning."
            }
        ]
    }

