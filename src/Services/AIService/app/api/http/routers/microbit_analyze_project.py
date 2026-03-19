"""
Microbit Analyze Project API Router
HTTP endpoints for analyzing micro:bit projects
"""

import logging

from fastapi import APIRouter, Depends, HTTPException
from app.api.http.dependencies import get_microbit_analyze_project_service
from app.features.microbit_analyze_project.models import (
    MicrobitAnalyzeProjectRequest,
    MicrobitAnalyzeProjectResponse,
)
from app.features.microbit_analyze_project.service import MicrobitAnalyzeProjectService

router = APIRouter(prefix="/microbit", tags=["microbit"])
logger = logging.getLogger(__name__)


@router.post(
    "/analyze-project", 
    response_model=MicrobitAnalyzeProjectResponse, 
    summary="Analyze a micro:bit project"
)
async def analyze_microbit_project(
    body: MicrobitAnalyzeProjectRequest,
    service: MicrobitAnalyzeProjectService = Depends(get_microbit_analyze_project_service),
) -> MicrobitAnalyzeProjectResponse:
    """
    Analyze a micro:bit project and provide comprehensive feedback.
    
    This endpoint accepts a complete micro:bit project (including blocks, TypeScript, 
    and metadata) and returns detailed analysis, suggestions, and learning points.
    
    **Analysis Types:**
    - `comprehensive`: Full project analysis with all sections
    - `specific_question`: Answer a specific question about the project
    
    **Languages Supported:**
    - `vi`: Vietnamese
    - `en`: English
    
    **Example Request:**
    ```json
    {
      "project_files": {
        "README.md": "",
        "main.blocks": "<xml>...</xml>",
        "main.ts": "basic.forever(function () { ... })",
        "pxt.json": {
          "name": "My Project",
          "dependencies": {"core": "*"}
        }
      },
      "question": "Student struggled with timing in previous assignment",
      "language": "vi",
      "analysis_type": "comprehensive"
    }
    ```
    """
    try:
        return await service.analyze_project(body)
    except Exception as e:
        logger.error(f"Error analyzing micro:bit project: {e}", exc_info=True)
        raise HTTPException(
            status_code=500, 
            detail=f"Failed to analyze project: {str(e)}"
        )