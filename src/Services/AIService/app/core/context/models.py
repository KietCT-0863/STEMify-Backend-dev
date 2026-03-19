from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional


@dataclass
class ContextItem:
    """Single context unit with scoring and provenance."""

    content: str
    score: float = 0.0
    source: str = ""
    metadata: Dict[str, Any] = field(default_factory=dict)


@dataclass
class ContextBundle:
    """Structured context bundle returned by the builder."""

    items: List[ContextItem] = field(default_factory=list)
    total_tokens: int = 0
    token_budget: int = 0
    notes: Optional[str] = None

