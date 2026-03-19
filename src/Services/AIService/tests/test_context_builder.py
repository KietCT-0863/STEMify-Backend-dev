import asyncio
from types import SimpleNamespace

from app.core.context.builder import JITContextBuilder
from app.core.context.selector import ContextSelector
from app.core.context.structurer import ContextStructurer
from app.core.context.compressor import ContextCompressor
from app.core.context.models import ContextItem, ContextBundle


class DummyGatherer:
    async def gather(self, query: str, user_id=None, top_k=10):
        return [
            ContextItem(content="alpha", score=0.9, source="memory:working"),
            ContextItem(content="beta", score=0.8, source="retrieval"),
        ]


def test_jit_context_builder_runs():
    builder = JITContextBuilder(
        gatherer=DummyGatherer(),
        selector=ContextSelector(max_items=5),
        structurer=ContextStructurer(),
        compressor=ContextCompressor(max_chars_per_item=100),
        token_budget=1000,
    )

    bundle: ContextBundle = asyncio.get_event_loop().run_until_complete(
        builder.build("test query")
    )

    assert bundle.items
    assert bundle.total_tokens > 0
    assert bundle.token_budget == 1000

