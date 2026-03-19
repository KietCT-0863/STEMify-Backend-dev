import json
from pydantic import BaseModel, Field, validator
from typing import Optional, Dict, Any

class MicrobitProjectFile(BaseModel):
    """Represents the micro:bit project files"""
    readme: str = Field(default="", alias="README.md")
    main_blocks: str = Field(..., alias="main.blocks")
    main_ts: str = Field(..., alias="main.ts")
    pxt_json: Dict[str, Any] = Field(..., alias="pxt.json")

    @validator("pxt_json", pre=True)
    def parse_pxt_json(cls, v):
        if isinstance(v, dict):
            return v
        if isinstance(v, str):
            try:
                return json.loads(v)
            except json.JSONDecodeError:
                raise ValueError("pxt.json: invalid JSON string")
        raise ValueError("pxt.json must be a dict or a JSON string")
    
    class Config:
        populate_by_name = True


class MicrobitAnalyzeProjectRequest(BaseModel):
    project_files: MicrobitProjectFile = Field(..., description="The micro:bit project files to analyze.")
    question: Optional[str] = Field(None, description="Specific question about the project (optional).")
    language: str = Field(default="vi", description="Language code for the response (e.g., 'vi' for Vietnamese, 'en' for English).")
    analysis_type: str = Field(
        default="comprehensive",
        description="Type of analysis: 'comprehensive', 'code_review', 'learning_feedback', or 'specific_question'"
    )


class MicrobitAnalyzeProjectResponse(BaseModel):
    analysis: str = Field(..., description="Detailed analysis of the micro:bit project.")
    suggestions: Optional[str] = Field(None, description="Suggestions for improvement.")
    learning_points: Optional[str] = Field(None, description="Key learning points from the project.")
    answer: Optional[str] = Field(None, description="Answer to specific question if provided.")
    provider: str = Field(..., description="LLM provider that generated the response.")
    model: str = Field(..., description="LLM model identifier used for the response.")