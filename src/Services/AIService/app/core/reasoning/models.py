"""
Reasoning Engine Models
Type definitions for the reasoning engine output contract
"""

from typing import List, Dict, Any, Optional
from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum


class EntityType(str, Enum):
    """Entity types in the graph"""
    CLASSROOM = "Classroom"
    STUDENT = "Student"
    QUIZ = "Quiz"
    ASSIGNMENT = "Assignment"
    ATTEMPT = "Attempt"
    QUESTION = "Question"
    TOPIC = "Topic"


@dataclass
class Entity:
    """Extracted entity from question"""
    type: EntityType
    identifier: str
    resolved_id: Optional[str] = None
    properties: Dict[str, Any] = field(default_factory=dict)


@dataclass
class Constraint:
    """Temporal or threshold constraint"""
    type: str  # "time_range", "threshold", "comparison"
    field: str
    value: Any
    operator: Optional[str] = None  # ">", "<", ">=", "<=", "="


@dataclass
class ReasoningPlan:
    """Plan for reasoning execution"""
    question: str
    entities: List[Entity] = field(default_factory=list)
    constraints: List[Constraint] = field(default_factory=list)
    max_hops: int = 3
    focus_areas: List[str] = field(default_factory=list)
    strategy: str = "causal_analysis"


@dataclass
class GraphNode:
    """Graph node reference"""
    node_id: str
    type: str
    props: Dict[str, Any] = field(default_factory=dict)


@dataclass
class GraphPath:
    """Graph path between nodes"""
    from_node: str
    rel: str
    to_node: str
    properties: Dict[str, Any] = field(default_factory=dict)


@dataclass
class TextEvidence:
    """Text evidence from vector search"""
    content: str
    source_id: str
    score: float
    metadata: Dict[str, Any] = field(default_factory=dict)


@dataclass
class CausalFinding:
    """Causal hypothesis with evidence"""
    hypothesis: str
    support: List[str] = field(default_factory=list)  # Evidence IDs supporting
    counter: List[str] = field(default_factory=list)  # Evidence IDs contradicting
    confidence: float = 0.0  # 0-1
    temporal_precedence: bool = False
    correlation_strength: float = 0.0


@dataclass
class EvidencePack:
    """Evidence package with graph and text references"""
    graph_refs: List[GraphNode] = field(default_factory=list)
    paths: List[GraphPath] = field(default_factory=list)
    texts: List[TextEvidence] = field(default_factory=list)


@dataclass
class ReasoningResult:
    """Complete reasoning result following the output contract"""
    plan: str
    cypher: List[str] = field(default_factory=list)
    graph_sample: Dict[str, List[Dict[str, Any]]] = field(default_factory=dict)
    causal_findings: List[CausalFinding] = field(default_factory=list)
    evidence_pack: EvidencePack = field(default_factory=EvidencePack)
    answer_teacher_friendly: str = ""
    next_actions: List[str] = field(default_factory=list)
    audit: Dict[str, Any] = field(default_factory=dict)
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization"""
        return {
            "plan": self.plan,
            "cypher": self.cypher,
            "graph_sample": self.graph_sample,
            "causal_findings": [
                {
                    "hypothesis": f.hypothesis,
                    "support": f.support,
                    "counter": f.counter,
                    "confidence": f.confidence,
                    "temporal_precedence": f.temporal_precedence,
                    "correlation_strength": f.correlation_strength
                }
                for f in self.causal_findings
            ],
            "evidence_pack": {
                "graph_refs": [
                    {
                        "node_id": ref.node_id,
                        "type": ref.type,
                        "props": ref.props
                    }
                    for ref in self.evidence_pack.graph_refs
                ],
                "paths": [
                    {
                        "from": path.from_node,
                        "rel": path.rel,
                        "to": path.to_node,
                        "properties": path.properties
                    }
                    for path in self.evidence_pack.paths
                ],
                "texts": [
                    {
                        "content": text.content,
                        "source_id": text.source_id,
                        "score": text.score,
                        "metadata": text.metadata
                    }
                    for text in self.evidence_pack.texts
                ]
            },
            "answer_teacher_friendly": self.answer_teacher_friendly,
            "next_actions": self.next_actions,
            "audit": self.audit
        }













