from typing import Dict, Any, List
import logging
import json

from app.core.tools.base import Tool

logger = logging.getLogger(__name__)


class ScoreCalculatorTool(Tool):
    """
    Calculate numeric score from rubric evaluations.

    Expected input:
    - rubric_evaluations: list of {criterion, max_score, achieved_score, weight?}
    """

    def __init__(self):
        super().__init__(
            name="score_calculator",
            description="Calculate total score from rubric-based evaluations",
        )

    async def run(self, parameters: Dict[str, Any]) -> str:
        rubric_evaluations: List[Dict[str, Any]] = parameters.get("rubric_evaluations") or []

        if not rubric_evaluations:
            return json.dumps({"error": "rubric_evaluations is required"})

        total_weighted_score = 0.0
        total_weight = 0.0
        raw_total = 0.0
        raw_max = 0.0

        for ev in rubric_evaluations:
            max_score = float(ev.get("max_score", 0.0))
            achieved = float(ev.get("achieved_score", 0.0))
            weight = float(ev.get("weight", 1.0))

            raw_total += achieved
            raw_max += max_score

            if max_score > 0:
                normalized = max(0.0, min(achieved / max_score, 1.0))
            else:
                normalized = 0.0

            total_weighted_score += normalized * weight
            total_weight += weight

        overall_percentage = 0.0
        if total_weight > 0:
            overall_percentage = (total_weighted_score / total_weight) * 100.0

        result = {
            "overall_percentage": round(overall_percentage, 2),
            "raw_total": raw_total,
            "raw_max": raw_max,
        }
        return json.dumps(result)

    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "rubric_evaluations": {
                    "type": "array",
                    "description": "Per-criterion rubric evaluations",
                    "items": {
                        "type": "object",
                        "properties": {
                            "criterion": {"type": "string"},
                            "max_score": {"type": "number"},
                            "achieved_score": {"type": "number"},
                            "weight": {"type": "number"},
                        },
                        "required": ["max_score", "achieved_score"],
                    },
                }
            },
            "required": ["rubric_evaluations"],
        }


