"""
Context Compressor - GSSC 
Applies lightweight compression/summarization when over token budget.
"""

from typing import List, Dict, Any
import logging

from app.core.context.models import ContextItem

logger = logging.getLogger(__name__)


class ContextCompressor:

    def __init__(self, max_chars_per_item: int = 800):
        self.max_chars_per_item = max_chars_per_item

    def compress(self, structured: Dict[str, List[ContextItem]]) -> Dict[str, List[ContextItem]]:
        compressed: Dict[str, List[ContextItem]] = {}

        for section, items in structured.items():
            new_items: List[ContextItem] = []
            for item in items:
                content = item.content
                if len(content) > self.max_chars_per_item:
                    content = content[: self.max_chars_per_item] + "..."
                new_items.append(
                    ContextItem(
                        content=content,
                        score=item.score,
                        source=item.source,
                        metadata=item.metadata,
                    )
                )
            compressed[section] = new_items

        return compressed

