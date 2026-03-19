"""
Graph Monitor
Monitor graph for conflicts, inconsistencies, and anomalies
"""

from typing import List, Dict, Any, Optional
from datetime import datetime
from enum import Enum
from dataclasses import dataclass, field
import logging

from app.core.graph.schema import (
    NodeType, RelationshipType, validate_node, validate_relationship
)

logger = logging.getLogger(__name__)


class ConflictType(str, Enum):
    """Types of graph conflicts"""
    DUPLICATE_NODE = "duplicate_node"
    INVALID_RELATIONSHIP = "invalid_relationship"
    MISSING_REQUIRED_PROPERTY = "missing_required_property"
    CIRCULAR_REFERENCE = "circular_reference"
    INCONSISTENT_DATA = "inconsistent_data"
    ORPHAN_NODE = "orphan_node"


@dataclass
class GraphConflict:
    """Represents a graph conflict"""
    conflict_type: ConflictType
    severity: str  # "low", "medium", "high", "critical"
    message: str
    node_id: Optional[str] = None
    relationship_id: Optional[str] = None
    details: Optional[Dict[str, Any]] = None
    timestamp: Optional[str] = None
    
    def __post_init__(self):
        if self.timestamp is None:
            self.timestamp = datetime.utcnow().isoformat()
        if self.details is None:
            self.details = {}


