"""
Graph Client
Neo4j client for graph operations
"""

from typing import List, Dict, Any, Optional
from neo4j import AsyncGraphDatabase
import logging

from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class GraphClient:
    """Neo4j graph client"""
    
    def __init__(self):
        neo4j_uri = settings.get_neo4j_uri()
        neo4j_username = settings.get_neo4j_username()
        neo4j_password = settings.get_neo4j_password()
        self.database = settings.get_neo4j_database()
        
        self.driver = AsyncGraphDatabase.driver(
            neo4j_uri,
            auth=(neo4j_username, neo4j_password)
        )
        
        environment = "production" if settings.is_production() else "local"
        logger.info(f"Connected to Neo4j ({environment}) at {neo4j_uri} (database: {self.database})")
    
    async def close(self):
        """Close database connection"""
        await self.driver.close()
        logger.info("Closed Neo4j connection")
    
    async def create_node(
        self,
        node_type: str,
        node_id: str,
        properties: Dict[str, Any]
    ) -> bool:
        """Create a single node"""
        try:
            async with self.driver.session(database=self.database) as session:
                query = f"""
                MERGE (n:{node_type} {{id: $id}})
                SET n += $properties
                RETURN n
                """
                result = await session.run(
                    query,
                    id=node_id,
                    properties=properties
                )
                await result.consume()
                return True
        except Exception as e:
            logger.error(f"Error creating node {node_type}:{node_id}: {e}")
            return False
    
    async def create_relationship(
        self,
        from_node_type: str,
        from_node_id: str,
        rel_type: str,
        to_node_type: str,
        to_node_id: str,
        properties: Dict[str, Any] = None
    ) -> bool:
        """Create a relationship between nodes"""
        try:
            async with self.driver.session(database=self.database) as session:
                query = f"""
                MATCH (from:{from_node_type} {{id: $from_id}})
                MATCH (to:{to_node_type} {{id: $to_id}})
                MERGE (from)-[r:{rel_type}]->(to)
                SET r += $properties
                RETURN r
                """
                result = await session.run(
                    query,
                    from_id=from_node_id,
                    to_id=to_node_id,
                    properties=properties or {}
                )
                await result.consume()
                return True
        except Exception as e:
            logger.error(
                f"Error creating relationship {from_node_type}:{from_node_id} "
                f"-[{rel_type}]-> {to_node_type}:{to_node_id}: {e}"
            )
            return False
    
    async def traverse_graph(
        self,
        start_node_type: str,
        start_node_id: str,
        relationship_types: List[str],
        max_depth: int = 3,
        limit: int = 100,
        bidirectional: bool = False
    ) -> List[Dict[str, Any]]:
        """
        Traverse graph from a starting node
        
        Args:
            start_node_type: Type of starting node
            start_node_id: ID of starting node
            relationship_types: List of relationship types to traverse
            max_depth: Maximum traversal depth
            limit: Maximum number of paths to return
            bidirectional: If True, traverse both directions (useful for Topic nodes)
        """
        try:
            async with self.driver.session(database=self.database) as session:
                # Build relationship pattern
                rel_pattern = "|".join(relationship_types)
                
                if bidirectional:
                    # Traverse both directions: outgoing and incoming
                    query = f"""
                    MATCH path = (start:{start_node_type} {{id: $start_id}})
                    -[:{rel_pattern}*1..{max_depth}]-(end)
                    RETURN nodes(path) as nodes, relationships(path) as rels,
                           [node in nodes(path) | labels(node)] as node_labels
                    LIMIT $limit
                    """
                else:
                    # Traverse only outgoing (default)
                    query = f"""
                    MATCH path = (start:{start_node_type} {{id: $start_id}})
                    -[:{rel_pattern}*1..{max_depth}]->(end)
                    RETURN nodes(path) as nodes, relationships(path) as rels,
                           [node in nodes(path) | labels(node)] as node_labels
                    LIMIT $limit
                    """
                
                result = await session.run(
                    query,
                    start_id=start_node_id,
                    limit=limit
                )
                
                paths = []
                async for record in result:
                    nodes = record.get("nodes", [])
                    node_labels = record.get("node_labels", [])
                    
                    # Create nodes with labels
                    nodes_with_labels = []
                    for i, node in enumerate(nodes):
                        node_dict = dict(node)
                        # Add labels to node dict for easier access
                        labels = node_labels[i] if i < len(node_labels) else []
                        node_dict["_labels"] = labels
                        nodes_with_labels.append(node_dict)
                    
                    paths.append({
                        "nodes": nodes_with_labels,
                        "relationships": [dict(rel) for rel in record.get("rels", [])]
                    })
                
                return paths
        except Exception as e:
            logger.error(f"Error traversing graph: {e}")
            return []
    
    async def query_cypher(self, query: str, parameters: Dict[str, Any] = None) -> List[Dict[str, Any]]:
        """Execute a custom Cypher query"""
        try:
            async with self.driver.session(database=self.database) as session:
                result = await session.run(query, parameters or {})
                records = []
                async for record in result:
                    records.append(dict(record))
                return records
        except Exception as e:
            logger.error(f"Error executing Cypher query: {e}")
            return []
    
    async def clear_graph(self, confirm: bool = False):
        """Clear all nodes and relationships (use with caution!)"""
        if not confirm:
            logger.warning("clear_graph called without confirmation")
            return
        
        try:
            async with self.driver.session(database=self.database) as session:
                await session.run("MATCH (n) DETACH DELETE n")
                logger.info("Graph cleared")
        except Exception as e:
            logger.error(f"Error clearing graph: {e}")

