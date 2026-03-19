"""
Pooled Agent Router

Implements AgentRouter using AgentPoolManager and existing LegacyAgentRouter.
Feature-flagged via settings.USE_NEW_AGENT_ROUTER.

Task type convention:
- "<role>:react"
- "<role>:plan_solve"
- "<role>:reflection"
Example: "student:react", "teacher:plan_solve"
"""

from typing import Dict, Any, Optional, Tuple
import logging

from app.core.agent.base_router import AgentRouter
from app.core.agent.pool.manager import AgentPoolManager
from app.core.agent.react_agent import ReActTeachingAgent
from app.core.agent.plan_solve_agent import PlanAndSolveInsightsAgent
from app.core.agent.reflection_agent import ReflectionContentAgent
from app.core.agent.router_legacy import LegacyAgentRouter
from app.core.tools.registry import ToolRegistry
from app.core.llm.client import LLMClient
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class PooledAgentRouter(AgentRouter):
    """
    Agent router that leases agents from pools when enabled,
    and falls back to LegacyAgentRouter otherwise.
    """

    def __init__(
        self,
        pool_manager: AgentPoolManager,
        legacy_router: LegacyAgentRouter,
        llm_client: LLMClient,
        tool_registry: ToolRegistry,
    ):
        self.pool_manager = pool_manager
        self.legacy_router = legacy_router
        self.llm_client = llm_client
        self.tool_registry = tool_registry

    async def route(self, query: str, task_type: Optional[str] = None) -> Dict[str, Any]:
        """
        Route query either through pooled agents or legacy router.

        - When USE_NEW_AGENT_ROUTER is False: delegate to LegacyAgentRouter.
        - When task_type is not in "<role>:<agent_type>" format: delegate to LegacyAgentRouter.
        - Otherwise: lease agent from pool, run, then release.
        """
        if not settings.USE_NEW_AGENT_ROUTER:
            return await self.legacy_router.route(query, task_type)

        role, agent_kind = self._parse_task_type(task_type)
        if not agent_kind:
            # Unknown or missing task_type -> keep legacy behavior
            return await self.legacy_router.route(query, task_type)

        # Map agent_kind to internal identifier
        if agent_kind not in {"react", "plan_solve", "reflection"}:
            return await self.legacy_router.route(query, task_type)

        # Lease from pool
        agent = await self.pool_manager.acquire(
            role=role,
            agent_type=agent_kind,
            llm=self.llm_client,
            tool_registry=self.tool_registry,
            system_prompt=None,
        )

        try:
            result = await agent.run(query)
            # Normalize result format
            return {
                "answer": result.get("answer", ""),
                "path": result.get("path", agent_kind),
                "metadata": {
                    "agent_type": agent_kind,
                    "role": role or "default",
                    "pooled": settings.ENABLE_AGENT_POOLING,
                },
            }
        finally:
            # Release only if agent came from pool (acquire handles pooling flag)
            await self.pool_manager.release(role=role, agent_type=agent_kind, agent=agent)

    def _parse_task_type(self, task_type: Optional[str]) -> Tuple[Optional[str], Optional[str]]:
        """
        Parse task_type in the form "<role>:<agent_type>".
        Returns (role, agent_type) or (None, None) if invalid.
        """
        if not task_type or ":" not in task_type:
            return None, None
        role, agent_kind = task_type.split(":", 1)
        role = role.strip() or None
        agent_kind = agent_kind.strip()
        return role, agent_kind or None



