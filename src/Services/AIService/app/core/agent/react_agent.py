"""
ReAct Agent
Think-Act-Observe loop for teaching assistant

"""

import re
import json
import logging
from typing import Dict, Any, Optional, List, Set

from app.core.agent.base import Agent
from app.core.llm.providers.base_provider import LLMMessage

logger = logging.getLogger(__name__)

# Markers for early exit conditions
EARLY_EXIT_MARKERS = [
    "CRITICAL_MISSING_CONTEXT",
    "MISSING_REQUIRED_DATA",
    "NO_DATA_AVAILABLE",
]


class ReActTeachingAgent(Agent):
    """
    ReAct Agent for Teaching Assistant
    
    Implements Think-Act-Observe loop:
    1. Think: Analyze problem and plan action
    2. Act: Execute tool or provide answer
    3. Observe: Process tool result
    """
    
    REACT_PROMPT_TEMPLATE = """
You are an intelligent teaching assistant capable of calling external tools.

Available tools:
{tools}

FORMAT (strict):
- Thought: Your thinking process.
- Action: Either a tool call OR Finish.
  * Tool call: tool_name[{{"key": "value"}}]
    - tool_input MUST be valid JSON.
    - Example: learning_progress[{{"action": "get_progress"}}]
  * Finish: Finish[final answer]
    - Use Finish only when you are ready to give the final answer.
    - The text inside [...] is ONLY the final answer.
    - Do NOT include any 'Thought:' or 'Action:' lines inside Finish[...].
    - You MAY use multiple lines and bullet points for formatting,
      but do NOT write another 'Finish[' inside the answer.

Question: {question}
History: {history}

Now begin your reasoning and action:
"""
    
    def __init__(
        self,
        name: str = "ReActTeachingAgent",
        llm=None,
        tool_registry=None,
        system_prompt=None,
        max_steps: int = 5,
        use_remote: bool = False
    ):
        """
        Initialize ReAct agent
        
        Args:
            name: Agent name
            llm: LLM client
            tool_registry: Tool registry
            system_prompt: System prompt
            max_steps: Maximum number of steps in ReAct loop
            use_remote: Whether to use remote LLM provider
        """
        super().__init__(name, llm, tool_registry, system_prompt, use_remote)
        self.max_steps = max_steps
    
    async def run(self, query: str, **kwargs) -> Dict[str, Any]:
        """
        
        Args:
            query: User query
            **kwargs: Additional parameters (max_steps_override, etc.)
        
        Returns:
            Dict with answer, path, steps, history
        """
        history = []
        current_step = 0
        executed_actions: Set[str] = set()  # Track executed actions for deduplication 
        
        # Allow dynamic max_steps override
        effective_max_steps = kwargs.get("max_steps_override", self.max_steps)
        
        while current_step < effective_max_steps:
            current_step += 1
            logger.info(f"[ReAct] Step {current_step}/{effective_max_steps}")
            
            # Build prompt
            tools_desc = self.tool_registry.get_tools_description()
            history_str = "\n".join(history) if history else "No previous steps."
            prompt = self.REACT_PROMPT_TEMPLATE.format(
                tools=tools_desc,
                question=query,
                history=history_str
            )
            
            # Call LLM
            messages: List[LLMMessage] = [{"role": "user", "content": prompt}]
            response = await self.llm.generate(messages, use_remote=self.use_remote)
            response_text = response.content if hasattr(response, 'content') else str(response)
            
            # Parse output
            thought, action = self._parse_output(response_text)
            
            if thought:
                logger.info(f"Thought: {thought[:100]}...")
                history.append(f"Thought: {thought}")
            
            # Check if finished
            if action and action.startswith("Finish"):
                match = re.search(r"Finish\[(.*)\]", action, re.DOTALL) 
                if match:
                    final_answer = match.group(1).strip()
                else:
                    final_answer = action[len("Finish["):].rstrip("]").strip()

                return {
                    "answer": final_answer,
                    "path": "react",
                    "steps": current_step,
                    "history": history,
                }
            
            # Execute action
            if action:
                tool_name, tool_input = self._parse_action(action)
                if tool_name:
                    # Deduplication: check if same action was already executed
                    action_key = self._get_action_key(tool_name, tool_input)
                    if action_key in executed_actions:
                        logger.warning(f"[ReAct] Skipping duplicate action: {action_key}")
                        history.append(f"Action: {action} (skipped - duplicate)")
                        history.append("Observation: Already executed this action. Try a different approach or Finish.")
                        continue
                    
                    executed_actions.add(action_key)
                    observation = await self._execute_tool(tool_name, tool_input)
                    history.append(f"Action: {action}")
                    if tool_name == "submission":
                        history.append(f"Observation: {observation}")
                        logger.info(f"Action: {action}")
                        logger.info(f"Observation: {observation[:500]}...")  # Log first 500 chars
                    else:
                        # Truncate other observations to save tokens
                        max_obs_length = 1000  # Increased from 200
                        if len(observation) > max_obs_length:
                            history.append(f"Observation: {observation[:max_obs_length]}... (truncated, full length: {len(observation)})")
                            logger.info(f"Action: {action}")
                            logger.info(f"Observation: {observation[:500]}...")
                        else:
                            history.append(f"Observation: {observation}")
                            logger.info(f"Action: {action}")
                            logger.info(f"Observation: {observation}")
                    
                    # Early exit check
                    early_exit_result = self._check_early_exit(observation, history, current_step)
                    if early_exit_result:
                        return early_exit_result
                else:
                    history.append(f"Action: {action} (invalid format)")
        
        return {
            "answer": "Sorry, I couldn't complete this task within the step limit.",
            "path": "react",
            "steps": current_step,
            "history": history
        }
    
    def _get_action_key(self, tool_name: str, tool_input: Dict[str, Any]) -> str:
        """Generate a unique key for an action to detect duplicates."""
        # Sort keys for consistent hashing
        input_str = json.dumps(tool_input, sort_keys=True)
        return f"{tool_name}:{input_str}"
    
    def _check_early_exit(
        self, 
        observation: str, 
        history: List[str], 
        current_step: int
    ) -> Optional[Dict[str, Any]]:
        """
        Check if observation indicates we should exit early.
        
        Returns result dict if early exit, None otherwise.
        """
        for marker in EARLY_EXIT_MARKERS:
            if marker in observation:
                logger.info(f"[ReAct] Early exit triggered by: {marker}")
                # Extract helpful message from observation
                try:
                    obs_data = json.loads(observation)
                    message = obs_data.get("message", obs_data.get("note", "Missing required context."))
                except:
                    message = "I need more context to help you properly."
                
                return {
                    "answer": message,
                    "path": "react",
                    "steps": current_step,
                    "history": history,
                    "early_exit": True,
                    "exit_reason": marker
                }
        
        return None
    
    def _parse_output(self, text: str):
        """Parse LLM output to extract Thought and Action"""
        thought_match = re.search(r"Thought:\s*(.*?)(?=Action:|$)", text, re.DOTALL)
        action_match = re.search(r"Action:\s*(.*?)(?=Thought:|$)", text, re.DOTALL)
        
        thought = thought_match.group(1).strip() if thought_match else None
        action = action_match.group(1).strip() if action_match else None
        
        return thought, action
    
    def _parse_action(self, action_text: str):
        """Parse action string to extract tool name and input (JSON expected)."""
        # Trim bullet prefix if present: "- tool[...]"
        cleaned = action_text.strip()
        if cleaned.startswith("-"):
            cleaned = cleaned.lstrip("-").strip()

        match = re.match(r"(\w+)\[(.*)\]", cleaned, re.DOTALL)
        if not match:
            return None, None

        tool_name = match.group(1)
        tool_input_str = match.group(2).strip()

        # Try JSON first
        tool_input = self._parse_tool_input(tool_input_str)
        return tool_name, tool_input

    def _parse_tool_input(self, tool_input_str: str) -> Dict[str, Any]:
        try:
            return json.loads(tool_input_str)
        except Exception:
            pass

        # Attempt key="value" pairs (e.g., action="get_progress")
        kv_pairs = re.findall(r'(\w+)\s*=\s*"([^"]*)"', tool_input_str)
        if kv_pairs:
            return {k: v for k, v in kv_pairs}

        # Fallback: wrap raw string
        return {"input": tool_input_str}
    
    async def _execute_tool(self, tool_name: str, tool_input: Dict[str, Any]):
        """Execute tool"""
        tool = self.tool_registry.get_tool(tool_name)
        if not tool:
            return f"Error: Tool '{tool_name}' not found"
        
        try:
            result = await tool.run(tool_input)
            return result
        except Exception as e:
            logger.error(f"[ReAct] Tool execution error: {e}")
            return f"Error executing tool '{tool_name}': {str(e)}"

