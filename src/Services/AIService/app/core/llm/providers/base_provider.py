"""
Base LLM Provider
Abstract base class for LLM providers
"""

from abc import ABC, abstractmethod
from typing import List, Dict, Any, Optional
from pydantic import BaseModel


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
    cost: Optional[float] = None  # Cost in USD (for remote providers)


class BaseLLMProvider(ABC):
    """
    Abstract base class for LLM providers
    
    Supports both local (Ollama) and remote (OpenAI) providers
    """
    
    def __init__(self, model: str, temperature: float = 0.7, max_tokens: int = 2000):
        """
        Initialize LLM provider
        
        Args:
            model: Model name/identifier
            temperature: Sampling temperature (0.0-2.0)
            max_tokens: Maximum tokens to generate
        """
        self.model = model
        self.temperature = temperature
        self.max_tokens = max_tokens
    
    @abstractmethod
    async def generate(
        self,
        messages: List[LLMMessage],
        **kwargs
    ) -> LLMResponse:
        pass
    
    @abstractmethod
    async def generate_stream(
        self,
        messages: List[LLMMessage],
        **kwargs
    ) -> Any:
        pass
    
    def is_local(self) -> bool:
        return False
