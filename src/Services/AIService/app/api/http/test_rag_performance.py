"""
RAG Performance Test Script
Test RAG pipeline with DeepSeek API

Usage:
    python -m app.api.http.test_rag_performance
"""

import asyncio
import sys
import os
import time
from typing import Dict, Any, List
from datetime import datetime
from pathlib import Path

# Add project root to path
project_root = Path(__file__).parent.parent.parent.parent
sys.path.insert(0, str(project_root))

from openai import AsyncOpenAI
import logging

# Import project modules
try:
    from app.infrastructure.data.fixtures.mock_lesson_data import get_mock_lec_sec_data
    from app.core.rag.document_processor import DocumentProcessor
    from app.core.embedding.pipeline import EmbeddingPipeline
    from app.core.vector_store import VectorStoreClient
    from app.core.rag.vector_retriever import VectorRetriever
    from app.core.rag.ingestion_pipeline import IngestionPipeline
    from app.infrastructure.config.settings import settings
except ImportError as e:
    print(f"Import error: {e}")
    print("Make sure you're running from the project root directory")
    print("Try: python -m app.api.http.test_rag_performance")
    sys.exit(1)

# Setup logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class RAGPerformanceTester:
    """Test RAG performance with different LLM providers"""
    
    def __init__(self):
        self.vector_store = VectorStoreClient()
        self.embedding_pipeline = EmbeddingPipeline()
        self.document_processor = DocumentProcessor()
        
        # Initialize LLM client
        self.deepseek_client = None
        
        # Test queries
        self.test_queries = [
            "Propose ONE new lesson section that would logically extend this lesson. "
             "Return JSON with fields: title (string), durationMinutes (integer), description (string). "
             "Example: { 'title': 'New Section', 'durationMinutes': 10, 'description': 'This section will cover the new topic.' }",

        ]
    
    def setup_llm_client(self):
        """Setup DeepSeek API client"""
        # DeepSeek uses OpenAI-compatible API
        deepseek_api_key = os.getenv("DEEPSEEK_API_KEY")
        if deepseek_api_key:
            self.deepseek_client = AsyncOpenAI(
                api_key=deepseek_api_key,
                base_url="https://api.deepseek.com/v1"
            )
            logger.info("DeepSeek client initialized")
        else:
            logger.warning("DEEPSEEK_API_KEY not found, skipping DeepSeek tests")
    
    async def ingest_mock_data(self) -> Dict[str, Any]:
        """Ingest mock lesson data into vector store"""
        logger.info("=" * 80)
        logger.info("STEP 1: Ingesting Mock Data")
        logger.info("=" * 80)
        
        # Get mock data
        mock_data = get_mock_lec_sec_data()
        
        # Convert to classroom data format (simplified)
        classroom_data = {
            "classroom": {
                "id": 1,
                "name": "Test Classroom",
                "grade": "7",
                "status": "Active"
            },
            "lesson": mock_data
        }
        
        # Process documents
        logger.info("Processing documents...")
        documents = self._process_lesson_data(mock_data)
        logger.info(f"Processed {len(documents)} documents")
        
        # Generate embeddings
        logger.info("Generating embeddings...")
        documents_with_embeddings = self.embedding_pipeline.generate_embeddings(documents)
        logger.info(f"Generated {len(documents_with_embeddings)} embeddings")
        
        # Store in vector database
        logger.info("Storing in vector database...")
        await self.vector_store.ensure_collection(vector_size=384)
        
        stored_count = 0
        for doc in documents_with_embeddings:
            try:
                embedding = doc.pop("embedding")
                doc_id = self._hash_to_id(doc["document_id"])
                
                # Qdrant accepts UUID string or integer
                await self.vector_store.upsert(
                    id=doc_id,  # UUID string
                    vector=embedding,
                    payload={
                        **doc.get("metadata", {}),
                        "content": doc["content"],
                        "document_id": doc["document_id"]
                    }
                )
                stored_count += 1
            except Exception as e:
                logger.error(f"Error storing document {doc.get('document_id', 'unknown')}: {e}")
        
        logger.info(f"Stored {stored_count} documents in vector database")
        return {"stored_count": stored_count, "documents": documents_with_embeddings}
    
    def _process_lesson_data(self, lesson_data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Process lesson data into documents"""
        documents = []
        
        # Main lesson document
        lesson_content = f"""
        Title: {lesson_data.get('title', 'N/A')}
        Publisher: {lesson_data.get('publisher', 'N/A')}
        Age Range: {lesson_data.get('ageRange', 'N/A')}
        Duration: {lesson_data.get('durationMinutes', 0)} minutes
        Description: {lesson_data.get('description', '')}
        Learning Outcomes: {', '.join(lesson_data.get('learningOutcomes', []))}
        Skills: {', '.join(lesson_data.get('skills', []))}
        Topics: {', '.join(lesson_data.get('topics', []))}
        Standards: {', '.join(lesson_data.get('standards', []))}
        """
        
        documents.append({
            "content": lesson_content.strip(),
            "metadata": {
                "document_type": "lesson",
                "lesson_title": lesson_data.get('title', ''),
                "publisher": lesson_data.get('publisher', ''),
            },
            "document_id": f"lesson_{lesson_data.get('title', 'unknown').lower().replace(' ', '_')}",
            "confidence_score": 1.0
        })
        
        # Section documents
        for idx, section in enumerate(lesson_data.get('sections', [])):
            section_content = f"""
            Section: {section.get('title', 'N/A')}
            Duration: {section.get('durationMinutes', 0)} minutes
            Description: {section.get('description', '')}
            """
            
            documents.append({
                "content": section_content.strip(),
                "metadata": {
                    "document_type": "section",
                    "section_title": section.get('title', ''),
                    "section_index": idx,
                    "duration_minutes": section.get('durationMinutes', 0),
                },
                "document_id": f"section_{idx}_{section.get('title', 'unknown').lower().replace(' ', '_')}",
                "confidence_score": 1.0
            })
        
        return documents
    
    def _hash_to_id(self, text: str):
        """
        Convert string to Qdrant ID
        Qdrant accepts unsigned integers (0 to 2^64-1) or UUID string
        We'll use UUID format for better compatibility
        """
        import hashlib
        import uuid
        
        # Generate deterministic UUID from text hash
        hash_obj = hashlib.md5(text.encode())
        hash_bytes = hash_obj.digest()
        
        # Create UUID v5 from namespace and hash
        namespace = uuid.UUID('6ba7b810-9dad-11d1-80b4-00c04fd430c8')  # DNS namespace
        qdrant_id = str(uuid.uuid5(namespace, text))
        
        return qdrant_id
    
    async def retrieve_context(self, query: str, top_k: int = 5) -> List[Dict[str, Any]]:
        """Retrieve relevant context using RAG"""
        # Generate query embedding
        query_doc = {"content": query}
        query_docs = self.embedding_pipeline.generate_embeddings([query_doc], update_confidence=False)
        
        if not query_docs or "embedding" not in query_docs[0]:
            return []
        
        query_embedding = query_docs[0]["embedding"]
        
        # Search in vector store
        results = await self.vector_store.search(
            query_vector=query_embedding,
            top_k=top_k
        )
        
        # Format results
        contexts = []
        for result in results:
            payload = result.get("payload", {})
            contexts.append({
                "content": payload.get("content", ""),
                "score": result.get("score", 0.0),
                "metadata": {k: v for k, v in payload.items() if k != "content"}
            })
        
        return contexts
    
    async def call_deepseek(self, query: str, context: List[Dict[str, Any]]) -> Dict[str, Any]:
        """Call DeepSeek API with RAG context"""
        if not self.deepseek_client:
            return {"error": "DeepSeek client not initialized"}
        
        # Build prompt with context
        context_text = "\n\n".join([
            f"[Document {i+1}]\n{doc['content']}"
            for i, doc in enumerate(context)
        ])
        
        prompt = f"""You are an educational content designer.

You are given:
1) A lesson overview (title, description, learning outcomes, skills, topics).
2) Existing lesson sections (title, duration, description).

