"""
Perceptual Memory
SQLite (metadata) + Qdrant (vectors) for multimodal data
"""

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


class PerceptualMemory:
    """
    Perceptual Memory (Layer 4)
    
    Stores multimodal data (images, 3D models).
    - SQLite: Metadata storage
    - Qdrant: Vector storage for similarity search
    """
    
    def __init__(
        self,
        sqlite_path: str = "./memory/perceptual.db",
        vector_store: Optional[VectorStoreClient] = None,
        collection_name: str = "perceptual_memory",
        postgres_dsn: Optional[str] = None
    ):
        """
        Initialize perceptual memory
        
        Args:
            sqlite_path: Path to SQLite database
            vector_store: Vector store client for embeddings
            collection_name: Qdrant collection name
            postgres_dsn: Optional Postgres connection string for metadata storage
        """
        self.sqlite_path = sqlite_path
        self.vector_store = vector_store
        self.collection_name = collection_name
        self.postgres_dsn = postgres_dsn or settings.AI_MEMORY_DB_CONNECTION
        self.use_postgres = bool(self.postgres_dsn)
        self._pg_pool = None
        self._initialized = False
    
    async def _ensure_initialized(self):
        """Ensure database and collection are initialized"""
        if self._initialized:
            return
        
        if self.use_postgres:
            try:
                self._pg_pool = await get_pool(self.postgres_dsn)
                async with self._pg_pool.connection() as conn:
                    async with conn.cursor() as cur:
                        await cur.execute("""
                            CREATE TABLE IF NOT EXISTS perceptual_memories (
                                memory_id TEXT PRIMARY KEY,
                                content_type TEXT NOT NULL,
                                content_path TEXT,
                                description TEXT,
                                metadata JSONB NOT NULL,
                                importance DOUBLE PRECISION DEFAULT 0.5,
                                user_id TEXT,
                                created_at TIMESTAMPTZ DEFAULT NOW()
                            )
                        """)
                        await cur.execute("CREATE INDEX IF NOT EXISTS idx_per_user ON perceptual_memories(user_id)")
                        await cur.execute("CREATE INDEX IF NOT EXISTS idx_per_type ON perceptual_memories(content_type)")
            except Exception as e:
                logger.error(f"[PerceptualMemory] Failed to init Postgres: {e}")
                raise
        else:
            # Initialize SQLite
            async with aiosqlite.connect(self.sqlite_path) as db:
                await db.execute("""
                    CREATE TABLE IF NOT EXISTS perceptual_memories (
                        memory_id TEXT PRIMARY KEY,
                        content_type TEXT NOT NULL,
                        content_path TEXT,
                        description TEXT,
                        metadata TEXT NOT NULL,
                        importance REAL DEFAULT 0.5,
                        user_id TEXT,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )
                """)
                await db.execute("""
                    CREATE INDEX IF NOT EXISTS idx_user_id ON perceptual_memories(user_id)
                """)
                await db.execute("""
                    CREATE INDEX IF NOT EXISTS idx_content_type ON perceptual_memories(content_type)
                """)
                await db.commit()
        
        # Initialize Qdrant collection (if vector store available)
        if self.vector_store:
            try:
                await self.vector_store.ensure_collection(vector_size=384)
            except Exception as e:
                logger.warning(f"[PerceptualMemory] Failed to ensure collection: {e}")
        
        self._initialized = True
    
    async def add(
        self,
        content: str,
        metadata: Dict[str, Any],
        content_type: str = "image",
        content_path: Optional[str] = None
    ) -> str:
        """
        Add perceptual memory
        
        Args:
            content: Description or text content
            metadata: Memory metadata
            content_type: Type of content (image, 3d_model, etc.)
            content_path: Path to content file (if applicable)
        
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
                        INSERT INTO perceptual_memories 
                        (memory_id, content_type, content_path, description, metadata, importance, user_id)
                        VALUES (%s, %s, %s, %s, %s::jsonb, %s, %s)
                        """,
                        (
                            memory_id,
                            content_type,
                            content_path,
                            content,
                            json.dumps(metadata),
                            importance,
                            user_id
                        )
                    )
        else:
            async with aiosqlite.connect(self.sqlite_path) as db:
                await db.execute("""
                    INSERT INTO perceptual_memories 
                    (memory_id, content_type, content_path, description, metadata, importance, user_id)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (
                    memory_id,
                    content_type,
                    content_path,
                    content,
                    json.dumps(metadata),
                    importance,
                    user_id
                ))
                await db.commit()
        
        # Store embedding in Qdrant (if available and content is text)
        if self.vector_store and content:
            try:
                from app.core.embedding.pipeline import get_embedding_pipeline
                embedding_pipeline = get_embedding_pipeline()
                embedding = embedding_pipeline.encode([content])[0].tolist()
                
                await self.vector_store.upsert(
                    id=memory_id,
                    vector=embedding,
                    payload={
                        "description": content,
                        "content_type": content_type,
                        "content_path": content_path,
                        "memory_type": "perceptual",
                        "importance": importance,
                        "user_id": user_id,
                        **metadata
                    }
                )
            except Exception as e:
                logger.warning(f"[PerceptualMemory] Failed to store embedding: {e}")
        
        logger.debug(f"[PerceptualMemory] Added memory: {memory_id}")
        return memory_id
    
    async def search(
        self,
        query: str,
        limit: int = 5,
        user_id: Optional[str] = None,
        content_type: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        """
        Search perceptual memories
        
        Args:
            query: Search query (description)
            limit: Maximum results
            user_id: Optional user ID filter
            content_type: Optional content type filter
        
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
                
                filters = {"memory_type": "perceptual"}
                if user_id:
                    filters["user_id"] = user_id
                if content_type:
                    filters["content_type"] = content_type
                
                vector_results = await self.vector_store.search(
                    query_vector=query_embedding,
                    top_k=limit,
                    filters=filters
                )
                
                # Get full metadata from SQLite
                if vector_results:
                    memory_ids = [r["id"] for r in vector_results]
                    if self.use_postgres and self._pg_pool:
                        async with self._pg_pool.connection() as conn:
                            async with conn.cursor(row_factory=dict_row) as cur:
                                await cur.execute(
                                    "SELECT * FROM perceptual_memories WHERE memory_id = ANY(%s)",
                                    (memory_ids,)
                                )
                                rows = await cur.fetchall()
                            metadata_map = {row["memory_id"]: row["metadata"] for row in rows}
                            results = []
                            for result in vector_results:
                                row_data = next((r for r in rows if r["memory_id"] == result["id"]), None)
                                if row_data:
                                    results.append({
                                        "memory_id": result["id"],
                                        "description": result.get("content", ""),
                                        "content_type": row_data["content_type"],
                                        "content_path": row_data["content_path"],
                                        "metadata": metadata_map.get(result["id"], {}),
                                        "relevance_score": result.get("score", 0.5),
                                        "importance": row_data["importance"]
                                    })
                            return results
                    else:
                        async with aiosqlite.connect(self.sqlite_path) as db:
                            db.row_factory = aiosqlite.Row
                            placeholders = ",".join("?" * len(memory_ids))
                            cursor = await db.execute(
                                f"SELECT * FROM perceptual_memories WHERE memory_id IN ({placeholders})",
                                memory_ids
                            )
                            rows = await cursor.fetchall()
                            
                            # Merge SQLite metadata with vector results
                            metadata_map = {row["memory_id"]: json.loads(row["metadata"]) for row in rows}
                            results = []
                            for result in vector_results:
                                row_data = next((r for r in rows if r["memory_id"] == result["id"]), None)
                                if row_data:
                                    results.append({
                                        "memory_id": result["id"],
                                        "description": result.get("content", ""),
                                        "content_type": row_data["content_type"],
                                        "content_path": row_data["content_path"],
                                        "metadata": metadata_map.get(result["id"], {}),
                                        "relevance_score": result.get("score", 0.5),
                                        "importance": row_data["importance"]
                                    })
                            
                            return results
            except Exception as e:
                logger.warning(f"[PerceptualMemory] Vector search failed: {e}")
        
        # Fallback to SQLite text search
        if self.use_postgres and self._pg_pool:
            async with self._pg_pool.connection() as conn:
                params = [f"%{query}%"]
                user_clause = ""
                content_clause = ""
                if user_id:
                    user_clause = " AND user_id = %s"
                    params.append(user_id)
                if content_type:
                    content_clause = " AND content_type = %s"
                    params.append(content_type)
                params.append(limit)
                query_text = f"""
                    SELECT * FROM perceptual_memories
                    WHERE description ILIKE %s
                    {user_clause}
                    {content_clause}
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
                        "description": row["description"],
                        "content_type": row["content_type"],
                        "content_path": row["content_path"],
                        "metadata": row["metadata"],
                        "relevance_score": 0.5,
                        "importance": row["importance"]
                    })
                return results

        async with aiosqlite.connect(self.sqlite_path) as db:
            db.row_factory = aiosqlite.Row
            query_sql = "SELECT * FROM perceptual_memories WHERE description LIKE ?"
            params = [f"%{query}%"]
            
            if user_id:
                query_sql += " AND user_id = ?"
                params.append(user_id)
            
            if content_type:
                query_sql += " AND content_type = ?"
                params.append(content_type)
            
            query_sql += " ORDER BY created_at DESC LIMIT ?"
            params.append(limit)
            
            cursor = await db.execute(query_sql, params)
            rows = await cursor.fetchall()
            
            results = []
            for row in rows:
                results.append({
                    "memory_id": row["memory_id"],
                    "description": row["description"],
                    "content_type": row["content_type"],
                    "content_path": row["content_path"],
                    "metadata": json.loads(row["metadata"]),
                    "relevance_score": 0.5,
                    "importance": row["importance"]
                })
            
            return results




