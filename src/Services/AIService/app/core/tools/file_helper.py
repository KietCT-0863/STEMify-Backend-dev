import logging
import aiohttp
from typing import Optional, Tuple
from urllib.parse import urlparse

logger = logging.getLogger(__name__)


async def download_file_bytes(url: str, max_size_mb: int = 10) -> Optional[bytes]:
    if not url or not url.strip():
        return None
    
    max_size_bytes = max_size_mb * 1024 * 1024
    
    try:
        async with aiohttp.ClientSession() as session:
            async with session.get(url, timeout=aiohttp.ClientTimeout(total=30)) as response:
                if response.status != 200:
                    logger.warning(
                        "Failed to download file",
                        extra={"url": url, "status": response.status}
                    )
                    return None
                
                content_length = response.headers.get("Content-Length")
                if content_length and int(content_length) > max_size_bytes:
                    logger.warning(
                        "File too large",
                        extra={"url": url, "size": content_length, "max": max_size_bytes}
                    )
                    return None
                
                data = b""
                async for chunk in response.content.iter_chunked(8192):
                    data += chunk
                    if len(data) > max_size_bytes:
                        logger.warning(
                            "File exceeds size limit during download",
                            extra={"url": url, "max": max_size_bytes}
                        )
                        return None
                
                logger.debug(
                    "Successfully downloaded file",
                    extra={"url": url, "size_bytes": len(data)}
                )
                return data
                
    except aiohttp.ClientError as e:
        logger.error(
            "Error downloading file",
            extra={"url": url, "error": str(e)},
            exc_info=True
        )
        return None
    except Exception as e:
        logger.error(
            "Unexpected error downloading file",
            extra={"url": url, "error": str(e)},
            exc_info=True
        )
        return None


def detect_file_type_from_url(url: str) -> Optional[str]:
    if not url:
        return None
    
    try:
        parsed = urlparse(url)
        path = parsed.path.lower()
        
        # Common extensions
        if path.endswith(('.pdf',)):
            return '.pdf'
        elif path.endswith(('.doc', '.docx')):
            return '.docx' if path.endswith('.docx') else '.doc'
        elif path.endswith(('.jpg', '.jpeg')):
            return '.jpg'
        elif path.endswith('.png'):
            return '.png'
        elif path.endswith('.gif'):
            return '.gif'
        elif path.endswith(('.bmp',)):
            return '.bmp'
        elif path.endswith(('.tiff', '.tif')):
            return '.tiff'
        elif path.endswith('.webp'):
            return '.webp'
        elif path.endswith(('.svg',)):
            return '.svg'
        elif path.endswith(('.mp4',)):
            return '.mp4'
        elif path.endswith(('.webm', '.mkv')):
            return '.webm'
        elif path.endswith(('.avi',)):
            return '.avi'
        elif path.endswith(('.mov',)):
            return '.mov'
        elif path.endswith(('.csv',)):
            return '.csv'
        elif path.endswith(('.txt',)):
            return '.txt'
        
        return None
    except Exception as e:
        logger.warning(
            "Error detecting file type from URL",
            extra={"url": url, "error": str(e)}
        )
        return None


def is_url(url: str) -> bool:
    if not url or not url.strip():
        return False
    
    try:
        parsed = urlparse(url)
        return parsed.scheme in ('http', 'https', 'file')
    except Exception:
        return False

