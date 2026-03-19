"""
Entity Extractor
Enhanced entity extraction with synonym mapping and intent detection
"""

from typing import List, Dict, Any, Tuple, Optional
import re
import logging

logger = logging.getLogger(__name__)


class EntityExtractor:
    """
    Extract entities from queries with:
    - Synonym mapping (en↔vi)
    - Intent detection (struggling, need help, performing poorly)
    - Topic name matching
    """
    
    def __init__(self):
        # Topic synonyms: English ↔ Vietnamese
        self.topic_synonyms: Dict[str, List[str]] = {
            "electrical circuits": ["mạch điện", "điện học", "circuit", "điện"],
            "force and motion": ["lực và chuyển động", "force", "motion", "chuyển động"],
            "mechanics": ["cơ học", "mechanics"],
            "thermodynamics": ["nhiệt động lực học", "thermodynamics", "nhiệt"],
            "optics": ["quang học", "optics", "ánh sáng"],
            "waves": ["sóng", "waves", "sóng học"],
            "electricity": ["điện", "electricity", "điện học"],
            "magnetism": ["từ tính", "magnetism", "từ học"],
        }
        
        # Intent keywords
        self.intent_keywords: Dict[str, List[str]] = {
            "struggling": ["struggling", "struggle", "yếu", "kém", "khó khăn", "gặp khó"],
            "need_help": ["need help", "need extra help", "cần hỗ trợ", "cần giúp", "cần giúp đỡ"],
            "performing_poorly": ["performing poorly", "poor performance", "kém", "yếu", "thấp"],
        }
        
        # Build reverse mapping (vi → en)
        self.vi_to_en_topics: Dict[str, str] = {}
        for en_topic, vi_variants in self.topic_synonyms.items():
            for vi_variant in vi_variants:
                self.vi_to_en_topics[vi_variant.lower()] = en_topic
    
    def extract_entities(self, query: str) -> List[Tuple[str, str]]:
        """
        Extract entities from query
        
        Returns:
            List of (node_type, node_id_or_name) tuples
        """
        entities = []
        query_lower = query.lower()
        
        # 1. Extract classroom IDs
        # Pattern: "class 7A", "classroom 7A", "class 7A performance" → extract "7A"
        # Avoid matching common words like "performance", "status", etc.
        # Priority: Match alphanumeric codes (7A, 8B, etc.) over common words
        classroom_pattern = r'(?:classroom|class)\s+([A-Za-z0-9_-]+)'
        classroom_matches = re.findall(classroom_pattern, query, re.IGNORECASE)
        
        # Filter out common words that are not classroom identifiers
        common_words = {'performance', 'status', 'info', 'details', 'summary', 'report', 
                       'data', 'information', 'overview', 'statistics', 'metrics'}
        
        for match in classroom_matches:
            match_lower = match.lower()
            # If it's a common word, skip it
            if match_lower in common_words:
                continue
            # If it looks like a classroom code (contains numbers or is short alphanumeric)
            if any(c.isdigit() for c in match) or len(match) <= 5:
                entities.append(("Classroom", match))
            # Also accept if it's explicitly mentioned as a class code
            elif re.match(r'^[A-Za-z]?\d+[A-Za-z]?$', match):
                entities.append(("Classroom", match))
        
        # 2. Extract student IDs or names
        student_pattern = r'(?:student|học sinh)\s+([A-Za-z0-9_\s]+?)(?:\s|,|$|with|are|need)'
        student_matches = re.findall(student_pattern, query, re.IGNORECASE)
        for match in student_matches:
            entities.append(("Student", match.strip()))
        
        # 3. Extract topic names with synonym matching
        topics = self._extract_topics(query_lower)
        for topic_name in topics:
            entities.append(("Topic", topic_name))
        
        # 4. Extract quiz/assignment IDs
        quiz_pattern = r'(?:quiz|bài kiểm tra)\s+([A-Za-z0-9_-]+)'
        quiz_matches = re.findall(quiz_pattern, query, re.IGNORECASE)
        for match in quiz_matches:
            entities.append(("Quiz", match))
        
        assignment_pattern = r'(?:assignment|bài tập)\s+([A-Za-z0-9_-]+)'
        assignment_matches = re.findall(assignment_pattern, query, re.IGNORECASE)
        for match in assignment_matches:
            entities.append(("Assignment", match))
        
        logger.info(f"Extracted {len(entities)} entities: {entities}")
        return entities
    
    def _extract_topics(self, query_lower: str) -> List[str]:
        """Extract topic names with synonym matching"""
        topics = []
        
        # Check for explicit topic mentions
        topic_pattern = r'(?:topic|chủ đề|subject)\s+([A-Za-z0-9_\s]+?)(?:\s|,|$|are|is)'
        explicit_matches = re.findall(topic_pattern, query_lower)
        for match in explicit_matches:
            topic_name = match.strip()
            # Normalize to English if possible
            normalized = self._normalize_topic(topic_name)
            if normalized:
                topics.append(normalized)
            else:
                topics.append(topic_name)
        
        # Check for topic synonyms in query
        for en_topic, vi_variants in self.topic_synonyms.items():
            # Check English
            if en_topic in query_lower:
                topics.append(en_topic)
            # Check Vietnamese variants
            for vi_variant in vi_variants:
                if vi_variant.lower() in query_lower:
                    topics.append(en_topic)  # Normalize to English
                    break
        
        return list(set(topics))  # Deduplicate
    
    def _normalize_topic(self, topic_name: str) -> Optional[str]:
        """Normalize topic name to English standard"""
        topic_lower = topic_name.lower()
        
        # Check direct mapping
        if topic_lower in self.vi_to_en_topics:
            return self.vi_to_en_topics[topic_lower]
        
        # Check partial match
        for vi_variant, en_topic in self.vi_to_en_topics.items():
            if vi_variant in topic_lower or topic_lower in vi_variant:
                return en_topic
        
        return None
    
    def detect_intent(self, query: str) -> Optional[str]:
        """
        Detect query intent with priority
        
        Priority order:
        1. performing_poorly (topic-level, more specific)
        2. Other intents (student-level)
        
        Returns:
            Intent type or None
        """
        query_lower = query.lower()
        
        # Check for topic-level intents first (more specific)
        # "performing poorly" is about topics, not individual students
        topic_indicators = ["topics", "chủ đề", "topic", "subjects", "subjects"]
        if any(indicator in query_lower for indicator in topic_indicators):
            if any(kw in query_lower for kw in ["performing poorly", "poor performance", "kém", "yếu", "weak"]):
                logger.info(f"Detected intent: performing_poorly (topic-level)")
                return "performing_poorly"
        
        # Check other intents (student-level)
        for intent, keywords in self.intent_keywords.items():
            if intent == "performing_poorly":
                continue  
            for keyword in keywords:
                if keyword in query_lower:
                    logger.info(f"Detected intent: {intent}")
                    return intent
        
        return None
    
    def get_intent_cypher_query(self, intent: str, classroom_id: Optional[str] = None) -> Optional[str]:
        """
        Generate Cypher query for intent-based retrieval
        
        Returns:
            Cypher query string or None
        """
        if intent == "struggling":
            
            if classroom_id:
                base_query = """
                MATCH (c:Classroom {id: $classroom_id})<-[:ENROLLED_IN]-(s:Student)
                MATCH (s)-[:ATTEMPTED]->(qa:QuizAttempt)-[:ATTEMPTED_FOR]->(q:Quiz)-[:HAS_TOPIC]->(t:Topic)
                WHERE qa.score < 0.5
                WITH s, t, avg(qa.score) as avg_score, count(qa) as attempt_count
                WHERE attempt_count >= 2
                RETURN s, t, avg_score as student_score, t.name as topic_name
                ORDER BY avg_score ASC
                LIMIT 20
                """
            else:
                base_query = """
                MATCH (s:Student)-[:ATTEMPTED]->(qa:QuizAttempt)-[:ATTEMPTED_FOR]->(q:Quiz)-[:HAS_TOPIC]->(t:Topic)
                WHERE qa.score < 0.5
                WITH s, t, avg(qa.score) as avg_score, count(qa) as attempt_count
                WHERE attempt_count >= 2
                RETURN s, t, avg_score as student_score, t.name as topic_name
                ORDER BY avg_score ASC
                LIMIT 20
                """
            return base_query
        
        elif intent == "need_help":
            # Find students needing help (low average score or low completion)
            # Scores can be 0-100 (percentage) or 0-1 (normalized)
            # Normalize: if score > 1, assume it's 0-100 and divide by 100
            if classroom_id:
                base_query = """
                MATCH (c:Classroom {id: $classroom_id})<-[:ENROLLED_IN]-(s:Student)
                OPTIONAL MATCH (s)-[:ATTEMPTED]->(qa:QuizAttempt)
                OPTIONAL MATCH (s)-[:SUBMITTED]->(aa:AssignmentAttempt)
                WITH s, 
                     collect(DISTINCT CASE WHEN qa.score IS NOT NULL 
                                          THEN CASE WHEN toFloat(qa.score) > 1 
                                                    THEN toFloat(qa.score) / 100.0 
                                                    ELSE toFloat(qa.score) 
                                               END
                                          ELSE null END) as quiz_scores,
                     collect(DISTINCT CASE WHEN aa.score IS NOT NULL 
                                          THEN CASE WHEN toFloat(aa.score) > 1 
                                                    THEN toFloat(aa.score) / 100.0 
                                                    ELSE toFloat(aa.score) 
                                               END
                                          ELSE null END) as assignment_scores
                WITH s, 
                     [score in quiz_scores WHERE score IS NOT NULL] + 
                     [score in assignment_scores WHERE score IS NOT NULL] as all_scores
                WITH s, 
                     CASE WHEN size(all_scores) > 0 
                          THEN reduce(total = 0.0, score in all_scores | total + score) / size(all_scores)
                          ELSE 1.0 END as avg_score,
                     size(all_scores) as attempt_count
                WHERE avg_score < 0.6 OR attempt_count < 3
                RETURN s, avg_score as student_score, attempt_count as completion_rate
                ORDER BY avg_score ASC, attempt_count ASC
                LIMIT 20
                """
            else:
                base_query = """
                MATCH (s:Student)
                OPTIONAL MATCH (s)-[:ATTEMPTED]->(qa:QuizAttempt)
                OPTIONAL MATCH (s)-[:SUBMITTED]->(aa:AssignmentAttempt)
                WITH s, 
                     collect(DISTINCT CASE WHEN qa.score IS NOT NULL 
                                          THEN CASE WHEN toFloat(qa.score) > 1 
                                                    THEN toFloat(qa.score) / 100.0 
                                                    ELSE toFloat(qa.score) 
                                               END
                                          ELSE null END) as quiz_scores,
                     collect(DISTINCT CASE WHEN aa.score IS NOT NULL 
                                          THEN CASE WHEN toFloat(aa.score) > 1 
                                                    THEN toFloat(aa.score) / 100.0 
                                                    ELSE toFloat(aa.score) 
                                               END
                                          ELSE null END) as assignment_scores
                WITH s, 
                     [score in quiz_scores WHERE score IS NOT NULL] + 
                     [score in assignment_scores WHERE score IS NOT NULL] as all_scores
                WITH s, 
                     CASE WHEN size(all_scores) > 0 
                          THEN reduce(total = 0.0, score in all_scores | total + score) / size(all_scores)
                          ELSE 1.0 END as avg_score,
                     size(all_scores) as attempt_count
                WHERE avg_score < 0.6 OR attempt_count < 3
                RETURN s, avg_score as student_score, attempt_count as completion_rate
                ORDER BY avg_score ASC, attempt_count ASC
                LIMIT 20
                """
            return base_query
        
        elif intent == "performing_poorly":
            # Find topics with poor performance (many low-score attempts)
            # Scores can be 0-100 (percentage) or 0-1 (normalized)
            # Normalize: if score > 1, assume it's 0-100 and divide by 100
            if classroom_id:
                base_query = """
                MATCH (c:Classroom {id: $classroom_id})<-[:ENROLLED_IN]-(s:Student)
                MATCH (s)-[:ATTEMPTED]->(qa:QuizAttempt)-[:ATTEMPTED_FOR]->(q:Quiz)-[:HAS_TOPIC]->(t:Topic)
                WHERE qa.score IS NOT NULL 
                  AND (CASE WHEN toFloat(qa.score) > 1 THEN toFloat(qa.score) / 100.0 ELSE toFloat(qa.score) END) < 0.6
                WITH t, collect(DISTINCT s.id) as quiz_struggling_students
                MATCH (c:Classroom {id: $classroom_id})<-[:ENROLLED_IN]-(s2:Student)
                MATCH (s2)-[:SUBMITTED]->(aa:AssignmentAttempt)-[:SUBMITTED_FOR]->(a:Assignment)-[:HAS_TOPIC]->(t)
                WHERE aa.score IS NOT NULL 
                  AND (CASE WHEN toFloat(aa.score) > 1 THEN toFloat(aa.score) / 100.0 ELSE toFloat(aa.score) END) < 0.6
                WITH t, quiz_struggling_students, collect(DISTINCT s2.id) as assignment_struggling_students
                WITH t, 
                     [s_id in quiz_struggling_students WHERE s_id IS NOT NULL] + 
                     [s_id in assignment_struggling_students WHERE s_id IS NOT NULL] as all_struggling_students
                WITH t, size(all_struggling_students) as struggling_count
                WHERE struggling_count > 0 AND t IS NOT NULL
                RETURN t, struggling_count
                ORDER BY struggling_count DESC
                LIMIT 20
                """
            else:
                base_query = """
                MATCH (s:Student)-[:ATTEMPTED]->(qa:QuizAttempt)-[:ATTEMPTED_FOR]->(q:Quiz)-[:HAS_TOPIC]->(t:Topic)
                WHERE qa.score IS NOT NULL 
                  AND (CASE WHEN toFloat(qa.score) > 1 THEN toFloat(qa.score) / 100.0 ELSE toFloat(qa.score) END) < 0.6
                WITH t, collect(DISTINCT s.id) as quiz_struggling_students
                MATCH (s2:Student)-[:SUBMITTED]->(aa:AssignmentAttempt)-[:SUBMITTED_FOR]->(a:Assignment)-[:HAS_TOPIC]->(t)
                WHERE aa.score IS NOT NULL 
                  AND (CASE WHEN toFloat(aa.score) > 1 THEN toFloat(aa.score) / 100.0 ELSE toFloat(aa.score) END) < 0.6
                WITH t, quiz_struggling_students, collect(DISTINCT s2.id) as assignment_struggling_students
                WITH t, 
                     [s_id in quiz_struggling_students WHERE s_id IS NOT NULL] + 
                     [s_id in assignment_struggling_students WHERE s_id IS NOT NULL] as all_struggling_students
                WITH t, size(all_struggling_students) as struggling_count
                WHERE struggling_count > 0 AND t IS NOT NULL
                RETURN t, struggling_count
                ORDER BY struggling_count DESC
                LIMIT 20
                """
            return base_query
        
        return None

