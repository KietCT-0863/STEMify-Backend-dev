"""
Microbit Analyze Project Service
Service for analyzing micro:bit projects
"""

import logging
from typing import Dict, Any

from app.core.llm.client import LLMClient
from app.core.llm.providers.base_provider import BaseLLMProvider, LLMMessage
from app.features.microbit_analyze_project.models import (
    MicrobitAnalyzeProjectRequest,
    MicrobitAnalyzeProjectResponse,
)
from app.features.microbit_analyze_project.prompts import (
    build_microbit_analyze_project_prompt,
)
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class MicrobitAnalyzeProjectService:
    """Service for analyzing micro:bit projects."""
    
    def __init__(self, llm_client: LLMClient):
        self.llm_client = llm_client

    async def analyze_project(
        self, 
        request: MicrobitAnalyzeProjectRequest
    ) -> MicrobitAnalyzeProjectResponse:
        """
        Analyze a micro:bit project and provide comprehensive feedback.
        
        Args:
            request: The analysis request containing project files and parameters
            
        Returns:
            MicrobitAnalyzeProjectResponse with analysis, suggestions, and learning points
        """
        try:
            # Get the remote LLM provider
            remote_provider = self.llm_client.get_remote_provider()
            provider_name = self._resolve_provider_name(remote_provider)

            # Convert project files to dict for prompt building
            project_files_dict = {
                "README.md": request.project_files.readme,
                "main.blocks": request.project_files.main_blocks,
                "main.ts": request.project_files.main_ts,
                "pxt.json": request.project_files.pxt_json,
            }

            # Build the analysis prompt
            user_prompt = build_microbit_analyze_project_prompt(
                project_files=project_files_dict,
                question=request.question,
                language=request.language,
                analysis_type=request.analysis_type
            )

            # Generate response using LLM
            response = await self.llm_client.generate_remote(
                messages=[
                    LLMMessage(
                        role="system",
                        content=settings.MICROBIT_EVALUATE_PROJECT_SYSTEM_PROMPT,
                    ),
                    LLMMessage(
                        role="user",
                        content=user_prompt
                    )
                ]
            )

            # Parse the response content
            analysis_content = response.content.strip()
            
            # Extract sections from the response (simple parsing)
            parsed_sections = self._parse_analysis_response(analysis_content)

            return MicrobitAnalyzeProjectResponse(
                analysis=parsed_sections.get("analysis", analysis_content),
                suggestions=parsed_sections.get("suggestions"),
                learning_points=parsed_sections.get("learning_points"),
                answer=parsed_sections.get("answer"),
                provider=provider_name,
                model=response.model,
            )
            
        except Exception as e:
            logger.error(f"Error analyzing micro:bit project: {e}")
            raise e

    def _parse_analysis_response(self, content: str) -> Dict[str, str]:
        """
        Parse the LLM response to extract different sections.
        
        This is a simple parser that looks for section headers.
        Returns a dict with 'analysis' (full content) and optionally
        'suggestions', 'learning_points', and 'answer'.
        """
        sections = {
            "analysis": content  # Full content as default
        }
        
        # Try to extract specific sections if they exist
        # This is a simple implementation - you might want to make it more robust
        
        # Look for suggestions section
        if "Gợi Ý Cải Thiện" in content or "Suggestions for Improvement" in content:
            # Simple extraction logic
            sections["suggestions"] = "See full analysis for suggestions"
        
        # Look for learning points section
        if "Bài Học Rút Ra" in content or "Learning Points" in content:
            sections["learning_points"] = "See full analysis for learning points"
        
        # Look for answer section
        if "Trả Lời Câu Hỏi" in content or "Answer to Question" in content:
            sections["answer"] = "See full analysis for answer"
        
        return sections

    def _resolve_provider_name(self, provider: BaseLLMProvider | None) -> str:
        """
        Resolve a user-friendly provider name for observability.
        """
        if provider is None:
            return "unknown"
        return getattr(provider, "provider_name", provider.__class__.__name__)