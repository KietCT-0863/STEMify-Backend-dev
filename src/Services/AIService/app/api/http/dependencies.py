"""
Shared dependencies for HTTP API
Dependency injection for services
"""

from functools import lru_cache
from typing import Optional
import logging

import redis.asyncio as redis

from app.core.llm.client import LLMClient
from app.core.rag.ingestion_pipeline import IngestionPipeline
from app.core.rag.ingestion_service import IngestionService
from app.core.rag.document_processor import DocumentProcessor
from app.core.embedding.pipeline import EmbeddingPipeline, get_embedding_pipeline
from app.core.graph.builder import GraphBuilder
from app.core.graph.client import GraphClient
from app.core.graph.monitor import GraphMonitor
from app.core.vector_store.client import VectorStoreClient
from app.features.content_generation.service import ContentGenerationService
from app.features.recommendations.service import RecommendationsService
from app.features.microbit_explain_error.service import MicrobitExplainErrorService
from app.features.student.service import StudentService
from app.features.teacher.service import TeacherService
from app.features.staff.service import StaffService
from app.infrastructure.config.settings import settings
from app.infrastructure.data.grpc_lesson_repository import GrpcLessonRepository
from app.infrastructure.data.mock_lesson_repository import MockLessonRepository
from app.infrastructure.data.grpc_classroom_repository import GrpcClassroomRepository
from app.infrastructure.data.mock_classroom_repository import MockClassroomRepository
from app.infrastructure.data.grpc_assignment_attempt_client import GrpcAssignmentAttemptClient
from app.core.data.lesson_repository import LessonRepository
from app.core.data.classroom_repository import ClassroomRepository
from app.core.rag.hybrid_retriever import HybridRetriever
from app.core.rag.vector_retriever import VectorRetriever
from app.core.graph.retriever import GraphRetriever
from app.core.memory.memory_manager import MemoryManager
from app.core.agent.pool.manager import AgentPoolManager
from app.core.context.builder import JITContextBuilder
from app.core.context.gather import ContextGatherer
from app.core.context.selector import ContextSelector
from app.core.context.structurer import ContextStructurer
from app.core.context.compressor import ContextCompressor
from app.core.cache.agent_cache import AgentResponseCache
from app.core.cache.multi_level_cache import MultiLevelCache
from app.core.tools.sentiment_analysis_tool import SentimentAnalysisTool
from app.core.rag.streaming_hooks import StreamingRAGRouter
from app.features.microbit_analyze_project.service import MicrobitAnalyzeProjectService
from app.core.snapshot.classroom_snapshot_store import (
    ClassroomSnapshotStore,
    ClassroomSnapshotUpdater,
)
from app.core.snapshot.events import ClassroomSnapshotEventHandler

logger = logging.getLogger(__name__)


@lru_cache(maxsize=1)
def get_microbit_analyze_project_service() -> MicrobitAnalyzeProjectService:
    """Dependency for microbit analyze project service."""
    return MicrobitAnalyzeProjectService(
        llm_client=get_llm_client(),
    )

@lru_cache(maxsize=1)
def get_lesson_repository() -> LessonRepository:
    """Provide lesson repository implementation."""
    if settings.RESOURCE_GRPC_ENDPOINT:
        return GrpcLessonRepository(
            endpoint=settings.RESOURCE_GRPC_ENDPOINT,
            fallback=MockLessonRepository(),
            use_tls=settings.RESOURCE_GRPC_USE_TLS,
            cert_path=settings.RESOURCE_GRPC_CERT_PATH,
            authority_override=settings.RESOURCE_GRPC_OVERRIDE_AUTHORITY,
        )
    return MockLessonRepository()


@lru_cache(maxsize=1)
def get_llm_client() -> LLMClient:
    return LLMClient()


@lru_cache(maxsize=1)
def get_content_generation_service() -> ContentGenerationService:
    """Dependency for content generation service."""
    return ContentGenerationService(
        lesson_repository=get_lesson_repository(),
        llm_client=get_llm_client(),
    )


