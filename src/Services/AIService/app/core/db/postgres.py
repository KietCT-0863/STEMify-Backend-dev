import asyncio
import logging
from typing import Optional, Dict

from psycopg_pool import AsyncConnectionPool

logger = logging.getLogger(__name__)

_pool: Optional[AsyncConnectionPool] = None
_pool_lock = asyncio.Lock()


def _normalize_conninfo(raw: str) -> str:
    if not raw or raw.strip().lower().startswith(("postgres://", "postgresql://")):
        return raw

    if ";" not in raw:
        # Assume already space-separated libpq options
        return raw

    parts = raw.split(";")
    kv: Dict[str, str] = {}
    for part in parts:
        if not part.strip():
            continue
        if "=" not in part:
            continue
        k, v = part.split("=", 1)
        k = k.strip().lower()
        v = v.strip()
        if not v:
            continue
        if k in ("host", "server"):
            kv["host"] = v
        elif k in ("port",):
            kv["port"] = v
        elif k in ("user id", "userid", "user", "username"):
            kv["user"] = v
        elif k in ("password", "pwd"):
            kv["password"] = v
        elif k in ("database", "dbname"):
            kv["dbname"] = v
        else:
            kv[k] = v

    # Build libpq conninfo string
    return " ".join(f"{k}={v}" for k, v in kv.items())


async def get_pool(dsn: str) -> AsyncConnectionPool:
    """
    Get (or create) a global psycopg AsyncConnectionPool for the given DSN.
    """
    global _pool

    if _pool:
        return _pool

    async with _pool_lock:
        if _pool:
            return _pool

        try:
            conninfo = _normalize_conninfo(dsn)
            _pool = AsyncConnectionPool(
                conninfo=conninfo,
                min_size=1,
                max_size=5,
                timeout=30,
                open=False,  # avoid deprecated auto-open
            )
            await _pool.open(wait=True)
            logger.info("[Postgres] Connection pool created")
        except Exception as e:
            logger.error(f"[Postgres] Failed to create pool: {e}")
            raise

    return _pool

