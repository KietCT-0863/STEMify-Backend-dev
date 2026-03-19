"""
Context Structurer - GSSC
Groups selected items into ordered sections for the LLM.
"""

from typing import List, Dict, Any
import logging

from app.core.context.models import ContextItem

logger = logging.getLogger(__name__)


class ContextStructurer:

    def structure(self, items: List[ContextItem]) -> Dict[str, Any]:
        sections = {
            "memory": [],
            "retrieval": [],
            "other": [],
        }

        for item in items:
            if item.source.startswith("memory"):
                sections["memory"].append(item)
            elif item.source == "retrieval":
                sections["retrieval"].append(item)
            else:
                sections["other"].append(item)

        return sections