TASK:
- Propose EXACTLY ONE NEW SECTION for this lesson.
- The new section MUST:
  - Fit logically with the overall lesson description and learning outcomes in the context.
  - Not duplicate existing sections.
  - Add real value (e.g. reflection, assessment, extension activity, debrief, etc.).

OUTPUT FORMAT (MUST be valid JSON, no extra text):
{{
  "title": "...",
  "durationMinutes": 0,
  "description": "..."
}}

Context:
{context_text}

Question: {query}
"""
        
        logger.info(f"DeepSeek Prompt: {prompt}")
        start_time = time.time()
        try:
            response = await self.deepseek_client.chat.completions.create(
                model="deepseek-chat",
                messages=[
                    {"role": "system", "content": "You are a helpful assistant."},
                    {"role": "user", "content": prompt}
                ],
                temperature=0.7,
                max_tokens=1000
            )
            
            elapsed_time = time.time() - start_time
            
            return {
                "provider": "DeepSeek",
                "answer": response.choices[0].message.content,
                "response_time": elapsed_time,
                "tokens": {
                    "prompt": response.usage.prompt_tokens if response.usage else 0,
                    "completion": response.usage.completion_tokens if response.usage else 0,
                    "total": response.usage.total_tokens if response.usage else 0
                },
                "model": "deepseek-chat"
            }
        except Exception as e:
            logger.error(f"DeepSeek API error: {e}")
            return {"error": str(e), "provider": "DeepSeek"}
    
    async def run_performance_test(self):
        """Run complete performance test"""
        logger.info("=" * 80)
        logger.info("RAG Performance Test - DeepSeek")
        logger.info("=" * 80)
        
        # Setup
        self.setup_llm_client()
        
        # Step 1: Ingest data
        ingestion_result = await self.ingest_mock_data()
        logger.info(f"\n✅ Ingested {ingestion_result['stored_count']} documents\n")
        
        # Step 2: Test queries
        logger.info("=" * 80)
        logger.info("STEP 2: Running Performance Tests")
        logger.info("=" * 80)
        
        results = []
        
        for query in self.test_queries:
            logger.info(f"\n{'='*80}")
            logger.info(f"Query: {query}")
            logger.info(f"{'='*80}\n")
            
            # Retrieve context
            logger.info("Retrieving context...")
            context = await self.retrieve_context(query, top_k=3)
            logger.info(f"Retrieved {len(context)} relevant documents")
            
            if not context:
                logger.warning("No context retrieved, skipping query")
                continue
            
            # Test DeepSeek
            logger.info("\n--- Testing DeepSeek ---")
            deepseek_result = await self.call_deepseek(query, context)
            if "error" not in deepseek_result:
                logger.info(f"✅ DeepSeek: {deepseek_result['response_time']:.2f}s")
                logger.info(f"   Answer: {deepseek_result['answer']}...")
            else:
                logger.error(f"❌ DeepSeek error: {deepseek_result.get('error')}")
            
            results.append({
                "query": query,
                "context_count": len(context),
                "deepseek": deepseek_result
            })
        
        # Step 3: Summary
        logger.info("\n" + "=" * 80)
        logger.info("STEP 3: Performance Summary")
        logger.info("=" * 80)
        
        self._print_summary(results)
    
    def _print_summary(self, results: List[Dict[str, Any]]):
        """Print performance summary"""
        deepseek_times = []
        deepseek_tokens = []
        
        for result in results:
            if "error" not in result.get("deepseek", {}):
                deepseek_times.append(result["deepseek"]["response_time"])
                deepseek_tokens.append(result["deepseek"]["tokens"]["total"])
        
        logger.info("\n📊 Performance Metrics:\n")
        
        if deepseek_times:
            logger.info("DeepSeek:")
            logger.info(f"  Average Response Time: {sum(deepseek_times)/len(deepseek_times):.2f}s")
            logger.info(f"  Min Response Time: {min(deepseek_times):.2f}s")
            logger.info(f"  Max Response Time: {max(deepseek_times):.2f}s")
            logger.info(f"  Average Tokens: {sum(deepseek_tokens)/len(deepseek_tokens):.0f}")
            logger.info(f"  Total Queries: {len(deepseek_times)}")
        else:
            logger.warning("No successful DeepSeek queries to summarize")


async def main():
    """Main entry point"""
    tester = RAGPerformanceTester()
    await tester.run_performance_test()


if __name__ == "__main__":
    print("\n" + "=" * 80)
    print("RAG Performance Test Script - DeepSeek")
    print("=" * 80)
    print("\nMake sure you have:")
    print("  - DEEPSEEK_API_KEY environment variable set")
    print("  - Qdrant running on localhost:6333")
    print("  - Vector store collection ready")
    print("\n" + "=" * 80 + "\n")
    
    asyncio.run(main())

