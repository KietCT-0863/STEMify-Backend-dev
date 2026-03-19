import re
import logging
from typing import Tuple, Optional, Dict, Any
from dataclasses import dataclass

logger = logging.getLogger(__name__)


@dataclass
class IntentResult:
    """Result of intent classification."""
    intent: str
    confidence: float
    suggested_max_steps: int
    direct_tool: Optional[str] = None  # Tool to call directly if applicable
    skip_react: bool = False  # If True, bypass ReAct loop entirely


class IntentClassifier:
    """
    Lightweight intent classifier using keyword matching.
    Falls back to LLM classification for ambiguous queries.
    """
    
    # Intent patterns with associated keywords
    INTENT_PATTERNS: Dict[str, Dict[str, Any]] = {
        "memory_recall": {
            "keywords": [
                "remember", "recall", "what did", "last time", "you said",
                "told you", "mentioned", "struggling with", "my struggles",
                "do you know", "did i tell", "what i said"
            ],
            "max_steps": 2,
            "direct_tool": "memory",
            "skip_react": False,  # Still use ReAct but with fewer steps
        },
        "progress_check": {
            "keywords": [
                "progress", "how am i doing", "completion", "completed",
                "how many lessons", "my score", "my grades", "achievements",
                "how far", "percentage", "status"
            ],
            "max_steps": 3,
            "direct_tool": "learning_progress",
            "skip_react": False,
        },
        "analysis_request": {
            "keywords": [
                "analyze", "why", "explain why", "recommend", "suggest",
                "what should i", "help me understand", "improve",
                "strengths", "weaknesses", "pattern", "trend"
            ],
            "max_steps": 8,
            "direct_tool": None,
            "skip_react": False,
        },
        "general_chat": {
            "keywords": [
                "hello", "hi", "thanks", "thank you", "bye", "goodbye",
                "how are you", "what's up", "good morning", "good night"
            ],
            "max_steps": 1,
            "direct_tool": None,
            "skip_react": True,  # Direct LLM response
        },
    }
    
    DEFAULT_INTENT = "analysis_request"
    DEFAULT_MAX_STEPS = 5
    
    def __init__(self, llm_client=None):
        self.llm_client = llm_client
    
    def classify(self, query: str) -> IntentResult:
        """
        Classify query intent using keyword matching.
        
        Args:
            query: User query string
        
        Returns:
            IntentResult with intent, confidence, and processing hints
        """
        query_lower = query.lower().strip()
        
        # Track best match
        best_intent = None
        best_confidence = 0.0
        best_match_count = 0
        
        for intent, config in self.INTENT_PATTERNS.items():
            keywords = config["keywords"]
            match_count = sum(1 for kw in keywords if kw in query_lower)
            
            if match_count > 0:
                # Calculate confidence based on match ratio
                confidence = min(1.0, match_count / 2)  # 2+ matches = high confidence
                
                if match_count > best_match_count:
                    best_match_count = match_count
                    best_confidence = confidence
                    best_intent = intent
        
        # Use best match or default
        if best_intent:
            config = self.INTENT_PATTERNS[best_intent]
            logger.info(f"[IntentClassifier] Query classified as '{best_intent}' (confidence: {best_confidence:.2f})")
            return IntentResult(
                intent=best_intent,
                confidence=best_confidence,
                suggested_max_steps=config["max_steps"],
                direct_tool=config.get("direct_tool"),
                skip_react=config.get("skip_react", False),
            )
        
        # Default to analysis for complex queries
        logger.info(f"[IntentClassifier] No clear intent match, defaulting to '{self.DEFAULT_INTENT}'")
        return IntentResult(
            intent=self.DEFAULT_INTENT,
            confidence=0.5,
            suggested_max_steps=self.DEFAULT_MAX_STEPS,
            direct_tool=None,
            skip_react=False,
        )
    
    async def classify_with_llm(self, query: str) -> IntentResult:
        """
        Classify query using LLM for ambiguous cases.
        
        Args:
            query: User query string
        
        Returns:
            IntentResult with LLM-determined intent
        """
        # First try keyword matching
        result = self.classify(query)
        
        # If confidence is high enough, use keyword result
        if result.confidence >= 0.7:
            return result
        
        # If no LLM client, return keyword result
        if not self.llm_client:
            return result
        
        # Use LLM for classification
        try:
            prompt = f"""Classify this student query into ONE of these categories:
- memory_recall: Asking about something previously discussed
- progress_check: Asking about learning progress, scores, completion
- analysis_request: Asking for analysis, recommendations, explanations
- general_chat: Greetings, thanks, casual conversation

Query: "{query}"

Respond with ONLY the category name (e.g., "memory_recall"):"""

            response = await self.llm_client.generate(
                messages=[{"role": "user", "content": prompt}],
                max_tokens=20
            )
            
            intent_text = response.content.strip().lower() if hasattr(response, 'content') else str(response).strip().lower()
            
            # Map response to valid intent
            for intent in self.INTENT_PATTERNS.keys():
                if intent in intent_text:
                    config = self.INTENT_PATTERNS[intent]
                    logger.info(f"[IntentClassifier] LLM classified as '{intent}'")
                    return IntentResult(
                        intent=intent,
                        confidence=0.8,
                        suggested_max_steps=config["max_steps"],
                        direct_tool=config.get("direct_tool"),
                        skip_react=config.get("skip_react", False),
                    )
            
            # Fallback to keyword result
            return result
            
        except Exception as e:
            logger.warning(f"[IntentClassifier] LLM classification failed: {e}")
            return result
    
    def get_quick_response(self, query: str, intent_result: IntentResult) -> Optional[str]:
        if intent_result.intent != "general_chat":
            return None
        
        query_lower = query.lower()
        
        # Simple response mapping
        if any(w in query_lower for w in ["hello", "hi", "hey"]):
            return "Hello! I'm your learning advisor. How can I help you today?"
        
        if any(w in query_lower for w in ["thanks", "thank you"]):
            return "You're welcome! Let me know if you need anything else."
        
        if any(w in query_lower for w in ["bye", "goodbye"]):
            return "Goodbye! Good luck with your studies!"
        
        if "how are you" in query_lower:
            return "I'm doing great, thanks for asking! Ready to help with your learning journey."
        
        return None

