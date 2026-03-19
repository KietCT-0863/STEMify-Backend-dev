"""
Document Processor
Process raw classroom data into documents for RAG
"""

from typing import List, Dict, Any, Optional
from datetime import datetime
import hashlib
import json
import logging

logger = logging.getLogger(__name__)


class DocumentProcessor:
    """Process raw data into hierarchical documents"""
    
    def __init__(self, chunk_size: int = 1000, chunk_overlap: int = 200):
        self.chunk_size = chunk_size
        self.chunk_overlap = chunk_overlap
    
    def process_classroom_data(self, data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """
        Process classroom data into hierarchical documents
        
        Returns list of documents with:
        - content: Text content
        - metadata: Rich metadata for filtering
        - provenance: Source tracking
        - confidence_score: Initial confidence (will be updated by embedding)
        """
        documents = []
        
        # Level 1: Classroom Summary
        documents.extend(self._create_classroom_chunks(data))
        
        # Level 2: Student Performance
        documents.extend(self._create_student_chunks(data))
        
        # Level 3: Activity Level (Quiz/Assignment)
        documents.extend(self._create_quiz_attempt_chunks(data))
        documents.extend(self._create_assignment_attempt_chunks(data))
        
        # Level 4: Question Level
        documents.extend(self._create_question_chunks(data))
        
        # Level 5: Virtual/Analytics Documents (for intent-based queries)
        documents.extend(self._create_virtual_analytics_documents(data))
        
        logger.info(f"Processed {len(documents)} documents from classroom data")
        return documents
    
    def _create_classroom_chunks(self, data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Level 1: Classroom summary"""
        classroom = data["classroom"]
        students = data["students"]
        enrollments = data.get("enrollments", {})
        
        # Calculate summary metrics
        total_students = len(students)
        curriculum_enrollments = enrollments.get("curriculum_enrollments", [])
        avg_progress = (
            sum(e.get("progress_percentage", 0) for e in curriculum_enrollments) / 
            len(curriculum_enrollments) if curriculum_enrollments else 0
        )
        
        content = f"""
        Classroom: {classroom.get('name', 'N/A')}
        Grade: {classroom.get('grade', 'N/A')}
        Status: {classroom.get('status', 'N/A')}
        Total Students: {total_students}
        Average Progress: {avg_progress:.1f}%
        Start Date: {classroom.get('start_date', 'N/A')}
        End Date: {classroom.get('end_date', 'N/A')}
        """
        
        doc = {
            "content": content.strip(),
            "metadata": {
                "document_type": "classroom_summary",
                "classroom_id": classroom.get("id"),
                "classroom_name": classroom.get("name"),
                "grade": classroom.get("grade", "N/A"),
                "teacher_id": classroom.get("teacher_id"),
                "curriculum_id": classroom.get("curriculum_id"),
                "status": classroom.get("status", "N/A"),
                "total_students": total_students,
                "average_progress": avg_progress,
                "date": datetime.utcnow().isoformat(),
            },
            "provenance": {
                "source": "classroom_entity",
                "source_id": str(classroom.get("id")),
                "extracted_at": datetime.utcnow().isoformat(),
                "data_version": "1.0",
                "extraction_method": "rule_based"
            },
            "confidence_score": 1.0,  # High confidence for structured data
            "document_id": f"classroom_{classroom.get('id')}_summary"
        }
        
        return [doc]
    
    def _create_student_chunks(self, data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Level 2: Student performance summaries"""
        documents = []
        students = data["students"]
        quizzes = data.get("quizzes", {})
        assignments = data.get("assignments", {})
        time_metrics = data.get("time_metrics", {}).get("engagement_metrics", [])
        
        for student in students:
            student_id = student["student_id"]
            
            # Get student's quiz attempts
            student_quizzes = [
                sq for sq in quizzes.get("student_quizzes", [])
                if sq["student_id"] == student_id
            ]
            
            # Get student's assignment attempts
            student_assignments = [
                sa for sa in assignments.get("student_assignments", [])
                if sa["student_id"] == student_id
            ]
            
            # Calculate metrics
            avg_quiz_score = (
                sum(sq.get("final_score", 0) or 0 for sq in student_quizzes) /
                len(student_quizzes) if student_quizzes else 0
            )
            avg_assignment_score = (
                sum(sa.get("final_score", 0) or 0 for sa in student_assignments) /
                len(student_assignments) if student_assignments else 0
            )
            
            # Get engagement metrics
            engagement = next(
                (em for em in time_metrics if em["student_id"] == student_id),
                None
            )
            
            content = f"""
            Student: {student['student_name']}
            Student ID: {student_id}
            Email: {student.get('email', 'N/A')}
            Joined: {student.get('joined_at', 'N/A')}
            
            Performance:
            - Average Quiz Score: {avg_quiz_score:.1f}%
            - Average Assignment Score: {avg_assignment_score:.1f}%
            - Total Quizzes: {len(student_quizzes)}
            - Total Assignments: {len(student_assignments)}
            """
            
            if engagement:
                content += f"""
            Engagement:
            - Last Activity: {engagement.get('last_activity_date', 'N/A')}
            - Days Since Last Activity: {engagement.get('days_since_last_activity', 'N/A')}
            - Activities Last 7 Days: {engagement.get('total_activities_last_7_days', 0)}
            - Completion Rate: {engagement.get('completion_rate', 0):.1%}
            """
            
            # Determine performance category
            overall_score = (avg_quiz_score + avg_assignment_score) / 2 if (avg_quiz_score + avg_assignment_score) > 0 else 0
            performance_category = (
                "good" if overall_score >= 80 else
                "average" if overall_score >= 60 else "poor"
            )
            
            # Determine engagement level
            engagement_level = "high"
            if engagement:
                days = engagement.get("days_since_last_activity", 0)
                engagement_level = (
                    "high" if days <= 1 else
                    "medium" if days <= 3 else "low"
                )
            
            doc = {
                "content": content.strip(),
                "metadata": {
                    "document_type": "student_summary",
                    "student_id": student_id,
                    "student_name": student["student_name"],
                    "classroom_id": data["classroom"]["id"],
                    "average_quiz_score": avg_quiz_score,
                    "average_assignment_score": avg_assignment_score,
                    "total_quizzes": len(student_quizzes),
                    "total_assignments": len(student_assignments),
                    "performance_category": performance_category,
                    "engagement_level": engagement_level,
                    "date": datetime.utcnow().isoformat(),
                },
                "provenance": {
                    "source": "student_entity",
                    "source_id": student_id,
                    "extracted_at": datetime.utcnow().isoformat(),
                    "data_version": "1.0",
                    "extraction_method": "rule_based",
                    "aggregated_from": [
                        "student_quizzes",
                        "student_assignments",
                        "time_metrics"
                    ]
                },
                "confidence_score": 0.95,  # High confidence for aggregated data
                "document_id": f"student_{student_id}_summary"
            }
            
            documents.append(doc)
        
        return documents
    
    def _create_quiz_attempt_chunks(self, data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Level 3: Quiz attempt documents"""
        documents = []
        quiz_attempts = data.get("quizzes", {}).get("quiz_attempts", [])
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        students = {s["student_id"]: s for s in data["students"]}
        
        for attempt in quiz_attempts:
            student_quiz = next(
                (sq for sq in student_quizzes if sq["id"] == attempt["student_quiz_id"]),
                None
            )
            if not student_quiz:
                continue
            
            student = students.get(student_quiz["student_id"])
            if not student:
                continue
            
            attempt_id = attempt.get("id") or f"{student_quiz['id']}-attempt-{attempt.get('attempt_number', 'unknown')}"
            
            # Extract topics from question attempts
            topics = set()
            question_attempts = attempt.get("question_attempts", [])
            for qa in question_attempts:
                topics.update(qa.get("topics", []))
            
            content = f"""
            Quiz Attempt: {student_quiz.get('quiz_title', 'Unknown Quiz')}
            Student: {student['student_name']}
            Attempt Number: {attempt['attempt_number']}
            Score: {attempt['total_score']:.1f}%
            Status: {attempt['status']}
            Started: {attempt.get('started_at', 'N/A')}
            Completed: {attempt.get('completed_at', 'N/A')}
            Time Spent: {attempt.get('time_spent_minutes', 'N/A')} minutes
            Topics: {', '.join(topics) if topics else 'N/A'}
            """
            
            doc = {
                "content": content.strip(),
                "metadata": {
                    "document_type": "quiz_attempt",
                    "attempt_id": attempt_id,
                    "student_id": student_quiz["student_id"],
                    "student_name": student["student_name"],
                    "quiz_id": student_quiz.get("quiz_id") or student_quiz.get("id"),
                    "quiz_title": student_quiz.get("quiz_title"),
                    "attempt_number": attempt["attempt_number"],
                    "score": attempt["total_score"],
                    "status": attempt["status"],
                    "topics": list(topics),
                    "date": attempt.get("started_at", datetime.utcnow().isoformat()),
                    "time_spent_minutes": attempt.get("time_spent_minutes"),
                },
                "provenance": {
                    "source": "quiz_attempt_entity",
                    "source_id": str(attempt_id),
                    "extracted_at": datetime.utcnow().isoformat(),
                    "data_version": "1.0",
                    "extraction_method": "rule_based",
                    "related_entities": {
                        "student_quiz_id": student_quiz["id"],
                        "quiz_id": student_quiz.get("quiz_id") or student_quiz.get("id")
                    }
                },
                "confidence_score": 0.9,  # High confidence
                "document_id": f"quiz_attempt_{attempt_id}"
            }
            
            documents.append(doc)
        
        return documents
    
    def _create_assignment_attempt_chunks(self, data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Level 3: Assignment attempt documents"""
        documents = []
        assignment_attempts = data.get("assignments", {}).get("assignment_attempts", [])
        student_assignments = data.get("assignments", {}).get("student_assignments", [])
        students = {s["student_id"]: s for s in data["students"]}
        
        for attempt in assignment_attempts:
            student_assignment = next(
                (sa for sa in student_assignments if sa["id"] == attempt["student_assignment_id"]),
                None
            )
            if not student_assignment:
                continue
            
            student = students.get(student_assignment["student_id"])
            if not student:
                continue
            
            attempt_id = attempt.get("id") or f"{student_assignment['id']}-attempt-{attempt.get('attempt_number', 'unknown')}"
            
            # Extract topics from question attempts
            topics = set()
            question_attempts = attempt.get("question_attempts", [])
            for qa in question_attempts:
                topics.update(qa.get("topics", []))
            
            content = f"""
            Assignment Attempt: {student_assignment.get('assignment_title', 'Unknown Assignment')}
            Student: {student['student_name']}
            Attempt Number: {attempt['attempt_number']}
            Score: {attempt['total_score']:.1f}%
            Status: {attempt['status']}
            Submitted: {attempt.get('submitted_at', 'N/A')}
            Teacher Feedback: {attempt.get('feedback', 'No feedback')}
            Topics: {', '.join(topics) if topics else 'N/A'}
            """
            
            doc = {
                "content": content.strip(),
                "metadata": {
                    "document_type": "assignment_attempt",
                    "attempt_id": attempt_id,
                    "student_id": student_assignment["student_id"],
                    "student_name": student["student_name"],
                    "assignment_id": student_assignment["assignment_id"],
                    "assignment_title": student_assignment.get("assignment_title"),
                    "attempt_number": attempt["attempt_number"],
                    "score": attempt["total_score"],
                    "status": attempt["status"],
                    "topics": list(topics),
                    "date": attempt.get("submitted_at", datetime.utcnow().isoformat()),
                    "has_feedback": bool(attempt.get("feedback")),
                },
                "provenance": {
                    "source": "assignment_attempt_entity",
                    "source_id": str(attempt_id),
                    "extracted_at": datetime.utcnow().isoformat(),
                    "data_version": "1.0",
                    "extraction_method": "rule_based",
                    "related_entities": {
                        "student_assignment_id": student_assignment["id"],
                        "assignment_id": student_assignment["assignment_id"]
                    }
                },
                "confidence_score": 0.9,
                "document_id": f"assignment_attempt_{attempt_id}"
            }
            
            documents.append(doc)
        
        return documents
    
    def _create_question_chunks(self, data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Level 4: Question-level documents"""
        documents = []
        quiz_attempts = data.get("quizzes", {}).get("quiz_attempts", [])
        students = {s["student_id"]: s for s in data["students"]}
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        
        for attempt in quiz_attempts:
            student_quiz = next(
                (sq for sq in student_quizzes if sq["id"] == attempt["student_quiz_id"]),
                None
            )
            if not student_quiz:
                continue
            
            student = students.get(student_quiz["student_id"])
            if not student:
                continue
            
            attempt_id = attempt.get("id") or f"{student_quiz['id']}-attempt-{attempt.get('attempt_number', 'unknown')}"
            
            question_attempts = attempt.get("question_attempts", [])
            for qa in question_attempts:
                topics = qa.get("topics", [])
                
                content = f"""
                Question: {qa.get('question_content', 'Unknown question')}
                Student: {student['student_name']}
                Quiz: {student_quiz.get('quiz_title', 'Unknown')}
                Correct: {'Yes' if qa.get('is_correct') else 'No'}
                Score: {qa.get('score', 0):.1f} / {qa.get('question_points', 0)}
                Topics: {', '.join(topics) if topics else 'N/A'}
                """
                
                doc = {
                    "content": content.strip(),
                    "metadata": {
                        "document_type": "question_attempt",
                        "question_id": qa.get("question_id"),
                        "question_content": qa.get("question_content"),
                        "student_id": student_quiz["student_id"],
                        "student_name": student["student_name"],
                        "quiz_id": student_quiz.get("quiz_id") or student_quiz.get("id"),
                        "attempt_id": attempt_id,
                        "is_correct": qa.get("is_correct", False),
                        "score": qa.get("score", 0),
                        "question_points": qa.get("question_points", 0),
                        "topics": topics,
                        "date": attempt.get("started_at", datetime.utcnow().isoformat()),
                    },
                    "provenance": {
                        "source": "question_attempt_entity",
                        "source_id": str(qa.get("id", "")),
                        "extracted_at": datetime.utcnow().isoformat(),
                        "data_version": "1.0",
                        "extraction_method": "rule_based",
                        "related_entities": {
                            "quiz_attempt_id": attempt_id,
                            "question_id": qa.get("question_id")
                        }
                    },
                    "confidence_score": 0.85, 
                    "document_id": f"question_{qa.get('question_id', 'unknown')}_attempt_{attempt_id}"
                }
                
                documents.append(doc)
        
        return documents
    
    def _create_virtual_analytics_documents(self, data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """
        Create virtual/analytics documents for intent-based queries
        
        These documents help answer questions like:
        - "Which students need help?"
        - "What topics are students performing poorly on?"
        - "Which students are struggling?"
        """
        documents = []
        classroom = data.get("classroom", {})
        classroom_id = str(classroom.get("id", "unknown"))
        students = data.get("students", [])
        
        # Get attempts from data structure
        quiz_attempts = data.get("quizzes", {}).get("quiz_attempts", [])
        assignment_attempts = data.get("assignments", {}).get("assignment_attempts", [])
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        student_assignments = data.get("assignments", {}).get("student_assignments", [])
        
        # Build student_id -> student_name mapping
        student_map = {
            s.get("student_id", s.get("id", "")): s.get("student_name", s.get("name", "unknown"))
            for s in students
        }
        
        # Calculate student performance metrics
        student_metrics = {}  # student_id -> {avg_score, completion_rate, attempt_count}
        
        for attempt in quiz_attempts:
            student_quiz = next(
                (sq for sq in student_quizzes if sq["id"] == attempt.get("student_quiz_id")),
                None
            )
            if not student_quiz:
                continue
            
            student_id = student_quiz.get("student_id")
            if student_id not in student_metrics:
                student_metrics[student_id] = {
                    "scores": [],
                    "attempt_count": 0
                }
            
            score = attempt.get("score", 0)
            if isinstance(score, (int, float)) and score > 0:
                student_metrics[student_id]["scores"].append(float(score))
            student_metrics[student_id]["attempt_count"] += 1
        
        for attempt in assignment_attempts:
            student_assignment = next(
                (sa for sa in student_assignments if sa["id"] == attempt.get("student_assignment_id")),
                None
            )
            if not student_assignment:
                continue
            
            student_id = student_assignment.get("student_id")
            if student_id not in student_metrics:
                student_metrics[student_id] = {
                    "scores": [],
                    "attempt_count": 0
                }
            
            score = attempt.get("score", 0)
            if isinstance(score, (int, float)) and score > 0:
                student_metrics[student_id]["scores"].append(float(score))
            student_metrics[student_id]["attempt_count"] += 1
        
        # Calculate averages
        for student_id, metrics in student_metrics.items():
            if metrics["scores"]:
                metrics["average_score"] = sum(metrics["scores"]) / len(metrics["scores"])
            else:
                metrics["average_score"] = 1.0  # No scores = assume good
            
            # Completion rate: attempt_count / expected (assume 5 expected)
            metrics["completion_rate"] = min(metrics["attempt_count"] / 5.0, 1.0) if metrics["attempt_count"] > 0 else 0.0
        
        # 1. Students needing help (low performance or low engagement)
        students_needing_help = []
        for student_id, metrics in student_metrics.items():
            avg_score = metrics.get("average_score", 1.0)
            completion_rate = metrics.get("completion_rate", 1.0)
            
            if avg_score < 0.6 or completion_rate < 0.5:
                students_needing_help.append({
                    "student_id": student_id,
                    "student_name": student_map.get(student_id, "unknown"),
                    "average_score": avg_score,
                    "completion_rate": completion_rate,
                    "reason": "low_performance" if avg_score < 0.6 else "low_engagement"
                })
        
        if students_needing_help:
            content = f"Students needing extra help in classroom {classroom_id}: "
            content += ", ".join([
                f"{s['student_name']} (score: {s['average_score']:.2f}, completion: {s['completion_rate']:.2f})"
                for s in students_needing_help[:10]  # Limit to top 10
            ])
            if len(students_needing_help) > 10:
                content += f" and {len(students_needing_help) - 10} more students."
            
            documents.append({
                "content": content,
                "metadata": {
                    "document_type": "analytics_students_need_help",
                    "classroom_id": classroom_id,
                    "student_count": len(students_needing_help),
                    "students": students_needing_help[:10],  # Store top 10
                    "intent_keywords": ["need help", "cần hỗ trợ", "cần giúp", "extra help"]
                },
                "provenance": {
                    "source": "analytics_virtual",
                    "source_id": f"analytics_need_help_{classroom_id}",
                    "extracted_at": datetime.utcnow().isoformat(),
                    "data_version": "1.0",
                    "extraction_method": "rule_based_analytics"
                },
                "confidence_score": 0.9,  # High confidence for rule-based analytics
                "document_id": f"analytics_need_help_{classroom_id}"
            })
        
        # 2. Topics with poor performance
        topic_performance = {}
        
        # Process quiz attempts
        for attempt in quiz_attempts:
            student_quiz = next(
                (sq for sq in student_quizzes if sq["id"] == attempt.get("student_quiz_id")),
                None
            )
            if not student_quiz:
                continue
            
            # Get topics from question attempts
            question_attempts = attempt.get("question_attempts", [])
            score = attempt.get("score", 0)
            if not isinstance(score, (int, float)):
                try:
                    score = float(score)
                except (ValueError, TypeError):
                    score = 0
            
            for qa in question_attempts:
                topics = qa.get("topics", [])
                
                # Handle topics: can be list of dicts or list of strings
                topic_list = []
                for topic in topics:
                    if isinstance(topic, dict):
                        topic_list.append(topic)
                    elif isinstance(topic, str):
                        topic_list.append({"name": topic, "id": topic})
                
                for topic in topic_list:
                    topic_name = topic.get("name", topic.get("id", "unknown"))
                    if topic_name not in topic_performance:
                        topic_performance[topic_name] = {
                            "total_attempts": 0,
                            "low_score_attempts": 0,
                            "topic_id": topic.get("id", topic.get("topic_id", topic_name))
                        }
                    
                    topic_performance[topic_name]["total_attempts"] += 1
                    if score < 0.6:
                        topic_performance[topic_name]["low_score_attempts"] += 1
        
        # Process assignment attempts
        for attempt in assignment_attempts:
            student_assignment = next(
                (sa for sa in student_assignments if sa["id"] == attempt.get("student_assignment_id")),
                None
            )
            if not student_assignment:
                continue
            
            # Get topics from question attempts
            question_attempts = attempt.get("question_attempts", [])
            score = attempt.get("score", 0)
            if not isinstance(score, (int, float)):
                try:
                    score = float(score)
                except (ValueError, TypeError):
                    score = 0
            
            for qa in question_attempts:
                topics = qa.get("topics", [])
                
                # Handle topics: can be list of dicts or list of strings
                topic_list = []
                for topic in topics:
                    if isinstance(topic, dict):
                        topic_list.append(topic)
                    elif isinstance(topic, str):
                        topic_list.append({"name": topic, "id": topic})
                
                for topic in topic_list:
                    topic_name = topic.get("name", topic.get("id", "unknown"))
                    if topic_name not in topic_performance:
                        topic_performance[topic_name] = {
                            "total_attempts": 0,
                            "low_score_attempts": 0,
                            "topic_id": topic.get("id", topic.get("topic_id", topic_name))
                        }
                    
                    topic_performance[topic_name]["total_attempts"] += 1
                    if score < 0.6:
                        topic_performance[topic_name]["low_score_attempts"] += 1
        
        # Find topics with poor performance (>30% low scores)
        poor_topics = []
        for topic_name, stats in topic_performance.items():
            if stats["total_attempts"] > 0:
                poor_ratio = stats["low_score_attempts"] / stats["total_attempts"]
                if poor_ratio > 0.3:  # More than 30% low scores
                    poor_topics.append({
                        "topic_name": topic_name,
                        "topic_id": stats["topic_id"],
                        "poor_ratio": poor_ratio,
                        "low_score_count": stats["low_score_attempts"],
                        "total_attempts": stats["total_attempts"]
                    })
        
        if poor_topics:
            poor_topics.sort(key=lambda x: x["poor_ratio"], reverse=True)
            content = f"Topics with poor performance in classroom {classroom_id}: "
            content += ", ".join([
                f"{t['topic_name']} ({t['poor_ratio']:.1%} low scores, {t['low_score_count']}/{t['total_attempts']} attempts)"
                for t in poor_topics[:10]
            ])
            
            documents.append({
                "content": content,
                "metadata": {
                    "document_type": "analytics_poor_topics",
                    "classroom_id": classroom_id,
                    "topic_count": len(poor_topics),
                    "topics": poor_topics[:10],
                    "intent_keywords": ["performing poorly", "poor performance", "weak topics", "chủ đề yếu"]
                },
                "provenance": {
                    "source": "analytics_virtual",
                    "source_id": f"analytics_poor_topics_{classroom_id}",
                    "extracted_at": datetime.utcnow().isoformat(),
                    "data_version": "1.0",
                    "extraction_method": "rule_based_analytics"
                },
                "confidence_score": 0.9,
                "document_id": f"analytics_poor_topics_{classroom_id}"
            })
        
        # 3. Students struggling with specific topics
        struggling_students = {}
        
        # Process quiz attempts
        for attempt in quiz_attempts:
            student_quiz = next(
                (sq for sq in student_quizzes if sq["id"] == attempt.get("student_quiz_id")),
                None
            )
            if not student_quiz:
                continue
            
            student_id = student_quiz.get("student_id")
            student_name = student_map.get(student_id, "unknown")
            score = attempt.get("score", 0)
            if not isinstance(score, (int, float)):
                try:
                    score = float(score)
                except (ValueError, TypeError):
                    score = 0
            
            # Get topics from question attempts
            question_attempts = attempt.get("question_attempts", [])
            for qa in question_attempts:
                topics = qa.get("topics", [])
                
                # Handle topics: can be list of dicts or list of strings
                topic_list = []
                for topic in topics:
                    if isinstance(topic, dict):
                        topic_list.append(topic)
                    elif isinstance(topic, str):
                        topic_list.append({"name": topic, "id": topic})
                
                if score < 0.5:  # Low score
                    for topic in topic_list:
                        topic_name = topic.get("name", topic.get("id", "unknown"))
                        key = f"{student_id}_{topic_name}"
                        
                        if key not in struggling_students:
                            struggling_students[key] = {
                                "student_id": student_id,
                                "student_name": student_name,
                                "topic_name": topic_name,
                                "topic_id": topic.get("id", topic.get("topic_id", "unknown")),
                                "low_score_count": 0,
                                "average_score": 0,
                                "scores": []
                            }
                        
                        struggling_students[key]["low_score_count"] += 1
                        struggling_students[key]["scores"].append(score)
                        struggling_students[key]["average_score"] = sum(struggling_students[key]["scores"]) / len(struggling_students[key]["scores"])
        
        # Process assignment attempts
        for attempt in assignment_attempts:
            student_assignment = next(
                (sa for sa in student_assignments if sa["id"] == attempt.get("student_assignment_id")),
                None
            )
            if not student_assignment:
                continue
            
            student_id = student_assignment.get("student_id")
            student_name = student_map.get(student_id, "unknown")
            score = attempt.get("score", 0)
            if not isinstance(score, (int, float)):
                try:
                    score = float(score)
                except (ValueError, TypeError):
                    score = 0
            
            # Get topics from question attempts
            question_attempts = attempt.get("question_attempts", [])
            for qa in question_attempts:
                topics = qa.get("topics", [])
                
                # Handle topics: can be list of dicts or list of strings
                topic_list = []
                for topic in topics:
                    if isinstance(topic, dict):
                        topic_list.append(topic)
                    elif isinstance(topic, str):
                        topic_list.append({"name": topic, "id": topic})
                
                if score < 0.5:  # Low score
                    for topic in topic_list:
                        topic_name = topic.get("name", topic.get("id", "unknown"))
                        key = f"{student_id}_{topic_name}"
                        
                        if key not in struggling_students:
                            struggling_students[key] = {
                                "student_id": student_id,
                                "student_name": student_name,
                                "topic_name": topic_name,
                                "topic_id": topic.get("id", topic.get("topic_id", "unknown")),
                                "low_score_count": 0,
                                "average_score": 0,
                                "scores": []
                            }
                        
                        struggling_students[key]["low_score_count"] += 1
                        struggling_students[key]["scores"].append(score)
                        struggling_students[key]["average_score"] = sum(struggling_students[key]["scores"]) / len(struggling_students[key]["scores"])
        
        # Filter: students with 2+ low scores on same topic
        struggling_list = [
            s for s in struggling_students.values()
            if s["low_score_count"] >= 2
        ]
        
        if struggling_list:
            struggling_list.sort(key=lambda x: x["average_score"])
            content = f"Students struggling with topics in classroom {classroom_id}: "
            content += ", ".join([
                f"{s['student_name']} with {s['topic_name']} (avg score: {s['average_score']:.2f}, {s['low_score_count']} low scores)"
                for s in struggling_list[:10]
            ])
            
            documents.append({
                "content": content,
                "metadata": {
                    "document_type": "analytics_struggling_students",
                    "classroom_id": classroom_id,
                    "struggling_count": len(struggling_list),
                    "struggling_students": struggling_list[:10],
                    "intent_keywords": ["struggling", "struggle", "yếu", "kém", "khó khăn"]
                },
                "provenance": {
                    "source": "analytics_virtual",
                    "source_id": f"analytics_struggling_{classroom_id}",
                    "extracted_at": datetime.utcnow().isoformat(),
                    "data_version": "1.0",
                    "extraction_method": "rule_based_analytics"
                },
                "confidence_score": 0.9,
                "document_id": f"analytics_struggling_{classroom_id}"
            })
        
        logger.info(f"Created {len(documents)} virtual analytics documents")
        return documents
    
    def _calculate_content_hash(self, content: str) -> str:
        """Calculate hash for content deduplication"""
        return hashlib.md5(content.encode()).hexdigest()