class GraphMonitor:
    """Monitor graph for conflicts and inconsistencies"""
    
    def __init__(self, log_level: str = "WARNING", enable_detection: bool = True):
        self.log_level = log_level
        self.enable_detection = enable_detection
        self.conflicts: List[GraphConflict] = []
        self._node_registry: Dict[str, Dict[str, Any]] = {}  # Track nodes by ID
        self._relationship_registry: List[Dict[str, Any]] = []  # Track relationships
    
    def check_node(self, node_type: NodeType, node_id: str, properties: Dict[str, Any]) -> List[GraphConflict]:
        """Check node for conflicts"""
        conflicts = []
        
        if not self.enable_detection:
            return conflicts
        
        # Validate against schema
        is_valid, errors = validate_node(node_type, properties)
        if not is_valid:
            for error in errors:
                conflict = GraphConflict(
                    conflict_type=ConflictType.MISSING_REQUIRED_PROPERTY,
                    severity="high",
                    message=f"Node validation failed: {error}",
                    node_id=node_id,
                    details={"node_type": node_type.value, "error": error}
                )
                conflicts.append(conflict)
        
        # Check for duplicates
        node_key = f"{node_type.value}:{node_id}"
        if node_key in self._node_registry:
            conflict = GraphConflict(
                conflict_type=ConflictType.DUPLICATE_NODE,
                severity="medium",
                message=f"Duplicate node detected: {node_key}",
                node_id=node_id,
                details={
                    "node_type": node_type.value,
                    "existing_properties": self._node_registry[node_key]
                }
            )
            conflicts.append(conflict)
        else:
            self._node_registry[node_key] = properties
        
        # Log conflicts
        for conflict in conflicts:
            self._log_conflict(conflict)
            self.conflicts.append(conflict)
        
        return conflicts
    
    def check_relationship(
        self,
        rel_type: RelationshipType,
        from_node_type: NodeType,
        from_node_id: str,
        to_node_type: NodeType,
        to_node_id: str,
        properties: Dict[str, Any] = None
    ) -> List[GraphConflict]:
        """Check relationship for conflicts"""
        conflicts = []
        
        if not self.enable_detection:
            return conflicts
        
        # Validate against schema
        is_valid, errors = validate_relationship(rel_type, from_node_type, to_node_type)
        if not is_valid:
            for error in errors:
                conflict = GraphConflict(
                    conflict_type=ConflictType.INVALID_RELATIONSHIP,
                    severity="high",
                    message=f"Relationship validation failed: {error}",
                    relationship_id=f"{from_node_id}->{to_node_id}",
                    details={
                        "relationship_type": rel_type.value,
                        "from": from_node_type.value,
                        "to": to_node_type.value,
                        "error": error
                    }
                )
                conflicts.append(conflict)
        
        # Check for circular references - only block if same node type
        # Different node types with same ID are OK (e.g., QuizAttempt:5 and Quiz:5 are different entities)
        if from_node_id == to_node_id and from_node_type == to_node_type:
            conflict = GraphConflict(
                conflict_type=ConflictType.CIRCULAR_REFERENCE,
                severity="high",
                message=f"Circular reference detected: {from_node_type.value} {from_node_id} -> {to_node_type.value} {to_node_id}. Same node type cannot reference itself.",
                relationship_id=f"{from_node_id}->{to_node_id}",
                details={
                    "relationship_type": rel_type.value,
                    "from_node_type": from_node_type.value,
                    "to_node_type": to_node_type.value,
                    "node_id": from_node_id
                }
            )
            conflicts.append(conflict)
            
            return conflicts
        
        # Register relationship
        rel_key = f"{rel_type.value}:{from_node_id}->{to_node_id}"
        self._relationship_registry.append({
            "key": rel_key,
            "type": rel_type,
            "from": from_node_id,
            "to": to_node_id,
            "properties": properties or {}
        })
        
        # Log conflicts
        for conflict in conflicts:
            self._log_conflict(conflict)
            self.conflicts.append(conflict)
        
        return conflicts
    
    def check_orphan_nodes(self, all_node_ids: set) -> List[GraphConflict]:
        """Check for orphan nodes (nodes with no relationships)"""
        conflicts = []
        
        if not self.enable_detection:
            return conflicts
        
        # Get all nodes involved in relationships
        connected_nodes = set()
        for rel in self._relationship_registry:
            connected_nodes.add(rel["from"])
            connected_nodes.add(rel["to"])
        
        # Extract node IDs from format "Type:ID" or just "ID"
        # all_node_ids can be in format "Student:uuid" or just "uuid"
        all_node_id_values = set()
        node_id_to_type = {}  # Map ID -> Type for logging
        
        for node_key in all_node_ids:
            if ":" in node_key:
                # Format: "Type:ID"
                node_type_str, node_id = node_key.split(":", 1)
                all_node_id_values.add(node_id)
                node_id_to_type[node_id] = node_type_str
            else:
                # Format: just "ID"
                all_node_id_values.add(node_key)
                # Try to get type from registry
                node_type = self._get_node_type(node_key)
                if node_type:
                    node_id_to_type[node_key] = node_type.value
        
        # Find orphan nodes (compare IDs only, not type prefix)
        orphan_node_ids = all_node_id_values - connected_nodes
        
        for node_id in orphan_node_ids:
            # Some nodes are expected to be orphans (e.g., root nodes)
            # Only flag if it's not a Classroom or Curriculum
            node_type_str = node_id_to_type.get(node_id, "unknown")
            try:
                node_type = NodeType(node_type_str)
            except ValueError:
                # Try to get from registry
                node_type = self._get_node_type(node_id)
            
            if node_type not in [NodeType.CLASSROOM, NodeType.CURRICULUM]:
                # Format message with type prefix if available
                display_id = f"{node_type_str}:{node_id}" if node_type_str != "unknown" else node_id
                conflict = GraphConflict(
                    conflict_type=ConflictType.ORPHAN_NODE,
                    severity="low",
                    message=f"Orphan node detected: {display_id}",
                    node_id=node_id,
                    details={"node_type": node_type.value if node_type else node_type_str}
                )
                conflicts.append(conflict)
                self._log_conflict(conflict)
        
        return conflicts
    
    def _get_node_type(self, node_id: str) -> Optional[NodeType]:
        """Get node type from registry"""
        for key, props in self._node_registry.items():
            if node_id in key:
                # Extract node type from key
                node_type_str = key.split(":")[0]
                try:
                    return NodeType(node_type_str)
                except ValueError:
                    return None
        return None
    
    def _log_conflict(self, conflict: GraphConflict):
        """Log conflict based on log level"""
        log_method = {
            "INFO": logger.info,
            "WARNING": logger.warning,
            "ERROR": logger.error,
            "CRITICAL": logger.critical
        }.get(self.log_level, logger.warning)
        
        log_method(
            f"Graph Conflict [{conflict.severity.upper()}]: {conflict.conflict_type.value} - {conflict.message}",
            extra={
                "conflict_type": conflict.conflict_type.value,
                "severity": conflict.severity,
                "node_id": conflict.node_id,
                "relationship_id": conflict.relationship_id,
                "details": conflict.details
            }
        )
    
    def get_conflicts_summary(self) -> Dict[str, Any]:
        """Get summary of all conflicts"""
        by_type = {}
        by_severity = {"low": 0, "medium": 0, "high": 0, "critical": 0}
        
        for conflict in self.conflicts:
            by_type[conflict.conflict_type.value] = by_type.get(conflict.conflict_type.value, 0) + 1
            by_severity[conflict.severity] = by_severity.get(conflict.severity, 0) + 1
        
        return {
            "total_conflicts": len(self.conflicts),
            "by_type": by_type,
            "by_severity": by_severity,
            "conflicts": [
                {
                    "type": c.conflict_type.value,
                    "severity": c.severity,
                    "message": c.message,
                    "timestamp": c.timestamp
                }
                for c in self.conflicts
            ]
        }
    
    def clear(self):
        """Clear all conflicts and registries"""
        self.conflicts.clear()
        self._node_registry.clear()
        self._relationship_registry.clear()

