"""
Context Selector - GSSC 
Filters and ranks candidate context items.
"""

from typing import List
import logging

from app.core.context.models import ContextItem

logger = logging.getLogger(__name__)


class ContextSelector:
    """
    Select top context items based on score and optional heuristics.
    """

    def __init__(self, max_items: int = 20):
        self.max_items = max_items

    def select(self, candidates: List[ContextItem]) -> List[ContextItem]:
        """
        Select top-k items by score.
        """
        if not candidates:
            return []

        sorted_items = sorted(candidates, key=lambda c: c.score, reverse=True)
        return sorted_items[: self.max_items]

