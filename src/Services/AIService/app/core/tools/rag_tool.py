"""
RAG Tool
MCP-compatible tool wrapping HybridRetriever
"""

from typing import Dict, Any
import logging
import json

from app.core.tools.base import Tool
from app.core.rag.hybrid_retriever import HybridRetriever

logger = logging.getLogger(__name__)


class RAGTool(Tool):
    """RAG Tool - MCP-compatible"""
    
    def __init__(self, hybrid_retriever: HybridRetriever):
        super().__init__(
            name="rag_search",
            description="Search educational content using RAG (Retrieval-Augmented Generation)"
        )
        self.hybrid_retriever = hybrid_retriever
    
    async def run(self, parameters: Dict[str, Any]) -> str:
        """Execute RAG search"""
        query = parameters.get("query", "")
        top_k = parameters.get("top_k", 5)
        
        try:
            results = await self.hybrid_retriever.retrieve(query, top_k=top_k)
            
            # Format results
            formatted = []
            for i, doc in enumerate(results, 1):
                content = doc.get('content', '')[:200]
                score = doc.get('score', 0.0)
                formatted.append(f"[{i}] (score: {score:.3f}) {content}...")
            
            return "\n\n".join(formatted) if formatted else "No results found."
        except Exception as e:
            logger.error(f"[RAGTool] Error: {e}")
            return json.dumps({"error": str(e)})
    
    def get_parameters_schema(self) -> Dict[str, Any]:
        return {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "Search query"
                },
                "top_k": {
                    "type": "integer",
                    "description": "Number of results",
                    "default": 5
                }
            },
            "required": ["query"]
        }




