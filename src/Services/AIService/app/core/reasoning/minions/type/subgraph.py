"""
Subgraph Minion
Generates Cypher queries and retrieves relevant subgraph
"""

from typing import Dict, Any, List, Set
import logging

from app.core.reasoning.minions.type.base import BaseMinion
from app.core.reasoning.models import ReasoningPlan, Entity, Constraint, GraphNode, EntityType

logger = logging.getLogger(__name__)


class SubgraphMinion(BaseMinion):
    """Subgraph minion: generates Cypher queries and retrieves subgraph"""
    
    @property
    def name(self) -> str:
        return "Subgraph"
    
    async def execute(self, context: Dict[str, Any]) -> Dict[str, Any]:
        """Generate Cypher queries and fetch subgraph"""
        plan: ReasoningPlan = context.get("plan")
        if not plan:
            self._log("No plan found in context", "WARNING")
            return {"cypher": [], "graph_sample": {"nodes": [], "edges": []}}
        
        self._log(f"Expanding subgraph for {len(plan.entities)} entities")
        
        # Resolve entity IDs first
        resolved_entities = await self._resolve_entities(plan.entities)
        
        # Generate Cypher queries
        cypher_queries = self._generate_cypher_queries(plan, resolved_entities)
        
        # Execute queries and collect results
        all_nodes: Dict[str, Dict[str, Any]] = {}
        all_edges: List[Dict[str, Any]] = []
        cypher_results = []
        
        for cypher_query in cypher_queries:
            self._log(f"Executing query: {cypher_query[:100]}...")
            try:
                results = await self.graph_tool.query(cypher_query)
                cypher_results.append({
                    "query": cypher_query,
                    "result_count": len(results)
                })
                
                # Extract nodes and edges from results
                for record in results:
                    self._extract_graph_elements(record, all_nodes, all_edges)
            except Exception as e:
                self._log(f"Query execution error: {e}", "ERROR")
                cypher_results.append({
                    "query": cypher_query,
                    "error": str(e)
                })
        
        # Convert to graph_sample format
        graph_sample = {
            "nodes": [self._node_to_dict(node_id, node_data) for node_id, node_data in all_nodes.items()],
            "edges": all_edges
        }
        
        self._log(f"Retrieved {len(graph_sample['nodes'])} nodes, {len(graph_sample['edges'])} edges")
        
        return {
            "cypher": [cq["query"] for cq in cypher_results],
            "cypher_results": cypher_results,
            "graph_sample": graph_sample,
            "resolved_entities": resolved_entities
        }
    
    async def _resolve_entities(self, entities: List[Entity]) -> List[Entity]:
        """Resolve entity identifiers to actual node IDs"""
        resolved = []
        
        for entity in entities:
            # Try to resolve entity ID
            resolved_id = await self._resolve_entity_id(entity.type, entity.identifier)
            if resolved_id:
                entity.resolved_id = resolved_id
                resolved.append(entity)
            else:
                self._log(f"Could not resolve {entity.type.value}:{entity.identifier}", "WARNING")
        
        return resolved
    
    async def _resolve_entity_id(self, entity_type: EntityType, identifier: str) -> str:
        """Resolve entity identifier to node ID"""
        # Check if identifier is already a valid ID
        if self._is_valid_id(identifier):
            # Verify node exists
            cypher = f"MATCH (n:{entity_type.value} {{id: $id}}) RETURN n.id as id LIMIT 1"
            results = await self.graph_tool.query(cypher, {"id": identifier})
            if results:
                return identifier
        
        # Try to lookup by name
        if entity_type == EntityType.TOPIC:
            cypher = """
            MATCH (t:Topic)
            WHERE toLower(t.name) CONTAINS toLower($name) OR toLower(t.name) = toLower($name)
            RETURN t.id as id, t.name as name
            LIMIT 1
            """
        elif entity_type == EntityType.CLASSROOM:
            cypher = """
            MATCH (c:Classroom)
            WHERE toLower(c.name) CONTAINS toLower($name) OR toLower(toString(c.id)) = toLower($name)
            RETURN c.id as id
            LIMIT 1
            """
        elif entity_type == EntityType.STUDENT:
            cypher = """
            MATCH (s:Student)
            WHERE toLower(s.name) CONTAINS toLower($name) OR 
                  toLower(s.email) CONTAINS toLower($name) OR
                  toLower(toString(s.id)) = toLower($name)
            RETURN s.id as id
            LIMIT 1
            """
        else:
            return None
        
        results = await self.graph_tool.query(cypher, {"name": identifier})
        if results:
            return str(results[0].get("id", ""))
        
        return None
    
    def _is_valid_id(self, identifier: str) -> bool:
        """Check if identifier looks like a valid ID"""
        import re
        # Numeric ID
        if identifier.isdigit():
            return True
        # UUID-like
        uuid_pattern = r'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
        if re.match(uuid_pattern, identifier.lower()):
            return True
        return False
    
    def _generate_cypher_queries(self, plan: ReasoningPlan, resolved_entities: List[Entity]) -> List[str]:
        """Generate minimal Cypher queries for subgraph expansion"""
        queries = []
        max_hops = plan.max_hops
        
        if not resolved_entities:
            # Fallback: query all relevant nodes
            queries.append(self._generate_fallback_query(plan))
            return queries
        
        # Generate query for each entity
        for entity in resolved_entities:
            if not entity.resolved_id:
                continue
            
            # Build relationship pattern based on entity type
            rel_pattern = self._get_relationship_pattern(entity.type)
            
            # Generate query with constraints
            query = f"""
            MATCH path = (start:{entity.type.value} {{id: $entity_id}})
            -[:{rel_pattern}*1..{max_hops}]-(connected)
            WHERE connected IS NOT NULL
            """
            
            # Add constraints
            where_clauses = []
            for constraint in plan.constraints:
                if constraint.type == "time_range":
                    where_clauses.append(
                        f"(connected.started_at >= datetime($start_time) AND connected.started_at <= datetime($end_time)) OR "
                        f"(connected.completed_at >= datetime($start_time) AND connected.completed_at <= datetime($end_time))"
                    )
                elif constraint.type == "threshold" and constraint.field == "score":
                    op = constraint.operator or ">="
                    where_clauses.append(f"connected.score {op} {constraint.value}")
            
            if where_clauses:
                query += " AND (" + " OR ".join(where_clauses) + ")"
            
            query += """
            RETURN DISTINCT
                start as start_node,
                connected as connected_node,
                relationships(path) as rels,
                nodes(path) as path_nodes
            LIMIT 100
            """
            
            # Build parameters
            params = {"entity_id": entity.resolved_id}
            if plan.constraints:
                time_constraint = next((c for c in plan.constraints if c.type == "time_range"), None)
                if time_constraint:
                    params["start_time"] = time_constraint.value.get("start", "")
                    params["end_time"] = time_constraint.value.get("end", "")
            
            queries.append(query)
        
        return queries
    
    def _get_relationship_pattern(self, entity_type: EntityType) -> str:
        """Get relationship pattern for entity type"""
        patterns = {
            EntityType.STUDENT: "ENROLLED_IN|HAS_QUIZ|HAS_ASSIGNMENT|ATTEMPTED|SUBMITTED|STRUGGLES_WITH|EXCELS_AT",
            EntityType.CLASSROOM: "ENROLLED_IN",
            EntityType.TOPIC: "HAS_TOPIC|COVERS|STRUGGLES_WITH|EXCELS_AT",
            EntityType.QUIZ: "HAS_TOPIC|ATTEMPTED_FOR|FOR_QUIZ|HAS_ATTEMPT",
            EntityType.ASSIGNMENT: "HAS_TOPIC|SUBMITTED_FOR|FOR_ASSIGNMENT|HAS_ATTEMPT",
        }
        return patterns.get(entity_type, "ENROLLED_IN|HAS_QUIZ|HAS_ASSIGNMENT|HAS_TOPIC")
    
    def _generate_fallback_query(self, plan: ReasoningPlan) -> str:
        """Generate fallback query when no entities are resolved"""
        return """
        MATCH (n)
        WHERE n:Student OR n:Classroom OR n:Topic OR n:Quiz OR n:Assignment
        RETURN n, labels(n) as labels
        LIMIT 50
        """
    
    def _extract_graph_elements(
        self,
        record: Dict[str, Any],
        all_nodes: Dict[str, Dict[str, Any]],
        all_edges: List[Dict[str, Any]]
    ):
        """Extract nodes and edges from query result record"""
        # Extract start_node
        if "start_node" in record:
            node = record["start_node"]
            node_id = node.get("id") if isinstance(node, dict) else getattr(node, "id", None)
            if node_id:
                all_nodes[str(node_id)] = self._normalize_node(node)
        
        # Extract connected_node
        if "connected_node" in record:
            node = record["connected_node"]
            node_id = node.get("id") if isinstance(node, dict) else getattr(node, "id", None)
            if node_id:
                all_nodes[str(node_id)] = self._normalize_node(node)
        
        # Extract path_nodes
        if "path_nodes" in record:
            for node in record["path_nodes"]:
                node_id = node.get("id") if isinstance(node, dict) else getattr(node, "id", None)
                if node_id:
                    all_nodes[str(node_id)] = self._normalize_node(node)
        
        # Extract relationships
        if "rels" in record:
            for rel in record["rels"]:
                if isinstance(rel, dict):
                    rel_type = rel.get("type", "")
                    from_id = rel.get("from", {}).get("id") if isinstance(rel.get("from"), dict) else None
                    to_id = rel.get("to", {}).get("id") if isinstance(rel.get("to"), dict) else None
                else:
                    rel_type = rel.type if hasattr(rel, "type") else ""
                    from_id = rel.start_node.get("id") if hasattr(rel, "start_node") else None
                    to_id = rel.end_node.get("id") if hasattr(rel, "end_node") else None
                
                if from_id and to_id and rel_type:
                    edge = {
                        "from": str(from_id),
                        "rel": rel_type,
                        "to": str(to_id),
                        "properties": dict(rel) if isinstance(rel, dict) else {}
                    }
                    # Avoid duplicates
                    if edge not in all_edges:
                        all_edges.append(edge)
    
    def _normalize_node(self, node: Any) -> Dict[str, Any]:
        """Normalize node to dictionary"""
        if isinstance(node, dict):
            return node
        if hasattr(node, "properties"):
            return dict(node.properties)
        if hasattr(node, "items"):
            return dict(node)
        return {}
    
    def _node_to_dict(self, node_id: str, node_data: Dict[str, Any]) -> Dict[str, Any]:
        """Convert node to dictionary format"""
        # Extract labels if available
        labels = node_data.pop("_labels", [])
        node_type = labels[0] if labels else "Unknown"
        
        return {
            "id": node_id,
            "type": node_type,
            "properties": node_data
        }













