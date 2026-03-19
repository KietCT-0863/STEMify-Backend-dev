"""
Content Generation Prompts
Prompt templates for content generation workflows
"""

from app.core.data.models import LessonDto
from app.infrastructure.config.settings import settings


def build_lesson_context(lesson: LessonDto) -> str:
    """Serialize lesson metadata into human-readable context."""
    lines = [
        f"Lesson Title: {lesson.title}",
        f"Description: {lesson.description}",
        "Learning Outcomes: " + ", ".join(lesson.learning_outcomes),
        "Skills: " + ", ".join(lesson.skills),
        "Topics: " + ", ".join(lesson.topics),
        "Standards: " + ", ".join(lesson.standards),
        "",
        "Existing Sections:",
    ]

    for idx, section in enumerate(lesson.sections, start=1):
        lines.append(f"[Section {idx}]")
        lines.append(f"Title: {section.title}")
        if section.duration_minutes is not None:
            lines.append(f"Duration: {section.duration_minutes} minutes")
        lines.append(f"Description: {section.description}")
        lines.append("")

    return "\n".join(lines)


def _get_default_section_prompt_template() -> str:
    return """You are an educational content designer.

You are given a STEM lesson and its existing sections.

CONTEXT:
{context_text}

TASK:
- Propose EXACTLY ONE NEW SECTION for this lesson.
- The new section MUST:
  - Fit logically with the overall lesson description and learning outcomes.
  - Not duplicate existing sections (title or description).
  - Add real value (reflection, assessment, extension activity, debrief, real-world link, or cross-curricular tie).

REQUIREMENTS:
- The section should be realistic for a classroom setting.
- Duration should be a reasonable integer number of minutes.

OUTPUT FORMAT (valid JSON, no commentary):
{{
  "title": "string",
  "durationMinutes": integer,
  "description": "string"
}}
"""


def build_section_prompt(context_text: str, lang: str = "vi") -> str:
    """
    Return LLM prompt for generating a new lesson section.
    
    Uses prompt template from settings if configured, otherwise falls back to default.
    The template should contain {context_text} placeholder.
    """
    # Get template from settings or use default
    template_str = settings.CONTENT_GENERATION_SECTION_PROMPT_TEMPLATE
    if template_str:
        template_str = template_str.strip()
        if not template_str:  # Empty after stripping
            template_str = None
    template = template_str or _get_default_section_prompt_template()
    
    # Add language instruction to the prompt
    lang_instruction = ""
    if lang == "vi":
        lang_instruction = "\n\nLANGUAGE REQUIREMENT:\nYou MUST respond in Vietnamese (Tiếng Việt). All text fields including title, description, and any other content MUST be in Vietnamese."
    elif lang == "en":
        lang_instruction = "\n\nLANGUAGE REQUIREMENT:\nYou MUST respond in English. All text fields including title, description, and any other content MUST be in English."
    else:
        lang_instruction = f"\n\nLANGUAGE REQUIREMENT:\nYou MUST respond in {lang}. All text fields including title, description, and any other content MUST be in {lang}."
    
    template_with_lang = template + lang_instruction
    
    # Format template with context
    return template_with_lang.format(context_text=context_text)