@lru_cache(maxsize=1)
def get_ingestion_pipeline() -> Optional[IngestionPipeline]:
    """
    Provide ingestion pipeline for RAG-based analysis.
    Returns None if RAG features are disabled or unavailable.
    """
    try:
        document_processor = DocumentProcessor()
        embedding_pipeline = get_embedding_pipeline()
        graph_client = GraphClient()
        monitor = GraphMonitor(
            log_level=settings.GRAPH_MONITOR_LOG_LEVEL,
            enable_detection=settings.GRAPH_CONFLICT_DETECTION,
        )
        graph_builder = GraphBuilder(graph_client, monitor)
        vector_store = VectorStoreClient()
        
        return IngestionPipeline(
            document_processor=document_processor,
            embedding_pipeline=embedding_pipeline,
            graph_builder=graph_builder,
            vector_store=vector_store,
            graph_client=graph_client,
        )
    except Exception as e:
        # RAG is optional
        import logging
        logger = logging.getLogger(__name__)
        logger.warning(f"Failed to initialize ingestion pipeline: {e}. Continuing without RAG.")
        return None


@lru_cache(maxsize=1)
def get_classroom_repository() -> ClassroomRepository:
    """Provide classroom repository implementation."""
    if settings.CLASSROOM_GRPC_ENDPOINT:
        # GrpcClassroomRepository uses fallback internally, so we pass None
        # The repository will use its own fallback mechanism
        return GrpcClassroomRepository(
            endpoint=settings.CLASSROOM_GRPC_ENDPOINT,
            fallback=None,  # Will use internal fallback to mock data
            use_tls=settings.CLASSROOM_GRPC_USE_TLS,
            cert_path=settings.CLASSROOM_GRPC_CERT_PATH,
            authority_override=settings.CLASSROOM_GRPC_OVERRIDE_AUTHORITY,
        )
    return MockClassroomRepository()


@lru_cache(maxsize=1)
def get_recommendations_service() -> RecommendationsService:
    """Dependency for recommendations service."""
    ingestion_pipeline = get_ingestion_pipeline()
    classroom_repository = get_classroom_repository()
    return RecommendationsService(
        llm_client=get_llm_client(),
        classroom_repository=classroom_repository,
        ingestion_pipeline=ingestion_pipeline,
    )

@lru_cache(maxsize=1)
def get_microbit_explain_error_service() -> MicrobitExplainErrorService:
    """Dependency for microbit explain error service."""
    return MicrobitExplainErrorService(
        llm_client=get_llm_client(),
    )


@lru_cache(maxsize=1)
def get_hybrid_retriever() -> HybridRetriever:
    """Dependency for hybrid retriever (vector + graph)."""
    from app.core.vector_store.client import VectorStoreClient
    
    vector_store = VectorStoreClient()
    embedding_pipeline = get_embedding_pipeline()
    vector_retriever = VectorRetriever(
        vector_store=vector_store,
        embedding_pipeline=embedding_pipeline
    )
    
    graph_client = GraphClient()
    graph_retriever = GraphRetriever(graph_client=graph_client)

    streaming_router = None
    if settings.ENABLE_STREAMING_RAG:
        streaming_router = StreamingRAGRouter(
            freshness_feature_flag=settings.STREAMING_RAG_PREFER_FRESH
        )

    return HybridRetriever(
        vector_retriever=vector_retriever,
        graph_retriever=graph_retriever,
        streaming_router=streaming_router,
    )


@lru_cache(maxsize=1)
def get_memory_manager() -> MemoryManager:
    """Dependency for memory manager."""
    from app.core.memory.types.working import WorkingMemory
    from app.core.memory.types.episodic import EpisodicMemory
    from app.core.memory.types.semantic import SemanticMemory
    from app.core.memory.types.perceptual import PerceptualMemory
    from app.core.vector_store.client import VectorStoreClient
    from app.core.graph.client import GraphClient
    
    vector_store = VectorStoreClient()
    graph_client = GraphClient()
    
    episodic_memory = EpisodicMemory(
        vector_store=vector_store,
        postgres_dsn=settings.AI_MEMORY_DB_CONNECTION
    )
    semantic_memory = SemanticMemory(vector_store=vector_store, graph_client=graph_client)
    perceptual_memory = PerceptualMemory(
        vector_store=vector_store,
        postgres_dsn=settings.AI_MEMORY_DB_CONNECTION
    )
    
    return MemoryManager(
        working_memory=WorkingMemory(),
        episodic_memory=episodic_memory,
        semantic_memory=semantic_memory,
        perceptual_memory=perceptual_memory
    )


