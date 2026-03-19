from typing import Dict, Any, List, Optional
import logging
import json
import time
from collections import OrderedDict

from app.core.tools.base import Tool
from app.core.memory.memory_manager import MemoryManager

logger = logging.getLogger(__name__)


class RubricTool(Tool):
    """
    Retrieve grading rubrics from semantic memory.

    Rubrics are stored as structured JSON under a rubric_id key.
    """

    _cache: Dict[str, tuple] = {}
    _cache_ttl: int = 3600  # 1 hour in seconds
    _max_cache_size: int = 100  # Maximum number of cached rubrics

    def __init__(self, memory_manager: MemoryManager):
        super().__init__(
            name="rubric",
            description="Fetch grading rubric definition from semantic memory",
        )
        self.memory_manager = memory_manager
    
    def can_run_parallel(self) -> bool:
        return True

    async def run(self, parameters: Dict[str, Any]) -> str:
        """
        Parameters:
        - rubric_id: str (required)
        """
        rubric_id = parameters.get("rubric_id")
        if not rubric_id:
            return json.dumps({"error": "rubric_id is required"})

        cached_result = self._get_from_cache(rubric_id)
        if cached_result is not None:
            logger.debug(
                "[RubricTool] Returning cached rubric",
                extra={"rubric_id": rubric_id}
            )
            return cached_result

        try:
            if not self.memory_manager.semantic_memory:
                default_rubric = {
                    "title": "Default Grading Rubric",
                    "criteria": [
                        {
                            "criterion": "Accuracy",
                            "description": "Correctness of the answer",
                            "max_points": 10,
                            "weight": 1.0
                        },
                        {
                            "criterion": "Completeness",
                            "description": "All required parts addressed",
                            "max_points": 5,
                            "weight": 0.8
                        },
                        {
                            "criterion": "Clarity",
                            "description": "Clear explanation and presentation",
                            "max_points": 5,
                            "weight": 0.5
                        }
                    ]
                }
                result = json.dumps({
                    "rubric_id": rubric_id,
                    "rubric": default_rubric,
                    "note": "Default rubric used - semantic memory not initialized"
                })
                self._add_to_cache(rubric_id, result)
                return result
            
            # High-level: search semantic memory for rubric document
            search_results = await self.memory_manager.semantic_memory.search(
                query=f"grading rubric {rubric_id}",
                limit=10,  # Get more results to filter
            )
            
            results = []
            for result in search_results:
                metadata = result.get("metadata", {})
                if metadata.get("rubric_id") == rubric_id:
                    results.append(result)
            if not results and search_results:
                for result in search_results:
                    content = result.get("content", "")
                    metadata = result.get("metadata", {})
                    if (rubric_id in str(content) or 
                        rubric_id in str(metadata) or
                        any(rubric_id in str(v) for v in metadata.values())):
                        results.append(result)
                        break
                if not results:
                    results = [search_results[0]]
        except Exception as e:
            logger.error("[RubricTool] Error querying semantic memory: %s", e, exc_info=True)

            default_rubric = {
                "title": "Default Grading Rubric",
                "criteria": [
                    {
                        "criterion": "Accuracy",
                        "description": "Correctness of the answer",
                        "max_points": 10,
                        "weight": 1.0
                    },
                    {
                        "criterion": "Completeness",
                        "description": "All required parts addressed",
                        "max_points": 5,
                        "weight": 0.8
                    },
                    {
                        "criterion": "Clarity",
                        "description": "Clear explanation and presentation",
                        "max_points": 5,
                        "weight": 0.5
                    }
                ]
            }
            result = json.dumps({
                "rubric_id": rubric_id,
                "rubric": default_rubric,
                "note": "Default rubric used - error querying semantic memory"
            })
            self._add_to_cache(rubric_id, result)
            return result

        if not results:
            # Return a default rubric if not found (for mock/testing)
            logger.warning(
                "[RubricTool] Rubric not found, returning default rubric",
                extra={"rubric_id": rubric_id}
            )
            default_rubric = {
                "title": "Default Grading Rubric",
                "criteria": [
                    {
                        "criterion": "Accuracy",
                        "description": "Correctness of the answer",
                        "max_points": 10,
                        "weight": 1.0
                    },
                    {
                        "criterion": "Completeness",
                        "description": "All required parts addressed",
                        "max_points": 5,
                        "weight": 0.8
                    },
                    {
                        "criterion": "Clarity",
                        "description": "Clear explanation and presentation",
                        "max_points": 5,
                        "weight": 0.5
                    }
                ]
            }
            result = json.dumps({
                "rubric_id": rubric_id,
                "rubric": default_rubric,
                "note": "Default rubric used - original rubric not found"
            })
            self._add_to_cache(rubric_id, result)
            return result

        result_data = results[0]
        metadata = result_data.get("metadata", {})
        content = result_data.get("content") or metadata.get("content", "")
        
        rubric = metadata.get("rubric")
        
        if rubric is None and content:
            try:
                # Try parsing content as JSON
                rubric = json.loads(content)
            except (json.JSONDecodeError, TypeError):
                # If content is not JSON, check if it's a string representation
                try:
                    # Try to extract JSON from content if it's embedded
                    if "{" in content and "}" in content:
                        start = content.find("{")
                        end = content.rfind("}") + 1
                        rubric = json.loads(content[start:end])
                    else:
                        # Use content as raw text
                        rubric = {"raw_text": content}
                except Exception:
                    rubric = {"raw_text": content}
        
        if not rubric or (isinstance(rubric, dict) and rubric.get("raw_text") == content and not rubric.get("title")):
            if "title" in metadata or "criteria" in metadata:
                rubric = {
                    "title": metadata.get("title", "Grading Rubric"),
                    "criteria": metadata.get("criteria", []),
                }
            elif not rubric:
                rubric = {
                    "title": metadata.get("title", "Grading Rubric"),
                    "criteria": metadata.get("criteria", []),
                }

        result = json.dumps(
            {
                "rubric_id": rubric_id,
                "rubric": rubric,
            }
        )
        
        # Cache the result
        self._add_to_cache(rubric_id, result)
        
        return result
    
    @classmethod
    def _get_from_cache(cls, rubric_id: str) -> Optional[str]:
        """Get rubric from cache if available and not expired"""
        if rubric_id not in cls._cache:
            return None
        
        rubric_data, timestamp = cls._cache[rubric_id]
        current_time = time.time()
        
        # Check if cache entry is still valid
        if current_time - timestamp > cls._cache_ttl:
            # Cache expired, remove it
            del cls._cache[rubric_id]
            logger.debug(
                "[RubricTool] Cache entry expired",
                extra={"rubric_id": rubric_id}
            )
            return None
        
        return rubric_data
    
    @classmethod
    def _add_to_cache(cls, rubric_id: str, rubric_data: str) -> None:
        """Add rubric to cache with timestamp"""
        # Remove oldest entries if cache is full
        if len(cls._cache) >= cls._max_cache_size:
            # Remove the oldest entry (first one)
            oldest_key = next(iter(cls._cache))
            del cls._cache[oldest_key]
            logger.debug(
                "[RubricTool] Cache full, removed oldest entry",
                extra={"removed_rubric_id": oldest_key}
            )
        
        cls._cache[rubric_id] = (rubric_data, time.time())
        logger.debug(
            "[RubricTool] Added to cache",
            extra={"rubric_id": rubric_id, "cache_size": len(cls._cache)}
        )

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "rubric_id": {
                    "type": "string",
                    "description": "Rubric identifier",
                },
            },
            "required": ["rubric_id"],
        }


