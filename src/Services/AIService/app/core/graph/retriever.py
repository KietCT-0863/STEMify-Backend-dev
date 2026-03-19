"""
Graph Retriever
Retrieval from knowledge graph (Neo4j) with entity extraction and traversal
"""

from typing import List, Dict, Any, Optional, Set
import logging
import re

from app.core.graph.client import GraphClient
from app.core.graph.entity_extractor import EntityExtractor
from app.infrastructure.config.settings import settings

logger = logging.getLogger(__name__)


class GraphRetriever:
    """
    Graph-based retriever using Neo4j
    
    Responsibilities:
    - Extract entities from query
    - Traverse graph from entities
    - Format graph results with provenance
    """
    
    def __init__(self, graph_client: GraphClient):
        self.graph_client = graph_client
        self.entity_extractor = EntityExtractor()
    
    async def retrieve(
        self,
        query: str,
        max_depth: int = None,
        limit: int = 100,
        relationship_types: Optional[List[str]] = None
    ) -> List[Dict[str, Any]]:
        """
        Retrieve documents from knowledge graph
        
        Args:
            query: Natural language query
            max_depth: Maximum traversal depth (default from settings)
            limit: Maximum number of paths to return
            relationship_types: Specific relationship types to traverse
        
        Returns:
            List of retrieved documents from graph traversal
        """
        if max_depth is None:
            max_depth = settings.GRAPH_TRAVERSAL_DEPTH
        
        if relationship_types is None:
            # Default: traverse all relevant relationships (only those that exist in graph)
            # Note: COVERS and RELATED_TO may not exist in all graphs, so we use only confirmed relationships
            relationship_types = [
                "ENROLLED_IN",  # Student -> Classroom (or Classroom <- Student)
                "HAS_QUIZ",  # Student -> StudentQuiz
                "HAS_ASSIGNMENT",  # Student -> StudentAssignment
                "FOR_QUIZ",  # StudentQuiz -> Quiz
                "FOR_ASSIGNMENT",  # StudentAssignment -> Assignment
                "HAS_ATTEMPT",  # StudentQuiz -> QuizAttempt, StudentAssignment -> AssignmentAttempt
                "ATTEMPTED",  # Student -> QuizAttempt
                "SUBMITTED",  # Student -> AssignmentAttempt
                "ATTEMPTED_FOR",  # QuizAttempt -> Quiz
                "SUBMITTED_FOR",  # AssignmentAttempt -> Assignment
                "HAS_TOPIC"  # Quiz/Assignment -> Topic (incoming to Topic, outgoing from Quiz/Assignment)
                # Note: COVERS and RELATED_TO are optional and may not exist
            ]
        
        logger.info(f"Graph retrieval: query='{query[:50]}...', max_depth={max_depth}")
        
        try:
            # Step 1: Detect intent
            intent = self.entity_extractor.detect_intent(query)
            
            # Step 2: Extract entities from query
            entities = self.entity_extractor.extract_entities(query)
            
            # Step 3: If intent detected but no entities, use intent-based query
            if intent and not entities:
                logger.info(f"Using intent-based retrieval for intent: {intent}")
                # Resolve classroom_id with fallback strategy
                classroom_id = await self._resolve_classroom_id_with_fallback(query)
                
                return await self._intent_based_retrieval(query, intent, limit, classroom_id)
            
            # Step 4: If no entities and no intent, use keyword search
            if not entities:
                logger.warning("No entities extracted from query, using keyword search")
                return await self._keyword_search(query, max_depth, limit)
            
            # Step 5: Resolve entity IDs (lookup from graph if entity is a name)
            resolved_entities = []
            for entity_type, entity_identifier in entities:
                # Try to resolve entity identifier to actual node ID
                resolved_id = await self._resolve_entity_id(entity_type, entity_identifier)
                if resolved_id:
                    resolved_entities.append((entity_type, resolved_id))
                else:
                    logger.warning(f"Could not resolve {entity_type} entity: {entity_identifier}")
            
            if not resolved_entities:
                logger.warning("No entities could be resolved, falling back to keyword search")
                return await self._keyword_search(query, max_depth, limit)
            
            # Step 6: Traverse graph from resolved entities
            results = []
            for entity_type, entity_id in resolved_entities:
                # Use bidirectional traversal for nodes that primarily have incoming relationships:
                # - Topic: Quiz/Assignment -[HAS_TOPIC]-> Topic (incoming)
                # - Classroom: Student -[ENROLLED_IN]-> Classroom (incoming)
                bidirectional = (entity_type in ["Topic", "Classroom"])
                
                paths = await self.graph_client.traverse_graph(
                    start_node_type=entity_type,
                    start_node_id=entity_id,
                    relationship_types=relationship_types,
                    max_depth=max_depth,
                    limit=limit,
                    bidirectional=bidirectional
                )
                
                # Step 7: Convert paths to documents
                documents = self._paths_to_documents(paths, query, entity_type, entity_id)
                results.extend(documents)
            
            # Step 8: Format results with provenance
            formatted_results = self._format_results(results, query)
            
            logger.info(f"Retrieved {len(formatted_results)} documents from graph")
            return formatted_results
            
        except Exception as e:
            logger.error(f"Error in graph retrieval: {e}", exc_info=True)
            return []
    
    async def _resolve_classroom_id_with_fallback(self, query: str) -> Optional[str]:
        """
        Resolve classroom_id from query with configurable fallback strategy
        
        
        Args:
            query: Natural language query
            
        Returns:
            classroom_id or None based on fallback strategy
        """
        # Step 1: Try to extract classroom identifier from query
        classroom_pattern = r'(?:classroom|class)\s+([A-Za-z0-9_-]+)'
        classroom_match = re.search(classroom_pattern, query, re.IGNORECASE)
        
        if classroom_match:
            classroom_identifier = classroom_match.group(1)
            # Filter out common words
            common_words = {'performance', 'status', 'info', 'details', 'summary', 'report', 
                           'data', 'information', 'overview', 'statistics', 'metrics'}
            if classroom_identifier.lower() not in common_words:
                resolved_id = await self._resolve_entity_id("Classroom", classroom_identifier)
                if resolved_id:
                    logger.info(f"Resolved classroom_id from query: {resolved_id}")
                    return resolved_id
        
        # Step 2: Apply fallback strategy from configuration
        fallback_strategy = settings.INTENT_RETRIEVAL_FALLBACK_STRATEGY
        default_classroom_id = settings.INTENT_RETRIEVAL_DEFAULT_CLASSROOM_ID
        
        if fallback_strategy == "require_classroom":
            logger.warning("No classroom_id found in query and strategy requires it. Returning None.")
            return None
        
        elif fallback_strategy == "first_classroom":
            # Get first available classroom from graph
            first_classroom = await self._get_first_classroom()
            if first_classroom:
                logger.info(f"Using first available classroom as fallback: {first_classroom}")
                return first_classroom
            elif default_classroom_id:
                logger.info(f"Using configured default classroom: {default_classroom_id}")
                return default_classroom_id
            else:
                logger.warning("No classroom available for fallback. Returning None.")
                return None
        
        elif fallback_strategy == "all_classrooms" or fallback_strategy == "default":
            # Query all classrooms (classroom_id = None)
            if default_classroom_id:
                logger.info(f"Using configured default classroom: {default_classroom_id}")
                return default_classroom_id
            else:
                logger.info("No classroom_id specified, querying all classrooms")
                return None
        
        else:
            # Unknown strategy, default to None (query all)
            logger.warning(f"Unknown fallback strategy: {fallback_strategy}. Defaulting to None (all classrooms)")
            return None
    
    async def _get_first_classroom(self) -> Optional[str]:
        """
        Get the first available classroom ID from graph
        
        Returns:
            First classroom ID or None if no classrooms exist
        """
        try:
            query = "MATCH (c:Classroom) RETURN c.id as classroom_id ORDER BY c.id LIMIT 1"
            results = await self.graph_client.query_cypher(query)
            if results and results[0].get("classroom_id"):
                return str(results[0]["classroom_id"])
            return None
        except Exception as e:
            logger.error(f"Error getting first classroom: {e}")
            return None
    
    async def _intent_based_retrieval(
        self,
        query: str,
        intent: str,
        limit: int,
        classroom_id: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        """
        Retrieve documents based on detected intent
        
        Uses Cypher queries optimized for specific intents
        
        Args:
            query: Original query text
            intent: Detected intent (struggling, need_help, etc.)
            limit: Maximum number of results
            classroom_id: Optional classroom ID if mentioned in query
        """
        # Get intent-based Cypher query
        cypher_query = self.entity_extractor.get_intent_cypher_query(intent, classroom_id)
        
        if not cypher_query:
            logger.warning(f"No Cypher query for intent: {intent}")
            return []
        
        logger.info(f"Executing intent-based query for '{intent}' with classroom_id: {classroom_id}")
        logger.debug(f"Cypher query: {cypher_query[:200]}...")  # Log first 200 chars
        
        try:
            # Execute Cypher query
            parameters = {}
            if classroom_id:
                parameters["classroom_id"] = classroom_id
            
            results = await self.graph_client.query_cypher(cypher_query, parameters)
            logger.info(f"Intent-based query returned {len(results)} records")
            
            # Convert to documents
            documents = []
            for record in results:
                # Build document from query results
                doc = self._intent_result_to_document(record, intent, query)
                if doc:
                    documents.append(doc)
            
            logger.info(f"Intent-based retrieval: {len(documents)} documents for intent '{intent}'")
            return documents
            
        except Exception as e:
            logger.error(f"Error in intent-based retrieval: {e}", exc_info=True)
            return []
    
    def _node_to_dict(self, node: Any) -> Dict[str, Any]:
        """
        Convert Neo4j node object to dictionary
        
        Best Practice: Centralized node conversion logic
        """
        if node is None:
            return {}
        
        if isinstance(node, dict):
            return node
        
        if hasattr(node, 'properties'):
            return dict(node.properties)
        
        if hasattr(node, 'items'):
            return dict(node)
        
        return {}
    
    def _intent_result_to_document(
        self,
        record: Dict[str, Any],
        intent: str,
        query: str
    ) -> Optional[Dict[str, Any]]:
        """Convert intent-based query result to document format"""
        try:
            if intent == "struggling":
                student = self._node_to_dict(record.get("s"))
                topic = self._node_to_dict(record.get("t"))
                student_score = record.get("student_score", 0)
                topic_name = record.get("topic_name", topic.get("name", topic.get("id", "unknown")))
                
                content = f"Student {student.get('id', 'unknown')} is struggling with topic {topic_name}. "
                content += f"Average score: {student_score:.2f}. Topic: {topic_name}"
                
                return {
                    "document_id": f"struggling_{student.get('id')}_{topic.get('id', 'unknown')}",
                    "content": content,
                    "metadata": {
                        "document_type": "struggling_student",
                        "student_id": student.get("id"),
                        "topic_id": topic.get("id"),
                        "topic_name": topic_name,
                        "student_score": float(student_score),
                        "intent": intent
                    },
                    "retrieval_source": "graph",
                    "retrieval_score": 0.8,
                    "retrieval_query": query,
                    "confidence_score": 0.8,
                    "provenance": {
                        "retrieval_method": "graph_intent_based",
                        "intent": intent,
                        "retrieval_timestamp": self._get_timestamp()
                    }
                }
            
            elif intent == "need_help":
                student = self._node_to_dict(record.get("s"))
                student_score = record.get("student_score", 0)
                completion_rate = record.get("completion_rate", 0)
                
                content = f"Student {student.get('id', 'unknown')} needs extra help. "
                content += f"Average score: {student_score:.2f}, Completion rate: {completion_rate:.2f}"
                
                return {
                    "document_id": f"need_help_{student.get('id')}",
                    "content": content,
                    "metadata": {
                        "document_type": "student_needs_help",
                        "student_id": student.get("id"),
                        "student_score": float(student_score),
                        "completion_rate": float(completion_rate),
                        "intent": intent
                    },
                    "retrieval_source": "graph",
                    "retrieval_score": 0.85,
                    "retrieval_query": query,
                    "confidence_score": 0.85,
                    "provenance": {
                        "retrieval_method": "graph_intent_based",
                        "intent": intent,
                        "retrieval_timestamp": self._get_timestamp()
                    }
                }
            
            elif intent == "performing_poorly":
                topic = self._node_to_dict(record.get("t"))
                struggling_count = record.get("struggling_count", 0)
                
                if not topic or not topic.get("id"):
                    logger.warning("Topic node is None or missing ID in performing_poorly result")
                    return None
                
                content = f"Topic {topic.get('name', 'unknown')} has poor performance. "
                content += f"{struggling_count} students are struggling with this topic."
                
                return {
                    "document_id": f"poor_topic_{topic.get('id')}",
                    "content": content,
                    "metadata": {
                        "document_type": "poor_performing_topic",
                        "topic_id": topic.get("id"),
                        "topic_name": topic.get("name"),
                        "struggling_count": int(struggling_count),
                        "intent": intent
                    },
                    "retrieval_source": "graph",
                    "retrieval_score": 0.8,
                    "retrieval_query": query,
                    "confidence_score": 0.8,
                    "provenance": {
                        "retrieval_method": "graph_intent_based",
                        "intent": intent,
                        "retrieval_timestamp": self._get_timestamp()
                    }
                }
            
            return None
            
        except Exception as e:
            logger.error(f"Error converting intent result to document: {e}")
            return None
    
    async def _keyword_search(
        self,
        query: str,
        max_depth: int,
        limit: int
    ) -> List[Dict[str, Any]]:
        """
        Fallback: keyword search in graph node properties
        """
        # Extract keywords from query
        keywords = re.findall(r'\b\w+\b', query.lower())
        
        # Search for nodes containing keywords
        cypher_query = """
        MATCH (n)
        WHERE any(keyword IN $keywords WHERE 
            any(prop IN keys(n) WHERE toLower(toString(n[prop])) CONTAINS keyword)
        )
        RETURN n, labels(n) AS labels
        LIMIT $limit
        """
        
        results = await self.graph_client.query_cypher(
            cypher_query,
            {"keywords": keywords, "limit": limit}
        )
        
        documents = []
        for record in results:
            node = record.get("n", {})
            labels = record.get("labels", [])
            
            if labels:
                node_type = labels[0]
                node_id = node.get("id", "unknown")
                
                # Create document from node
                content = self._node_to_content(node, node_type)
                documents.append({
                    "document_id": f"{node_type}_{node_id}",
                    "content": content,
                    "metadata": {
                        "document_type": node_type.lower(),
                        "node_id": node_id,
                        "node_properties": dict(node)
                    },
                    "retrieval_source": "graph",
                    "retrieval_score": 0.7,  # Default score for keyword match
                    "retrieval_query": query,
                    "confidence_score": 0.7,
                    "provenance": {
                        "retrieval_method": "graph_keyword_search",
                        "retrieval_timestamp": self._get_timestamp(),
                        "matched_keywords": keywords
                    }
                })
        
        return documents
    
    def _paths_to_documents(
        self,
        paths: List[Dict[str, Any]],
        query: str,
        start_entity_type: str,
        start_entity_id: str
    ) -> List[Dict[str, Any]]:
        """Convert graph paths to document format"""
        documents = []
        seen_nodes: Set[str] = set()
        
        for path in paths:
            nodes = path.get("nodes", [])
            relationships = path.get("relationships", [])
            
            for node in nodes:
                # Node is already a dict from graph_client (with _labels added)
                if isinstance(node, dict):
                    node_dict = node
                    labels = node_dict.get("_labels", [])
                else:
                    # Fallback: try to extract from node object
                    node_dict = dict(node)
                    labels = list(node.labels) if hasattr(node, 'labels') else []
                
                node_id = node_dict.get("id")
                
                if not node_id or node_id in seen_nodes:
                    continue
                
                seen_nodes.add(node_id)
                
                # Get node type from labels
                node_type = labels[0] if labels and len(labels) > 0 else "Unknown"
                
                # Create document content from node
                content = self._node_to_content(node_dict, node_type)
                
                # Calculate relevance score based on path depth
                depth = len(nodes) - 1
                relevance_score = max(0.5, 1.0 - (depth * 0.1))
                
                documents.append({
                    "document_id": f"{node_type}_{node_id}",
                    "content": content,
                    "metadata": {
                        "document_type": node_type.lower(),
                        "node_id": node_id,
                        "node_properties": node_dict,
                        "path_depth": depth,
                        "start_entity": f"{start_entity_type}:{start_entity_id}"
                    },
                    "retrieval_source": "graph",
                    "retrieval_score": relevance_score,
                    "retrieval_query": query,
                    "confidence_score": relevance_score,
                    "provenance": {
                        "retrieval_method": "graph_traversal",
                        "retrieval_timestamp": self._get_timestamp(),
                        "start_entity": f"{start_entity_type}:{start_entity_id}",
                        "path_depth": depth,
                        "relationships": [dict(rel) for rel in relationships]
                    }
                })
        
        return documents
    
    def _node_to_content(self, node: Dict[str, Any], node_type: str) -> str:
        """Convert graph node to text content"""
        content_parts = []
        
        # Format based on node type
        if node_type == "Student":
            name = node.get("name", "Unknown Student")
            student_id = node.get("id", "unknown")
            content_parts.append(f"Student: {name} (ID: {student_id})")
            if "email" in node:
                content_parts.append(f"Email: {node['email']}")
        
        elif node_type == "Topic":
            name = node.get("name", "Unknown Topic")
            topic_id = node.get("id", "unknown")
            content_parts.append(f"Topic: {name} (ID: {topic_id})")
        
        elif node_type == "Classroom":
            name = node.get("name", "Unknown Classroom")
            classroom_id = node.get("id", "unknown")
            content_parts.append(f"Classroom: {name} (ID: {classroom_id})")
            if "grade" in node:
                content_parts.append(f"Grade: {node['grade']}")
        
        elif node_type == "QuizAttempt":
            attempt_id = node.get("id", "unknown")
            score = node.get("score", "N/A")
            status = node.get("status", "Unknown")
            content_parts.append(f"Quiz Attempt: ID {attempt_id}, Score: {score}, Status: {status}")
        
        elif node_type == "AssignmentAttempt":
            attempt_id = node.get("id", "unknown")
            score = node.get("score", "N/A")
            status = node.get("status", "Unknown")
            content_parts.append(f"Assignment Attempt: ID {attempt_id}, Score: {score}, Status: {status}")
        
        elif node_type == "Quiz":
            title = node.get("title", "Unknown Quiz")
            quiz_id = node.get("id", "unknown")
            content_parts.append(f"Quiz: {title} (ID: {quiz_id})")
        
        elif node_type == "Assignment":
            title = node.get("title", "Unknown Assignment")
            assignment_id = node.get("id", "unknown")
            content_parts.append(f"Assignment: {title} (ID: {assignment_id})")
        
        else:
            # Generic format
            node_id = node.get("id", "unknown")
            content_parts.append(f"{node_type}: {node_id}")
            if "name" in node:
                content_parts.append(f"Name: {node['name']}")
            if "title" in node:
                content_parts.append(f"Title: {node['title']}")
        
        # Add performance metrics if available
        if "score" in node and node_type not in ["QuizAttempt", "AssignmentAttempt"]:
            content_parts.append(f"Score: {node['score']}")
        if "average_score" in node:
            content_parts.append(f"Average Score: {node['average_score']}")
        if "completion_rate" in node:
            content_parts.append(f"Completion Rate: {node['completion_rate']}")
        
        # Add topic information
        if "topic_name" in node:
            content_parts.append(f"Topic: {node['topic_name']}")
        
        return " | ".join(content_parts) if content_parts else f"{node_type}: {node.get('id', 'unknown')}"
    
    def _format_results(
        self,
        results: List[Dict[str, Any]],
        query: str
    ) -> List[Dict[str, Any]]:
        """Format graph results with provenance"""
        # Sort by retrieval score (descending)
        results.sort(key=lambda x: x.get("retrieval_score", 0), reverse=True)
        
        return results
    
    async def _resolve_entity_id(self, entity_type: str, entity_identifier: str) -> Optional[str]:
        """
        Resolve entity identifier (name or ID) to actual node ID in graph
        
        Best Practice:
        - If identifier is already a valid ID (numeric/UUID), use it directly
        - If identifier is a name, lookup in graph by name property
        - Support both English and Vietnamese names via synonym mapping
        
        Args:
            entity_type: Node type (Classroom, Student, Topic, etc.)
            entity_identifier: Entity name or ID from extraction
        
        Returns:
            Resolved node ID or None if not found
        """
        # Check if identifier is already a valid ID (numeric or UUID-like)
        if self._is_valid_id(entity_identifier):
            # Try to verify node exists
            exists = await self._verify_node_exists(entity_type, entity_identifier)
            if exists:
                return entity_identifier
            # If not found, fall through to name lookup
        
        # Lookup by name property
        if entity_type == "Topic":
            return await self._lookup_topic_id(entity_identifier)
        elif entity_type == "Classroom":
            return await self._lookup_classroom_id(entity_identifier)
        elif entity_type == "Student":
            return await self._lookup_student_id(entity_identifier)
        elif entity_type in ["Quiz", "Assignment"]:
            # For Quiz/Assignment, identifier is usually already an ID
            return entity_identifier if self._is_valid_id(entity_identifier) else None
        
        return None
    
    def _is_valid_id(self, identifier: str) -> bool:
        """Check if identifier looks like a valid ID (numeric or UUID)"""
        # Check if it's numeric
        if identifier.isdigit():
            return True
        
        # Check if it's UUID-like (contains hyphens and is alphanumeric)
        import re
        uuid_pattern = r'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
        if re.match(uuid_pattern, identifier.lower()):
            return True
        
        return False
    
    async def _verify_node_exists(self, node_type: str, node_id: str) -> bool:
        """Verify if a node exists in graph"""
        try:
            cypher_query = f"""
            MATCH (n:{node_type} {{id: $node_id}})
            RETURN n
            LIMIT 1
            """
            results = await self.graph_client.query_cypher(
                cypher_query,
                {"node_id": node_id}
            )
            return len(results) > 0
        except Exception as e:
            logger.error(f"Error verifying node existence: {e}")
            return False
    
    async def _lookup_topic_id(self, topic_name: str) -> Optional[str]:
        """
        Lookup topic ID from topic name (supports both English and Vietnamese)
        
        Best Practice:
        - Search by exact name match first
        - Fall back to synonym matching
        - Support case-insensitive search
        """
        try:
            # Normalize topic name using entity extractor
            normalized_name = self.entity_extractor._normalize_topic(topic_name)
            search_names = []
            
            # Add original name
            if topic_name:
                search_names.append(topic_name)
            
            # Add normalized name if different
            if normalized_name and normalized_name != topic_name:
                search_names.append(normalized_name)
            
            # Add Vietnamese variants from synonym mapping
            topic_lower = topic_name.lower()
            
            # Strategy 1: If topic_name is an English topic in synonyms, add all its Vietnamese variants
            if topic_lower in self.entity_extractor.topic_synonyms:
                vi_variants = self.entity_extractor.topic_synonyms[topic_lower]
                for vi_variant in vi_variants:
                    if vi_variant not in search_names:
                        search_names.append(vi_variant)  # Add Vietnamese variant directly
            
            # Strategy 2: Check if topic_name contains or is contained in Vietnamese variants
            for vi_variant, en_topic in self.entity_extractor.vi_to_en_topics.items():
                if vi_variant in topic_lower or topic_lower in vi_variant:
                    # Add the English topic (normalized)
                    if en_topic not in search_names:
                        search_names.append(en_topic)
                    # Also add the Vietnamese variant itself
                    if vi_variant not in search_names:
                        search_names.append(vi_variant)
            
            # Strategy 3: If normalized_name is different and is an English topic, add its Vietnamese variants
            if normalized_name and normalized_name != topic_name:
                normalized_lower = normalized_name.lower()
                if normalized_lower in self.entity_extractor.topic_synonyms:
                    vi_variants = self.entity_extractor.topic_synonyms[normalized_lower]
                    for vi_variant in vi_variants:
                        if vi_variant not in search_names:
                            search_names.append(vi_variant)
            
            # Remove duplicates and empty strings
            search_names = [name for name in set(search_names) if name]
            
            if not search_names:
                logger.warning(f"No search names generated for topic: {topic_name}")
                return None
            
            logger.info(f"Looking up topic '{topic_name}' with search names: {search_names}")
            
            # Search in graph by name (case-insensitive)
            # Try exact match first, then partial match
            cypher_query = """
            MATCH (t:Topic)
            WHERE any(name in $search_names WHERE 
                toLower(t.name) = toLower(name) OR
                toLower(t.name) CONTAINS toLower(name) OR
                toLower(name) CONTAINS toLower(t.name)
            )
            RETURN t.id as topic_id, t.name as topic_name
            ORDER BY 
                CASE 
                    WHEN any(name in $search_names WHERE toLower(t.name) = toLower(name)) THEN 1
                    WHEN any(name in $search_names WHERE toLower(t.name) CONTAINS toLower(name)) THEN 2
                    ELSE 3
                END
            LIMIT 5
            """
            
            results = await self.graph_client.query_cypher(
                cypher_query,
                {"search_names": search_names}
            )
            
            if results:
                topic_id = str(results[0].get("topic_id", ""))
                topic_found = results[0].get("topic_name", "")
                logger.info(f"Found topic '{topic_name}' → ID: {topic_id} (name in graph: '{topic_found}')")
                return topic_id
            
            logger.warning(f"Topic '{topic_name}' not found in graph. Searched with: {search_names}")
            return None
            
        except Exception as e:
            logger.error(f"Error looking up topic ID for '{topic_name}': {e}", exc_info=True)
            return None
    
    async def _lookup_classroom_id(self, classroom_identifier: str) -> Optional[str]:
        """
        Lookup classroom ID from name or code
        
        Supports:
        - "7A" → find by name containing "7A"
        - "class 7A" → extract "7A" and search
        - Numeric ID → use directly
        """
        try:
            # Extract class code/number from identifier
            # "class 7A" → "7A", "7A" → "7A"
            class_code_match = re.search(r'([A-Za-z0-9]+)', classroom_identifier, re.IGNORECASE)
            if class_code_match:
                search_code = class_code_match.group(1)
            else:
                search_code = classroom_identifier
            
            # If it's numeric, try as ID first
            if search_code.isdigit():
                exists = await self._verify_node_exists("Classroom", search_code)
                if exists:
                    return search_code
            
            # Search by name or id
            # Note: class_code is not stored in graph, only in raw data
            cypher_query = """
            MATCH (c:Classroom)
            WHERE toLower(c.name) CONTAINS toLower($search_code) OR
                  toLower(toString(c.id)) = toLower($search_code)
            RETURN c.id as classroom_id, c.name as classroom_name
            LIMIT 5
            """
            
            results = await self.graph_client.query_cypher(
                cypher_query,
                {"search_code": search_code}
            )
            
            if results:
                classroom_id = str(results[0].get("classroom_id", ""))
                classroom_name = results[0].get("classroom_name", "")
                logger.info(f"Found classroom '{classroom_identifier}' → ID: {classroom_id} (name: '{classroom_name}')")
                return classroom_id
            
            logger.warning(f"Classroom '{classroom_identifier}' not found in graph. Searched with: '{search_code}'")
            return None
            
        except Exception as e:
            logger.error(f"Error looking up classroom ID: {e}")
            return None
    
    async def _lookup_student_id(self, student_identifier: str) -> Optional[str]:
        """
        Lookup student ID from name or email
        
        Supports:
        - Student name (e.g., "Nguyễn Văn A")
        - Email
        - UUID
        """
        try:
            # If it's UUID-like, try as ID first
            if self._is_valid_id(student_identifier):
                exists = await self._verify_node_exists("Student", student_identifier)
                if exists:
                    return student_identifier
            
            # Search by name or email
            cypher_query = """
            MATCH (s:Student)
            WHERE toLower(s.name) CONTAINS toLower($search_term) OR
                  toLower(s.email) CONTAINS toLower($search_term) OR
                  toLower(toString(s.id)) = toLower($search_term)
            RETURN s.id as student_id, s.name as student_name
            LIMIT 5
            """
            
            results = await self.graph_client.query_cypher(
                cypher_query,
                {"search_term": student_identifier}
            )
            
            if results:
                return str(results[0].get("student_id", ""))
            
            return None
            
        except Exception as e:
            logger.error(f"Error looking up student ID: {e}")
            return None
    
    def _get_timestamp(self) -> str:
        """Get current timestamp"""
        from datetime import datetime
        return datetime.utcnow().isoformat()

