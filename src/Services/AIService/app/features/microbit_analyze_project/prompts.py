from app.infrastructure.config.settings import settings
from typing import Dict, Any, Optional


def build_microbit_analyze_project_prompt(
    project_files: Dict[str, Any],
    question: Optional[str] = None,
    language: str = "vi",
    analysis_type: str = "comprehensive"
) -> str:
    """Build the prompt for micro:bit project analysis."""

    # Extract project information
    project_name = project_files.get("pxt.json", {}).get("name", "Unnamed Project")
    main_blocks = project_files.get("main.blocks", "")
    main_ts = project_files.get("main.ts", "")
    dependencies = project_files.get("pxt.json", {}).get("dependencies", {})

    deps_list = ", ".join(dependencies.keys()) if dependencies else "core only"

    # Select template by analysis type
    if analysis_type == "specific_question":
        template = (
            settings.MICROBIT_SPECIFIC_QUESTION_PROMPT_TEMPLATE
            or _get_specific_question_prompt_template()
        )
    else:
        template = (
            settings.MICROBIT_COMPREHENSIVE_PROMPT_TEMPLATE
            or _get_comprehensive_prompt_template()
        )

    return template.format(
        language=language,
        project_name=project_name,
        dependencies=deps_list,
        main_blocks=main_blocks,
        main_ts=main_ts,
        question=question or "No specific question"
    )



def _get_evaluation_focus(evaluation_type: str, language: str) -> str:
    """Get the evaluation focus text based on type and language."""
    
    focuses = {
        "comprehensive": {
            "vi": "Đánh giá toàn diện dự án, bao gồm chất lượng code, hiểu biết về khái niệm, và đề xuất điểm số.",
            "en": "Comprehensive evaluation including code quality, concept understanding, and scoring recommendation."
        },
        "specific_question": {
            "vi": "Trả lời câu hỏi cụ thể về dự án.",
            "en": "Answer specific question about the project."
        }
    }
    
    return focuses.get(evaluation_type, focuses["comprehensive"]).get(
        language, focuses["comprehensive"]["en"]
    )

def _get_comprehensive_prompt_template() -> str:
    """Prompt for full teacher-style project evaluation."""
    return """
You are an experienced STEM educator and micro:bit expert assisting teachers in evaluating student projects.

IMPORTANT: Respond in {language}. Use professional, clear language for teachers.

PROJECT:
- Name: {project_name}
- Dependencies: {dependencies}

BLOCKS:
{main_blocks}

TYPESCRIPT:
{main_ts}

TASK (keep answers concise):
1) Score the project from 0–10 with a one-sentence justification.
2) Provide 2–3 actionable improvement suggestions.
3) Provide 2–3 learning points demonstrated or to learn next.

Tone: professional, constructive, encouraging.
"""

def _get_specific_question_prompt_template() -> str:
    """Prompt for answering a specific question only (no evaluation)."""
    return """
You are a micro:bit expert.

IMPORTANT: Respond in {language}. Use professional, clear language for teachers.

IMPORTANT RULES:
- Answer ONLY the question
- Do NOT score, evaluate, or give general feedback
- Be as short and direct as possible
- Reference relevant blocks or TypeScript if needed

PROJECT:
- Name: {project_name}
- Dependencies: {dependencies}

QUESTION:
{question}

BLOCKS:
{main_blocks}

TYPESCRIPT:
{main_ts}

TASK:
Answer the question concisely. If the answer is visible in code, point to it briefly.
"""
