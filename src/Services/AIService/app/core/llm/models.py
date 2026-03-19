"""
LLM Models
Models for LLM requests and responses
"""

from pydantic import BaseModel
from typing import List, Dict, Any, Optional


class LLMMessage(BaseModel):
    """LLM message model"""
    role: str  # "system", "user", "assistant"
    content: str


class LLMResponse(BaseModel):
    """LLM response model"""
    content: str
    model: str
    usage: Optional[Dict[str, int]] = None  # tokens, prompt_tokens, completion_tokens
    finish_reason: Optional[str] = None
    cost: Optional[float] = None  # Cost in USD if applicable


class LLMRequest(BaseModel):
    """LLM request model"""
    messages: List[LLMMessage]
    temperature: float = 0.7
    max_tokens: int = 2000
    stream: bool = False
