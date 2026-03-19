"""
Reflection Agent
Execute → Reflect → Refine loop for content generation
"""

import logging
from typing import Dict, Any, List

from app.core.agent.base import Agent
from app.core.llm.providers.base_provider import LLMMessage

logger = logging.getLogger(__name__)


class ReflectionContentAgent(Agent):
    """
    Reflection Agent for Content Generation
    
    Execute → Reflect → Refine loop:
    1. Execute: Generate initial content
    2. Reflect: Review and provide feedback
    3. Refine: Improve content based on feedback
    """
    
    INITIAL_PROMPT = """
You are a content generation expert. Generate content according to the requirements.

Requirement: {task}

Output the content directly.
"""
    
    REFLECT_PROMPT = """
You are a strict content review expert. Review the following content:

Original Task: {task}
Content: {content}

Analyze quality, point out deficiencies, and provide improvement suggestions.
If the content is already good, respond "No improvement needed".
"""
    
    REFINE_PROMPT = """
You are a content generation expert. Improve your content based on feedback:

Original Task: {task}
Previous Content: {last_attempt}
Feedback: {feedback}

Output the improved content.
"""
    
    def __init__(
        self,
        name: str = "ReflectionContentAgent",
        llm=None,
        tool_registry=None,
        system_prompt=None,
        max_iterations: int = 3,
        use_remote: bool = False
    ):
        super().__init__(name, llm, tool_registry, system_prompt, use_remote)
        self.max_iterations = max_iterations
    
    async def run(self, query: str, **kwargs) -> Dict[str, Any]:
        """Run Reflection loop"""
        # Initial execution
        logger.info("[Reflection] Initial execution...")
        initial_content = await self._generate_initial(query)
        memory = [{"type": "execution", "content": initial_content}]
        
        # Reflection loop
        for i in range(self.max_iterations):
            logger.info(f"[Reflection] Iteration {i+1}/{self.max_iterations}")
            
            # Reflect
            last_content = memory[-1]["content"] if memory else initial_content
            feedback = await self._reflect(query, last_content)
            memory.append({"type": "reflection", "content": feedback})
            
            # Check if improvement needed
            if "no improvement needed" in feedback.lower():
                logger.info("[Reflection] No improvement needed, stopping")
                break
            
            # Refine
            refined_content = await self._refine(query, last_content, feedback)
            memory.append({"type": "execution", "content": refined_content})
        
        final_content = memory[-1]["content"] if memory else initial_content
        
        return {
            "answer": final_content,
            "path": "reflection",
            "iterations": len([m for m in memory if m["type"] == "execution"]),
            "memory": memory
        }
    
    async def _generate_initial(self, task: str) -> str:
        """Generate initial content"""
        prompt = self.INITIAL_PROMPT.format(task=task)
        messages: List[LLMMessage] = [{"role": "user", "content": prompt}]
        response = await self.llm.generate(messages, use_remote=self.use_remote)
        return response.content if hasattr(response, 'content') else str(response)
    
    async def _reflect(self, task: str, content: str) -> str:
        """Reflect on content"""
        prompt = self.REFLECT_PROMPT.format(task=task, content=content)
        messages: List[LLMMessage] = [{"role": "user", "content": prompt}]
        response = await self.llm.generate(messages, use_remote=self.use_remote)
        return response.content if hasattr(response, 'content') else str(response)
    
    async def _refine(self, task: str, last_attempt: str, feedback: str) -> str:
        """Refine content"""
        prompt = self.REFINE_PROMPT.format(
            task=task,
            last_attempt=last_attempt,
            feedback=feedback
        )
        messages: List[LLMMessage] = [{"role": "user", "content": prompt}]
        response = await self.llm.generate(messages, use_remote=self.use_remote)
        return response.content if hasattr(response, 'content') else str(response)




