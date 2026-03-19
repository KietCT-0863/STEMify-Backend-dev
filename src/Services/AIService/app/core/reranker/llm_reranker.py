"""
LLM Reranker
Rerank using LLM to score relevance
"""

from typing import List, Dict, Any
import logging
import json

from app.core.reranker.base_reranker import BaseReranker
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)

# Lazy import for LLMClient (may not be implemented yet)
try:
    from app.core.llm.client import LLMClient
    LLM_AVAILABLE = True
except ImportError:
    LLM_AVAILABLE = False
    LLMClient = None


class LLMReranker(BaseReranker):
    """LLM-based reranker using GPT to score relevance"""
    
    def __init__(self, llm_client = None):
        if not LLM_AVAILABLE:
            raise ImportError(
                "LLM module not available. LLMClient is not implemented yet. "
                "Please use 'cohere' or 'local' reranker instead."
            )
        
        if llm_client is None:
            try:
                from app.core.llm.client import LLMClient
                llm_client = LLMClient()
            except (ImportError, AttributeError) as e:
                raise ImportError(
                    f"LLMClient not available: {e}. "
                    "Please use 'cohere' or 'local' reranker instead."
                )
        
        self.llm_client = llm_client
        logger.info("Initialized LLM reranker")
    
    async def rerank(
        self,
        query: str,
        documents: List[Dict[str, Any]],
        top_k: int = 5
    ) -> List[Dict[str, Any]]:
        """
        Re-rank documents using LLM to score relevance
        
        Args:
            query: Natural language query
            documents: List of documents to rerank
            top_k: Number of top documents to return
        
        Returns:
            Re-ranked documents
        """
        if not documents:
            return []
        
        logger.info(f"Reranking {len(documents)} documents with LLM")
        
        try:
            # Build prompt for LLM
            prompt = self._build_rerank_prompt(query, documents)
            
            # Call LLM
            response = await self.llm_client.generate(
                prompt=prompt,
                temperature=0.0,  # Low temperature for consistent scoring
                max_tokens=1000
            )
            
            # Parse LLM response (JSON format)
            scores = self._parse_llm_response(response, len(documents))
            
            # Create list of (index, score, document)
            scored_docs = list(zip(range(len(documents)), scores, documents))
            
            # Sort by score (descending)
            scored_docs.sort(key=lambda x: x[1], reverse=True)
            
            # Build reranked results
            reranked_docs = []
            for rank, (original_index, score, original_doc) in enumerate(scored_docs[:top_k]):
                # Normalize score to [0, 1]
                normalized_score = max(0.0, min(1.0, float(score)))
                
                # Add rerank metadata
                reranked_doc = self._add_rerank_metadata(
                    original_doc,
                    normalized_score,
                    rank + 1
                )
                
                reranked_docs.append(reranked_doc)
            
            logger.info(f"Reranked to {len(reranked_docs)} documents")
            return reranked_docs
            
        except Exception as e:
            logger.error(f"Error in LLM reranking: {e}", exc_info=True)
            # Fallback: return original documents sorted by retrieval score
            logger.warning("Falling back to original order")
            sorted_docs = sorted(
                documents,
                key=lambda x: x.get("retrieval_score", 0),
                reverse=True
            )
            return sorted_docs[:top_k]
    
    def _build_rerank_prompt(self, query: str, documents: List[Dict[str, Any]]) -> str:
        """Build prompt for LLM reranking"""
        doc_texts = []
        for i, doc in enumerate(documents):
            content = self._extract_content(doc)
            doc_texts.append(f"Document {i}:\n{content[:500]}...")  # Truncate for prompt
        
        prompt = f"""You are a relevance scorer. Given a query and a list of documents, score each document's relevance to the query on a scale of 0.0 to 1.0.

Query: {query}

Documents:
{chr(10).join(doc_texts)}

Return a JSON array of scores, one for each document, in order. Example: [0.9, 0.7, 0.5, 0.3, 0.1]

Scores:"""
        
        return prompt
    
    def _parse_llm_response(self, response: str, expected_count: int) -> List[float]:
        """Parse LLM response to extract scores"""
        try:
            # Try to extract JSON array
            import re
            json_match = re.search(r'\[[\d\.,\s]+\]', response)
            if json_match:
                scores = json.loads(json_match.group())
                if len(scores) == expected_count:
                    return [float(s) for s in scores]
            
            # Fallback: try to parse as comma-separated values
            numbers = re.findall(r'\d+\.?\d*', response)
            if len(numbers) >= expected_count:
                return [float(n) for n in numbers[:expected_count]]
            
            # Last resort: return equal scores
            logger.warning("Could not parse LLM response, using equal scores")
            return [0.5] * expected_count
            
        except Exception as e:
            logger.error(f"Error parsing LLM response: {e}")
            return [0.5] * expected_count

