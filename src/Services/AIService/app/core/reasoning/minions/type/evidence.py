"""
Evidence Minion
Assembles evidence pack from graph and vector sources
"""

from typing import Dict, Any, List
import logging

from app.core.reasoning.minions.type.base import BaseMinion
from app.core.reasoning.models import EvidencePack, GraphNode, GraphPath, TextEvidence

logger = logging.getLogger(__name__)


class EvidenceMinion(BaseMinion):
    """Evidence minion: assembles evidence pack"""
    
    @property
    def name(self) -> str:
        return "Evidence"
    
    async def execute(self, context: Dict[str, Any]) -> Dict[str, Any]:
        """Assemble evidence pack from graph and vector sources"""
        plan = context.get("plan")
        graph_sample = context.get("graph_sample", {})
        causal_findings = context.get("causal_findings", [])
        
        if not plan:
            self._log("No plan found", "WARNING")
            return {"evidence_pack": EvidencePack()}
        
        self._log("Assembling evidence pack")
        
        # Extract graph references
        graph_refs = self._extract_graph_refs(graph_sample)
        
        # Extract paths
        paths = self._extract_paths(graph_sample)
        
        # Search for text evidence from vector store
        texts = await self._search_text_evidence(plan, causal_findings)
        
        # Rerank text evidence
        if texts:
            texts = await self._rerank_text_evidence(texts, plan.question)
        
        evidence_pack = EvidencePack(
            graph_refs=graph_refs,
            paths=paths,
            texts=texts
        )
        
        self._log(f"Assembled evidence: {len(graph_refs)} graph refs, {len(paths)} paths, {len(texts)} texts")
        
        return {"evidence_pack": evidence_pack}
    
    def _extract_graph_refs(self, graph_sample: Dict[str, Any]) -> List[GraphNode]:
        """Extract graph node references"""
        graph_refs = []
        nodes = graph_sample.get("nodes", [])
        
        for node in nodes:
            graph_refs.append(GraphNode(
                node_id=str(node.get("id", "")),
                type=node.get("type", "Unknown"),
                props=node.get("properties", {})
            ))
        
        return graph_refs
    
    def _extract_paths(self, graph_sample: Dict[str, Any]) -> List[GraphPath]:
        """Extract graph paths"""
        paths = []
        edges = graph_sample.get("edges", [])
        
        for edge in edges:
            paths.append(GraphPath(
                from_node=str(edge.get("from", "")),
                rel=edge.get("rel", ""),
                to_node=str(edge.get("to", "")),
                properties=edge.get("properties", {})
            ))
        
        return paths
    
    async def _search_text_evidence(
        self,
        plan,
        causal_findings: List
    ) -> List[TextEvidence]:
        """Search for text evidence from vector store"""
        texts = []
        
        # Build search queries from plan and findings
        queries = []
        
        # Query from plan question
        queries.append(plan.question)
        
        # Query from focus areas
        for focus in plan.focus_areas:
            queries.append(f"student {focus} performance")
        
        # Query from causal findings
        for finding in causal_findings:
            if finding.confidence > 0.5:
                queries.append(finding.hypothesis)
        
        # Search for each query
        all_results = []
        for query in queries[:3]:  # Limit to 3 queries
            try:
                results = await self.vector_tool.search(query, top_k=5)
                all_results.extend(results)
            except Exception as e:
                self._log(f"Vector search error: {e}", "ERROR")
        
        # Convert to TextEvidence
        seen_ids = set()
        for result in all_results:
            source_id = result.get("id", "")
            if source_id and source_id not in seen_ids:
                seen_ids.add(source_id)
                texts.append(TextEvidence(
                    content=result.get("content", ""),
                    source_id=source_id,
                    score=result.get("score", 0.0),
                    metadata=result.get("metadata", {})
                ))
        
        return texts
    
    async def _rerank_text_evidence(
        self,
        texts: List[TextEvidence],
        query: str
    ) -> List[TextEvidence]:
        """Rerank text evidence by relevance"""
        if not texts:
            return texts
        
        # Convert to rerank format
        entries = [
            {
                "text": text.content,
                "meta": text.metadata,
                "score": text.score
            }
            for text in texts
        ]
        
        try:
            reranked = await self.rerank_tool.rerank(entries, query, top_k=min(10, len(entries)))
            
            # Map back to TextEvidence
            reranked_texts = []
            for entry in reranked:
                # Find matching text
                for text in texts:
                    if text.content == entry.get("text"):
                        text.score = entry.get("score", text.score)
                        reranked_texts.append(text)
                        break
            
            return reranked_texts
        except Exception as e:
            self._log(f"Rerank error: {e}", "ERROR")
            return texts













