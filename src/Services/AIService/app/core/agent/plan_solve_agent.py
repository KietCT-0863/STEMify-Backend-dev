"""
Plan-and-Solve Agent
Planning → Execution phases for insights generation
"""

import ast
import logging
from typing import Dict, Any, List

from app.core.agent.base import Agent
from app.core.llm.providers.base_provider import LLMMessage

logger = logging.getLogger(__name__)


class PlanAndSolveInsightsAgent(Agent):
    """
    Plan-and-Solve Agent for Insights Generation
    
    Two-phase approach:
    1. Planning: Decompose problem into steps
    2. Execution: Execute steps sequentially
    """
    
    PLANNER_PROMPT = """
You are a top AI planning expert. Decompose the complex problem into an action plan.

Question: {question}

Output your plan as a Python list:
```python
["Step 1", "Step 2", "Step 3", ...]
```
"""
    
    EXECUTOR_PROMPT = """
You are a top AI execution expert. Solve the current step based on the plan and history.

Original Question: {question}
Complete Plan: {plan}
History: {history}
Current Step: {current_step}

Output only the answer for the current step:
"""
    
    def __init__(
        self,
        name: str = "PlanAndSolveInsightsAgent",
        llm=None,
        tool_registry=None,
        system_prompt=None,
        use_remote: bool = False
    ):
        super().__init__(name, llm, tool_registry, system_prompt, use_remote)
    
    async def run(self, query: str, **kwargs) -> Dict[str, Any]:
        """Run Plan-and-Solve"""
        # Phase 1: Planning
        logger.info("[Plan-Solve] Generating plan...")
        plan = await self._generate_plan(query)
        
        if not plan:
            return {"answer": "Failed to generate plan", "path": "plan-solve"}
        
        # Phase 2: Execution
        logger.info(f"[Plan-Solve] Executing {len(plan)} steps...")
        history = ""
        results = []
        
        for i, step in enumerate(plan):
            logger.info(f"[Plan-Solve] Step {i+1}/{len(plan)}: {step}")
            result = await self._execute_step(query, plan, history, step)
            history += f"Step {i+1}: {step}\nResult: {result}\n\n"
            results.append({"step": step, "result": result})
        
        return {
            "answer": results[-1]["result"] if results else "No result",
            "path": "plan-solve",
            "plan": plan,
            "history": history,
            "results": results
        }
    
    async def _generate_plan(self, question: str) -> List[str]:
        """Generate action plan"""
        prompt = self.PLANNER_PROMPT.format(question=question)
        messages: List[LLMMessage] = [{"role": "user", "content": prompt}]
        response = await self.llm.generate(messages, use_remote=self.use_remote)
        response_text = response.content if hasattr(response, 'content') else str(response)
       
        # Parse Python list
        try:
            if "```python" in response_text:
                plan_str = response_text.split("```python")[1].split("```")[0].strip()
            elif "```" in response_text:
                plan_str = response_text.split("```")[1].split("```")[0].strip()
            else:
                plan_str = response_text.strip()
            
            plan = ast.literal_eval(plan_str)
           
            return plan if isinstance(plan, list) else []
        except Exception as e:
            logger.warning(f"[Plan-Solve] Failed to parse plan: {e}")
            return []
    
    async def _execute_step(
        self,
        question: str,
        plan: List[str],
        history: str,
        current_step: str,
        context: Dict[str, Any] | None = None,
    ) -> str:
        """Execute a single step"""
        prompt = self.EXECUTOR_PROMPT.format(
            question=question,
            plan="\n".join([f"{i+1}. {step}" for i, step in enumerate(plan)]),
            history=history if history else "None",
            current_step=current_step
        )
        messages: List[LLMMessage] = [{"role": "user", "content": prompt}]
        response = await self.llm.generate(messages, use_remote=self.use_remote)
        return response.content if hasattr(response, 'content') else str(response)




