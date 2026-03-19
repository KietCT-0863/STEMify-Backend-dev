

from typing import Dict, Any, List, Optional
import aiosqlite
import logging
import json
import uuid

from psycopg.rows import dict_row

from app.core.vector_store.client import VectorStoreClient
from app.core.db.postgres import get_pool
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class EpisodicMemory:
    """
    Episodic Memory (Layer 2)
    
    Stores events and experiences.
    - SQLite: Metadata storage
    - Qdrant: Vector storage for semantic search
    """
    
    def __init__(
        self,
        sqlite_path: str = "./memory/episodic.db",
        vector_store: Optional[VectorStoreClient] = None,
        collection_name: str = "episodic_memory",
        postgres_dsn: Optional[str] = None
    ):
        """
        Initialize episodic memory
        
        Args:
            vector_store: Vector store client for embeddings
            collection_name: Qdrant collection name
        """
        self.sqlite_path = sqlite_path
        self.vector_store = vector_store
        self.collection_name = collection_name
        self.postgres_dsn = postgres_dsn or settings.AI_MEMORY_DB_CONNECTION
        self.use_postgres = bool(self.postgres_dsn)
        self._initialized = False
        self._pg_pool = None
    
    async def _ensure_initialized(self):
        """Ensure database and collection are initialized"""
        if self._initialized:
            return

        if self.use_postgres:
            # Initialize Postgres schema
            try:
                self._pg_pool = await get_pool(self.postgres_dsn)
                async with self._pg_pool.connection() as conn:
                    async with conn.cursor() as cur:
                        await cur.execute("""
                            CREATE TABLE IF NOT EXISTS episodic_memories (
                                memory_id TEXT PRIMARY KEY,
                                content TEXT NOT NULL,
                                metadata JSONB NOT NULL,
                                importance DOUBLE PRECISION DEFAULT 0.5,
                                user_id TEXT,
                                created_at TIMESTAMPTZ DEFAULT NOW(),
                                updated_at TIMESTAMPTZ DEFAULT NOW()
                            )
                        """)
                        await cur.execute("CREATE INDEX IF NOT EXISTS idx_ep_user ON episodic_memories(user_id)")
                        await cur.execute("CREATE INDEX IF NOT EXISTS idx_ep_created ON episodic_memories(created_at)")
            except Exception as e:
                logger.error(f"[EpisodicMemory] Failed to init Postgres: {e}")
                raise
        else:
            # Initialize SQLite (fallback)
            async with aiosqlite.connect(self.sqlite_path) as db:
                await db.execute("""
                    CREATE TABLE IF NOT EXISTS episodic_memories (
                        memory_id TEXT PRIMARY KEY,
                        content TEXT NOT NULL,
                        metadata TEXT NOT NULL,
                        importance REAL DEFAULT 0.5,
                        user_id TEXT,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )
                """)
                await db.execute("""
                    CREATE INDEX IF NOT EXISTS idx_user_id ON episodic_memories(user_id)
                """)
                await db.execute("""
                    CREATE INDEX IF NOT EXISTS idx_created_at ON episodic_memories(created_at)
                """)
                await db.commit()

        # Initialize Qdrant collection (if vector store available)
        if self.vector_store:
            try:
                await self.vector_store.ensure_collection(vector_size=384)  # Default embedding size
            except Exception as e:
                logger.warning(f"[EpisodicMemory] Failed to ensure collection: {e}")

        self._initialized = True
    
    async def add(self, content: str, metadata: Dict[str, Any]) -> str:
        """
        Add episodic memory
        
        Args:
            content: Memory content
            metadata: Memory metadata
        
        Returns:
            Memory ID
        """
        await self._ensure_initialized()
        
        memory_id = str(uuid.uuid4())
        importance = metadata.get("importance", 0.5)
        user_id = metadata.get("user_id")
        
        # Store metadata
        if self.use_postgres and self._pg_pool:
            async with self._pg_pool.connection() as conn:
                async with conn.cursor() as cur:
                    await cur.execute(
                        """
                        INSERT INTO episodic_memories (memory_id, content, metadata, importance, user_id)
                        VALUES (%s, %s, %s::jsonb, %s, %s)
                        """,
                        (
                            memory_id,
                            content,
                            json.dumps(metadata),
                            importance,
                            user_id
                        )
                    )
        else:
            async with aiosqlite.connect(self.sqlite_path) as db:
                await db.execute("""
                    INSERT INTO episodic_memories (memory_id, content, metadata, importance, user_id)
                    VALUES (?, ?, ?, ?, ?)
                """, (
                    memory_id,
                    content,
                    json.dumps(metadata),
                    importance,
                    user_id
                ))
                await db.commit()
        
        # Store embedding in Qdrant (if available)
        if self.vector_store:
            try:
                from app.core.embedding.pipeline import get_embedding_pipeline
                embedding_pipeline = get_embedding_pipeline()
                embedding = embedding_pipeline.encode([content])[0].tolist()
                
                await self.vector_store.upsert(
                    id=memory_id,
                    vector=embedding,
                    payload={
                        "content": content,
                        "memory_type": "episodic",
                        "importance": importance,
                        "user_id": user_id,
                        **metadata
                    }
                )
            except Exception as e:
                logger.warning(f"[EpisodicMemory] Failed to store embedding: {e}")
        
        logger.debug(f"[EpisodicMemory] Added memory: {memory_id}")
        return memory_id
    
    async def search(
        self,
        query: str,
        limit: int = 5,
        user_id: Optional[str] = None,
        min_importance: float = 0.1
    ) -> List[Dict[str, Any]]:
        """
        Search episodic memories
        
        Args:
            query: Search query
            limit: Maximum results
            user_id: Optional user ID filter
            min_importance: Minimum importance threshold
        
        Returns:
            List of matching memories
        """
        await self._ensure_initialized()
        
        # Try vector search first (if available)
        if self.vector_store:
            try:
                from app.core.embedding.pipeline import get_embedding_pipeline
                embedding_pipeline = get_embedding_pipeline()
                query_embedding = embedding_pipeline.encode([query])[0].tolist()
                
                filters = {}
                if user_id:
                    filters["user_id"] = user_id
                filters["memory_type"] = "episodic"
                
                vector_results = await self.vector_store.search(
                    query_vector=query_embedding,
                    top_k=limit * 2,  # Get more for filtering
                    filters=filters
                )
                
                # Filter by importance and format
                results = []
                for result in vector_results:
                    importance = result.get("payload", {}).get("importance", 0.5)
                    if importance >= min_importance:
                        results.append({
                            "memory_id": result["id"],
                            "content": result.get("content", ""),
                            "metadata": result.get("payload", {}),
                            "relevance_score": result.get("score", 0.5),
                            "importance": importance
                        })
                
                # Get full metadata from SQLite
                if results:
                    memory_ids = [r["memory_id"] for r in results]
                    if self.use_postgres and self._pg_pool:
                        async with self._pg_pool.connection() as conn:
                            async with conn.cursor(row_factory=dict_row) as cur:
                                await cur.execute(
                                    "SELECT memory_id, metadata FROM episodic_memories WHERE memory_id = ANY(%s)",
                                    (memory_ids,)
                                )
                                rows = await cur.fetchall()
                            metadata_map = {row["memory_id"]: row["metadata"] for row in rows}
                            for result in results:
                                result["metadata"].update(metadata_map.get(result["memory_id"], {}))
                    else:
                        async with aiosqlite.connect(self.sqlite_path) as db:
                            db.row_factory = aiosqlite.Row
                            placeholders = ",".join("?" * len(memory_ids))
                            cursor = await db.execute(
                                f"SELECT * FROM episodic_memories WHERE memory_id IN ({placeholders})",
                                memory_ids
                            )
                            rows = await cursor.fetchall()
                            
                            # Merge SQLite metadata with vector results
                            metadata_map = {row["memory_id"]: json.loads(row["metadata"]) for row in rows}
                            for result in results:
                                result["metadata"].update(metadata_map.get(result["memory_id"], {}))
                
                return results[:limit]
            except Exception as e:
                logger.warning(f"[EpisodicMemory] Vector search failed: {e}")
        
        # Fallback to SQL text search
        if self.use_postgres and self._pg_pool:
            async with self._pg_pool.connection() as conn:
                params = [f"%{query}%", min_importance]
                user_clause = ""
                if user_id:
                    user_clause = " AND user_id = %s"
                    params.append(user_id)
                params.append(limit)
                query_text = f"""
                    SELECT * FROM episodic_memories
                    WHERE content ILIKE %s AND importance >= %s
                    {user_clause}
                    ORDER BY created_at DESC
                    LIMIT %s
                """
                async with conn.cursor(row_factory=dict_row) as cur:
                    await cur.execute(query_text, params)
                    rows = await cur.fetchall()
                results = []
                for row in rows:
                    results.append({
                        "memory_id": row["memory_id"],
                        "content": row["content"],
                        "metadata": row["metadata"],
                        "relevance_score": 0.5,
                        "importance": row["importance"]
                    })
                return results
        else:
            async with aiosqlite.connect(self.sqlite_path) as db:
                db.row_factory = aiosqlite.Row
                query_sql = """
                    SELECT * FROM episodic_memories
                    WHERE content LIKE ? AND importance >= ?
                """
                params = [f"%{query}%", min_importance]
                
                if user_id:
                    query_sql += " AND user_id = ?"
                    params.append(user_id)
                
                query_sql += " ORDER BY created_at DESC LIMIT ?"
                params.append(limit)
                
                cursor = await db.execute(query_sql, params)
                rows = await cursor.fetchall()
                
                results = []
                for row in rows:
                    results.append({
                        "memory_id": row["memory_id"],
                        "content": row["content"],
                        "metadata": json.loads(row["metadata"]),
                        "relevance_score": 0.5,  # Simple match
                        "importance": row["importance"]
                    })
                
                return results
    
    async def get_important(self, user_id: Optional[str] = None) -> List[Dict[str, Any]]:
        """
        Get important episodic memories
        
        Args:
            user_id: Optional user ID filter
        
        Returns:
            List of important memories
        """
        await self._ensure_initialized()
        
        if self.use_postgres and self._pg_pool:
            async with self._pg_pool.connection() as conn:
                params = []
                user_clause = ""
                if user_id:
                    user_clause = " AND user_id = %s"
                    params.append(user_id)

                query = f"""
                    SELECT * FROM episodic_memories
                    WHERE importance > 0.7
                    {user_clause}
                    ORDER BY importance DESC, created_at DESC
                    LIMIT 50
                """
                async with conn.cursor(row_factory=dict_row) as cur:
                    await cur.execute(query, params)
                    rows = await cur.fetchall()
                results = []
                for row in rows:
                    results.append({
                        "memory_id": row["memory_id"],
                        "content": row["content"],
                        "metadata": row["metadata"],
                        "importance": row["importance"]
                    })
                return results

        async with aiosqlite.connect(self.sqlite_path) as db:
            db.row_factory = aiosqlite.Row
            query = """
                SELECT * FROM episodic_memories
                WHERE importance > 0.7
            """
            params = []
            
            if user_id:
                query += " AND user_id = ?"
                params.append(user_id)
            
            query += " ORDER BY importance DESC, created_at DESC LIMIT 50"
            
            cursor = await db.execute(query, params)
            rows = await cursor.fetchall()
            
            results = []
            for row in rows:
                results.append({
                    "memory_id": row["memory_id"],
                    "content": row["content"],
                    "metadata": json.loads(row["metadata"]),
                    "importance": row["importance"]
                })
            
            return results




