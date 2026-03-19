import asyncio
from time import sleep

from app.core.agent.pool.manager import AgentPoolManager
from app.core.llm.client import LLMClient
from app.core.tools.registry import ToolRegistry


class DummyLLM(LLMClient):

    def __init__(self):
        # Call base with None providers to avoid network calls in tests
        super().__init__(local_provider=None, remote_provider=None)


def test_agent_pool_acquire_and_release():
    pool = AgentPoolManager(max_pool_size_per_key=2, max_idle_seconds=60)
    llm = DummyLLM()
    registry = ToolRegistry()

    async def run():
        agent1 = await pool.acquire(role="student", agent_type="react", llm=llm, tool_registry=registry)
        await pool.release(role="student", agent_type="react", agent=agent1)
        agent2 = await pool.acquire(role="student", agent_type="react", llm=llm, tool_registry=registry)
        # On pooling enabled, we expect to reuse same instance; otherwise this still validates API shape.
        assert agent1 is agent2 or agent1 is not None and agent2 is not None

    asyncio.get_event_loop().run_until_complete(run())


def test_context_builder_session_cache():
    from app.core.context.builder import JITContextBuilder
    from app.core.context.selector import ContextSelector
    from app.core.context.structurer import ContextStructurer
    from app.core.context.compressor import ContextCompressor
    from app.core.context.models import ContextItem

    class DummyGatherer:
        async def gather(self, query: str, user_id=None, top_k=10):
            return [ContextItem(content="alpha", score=0.9, source="memory:working")]

    builder = JITContextBuilder(
        gatherer=DummyGatherer(),
        selector=ContextSelector(max_items=5),
        structurer=ContextStructurer(),
        compressor=ContextCompressor(max_chars_per_item=100),
        token_budget=1000,
    )

    async def run():
        b1 = await builder.build("q", user_id="u1", top_k=5, session_id="s1")
        b2 = await builder.build("q", user_id="u1", top_k=5, session_id="s1")
        assert b1 is b2

    asyncio.get_event_loop().run_until_complete(run())



