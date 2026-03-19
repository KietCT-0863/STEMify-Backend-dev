"""
Streaming RAG readiness: interfaces for freshness tracking and routing.
"""

from typing import List
import logging
import collections
import math

logger = logging.getLogger(__name__)


class HeavyHitterCounter:
    """
    Track high-frequency items for prioritizing freshness.
    Lightweight frequency counter used to decide which
    documents/lessons should be refreshed first.
    """

    def __init__(self, max_size: int = 500) -> None:
        self.max_size = max_size
        self.counter: collections.Counter[str] = collections.Counter()

    def add(self, item_id: str) -> None:
        """Increment access count for an item and keep only top-N."""
        self.counter[item_id] += 1
        if len(self.counter) > self.max_size:
            self.counter = collections.Counter(
                dict(self.counter.most_common(self.max_size))
            )

    def topk(self, k: int = 50) -> List[str]:
        """Return up to k most frequently accessed item ids."""
        return [item for item, _ in self.counter.most_common(k)]


class MultiVectorCosineFilter:
    """
    Multi-vector cosine similarity filter.

    Given a list of embedding vectors and a query vector,
    returns indices whose cosine similarity is above a threshold.
    """

    @staticmethod
    def _cosine(a: List[float], b: List[float]) -> float:
        if not a or not b or len(a) != len(b):
            return 0.0
        dot = sum(x * y for x, y in zip(a, b))
        norm_a = math.sqrt(sum(x * x for x in a))
        norm_b = math.sqrt(sum(y * y for y in b))
        if norm_a == 0.0 or norm_b == 0.0:
            return 0.0
        return dot / (norm_a * norm_b)

    def filter(
        self,
        vectors: List[List[float]],
        query_vector: List[float],
        threshold: float = 0.8,
    ) -> List[int]:
        """
        Return indices of vectors whose cosine similarity to query_vector
        is >= threshold.
        """
        indices: List[int] = []
        for idx, v in enumerate(vectors):
            try:
                score = self._cosine(v, query_vector)
            except Exception as exc:  # defensive: never break retrieval
                logger.warning("Cosine similarity failed for index %s: %s", idx, exc)
                continue
            if score >= threshold:
                indices.append(idx)
        return indices


class StreamingRAGRouter:
    """
    Route between batch and streaming paths based on freshness signals.

    The router itself is intentionally simple; callers decide what
    'has_fresh_chunks' means (e.g., recent updates, high-priority docs).
    """

    def __init__(self, freshness_feature_flag: bool = False) -> None:
        self.freshness_feature_flag = freshness_feature_flag

    def route(self, has_fresh_chunks: bool) -> str:
        """
        Decide which path to use.

        Returns:
            \"streaming\" if freshness is enabled and fresh chunks exist,
            otherwise \"batch\".
        """
        if not self.freshness_feature_flag:
            return "batch"
        return "streaming" if has_fresh_chunks else "batch"