@lru_cache(maxsize=1)
def get_agent_pool_manager() -> AgentPoolManager:
    """Dependency for agent pool manager."""
    return AgentPoolManager(
        max_pool_size_per_key=settings.AGENT_POOL_MAX_SIZE_PER_KEY,
        max_idle_seconds=settings.AGENT_POOL_MAX_IDLE_SECONDS
    )


@lru_cache(maxsize=1)
def get_context_builder() -> JITContextBuilder:
    """Dependency for JIT context builder."""
    memory_manager = get_memory_manager()
    hybrid_retriever = get_hybrid_retriever()
    
    gatherer = ContextGatherer(
        memory_manager=memory_manager,
        hybrid_retriever=hybrid_retriever
    )
    selector = ContextSelector(max_items=settings.CONTEXT_MAX_ITEMS)
    structurer = ContextStructurer()
    compressor = ContextCompressor()
    
    return JITContextBuilder(
        gatherer=gatherer,
        selector=selector,
        structurer=structurer,
        compressor=compressor,
        token_budget=settings.CONTEXT_MAX_TOKENS
    )


@lru_cache(maxsize=1)
def get_agent_cache() -> Optional[AgentResponseCache]:
    """Dependency for agent response cache."""
    from app.infrastructure.cache.cache_manager import CacheManager
    
    cache_manager = CacheManager(redis_client=get_redis_client())
    if hasattr(cache_manager, 'multi_level_cache') and cache_manager.multi_level_cache:
        return AgentResponseCache(
            multi_cache=cache_manager.multi_level_cache,
            embedding_service=None,  
            similarity_threshold=0.85,
            default_ttl=3600
        )
    return None


@lru_cache(maxsize=1)
def get_redis_client():
    if not settings.REDIS_HOST:
        return None
    client = redis.Redis(
        host=settings.REDIS_HOST,
        port=settings.REDIS_PORT,
        password=settings.REDIS_PASSWORD,
        db=settings.REDIS_DB,
        ssl=settings.REDIS_SSL,
        decode_responses=False,
    )
    logger.info(
        "[Dependencies] Redis client initialized",
        extra={
            "host": settings.REDIS_HOST,
            "port": settings.REDIS_PORT,
            "ssl": settings.REDIS_SSL,
        },
    )
    return client


@lru_cache(maxsize=1)
def get_sentiment_tool() -> Optional[SentimentAnalysisTool]:
    """Dependency for sentiment analysis tool."""
    if getattr(settings, 'ENABLE_SENTIMENT_ANALYSIS', False):
        return SentimentAnalysisTool()
    return None


@lru_cache(maxsize=1)
def get_classroom_snapshot_store() -> ClassroomSnapshotStore:
    return ClassroomSnapshotStore(
        full_refresh_cooldown_seconds=settings.CLASSROOM_SNAPSHOT_REFRESH_COOLDOWN_SECONDS
    )


@lru_cache(maxsize=1)
def get_classroom_snapshot_updater() -> ClassroomSnapshotUpdater:
    return ClassroomSnapshotUpdater(
        classroom_repository=get_classroom_repository(),
        snapshot_store=get_classroom_snapshot_store(),
    )


@lru_cache(maxsize=1)
def get_ingestion_service() -> Optional[IngestionService]:
    """
    Provide ingestion service for RAG indexing with debouncing.
    Returns None if RAG features are disabled or unavailable.
    """
    try:
        ingestion_pipeline = get_ingestion_pipeline()
        if not ingestion_pipeline:
            return None
        
        classroom_repository = get_classroom_repository()
        
        return IngestionService(
            ingestion_pipeline=ingestion_pipeline,
            classroom_repository=classroom_repository,
            debounce_seconds=getattr(settings, 'RAG_INGESTION_DEBOUNCE_SECONDS', 300),
            ingestion_ttl_hours=getattr(settings, 'RAG_INGESTION_TTL_HOURS', 24),
        )
    except Exception as e:
        logger = logging.getLogger(__name__)
        logger.warning(f"Failed to initialize ingestion service: {e}. Continuing without RAG ingestion.")
        return None


