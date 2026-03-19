from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, Type, Optional, Any, List, Tuple
from time import time
import logging

from app.core.agent.base import Agent
from app.core.agent.react_agent import ReActTeachingAgent
from app.core.agent.plan_solve_agent import PlanAndSolveInsightsAgent
from app.core.agent.reflection_agent import ReflectionContentAgent
from app.core.tools.registry import ToolRegistry
from app.core.llm.client import LLMClient
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


AgentClass = Type[Agent]


@dataclass
class _PooledAgentEntry:
    agent: Agent
    in_use: bool
    last_used_ts: float


class AgentPoolManager:
    """
    Role-based Agent Pool Manager.

    """

    def __init__(
        self,
        max_pool_size_per_key: int = 4,
        max_idle_seconds: int = 300,
    ):
        self.max_pool_size_per_key = max_pool_size_per_key
        self.max_idle_seconds = max_idle_seconds

        self._pools: Dict[Tuple[str, str], List[_PooledAgentEntry]] = {}

        self._metrics: Dict[str, int] = {
            "pool_hits": 0,
            "pool_misses": 0,
            "pool_evictions": 0,
            "created": 0,
        }

    def _get_key(self, role: Optional[str], agent_type: str) -> Tuple[str, str]:
        return (role or "default", agent_type)

    def _evict_idle(self, key: Tuple[str, str]) -> None:
        """Evict idle agents beyond idle timeout."""
        now = time()
        pool = self._pools.get(key, [])
        kept: List[_PooledAgentEntry] = []
        for entry in pool:
            if entry.in_use:
                kept.append(entry)
                continue
            if now - entry.last_used_ts > self.max_idle_seconds:
                self._metrics["pool_evictions"] += 1
            else:
                kept.append(entry)
        self._pools[key] = kept

    async def acquire(
        self,
        role: Optional[str],
        agent_type: str,
        llm: LLMClient,
        tool_registry: Optional[ToolRegistry] = None,
        system_prompt: Optional[str] = None,
    ) -> Agent:
        """
        Acquire an agent from the pool (or create new).

        agent_type: "react" | "plan_solve" | "reflection"
        """
        key = self._get_key(role, agent_type)
        self._evict_idle(key)

        pool = self._pools.setdefault(key, [])

        # Simple pooling is feature-flagged
        if not settings.ENABLE_AGENT_POOLING:
            self._metrics["pool_misses"] += 1
            agent = self._create_agent(agent_type, llm, tool_registry, system_prompt)
            return agent

        # Find idle agent
        for entry in pool:
            if not entry.in_use:
                entry.in_use = True
                entry.last_used_ts = time()
                self._metrics["pool_hits"] += 1
                logger.debug(f"[AgentPool] Hit: {key}, current pool size={len(pool)}")
                return entry.agent

        # No idle agent -> create new if under limit
        if len(pool) < self.max_pool_size_per_key:
            agent = self._create_agent(agent_type, llm, tool_registry, system_prompt)
            entry = _PooledAgentEntry(agent=agent, in_use=True, last_used_ts=time())
            pool.append(entry)
            self._metrics["pool_misses"] += 1
            self._metrics["created"] += 1
            logger.debug(f"[AgentPool] Miss-create: {key}, new size={len(pool)}")
            return agent

        # Pool full -> simple fallback (no pooling for this call)
        logger.debug(f"[AgentPool] Pool full for {key}, creating ephemeral agent")
        self._metrics["pool_misses"] += 1
        agent = self._create_agent(agent_type, llm, tool_registry, system_prompt)
        return agent

    async def release(self, role: Optional[str], agent_type: str, agent: Agent) -> None:
        """Mark agent as idle if it belongs to the pool; otherwise ignore."""
        key = self._get_key(role, agent_type)
        pool = self._pools.get(key, [])
        for entry in pool:
            if entry.agent is agent:
                entry.in_use = False
                entry.last_used_ts = time()
                logger.debug(f"[AgentPool] Release: {key}")
                return

    def _create_agent(
        self,
        agent_type: str,
        llm: LLMClient,
        tool_registry: Optional[ToolRegistry],
        system_prompt: Optional[str],
    ) -> Agent:
        """Factory for agent instances."""
        if agent_type == "react":
            return ReActTeachingAgent(llm=llm, tool_registry=tool_registry, system_prompt=system_prompt)
        if agent_type == "plan_solve":
            return PlanAndSolveInsightsAgent(llm=llm, tool_registry=tool_registry, system_prompt=system_prompt)
        if agent_type == "reflection":
            return ReflectionContentAgent(llm=llm, tool_registry=tool_registry, system_prompt=system_prompt)
        raise ValueError(f"Unknown agent_type: {agent_type}")

    def get_stats(self) -> Dict[str, Any]:
        """Expose metrics for monitoring."""
        pools_summary = {
            f"{role}:{atype}": len(entries)
            for (role, atype), entries in self._pools.items()
        }
        return {
            "metrics": dict(self._metrics),
            "pools": pools_summary,
            "max_pool_size_per_key": self.max_pool_size_per_key,
            "max_idle_seconds": self.max_idle_seconds,
        }



