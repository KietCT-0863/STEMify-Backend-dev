"""
Synthesizer Minion
Writes final teacher-friendly answer with next steps
"""

from typing import Dict, Any, List
import logging

from app.core.reasoning.minions.type.base import BaseMinion
from app.core.reasoning.models import ReasoningResult, CausalFinding, EvidencePack
from app.core.llm.models import LLMMessage

logger = logging.getLogger(__name__)


class SynthesizerMinion(BaseMinion):
    """Synthesizer minion: writes final answer and next actions
    
    Uses Remote LLM (GPT-4o) for high-quality answer synthesis
    """
    
    @property
    def name(self) -> str:
        return "Synthesizer"
    
    async def execute(self, context: Dict[str, Any]) -> Dict[str, Any]:
        """Synthesize final answer and next actions"""
        plan = context.get("plan")
        causal_findings = context.get("causal_findings", [])
        evidence_pack: EvidencePack = context.get("evidence_pack", EvidencePack())
        graph_sample = context.get("graph_sample", {})
        
        if not plan:
            self._log("No plan found", "WARNING")
            return {
                "answer_teacher_friendly": "Unable to generate answer: missing plan",
                "next_actions": []
            }
        
        self._log("Synthesizing final answer")
        
        # Use Remote LLM if available, otherwise fallback to template-based
        if self.llm_client and self.llm_client.get_remote_provider():
            answer = await self._generate_answer_with_llm(plan, causal_findings, evidence_pack, graph_sample)
            next_actions = await self._generate_next_actions_with_llm(plan, causal_findings, evidence_pack)
        else:
            # Fallback to template-based generation
            answer = self._generate_answer(plan, causal_findings, evidence_pack, graph_sample)
            next_actions = self._generate_next_actions(plan, causal_findings, evidence_pack)
        
        self._log("Synthesis complete")
        
        return {
            "answer_teacher_friendly": answer,
            "next_actions": next_actions
        }
    
    async def _generate_answer_with_llm(
        self,
        plan,
        causal_findings: List[CausalFinding],
        evidence_pack: EvidencePack,
        graph_sample: Dict[str, Any]
    ) -> str:
        """Generate teacher-friendly answer using Remote LLM (GPT-4o)"""
        if not self.llm_client:
            return self._generate_answer(plan, causal_findings, evidence_pack, graph_sample)
        
        try:
            # Prepare findings summary
            findings_text = "\n".join([
                f"- {f.hypothesis.replace('_', ' ').title()}: "
                f"Confidence {f.confidence:.0%}, "
                f"Support: {len(f.support)} items, "
                f"Counter: {len(f.counter)} items"
                for f in causal_findings[:5]
            ])
            
            evidence_summary = f"""
Graph Evidence: {len(evidence_pack.graph_refs)} nodes, {len(evidence_pack.paths)} relationships
Text Evidence: {len(evidence_pack.texts)} sources
"""
            
            prompt = f"""You are an expert educational analyst. Synthesize a clear, teacher-friendly answer based on the analysis below.

Original Question: {plan.question}

Key Findings:
{findings_text}

Evidence Summary:
{evidence_summary}

Write a comprehensive, easy-to-understand answer that:
1. Directly addresses the teacher's question
2. Highlights the most important findings with confidence levels
3. Explains what the evidence shows
4. Uses clear, professional language suitable for educators
5. Is structured and easy to scan

Format the answer with clear sections and bullet points where appropriate."""

            messages = [
                LLMMessage(
                    role="system",
                    content="You are an expert educational analyst who synthesizes complex data into clear, actionable insights for teachers."
                ),
                LLMMessage(role="user", content=prompt)
            ]
            
            response = await self.llm_client.generate_remote(messages, max_tokens=2000, temperature=0.7)
            return response.content.strip()
            
        except Exception as e:
            self._log(f"Error generating answer with LLM: {e}, falling back to template", "WARNING")
            return self._generate_answer(plan, causal_findings, evidence_pack, graph_sample)
    
    async def _generate_next_actions_with_llm(
        self,
        plan,
        causal_findings: List[CausalFinding],
        evidence_pack: EvidencePack
    ) -> List[str]:
        """Generate next actions using Remote LLM"""
        if not self.llm_client:
            return self._generate_next_actions(plan, causal_findings, evidence_pack)
        
        try:
            findings_summary = "\n".join([
                f"- {f.hypothesis}: confidence {f.confidence:.0%}"
                for f in causal_findings if f.confidence > 0.6
            ])
            
            prompt = f"""Based on this educational analysis, suggest 3-5 specific, actionable next steps for the teacher.

Question: {plan.question}

Key Findings:
{findings_summary}

Evidence Available: {len(evidence_pack.graph_refs)} graph nodes, {len(evidence_pack.texts)} text sources

Provide 3-5 concrete, actionable recommendations. Each should be:
- Specific and actionable
- Relevant to the findings
- Practical for a teacher to implement

Return as a JSON array of strings, example: ["Action 1", "Action 2", "Action 3"]
Only return the JSON array, nothing else."""

            messages = [
                LLMMessage(
                    role="system",
                    content="You are an expert educational consultant who provides practical, actionable recommendations."
                ),
                LLMMessage(role="user", content=prompt)
            ]
            
            response = await self.llm_client.generate_remote(messages, max_tokens=500, temperature=0.8)
            
            import json
            try:
                actions = json.loads(response.content)
                if isinstance(actions, list):
                    return actions[:5]  # Limit to 5
            except json.JSONDecodeError:
                self._log("Failed to parse LLM response for next actions", "WARNING")
        
        except Exception as e:
            self._log(f"Error generating next actions with LLM: {e}", "WARNING")
        
        return self._generate_next_actions(plan, causal_findings, evidence_pack)
    
    def _generate_answer(
        self,
        plan,
        causal_findings: List[CausalFinding],
        evidence_pack: EvidencePack,
        graph_sample: Dict[str, Any]
    ) -> str:
        """Generate teacher-friendly answer (fallback template-based method)"""
        parts = []
        
        # Introduction
        parts.append(f"Based on the analysis of: {plan.question}")
        parts.append("")
        
        # Key findings
        high_confidence_findings = [f for f in causal_findings if f.confidence > 0.6]
        if high_confidence_findings:
            parts.append("Key Findings:")
            for finding in high_confidence_findings[:3]:  # Top 3 findings
                parts.append(f"• {self._format_finding(finding)}")
            parts.append("")
        
        # Evidence summary
        if evidence_pack.graph_refs:
            parts.append(f"Evidence: Analyzed {len(evidence_pack.graph_refs)} graph nodes and "
                        f"{len(evidence_pack.paths)} relationships.")
        
        if evidence_pack.texts:
            parts.append(f"Found {len(evidence_pack.texts)} relevant text sources.")
        
        parts.append("")
        
        # Specific insights
        if causal_findings:
            parts.append("Insights:")
            for finding in causal_findings[:3]:
                if finding.support:
                    parts.append(f"• {finding.hypothesis.replace('_', ' ').title()}")
                    parts.append(f"  Confidence: {finding.confidence:.0%}")
                    if finding.correlation_strength > 0.5:
                        parts.append(f"  Strong correlation detected")
            parts.append("")
        
        # Uncertainty statement if needed
        if not high_confidence_findings:
            parts.append("Note: Limited evidence available. Consider collecting more data.")
        
        return "\n".join(parts)
    
    def _format_finding(self, finding: CausalFinding) -> str:
        """Format causal finding for display"""
        hypothesis = finding.hypothesis.replace("_", " ").title()
        confidence = finding.confidence
        
        if confidence > 0.7:
            strength = "Strong evidence"
        elif confidence > 0.5:
            strength = "Moderate evidence"
        else:
            strength = "Weak evidence"
        
        return f"{hypothesis} ({strength}: {confidence:.0%})"
    
    def _generate_next_actions(
        self,
        plan,
        causal_findings: List[CausalFinding],
        evidence_pack: EvidencePack
    ) -> List[str]:
        """Generate recommended next actions for teacher"""
        actions = []
        
        # Actions based on findings
        for finding in causal_findings:
            if finding.confidence > 0.6:
                if "struggling" in finding.hypothesis.lower() or "low" in finding.hypothesis.lower():
                    actions.append("Review struggling students' recent attempts and provide targeted support")
                
                if "topic" in finding.hypothesis.lower():
                    actions.append("Consider additional practice materials for identified topics")
                
                if "engagement" in finding.hypothesis.lower():
                    actions.append("Investigate engagement patterns and consider intervention strategies")
        
        # Actions based on evidence gaps
        if not evidence_pack.graph_refs and not evidence_pack.texts:
            actions.append("Collect more data to improve analysis accuracy")
        
        if len(causal_findings) == 0:
            actions.append("Gather more specific information about student performance")
        
        # Default actions if none generated
        if not actions:
            actions.append("Review the evidence pack for detailed insights")
            actions.append("Monitor student progress over the next period")
        
        # Limit to 5 actions
        return actions[:5]

