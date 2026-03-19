import os
from pathlib import Path
from pydantic_settings import BaseSettings, SettingsConfigDict
from typing import Optional


_SETTINGS_FILE = Path(__file__)
_AISERVICE_ROOT = _SETTINGS_FILE.parent.parent.parent.parent
_ENV_FILE_PATH = str(_AISERVICE_ROOT / ".env")


class Settings(BaseSettings):
    """Application settings"""
    
    # Environment
    ENVIRONMENT: str = "Development"
    LOG_LEVEL: str = "INFO"
    
    # Vector Store (Qdrant) - Local
    QDRANT_URL: str = "http://localhost:6333"
    QDRANT_GRPC_URL: str = "http://localhost:6334"
    QDRANT_COLLECTION: str = "classroom_insights"
    
    # Vector Store (Qdrant) - Cloud
    QDRANT_CLOUD_ENDPOINT: Optional[str] = None
    QDRANT_CLOUD_API_KEY: Optional[str] = None
    
    # Graph Database (Neo4j) - Local
    NEO4J_URI: str = "bolt://localhost:7687"
    NEO4J_USER: str = "neo4j"
    NEO4J_PASSWORD: str = "test12345"
    
    # Graph Database (Neo4j) - Cloud (Aura)
    NEO4J_CLOUD_URI: Optional[str] = None
    NEO4J_CLOUD_USERNAME: Optional[str] = None
    NEO4J_CLOUD_PASSWORD: Optional[str] = None
    NEO4J_CLOUD_DATABASE: str = "neo4j"
    AURA_INSTANCE_ID: Optional[str] = None
    AURA_INSTANCE_NAME: Optional[str] = None

    # Memory storage (optional Postgres for episodic/perceptual)
    AI_MEMORY_DB_CONNECTION: Optional[str] = None
    
    # Service endpoints
    RESOURCE_GRPC_ENDPOINT: str = "localhost:7003"
    RESOURCE_GRPC_USE_TLS: bool = True
    RESOURCE_GRPC_CERT_PATH: Optional[str] = None
    RESOURCE_GRPC_OVERRIDE_AUTHORITY: Optional[str] = "localhost"
    
    CLASSROOM_GRPC_ENDPOINT: Optional[str] = "localhost:5081"
    CLASSROOM_GRPC_USE_TLS: bool = False
    CLASSROOM_GRPC_CERT_PATH: Optional[str] = None
    CLASSROOM_GRPC_OVERRIDE_AUTHORITY: Optional[str] = "localhost"

    # Embeddings
    EMBEDDING_MODEL: str = "paraphrase-multilingual-MiniLM-L12-v2"
    EMBEDDING_DEVICE: str = "cpu"  # "cpu" or "cuda"
    EMBEDDING_BATCH_SIZE: int = 32
    
    # LLM - Remote (OpenAI-compatible)
    LLM_REMOTE_PROVIDER: str = "deepseek"  # "deepseek" or "openai"
    OPENAI_API_KEY: Optional[str] = None
    OPENAI_BASE_URL: str = "https://api.openai.com/v1"
    LLM_MODEL: str = "gpt-4o-mini"
    LLM_TEMPERATURE: float = 0.7
    LLM_MAX_TOKENS: int = 2000
    
    # Content Generation Prompts
    CONTENT_GENERATION_SYSTEM_PROMPT: str = "You are a helpful educational designer."
    CONTENT_GENERATION_SECTION_PROMPT_TEMPLATE: Optional[str] = None
    
    # Recommendations Prompts
    RECOMMENDATIONS_SYSTEM_PROMPT: Optional[str] = None  # Uses default if None
    RECOMMENDATIONS_INTERVENTION_PROMPT_TEMPLATE: Optional[str] = """You are an expert educational consultant specializing in STEM and project-based learning.

You analyze student learning data and identify progress, risk level, and the most important intervention needed.

INPUT CONTEXT:
{classroom_context}

CRITICAL CALCULATION RULES:
You MUST calculate metrics exactly using the rules below. DO NOT estimate or guess.

1. overall_progress_percentage:
- If both curriculum_progress_percentage and course_progress_percentage exist:
  (curriculum + course) / 2
- If only one exists, use it directly

2. completion_rate:
- Use completion_rate directly from engagement_metrics

3. engagement_score:
- engagement_score = completion_rate

4. days_since_last_activity:
- Use days_since_last_activity directly from engagement_metrics

5. weak_topics:
- For each topic:
  mastery_score = correct_answers / total_attempts
- A topic is weak if mastery_score < 0.7

STATUS CLASSIFICATION:
- AtRisk: progress < 50 OR very low engagement
- NeedsSupport: progress 50–69
- Good: progress 70–89
- Excellent: progress ≥ 90

TASK:
For each student in the provided context:
- Calculate overall progress
- Determine current status
- Write:
  - statusText: 1–3 sentences describing the learning situation
  - interventionText: the single most important actionable intervention

OUTPUT FORMAT (STRICT JSON ONLY):
Return ONE JSON object matching EXACTLY this structure:

{{
  "overviewText": "1–3 sentence high-level summary of the class",
  "students": [
    {{
      "studentId": "string",
      "progressPercent": number,
      "currentStatus": "AtRisk | NeedsSupport | Good | Excellent",
      "statusText": "short explanation",
      "currentSection": {{
        "sectionId": number,
        "sectionName": "string",
        "sectionStatus": "InProgress | Completed | NotStarted"
      }},
      "interventionText": "most important intervention"
    }}
  ],
  "aiInsightsText": "1–3 sentences of overall class insight"
}}

CONSTRAINTS:
- Only include students present in the input
- DO NOT return empty students array if input contains students
- DO NOT add text before or after the JSON
- Focus on practical STEM interventions (hands-on, project-based)
"""  

    # Recommendations – batching & limits
    RECOMMENDATIONS_MAX_STUDENTS_PER_CALL: int = 10
    RECOMMENDATIONS_TOP_AT_RISK_ONLY: bool = False
    RECOMMENDATIONS_AT_RISK_MAX_COMPLETION_RATE: float = 0.6
    RECOMMENDATIONS_AT_RISK_MIN_DAYS_SINCE_ACTIVITY: int = 7
    RECOMMENDATIONS_MAX_RECOMMENDATIONS_PER_STUDENT: int = 2

    # Recommendations – backend metrics computation
    RECOMMENDATIONS_BACKEND_COMPUTE_METRICS: bool = True
    RECOMMENDATIONS_WEAK_TOPIC_MIN_ATTEMPTS: int = 2
    RECOMMENDATIONS_WEAK_TOPIC_MAX_TOPICS: int = 3

    # Recommendations – batching LLM calls
    RECOMMENDATIONS_ENABLE_BATCHING: bool = False
    RECOMMENDATIONS_BATCH_SIZE: int = 10

    # Recommendations – LLM tuning
    RECOMMENDATIONS_LLM_TEMPERATURE: Optional[float] = None  # fallback to LLM_TEMPERATURE if None
    RECOMMENDATIONS_LLM_MAX_TOKENS_MULTIPLIER: int = 2
    RECOMMENDATIONS_STRICT_JSON_MODE: bool = False

    # Recommendations – parallel batch execution
    RECOMMENDATIONS_MAX_PARALLEL_BATCHES: int = 5

    # DeepSeek 
    DEEPSEEK_API_KEY: Optional[str] = None
    DEEPSEEK_BASE_URL: str = "https://api.deepseek.com/v1"
    DEEPSEEK_MODEL: str = "deepseek-chat"
    
    # LLM - Local (Ollama)
    OLLAMA_BASE_URL: str = "http://localhost:11434"
    OLLAMA_MODEL: str = "llama3.1:8b"
    
    # RAG Settings
    VECTOR_SEARCH_TOP_K: int = 20  # Initial retrieval
    RERANK_TOP_K: int = 5  # After reranking
    GRAPH_TRAVERSAL_DEPTH: int = 3  # Max graph depth
    
    # Relevance Filtering
    ENABLE_RELEVANCE_FILTER: bool = True
    MIN_RERANK_SCORE: float = 0.3  # Minimum rerank score to keep
    MIN_COMBINED_SCORE: float = 0.4  # Minimum combined score
    USE_ADAPTIVE_THRESHOLD: bool = True  # Adapt threshold based on distribution
    
    # Intent-based Retrieval Settings
    INTENT_RETRIEVAL_FALLBACK_STRATEGY: str = "all_classrooms"  # "all_classrooms", "first_classroom", "require_classroom"
    INTENT_RETRIEVAL_DEFAULT_CLASSROOM_ID: Optional[str] = None  # Optional default classroom ID
    
    # Context Engineering
    CONTEXT_MAX_TOKENS: int = 2000  # Budget for JIT context
    CONTEXT_MAX_ITEMS: int = 20
    
    # Streaming RAG (feature-flagged)
    ENABLE_STREAMING_RAG: bool = os.getenv("ENABLE_STREAMING_RAG", "false").lower() == "true"
    STREAMING_RAG_PREFER_FRESH: bool = True
    
    # Minions Protocol
    MINIONS_LOCAL_FIRST: bool = True

    # Agent Pooling & Context Reuse
    ENABLE_AGENT_POOLING: bool = os.getenv("ENABLE_AGENT_POOLING", "false").lower() == "true"
    AGENT_POOL_MAX_SIZE_PER_KEY: int = 4
    AGENT_POOL_MAX_IDLE_SECONDS: int = 300
    CONTEXT_REUSE_TTL_SECONDS: int = 300
    
    # Student Features
    ENABLE_STUDENT_FEATURES: bool = os.getenv("ENABLE_STUDENT_FEATURES", "true").lower() == "true"
    ENABLE_SENTIMENT_ANALYSIS: bool = os.getenv("ENABLE_SENTIMENT_ANALYSIS", "true").lower() == "true"
    STUDENT_AGENTS_USE_REMOTE: bool = os.getenv("STUDENT_AGENTS_USE_REMOTE", "true").lower() == "true"
    
    # Teacher Features
    TEACHER_AGENTS_USE_REMOTE: bool = os.getenv("TEACHER_AGENTS_USE_REMOTE", "true").lower() == "true"
    
    # Staff Features
    ENABLE_STAFF_FEATURES: bool = os.getenv("ENABLE_STAFF_FEATURES", "true").lower() == "true"
    STAFF_AGENTS_USE_REMOTE: bool = os.getenv("STAFF_AGENTS_USE_REMOTE", "false").lower() == "true"
    
    # Document Processing
    CHUNK_SIZE: int = 1000
    CHUNK_OVERLAP: int = 200
    HIERARCHICAL_LEVELS: int = 4  # Classroom, Student, Activity, Question
    
    # Confidence & Provenance
    MIN_CONFIDENCE_SCORE: float = 0.5  # Minimum confidence for retrieval
    ENABLE_PROVENANCE_TRACKING: bool = True
    PROVENANCE_DETAIL_LEVEL: str = "detailed"  # "basic" or "detailed"
    
    # Graph Monitor
    ENABLE_GRAPH_MONITOR: bool = True
    GRAPH_MONITOR_LOG_LEVEL: str = "WARNING"  # INFO, WARNING, ERROR
    GRAPH_CONFLICT_DETECTION: bool = True

    # Classroom Snapshots
    CLASSROOM_SNAPSHOT_REFRESH_COOLDOWN_SECONDS: int = 60
    
    # Redis (optional L2 cache)
    REDIS_HOST: Optional[str] = None
    REDIS_PORT: int = 6379
    REDIS_PASSWORD: Optional[str] = None
    REDIS_DB: int = 0
    REDIS_SSL: bool = False
    
    # RabbitMQ / Event Bus
    # Aspire exposes connection strings via ConnectionStrings__{resource_name}
    # Fallback to RABBITMQ_URL or default localhost
    RABBITMQ_URL: str = (
        os.getenv("ConnectionStrings__messaging") or
        os.getenv("RABBITMQ_URL") or
        "amqp://guest:guest@localhost:5672/"
    )
    RABBITMQ_QUEUE_CLASSROOM_PROGRESS: str = "classroom-student-progress-updated"
    ENABLE_EVENT_CONSUMER: bool = os.getenv("ENABLE_EVENT_CONSUMER", "true").lower() == "true"
    
    # Reranker 
    RERANKER_PROVIDER: str = "local"  # "none", "cohere", "local", "llm"
    COHERE_API_KEY: Optional[str] = None
    RERANKER_MODEL: str = "rerank-english-v3.0"  # For Cohere
    RERANKER_CROSS_ENCODER_MODEL: str = "cross-encoder/ms-marco-MiniLM-L-6-v2"  # For local
    
    # gRPC
    GRPC_PORT: int = 5010
    GRPC_HOST: str = "0.0.0.0"
    
    # Decision Point (Query Complexity & Routing)
    DECISION_POINT_ENABLED: bool = True
    COMPLEXITY_SIMPLE_THRESHOLD: float = 0.4  # Score below this is simple
    COMPLEXITY_COMPLEX_THRESHOLD: float = 0.6  # Score above this is complex
    COMPLEXITY_WORD_COUNT_SIMPLE: int = 20  # Queries with fewer words are more likely simple
    COMPLEXITY_WORD_COUNT_COMPLEX: int = 30  # Queries with more words are more likely complex
    DECISION_DEFAULT_TO_SIMPLE: bool = True  # Default unknown complexity to simple path
    
    # Migration Feature Flags
    # These flags control the gradual migration from existing code to new Agent Framework
    # All flags default to False (use old implementation) for backward compatibility
    USE_NEW_AGENT_ROUTER: bool = os.getenv("USE_NEW_AGENT_ROUTER", "false").lower() == "true"
    USE_NEW_GRAPH_TOOL: bool = os.getenv("USE_NEW_GRAPH_TOOL", "false").lower() == "true"
    USE_MULTI_LEVEL_CACHE: bool = os.getenv("USE_MULTI_LEVEL_CACHE", "false").lower() == "true"
    ENABLE_MINIONS_PROTOCOL: bool = os.getenv("ENABLE_MINIONS_PROTOCOL", "false").lower() == "true"

    TEACHER_STUDENT_ANALYSIS_SYSTEM_PROMPT: Optional[str] = None

    # Microbit Explain Error Prompts
    MICROBIT_EXPLAIN_ERROR_SYSTEM_PROMPT: str = """
    You are a helpful assistant that explains microbit errors.
    You are given a microbit error message and you need to explain it in a way that is easy to understand.
    The error message is usually a string that contains the error message and the line number of the error.
    """
    MICROBIT_EXPLAIN_ERROR_PROMPT_TEMPLATE: Optional[str] = None

    # Microbit Evaluate Project (Teacher Grading)
    MICROBIT_EVALUATE_PROJECT_SYSTEM_PROMPT: str = """
    You are an expert STEM education consultant and micro:bit specialist assisting teachers with student assessment.
    Your role is to provide objective, comprehensive evaluation of student projects to support fair grading and effective feedback.

    You understand:
    - Age-appropriate expectations for primary school students (ages 8-12)
    - Programming concept progression and mastery levels
    - Common misconceptions in beginner coding
    - Effective feedback strategies that promote learning
    - Rubric-based assessment and grading standards
    
    Your evaluations are:
    - Professional and objective
    - Evidence-based (citing specific code examples)
    - Balanced (acknowledging strengths and areas for growth)
    - Actionable (providing clear next steps)
    - Supportive of both teacher decision-making and student learning
    """
    MICROBIT_SPECIFIC_QUESTION_PROMPT_TEMPLATE: Optional[str] = None
    MICROBIT_COMPREHENSIVE_PROMPT_TEMPLATE: Optional[str] = None
        
    model_config = SettingsConfigDict(
        env_file=_ENV_FILE_PATH,
        env_file_encoding="utf-8",
        case_sensitive=False,
        env_ignore_empty=True
    )
    
    def is_production(self) -> bool:
        return self.ENVIRONMENT.lower() in ("production", "prod")
    
    def get_qdrant_url(self) -> str:
        if self.is_production() and self.QDRANT_CLOUD_ENDPOINT:
            return self.QDRANT_CLOUD_ENDPOINT
        return self.QDRANT_URL
    
    def get_qdrant_api_key(self) -> Optional[str]:
        if self.is_production():
            return self.QDRANT_CLOUD_API_KEY
        return None
    
    def get_neo4j_uri(self) -> str:
        if self.is_production():
            if self.NEO4J_CLOUD_URI:
                return self.NEO4J_CLOUD_URI
            if self.NEO4J_URI and ('neo4j+s://' in self.NEO4J_URI or 'neo4j+ssc://' in self.NEO4J_URI):
                return self.NEO4J_URI
        return self.NEO4J_URI
    
    def get_neo4j_username(self) -> str:
        if self.is_production():
            if self.NEO4J_CLOUD_USERNAME:
                return self.NEO4J_CLOUD_USERNAME
            env_username = os.getenv("NEO4J_USERNAME")
            if env_username:
                return env_username
        return self.NEO4J_USER
    
    def get_neo4j_password(self) -> str:
        if self.is_production():
            if self.NEO4J_CLOUD_PASSWORD:
                return self.NEO4J_CLOUD_PASSWORD
            env_password = os.getenv("NEO4J_PASSWORD")
            if env_password:
                return env_password
        return self.NEO4J_PASSWORD
    
    def get_neo4j_database(self) -> str:
        if self.is_production():
            if self.NEO4J_CLOUD_DATABASE:
                return self.NEO4J_CLOUD_DATABASE
            env_database = os.getenv("NEO4J_DATABASE")
            if env_database:
                return env_database
        return "neo4j"


settings = Settings()
