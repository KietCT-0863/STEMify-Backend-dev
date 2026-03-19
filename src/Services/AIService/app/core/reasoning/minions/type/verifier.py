"""
Verifier Minion
Checks logical gaps, hallucination, and enforces citations
"""

from typing import Dict, Any, List
import logging

from app.core.reasoning.minions.type.base import BaseMinion
from app.core.reasoning.models import ReasoningResult, CausalFinding, EvidencePack
from app.core.llm.models import LLMMessage

logger = logging.getLogger(__name__)


class VerifierMinion(BaseMinion):
    """Verifier minion: checks logical gaps and enforces citations
    
    Uses Local LLM (Ollama) for logical consistency checking
    """
    
    @property
    def name(self) -> str:
        return "Verifier"
    
    async def execute(self, context: Dict[str, Any]) -> Dict[str, Any]:
        """Verify reasoning result for logical gaps and hallucination"""
        result: ReasoningResult = context.get("result")
        if not result:
            self._log("No result found to verify", "WARNING")
            return {"verification": {"passed": False, "issues": ["No result to verify"]}}
        
        self._log("Verifying reasoning result")
        
        issues = []
        warnings = []
        
        # Check 1: All claims must have evidence
        issues.extend(self._check_evidence_coverage(result))
        
        # Check 2: All graph references must have node IDs
        issues.extend(self._check_graph_citations(result.evidence_pack))
        
        # Check 3: Causal findings must have support
        issues.extend(self._check_causal_support(result.causal_findings))
        
        # Check 4: Check for logical gaps
        warnings.extend(self._check_logical_gaps(result))
        
        # Check 5: Verify timestamps are present where needed
        issues.extend(self._check_timestamps(result))
        
        # Check 6: Use Local LLM for logical consistency check
        if self.llm_client and self.llm_client.get_local_provider():
            llm_issues = await self._check_logical_consistency_with_llm(result)
            warnings.extend(llm_issues)
        
        passed = len(issues) == 0
        
        verification = {
            "passed": passed,
            "issues": issues,
            "warnings": warnings,
            "timestamp": self.clock_tool.now()
        }
        
        if passed:
            self._log("Verification passed")
        else:
            self._log(f"Verification found {len(issues)} issues, {len(warnings)} warnings", "WARNING")
        
        return {"verification": verification}
    
    def _check_evidence_coverage(self, result: ReasoningResult) -> List[str]:
        """Check that all claims are backed by evidence"""
        issues = []
        
        # Check answer has evidence references
        answer = result.answer_teacher_friendly
        if answer and not result.evidence_pack.graph_refs and not result.evidence_pack.texts:
            issues.append("Answer provided but no evidence in evidence pack")
        
        # Check causal findings have support
        for finding in result.causal_findings:
            if finding.confidence > 0.5 and not finding.support:
                issues.append(f"High-confidence finding '{finding.hypothesis}' has no supporting evidence")
        
        return issues
    
    def _check_graph_citations(self, evidence_pack: EvidencePack) -> List[str]:
        """Check that graph references have valid node IDs"""
        issues = []
        
        for ref in evidence_pack.graph_refs:
            if not ref.node_id or ref.node_id == "":
                issues.append(f"Graph reference missing node_id for type {ref.type}")
            if not ref.type or ref.type == "Unknown":
                issues.append(f"Graph reference missing type for node_id {ref.node_id}")
        
        return issues
    
    def _check_causal_support(self, causal_findings: List[CausalFinding]) -> List[str]:
        """Check that causal findings have adequate support"""
        issues = []
        
        for finding in causal_findings:
            if finding.confidence > 0.7 and len(finding.support) < 2:
                issues.append(
                    f"High-confidence finding '{finding.hypothesis}' has insufficient support "
                    f"({len(finding.support)} items)"
                )
        
        return issues
    
    def _check_logical_gaps(self, result: ReasoningResult) -> List[str]:
        """Check for logical gaps in reasoning"""
        warnings = []
        
        # Check if plan entities were resolved
        if result.plan and "entities" in result.plan:
            # This would require plan to be in result, which we'll add
            pass
        
        # Check if causal findings contradict each other
        findings = result.causal_findings
        for i, f1 in enumerate(findings):
            for f2 in findings[i+1:]:
                if self._findings_contradict(f1, f2):
                    warnings.append(
                        f"Contradictory findings: '{f1.hypothesis}' vs '{f2.hypothesis}'"
                    )
        
        return warnings
    
    def _findings_contradict(self, f1: CausalFinding, f2: CausalFinding) -> bool:
        """Check if two findings contradict each other"""
        # Simple heuristic: if one says "improves" and other says "worsens"
        h1_lower = f1.hypothesis.lower()
        h2_lower = f2.hypothesis.lower()
        
        improvement_words = ["improve", "increase", "positive", "better", "excel"]
        decline_words = ["decline", "decrease", "negative", "worse", "struggle", "low"]
        
        h1_improves = any(word in h1_lower for word in improvement_words)
        h1_declines = any(word in h1_lower for word in decline_words)
        h2_improves = any(word in h2_lower for word in improvement_words)
        h2_declines = any(word in h2_lower for word in decline_words)
        
        return (h1_improves and h2_declines) or (h1_declines and h2_improves)
    
    def _check_timestamps(self, result: ReasoningResult) -> List[str]:
        """Check that timestamps are present where needed"""
        issues = []
        
        # Check graph references for temporal data
        for ref in result.evidence_pack.graph_refs:
            props = ref.props
            if ref.type in ["QuizAttempt", "AssignmentAttempt"]:
                if "completed_at" not in props and "submitted_at" not in props and "started_at" not in props:
                    issues.append(f"Attempt node {ref.node_id} missing timestamp")
        
        return issues
    
    async def _check_logical_consistency_with_llm(self, result: ReasoningResult) -> List[str]:
        """Use Local LLM to check for logical inconsistencies"""
        if not self.llm_client:
            return []
        
        warnings = []
        
        try:
            # Prepare summary of findings
            findings_summary = "\n".join([
                f"- {f.hypothesis}: confidence={f.confidence:.2f}, support={len(f.support)} items"
                for f in result.causal_findings[:5]  # Limit to top 5
            ])
            
            answer_preview = result.answer_teacher_friendly[:500] if result.answer_teacher_friendly else "No answer"
            
            prompt = f"""Review this educational analysis for logical inconsistencies or contradictions.

Causal Findings:
{findings_summary}

Answer Preview:
{answer_preview}

Check for:
1. Contradictory statements
2. Claims not supported by findings
3. Logical gaps in reasoning

If you find any issues, list them briefly. If everything is consistent, respond with "OK".

Response format: One issue per line, or "OK" if no issues."""

            messages = [
                LLMMessage(role="system", content="You are an expert at detecting logical inconsistencies in educational analysis."),
                LLMMessage(role="user", content=prompt)
            ]
            
            response = await self.llm_client.generate_local(messages, max_tokens=300)
            
            if response.content.strip().upper() != "OK":
                # Parse warnings from response
                lines = [line.strip() for line in response.content.strip().split("\n") if line.strip()]
                warnings.extend([f"LLM consistency check: {line}" for line in lines if line])
        
        except Exception as e:
            self._log(f"Error in LLM consistency check: {e}", "WARNING")
        
        return warnings