@lru_cache(maxsize=1)
def get_classroom_snapshot_event_handler() -> ClassroomSnapshotEventHandler:
    ingestion_service = get_ingestion_service()
    return ClassroomSnapshotEventHandler(
        snapshot_store=get_classroom_snapshot_store(),
        snapshot_updater=get_classroom_snapshot_updater(),
        ingestion_service=ingestion_service,
    )


@lru_cache(maxsize=1)
def get_student_service() -> StudentService:
    """Dependency for student service."""
    return StudentService(
        llm=get_llm_client(),
        context_builder=get_context_builder(),
        memory_manager=get_memory_manager(),
        agent_pool_manager=get_agent_pool_manager(),
        classroom_repository=get_classroom_repository(),
        graph_client=GraphClient(),
        hybrid_retriever=get_hybrid_retriever(),
        agent_cache=get_agent_cache(),
        sentiment_tool=get_sentiment_tool()
    )


@lru_cache(maxsize=1)
def get_teacher_service() -> TeacherService:
    from app.core.reasoning.orchestrator import GraphReasoningOrchestrator
    from app.core.reasoning.tool_implementations import (
        GraphToolImpl,
        VectorToolImpl,
        RerankToolImpl,
        MathToolImpl,
        ClockToolImpl,
    )
    from app.core.vector_store.providers.qdrant_provider import QdrantProvider
    from app.core.embedding.pipeline import get_embedding_pipeline

    graph_client = GraphClient()
    graph_tool = GraphToolImpl(graph_client=graph_client)

    qdrant_provider = QdrantProvider()
    embedding_pipeline = get_embedding_pipeline()
    vector_tool = VectorToolImpl(qdrant_provider=qdrant_provider, embedding_pipeline=embedding_pipeline)

    rerank_tool = RerankToolImpl()
    math_tool = MathToolImpl()
    clock_tool = ClockToolImpl()

    reasoning_orchestrator = GraphReasoningOrchestrator(
        graph_tool=graph_tool,
        vector_tool=vector_tool,
        rerank_tool=rerank_tool,
        math_tool=math_tool,
        clock_tool=clock_tool,
        llm_client=get_llm_client(),
    )

    # Create assignment attempt client if classroom endpoint is available
    assignment_attempt_client = None
    if settings.CLASSROOM_GRPC_ENDPOINT:
        try:
            assignment_attempt_client = GrpcAssignmentAttemptClient(
                endpoint=settings.CLASSROOM_GRPC_ENDPOINT,
                use_tls=settings.CLASSROOM_GRPC_USE_TLS,
                cert_path=settings.CLASSROOM_GRPC_CERT_PATH,
                authority_override=settings.CLASSROOM_GRPC_OVERRIDE_AUTHORITY,
            )
        except ImportError as e:
            logger.warning(
                "AssignmentAttempt proto files not found. Auto-grading will be unavailable.",
                extra={"error": str(e)}
            )
        except Exception as e:
            logger.warning(
                "Failed to create AssignmentAttemptClient",
                extra={"error": str(e)}
            )

    from app.features.teacher.direct_grading_pipeline import DirectGradingPipeline
    direct_grading_pipeline = DirectGradingPipeline(
        llm=get_llm_client(),
        memory_manager=get_memory_manager(),
    )

    return TeacherService(
        llm=get_llm_client(),
        context_builder=get_context_builder(),
        memory_manager=get_memory_manager(),
        agent_pool_manager=get_agent_pool_manager(),
        classroom_repository=get_classroom_repository(),
        lesson_repository=get_lesson_repository(),
        graph_client=graph_client,
        graph_reasoning_orchestrator=reasoning_orchestrator,
        assignment_attempt_client=assignment_attempt_client,
        sentiment_tool=get_sentiment_tool(),
        classroom_snapshot_store=get_classroom_snapshot_store(),
        classroom_snapshot_updater=get_classroom_snapshot_updater(),
        direct_grading_pipeline=direct_grading_pipeline,
        ingestion_service=get_ingestion_service(),
    )


@lru_cache(maxsize=1)
def get_staff_service() -> StaffService:
    """Dependency for staff service."""
    return StaffService(
        llm=get_llm_client(),
        context_builder=get_context_builder(),
        memory_manager=get_memory_manager(),
        agent_pool_manager=get_agent_pool_manager(),
        hybrid_retriever=get_hybrid_retriever(),
        vision_llm=None,  # Can be extended with vision-specific LLM if needed
    )