"""
Graph Module
Knowledge graph building and management
"""

# Import order matters - avoid circular imports
from app.core.graph.client import GraphClient
from app.core.graph.monitor import GraphMonitor, ConflictType
from app.core.graph.schema import NodeType, RelationshipType
from app.core.graph.retriever import GraphRetriever
from app.core.graph.entity_extractor import EntityExtractor

# GraphBuilder imported separately to avoid circular import
# from app.core.graph.builder import GraphBuilder

__all__ = [
    "GraphClient",
    "GraphMonitor",
    "ConflictType",
    "NodeType",
    "RelationshipType",
    "GraphRetriever",
    "EntityExtractor"
]

