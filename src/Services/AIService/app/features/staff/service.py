from typing import Dict, Any, Optional
import logging

from app.features.staff.course_generator_agent import CourseGeneratorAgent
from app.features.staff.image_3d_description_agent import Image3DDescriptionAgent
from app.features.staff.step_description_agent import StepDescriptionAgent
from app.features.staff.kit_description_agent import KitDescriptionAgent
from app.features.staff.stem_category_agent import STEMCategoryAgent
from app.core.agent.pool.manager import AgentPoolManager
from app.core.context.builder import JITContextBuilder
from app.core.memory.memory_manager import MemoryManager
from app.core.llm.client import LLMClient
from app.core.rag.hybrid_retriever import HybridRetriever
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class StaffService:
    """
    Service layer orchestrating staff-facing agents:
    - CourseGeneratorAgent
    - Image3DDescriptionAgent
    - StepDescriptionAgent
    - KitDescriptionAgent
    - STEMCategoryAgent

    Integrates:
    - JITContextBuilder (context engineering + reuse)
    - MemoryManager (episodic/semantic/perceptual)
    - AgentPoolManager (pooling if extended in future)
    """

    def __init__(
        self,
        llm: LLMClient,
        context_builder: JITContextBuilder,
        memory_manager: MemoryManager,
        agent_pool_manager: AgentPoolManager,
        hybrid_retriever: HybridRetriever,
        vision_llm: Optional[LLMClient] = None,
    ):
        self.llm = llm
        self.context_builder = context_builder
        self.memory_manager = memory_manager
        self.agent_pool_manager = agent_pool_manager
        self.hybrid_retriever = hybrid_retriever
        self.vision_llm = vision_llm

        logger.info("StaffService initialized")

    

    async def generate_step_description(
        self,
        staff_id: str,
        model_id: str,
        action_type: str = "assembly",
        model_data: Optional[Dict[str, Any]] = None,
        session_id: Optional[str] = None,
    ) -> Dict[str, Any]:
        """
        Generate step-by-step instructions using StepDescriptionAgent.
        """
        agent = StepDescriptionAgent(
            llm=self.llm,
            use_remote=settings.STAFF_AGENTS_USE_REMOTE,
        )

        query = f"Generate {action_type} steps for 3D model {model_id}"

        context_bundle = await self.context_builder.build(
            query=query,
            user_id=staff_id,
            top_k=10,
            session_id=session_id,
        )

        result = await agent.generate_steps(
            model_id=model_id,
            action_type=action_type,
            model_data=model_data,
        )

        # Store in memory
        try:
            await self.memory_manager.add_memory(
                content=f"Step description generated for model {model_id}",
                memory_type="episodic",
                metadata={
                    "type": "staff_step_description",
                    "staff_id": staff_id,
                    "model_id": model_id,
                    "action_type": action_type,
                    "session_id": session_id,
                    "context_tokens": context_bundle.total_tokens,
                },
            )
        except Exception as e:
            logger.warning("[StaffService] Failed to store step description: %s", e)

        result.setdefault("metadata", {})
        result["metadata"]["context_bundle"] = {
            "total_tokens": context_bundle.total_tokens,
            "token_budget": context_bundle.token_budget,
            "items_count": len(context_bundle.items),
        }

        return result


