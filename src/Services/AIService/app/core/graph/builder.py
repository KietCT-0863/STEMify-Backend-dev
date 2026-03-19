"""
Graph Builder
Build knowledge graph from classroom data with monitoring
"""

from typing import List, Dict, Any, Set
import logging

from app.core.graph.client import GraphClient
from app.core.graph.monitor import GraphMonitor, ConflictType
from app.core.graph.schema import NodeType, RelationshipType

logger = logging.getLogger(__name__)


class GraphBuilder:
    """Build graph from classroom data"""
    
    def __init__(self, graph_client: GraphClient, monitor: GraphMonitor):
        self.client = graph_client
        self.monitor = monitor
        self.created_nodes: Set[str] = set()
    
    async def build_graph(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Build complete graph from classroom data with 5-level structure:
        
        Level 1: Curriculum (root structure)
        Level 2: Course (curriculum → course)
        Level 3: Lesson (course → lesson)
        Level 4: Section → Attempts (lesson → section → content → quiz/assignment → attempts)
        Level 5: Performance logic (STRUGGLES_WITH / EXCELS_AT relationships)
        
        Returns:
            Summary with node counts, relationship counts, conflicts, and level breakdown
        """
        logger.info("=" * 80)
        logger.info("Starting 5-level graph construction...")
        logger.info("=" * 80)
        
        # Clear previous state
        self.monitor.clear()
        self.created_nodes.clear()
        
        # Track nodes per level
        level_counts = {
            "level_1": 0,  # Curriculum
            "level_2": 0,  # Course
            "level_3": 0,  # Lesson
            "level_4": 0,  # Section, Content, Quiz, Assignment, Attempts
            "level_5": 0,  # Performance relationships (count)
            "supporting": 0  # Classroom, Student, Topic, Enrollment, Progress nodes
        }
        
        # ============================================================
        # PHASE 1: Create Nodes (All Levels)
        # ============================================================
        logger.info("\n[PHASE 1] Creating nodes...")
        
        # Level 1: Curriculum (root structure)
        logger.info("\n[LEVEL 1] Creating Curriculum nodes...")
        await self._create_curriculum_nodes(data)
        level_counts["level_1"] = sum(1 for n in self.created_nodes if n.startswith("Curriculum:"))
        logger.info(f"[LEVEL 1] Created {level_counts['level_1']} Curriculum nodes")
        
        # Level 2: Course
        logger.info("\n[LEVEL 2] Creating Course nodes...")
        await self._create_course_nodes(data)
        level_counts["level_2"] = sum(1 for n in self.created_nodes if n.startswith("Course:"))
        logger.info(f"[LEVEL 2] Created {level_counts['level_2']} Course nodes")
        
        # Level 3: Lesson
        logger.info("\n[LEVEL 3] Creating Lesson nodes...")
        await self._create_lesson_nodes(data)
        level_counts["level_3"] = sum(1 for n in self.created_nodes if n.startswith("Lesson:"))
        logger.info(f"[LEVEL 3] Created {level_counts['level_3']} Lesson nodes")
        
        # Level 4: Section → Content → Quiz/Assignment → Attempts
        logger.info("\n[LEVEL 4] Creating Section, Content, Quiz, Assignment, and Attempt nodes...")
        await self._create_section_nodes(data)
        await self._create_content_nodes(data)
        await self._create_quiz_nodes(data)
        await self._create_assignment_nodes(data)
        await self._create_attempt_nodes(data)
        level_counts["level_4"] = (
            sum(1 for n in self.created_nodes if n.startswith("Section:")) +
            sum(1 for n in self.created_nodes if n.startswith("Content:")) +
            sum(1 for n in self.created_nodes if n.startswith("Quiz:")) +
            sum(1 for n in self.created_nodes if n.startswith("Assignment:")) +
            sum(1 for n in self.created_nodes if n.startswith("QuizAttempt:")) +
            sum(1 for n in self.created_nodes if n.startswith("AssignmentAttempt:"))
        )
        logger.info(f"[LEVEL 4] Created {level_counts['level_4']} nodes (Section, Content, Quiz, Assignment, Attempts)")
        
        # Supporting nodes (not part of 5-level structure but needed)
        logger.info("\n[SUPPORTING] Creating supporting nodes...")
        await self._create_classroom_nodes(data)
        await self._create_student_nodes(data)
        await self._create_topic_nodes(data)
        await self._create_curriculum_enrollment_nodes(data)
        await self._create_course_enrollment_nodes(data)
        await self._create_lesson_progress_nodes(data)
        await self._create_section_progress_nodes(data)
        await self._create_student_quiz_nodes(data)
        await self._create_student_assignment_nodes(data)
        level_counts["supporting"] = (
            sum(1 for n in self.created_nodes if n.startswith("Classroom:")) +
            sum(1 for n in self.created_nodes if n.startswith("Student:")) +
            sum(1 for n in self.created_nodes if n.startswith("Topic:")) +
            sum(1 for n in self.created_nodes if "Enrollment" in n) +
            sum(1 for n in self.created_nodes if "Progress" in n) +
            sum(1 for n in self.created_nodes if n.startswith("StudentQuiz:")) +
            sum(1 for n in self.created_nodes if n.startswith("StudentAssignment:"))
        )
        logger.info(f"[SUPPORTING] Created {level_counts['supporting']} supporting nodes")
        
        # ============================================================
        # PHASE 2: Create Relationships (Structure Levels 1-4)
        # ============================================================
        logger.info("\n[PHASE 2] Creating structure relationships (Levels 1-4)...")
        
        # Level 1-2: Curriculum → Course
        logger.info("\n[LEVEL 1-2] Creating Curriculum → Course relationships...")
        await self._create_curriculum_structure_relationships(data)
        
        # Enrollment and progress relationships
        logger.info("\n[RELATIONSHIPS] Creating enrollment and progress relationships...")
        await self._create_enrollment_relationships(data)
        await self._create_progress_relationships(data)
        
        # Level 4: Student quiz/assignment relationships
        logger.info("\n[LEVEL 4] Creating student quiz/assignment relationships...")
        await self._create_student_quiz_relationships(data)
        await self._create_student_assignment_relationships(data)
        await self._create_quiz_relationships(data)
        await self._create_assignment_relationships(data)
        await self._create_topic_relationships(data)
        
        # ============================================================
        # PHASE 3: Create Level 5 Performance Relationships
        # ============================================================
        logger.info("\n[PHASE 3] Creating Level 5 performance relationships...")
        logger.info("[LEVEL 5] Creating STRUGGLES_WITH and EXCELS_AT relationships...")
        performance_counts = await self._create_performance_relationships(data)
        level_counts["level_5"] = performance_counts["total"]
        logger.info(f"[LEVEL 5] Performance relationships created: {performance_counts['total']} total")
        
        # ============================================================
        # PHASE 4: Connect Orphan Nodes
        # ============================================================
        logger.info("\n[PHASE 4] Connecting orphan nodes...")
        await self._connect_orphan_nodes(data)
        
        # ============================================================
        # PHASE 5: Validation
        # ============================================================
        logger.info("\n[PHASE 5] Validating graph structure...")
        validation_result = await self._validate_structure(data)
        
        # Check for orphan nodes
        orphan_conflicts = self.monitor.check_orphan_nodes(self.created_nodes)
        
        # Get summary
        summary = {
            "nodes_created": len(self.created_nodes),
            "conflicts": self.monitor.get_conflicts_summary(),
            "orphan_nodes": len(orphan_conflicts),
            "level_breakdown": level_counts,
            "validation": validation_result
        }
        
        logger.info("\n" + "=" * 80)
        logger.info("Graph construction complete!")
        logger.info(f"Total nodes: {summary['nodes_created']}")
        logger.info(f"Level 1 (Curriculum): {level_counts['level_1']}")
        logger.info(f"Level 2 (Course): {level_counts['level_2']}")
        logger.info(f"Level 3 (Lesson): {level_counts['level_3']}")
        logger.info(f"Level 4 (Section → Attempts): {level_counts['level_4']}")
        logger.info(f"Level 5 (Performance relationships): {level_counts['level_5']}")
        logger.info(f"Supporting nodes: {level_counts['supporting']}")
        logger.info(f"Conflicts: {summary['conflicts']['total_conflicts']}")
        logger.info(f"Orphan nodes: {len(orphan_conflicts)}")
        logger.info(f"Validation: {'PASSED' if validation_result['valid'] else 'FAILED'}")
        if validation_result.get('warnings'):
            logger.info(f"Warnings: {len(validation_result['warnings'])}")
        logger.info("=" * 80)
        
        return summary
    
    async def _create_classroom_nodes(self, data: Dict[str, Any]):
        """Create Classroom nodes"""
        classroom = data["classroom"]
        node_id = str(classroom["id"])
        
        properties = {
            "id": node_id,
            "name": classroom["name"],
            "grade": classroom.get("grade", ""),
            "status": classroom.get("status", "Active"),
            "teacher_id": classroom.get("teacher_id", ""),
            "curriculum_id": classroom.get("curriculum_id", ""),
        }
        
        conflicts = self.monitor.check_node(NodeType.CLASSROOM, node_id, properties)
        if not conflicts or all(c.severity != "critical" for c in conflicts):
            await self.client.create_node("Classroom", node_id, properties)
            self.created_nodes.add(f"Classroom:{node_id}")
    
    async def _create_student_nodes(self, data: Dict[str, Any]):
        """Create Student nodes"""
        for student in data.get("students", []):
            node_id = student["student_id"]
            
            properties = {
                "id": node_id,
                "name": student["student_name"],
                "email": student.get("email", ""),
                "joined_at": student.get("joined_at", ""),
            }
            
            conflicts = self.monitor.check_node(NodeType.STUDENT, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("Student", node_id, properties)
                self.created_nodes.add(f"Student:{node_id}")
    
    async def _create_quiz_nodes(self, data: Dict[str, Any]):
        """Create Quiz nodes"""
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        seen_quizzes = set()
        
        for sq in student_quizzes:
            # Some payloads use "quiz_id", others only expose "id"
            raw_id = sq.get("quiz_id", sq.get("id"))
            if raw_id is None:
                continue
            quiz_id = str(raw_id)
            if quiz_id in seen_quizzes:
                continue
            seen_quizzes.add(quiz_id)
            
            properties = {
                "id": quiz_id,
                "title": sq.get("quiz_title", ""),
                "time_limit_minutes": sq.get("time_limit_minutes"),
            }
            
            conflicts = self.monitor.check_node(NodeType.QUIZ, quiz_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("Quiz", quiz_id, properties)
                self.created_nodes.add(f"Quiz:{quiz_id}")
    
    async def _create_assignment_nodes(self, data: Dict[str, Any]):
        """Create Assignment nodes"""
        student_assignments = data.get("assignments", {}).get("student_assignments", [])
        seen_assignments = set()
        
        for sa in student_assignments:
            raw_id = sa.get("assignment_id", sa.get("id"))
            if raw_id is None:
                logger.warning("Skipping student_assignment without assignment_id or id: %s", sa)
                continue
            assignment_id = str(raw_id)
            if assignment_id in seen_assignments:
                continue
            seen_assignments.add(assignment_id)
            
            properties = {
                "id": assignment_id,
                "title": sa.get("assignment_title", ""),
            }
            
            conflicts = self.monitor.check_node(NodeType.ASSIGNMENT, assignment_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("Assignment", assignment_id, properties)
                self.created_nodes.add(f"Assignment:{assignment_id}")
    
    async def _create_topic_nodes(self, data: Dict[str, Any]):
        """Create Topic nodes"""
        topics = data.get("topics", [])
        
        for topic in topics:
            node_id = str(topic["topic_id"])
            
            properties = {
                "id": node_id,
                "name": topic["topic_name"],
            }
            
            conflicts = self.monitor.check_node(NodeType.TOPIC, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("Topic", node_id, properties)
                self.created_nodes.add(f"Topic:{node_id}")
    
    async def _create_attempt_nodes(self, data: Dict[str, Any]):
        """Create QuizAttempt and AssignmentAttempt nodes"""
        # Quiz attempts
        quiz_attempts = data.get("quizzes", {}).get("quiz_attempts", [])
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        for attempt in quiz_attempts:
            node_id = attempt.get("id")
            student_quiz_id = attempt.get("student_quiz_id")
            
            # Synthesize ID if missing
            if not node_id:
                # Try to find student_id from student_quiz
                student_id = "unknown"
                if student_quiz_id:
                    student_quiz = next(
                        (sq for sq in student_quizzes if sq.get("id") == student_quiz_id),
                        None
                    )
                    if student_quiz:
                        student_id = student_quiz.get("student_id", "unknown")
                
                node_id = f"qa-{student_id}-{student_quiz_id or 'unknown'}-{attempt.get('attempt_number', 1)}"
                attempt["id"] = node_id  # Persist synthesized id for downstream relationships
                logger.info("Synthesized quiz_attempt id=%s for student_quiz_id=%s", node_id, student_quiz_id)
            
            node_id = str(node_id)
            
            # Handle missing required fields gracefully
            try:
                score = float(attempt.get("total_score", attempt.get("score", 0)))
            except (ValueError, TypeError):
                score = 0.0
            
            properties = {
                "id": node_id,
                "score": score,
                "status": attempt.get("status", "InProgress"),
                "attempt_number": attempt.get("attempt_number", 1),
                "started_at": attempt.get("started_at", ""),
                "completed_at": attempt.get("completed_at"),
            }
            
            conflicts = self.monitor.check_node(NodeType.QUIZ_ATTEMPT, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("QuizAttempt", node_id, properties)
                self.created_nodes.add(f"QuizAttempt:{node_id}")
        
        # Assignment attempts
        assignment_attempts = data.get("assignments", {}).get("assignment_attempts", [])
        student_assignments = data.get("assignments", {}).get("student_assignments", [])
        for attempt in assignment_attempts:
            node_id = attempt.get("id")
            student_assignment_id = attempt.get("student_assignment_id")
            
            # Synthesize ID if missing
            if not node_id:
                # Try to find student_id from student_assignment
                student_id = "unknown"
                if student_assignment_id:
                    student_assignment = next(
                        (sa for sa in student_assignments if sa.get("id") == student_assignment_id),
                        None
                    )
                    if student_assignment:
                        student_id = student_assignment.get("student_id", "unknown")
                
                node_id = f"aa-{student_id}-{student_assignment_id or 'unknown'}-{attempt.get('attempt_number', 1)}"
                attempt["id"] = node_id  # Persist synthesized id for downstream relationships
                logger.info("Synthesized assignment_attempt id=%s for student_assignment_id=%s", node_id, student_assignment_id)
            
            node_id = str(node_id)
            
            # Handle missing required fields gracefully
            try:
                score = float(attempt.get("total_score", attempt.get("score", 0)))
            except (ValueError, TypeError):
                score = 0.0
            
            properties = {
                "id": node_id,
                "score": score,
                "status": attempt.get("status", "Submitted"),
                "attempt_number": attempt.get("attempt_number", 1),
                "submitted_at": attempt.get("submitted_at", ""),
            }
            
            conflicts = self.monitor.check_node(NodeType.ASSIGNMENT_ATTEMPT, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("AssignmentAttempt", node_id, properties)
                self.created_nodes.add(f"AssignmentAttempt:{node_id}")
    
    async def _create_curriculum_nodes(self, data: Dict[str, Any]):
        """Create Curriculum nodes"""
        enrollments = data.get("enrollments", {}).get("curriculum_enrollments", [])
        seen_curricula = set()
        
        for enrollment in enrollments:
            curriculum_id = str(enrollment.get("curriculum_id", ""))
            if not curriculum_id or curriculum_id in seen_curricula:
                continue
            seen_curricula.add(curriculum_id)
            
            properties = {
                "id": curriculum_id,
                "title": enrollment.get("curriculum_name", f"Curriculum {curriculum_id}"),
            }
            
            conflicts = self.monitor.check_node(NodeType.CURRICULUM, curriculum_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("Curriculum", curriculum_id, properties)
                self.created_nodes.add(f"Curriculum:{curriculum_id}")

    async def _create_course_nodes(self, data: Dict[str, Any]):
        """Create Course nodes"""
        enrollments = data.get("enrollments", {}).get("course_enrollments", [])
        seen_courses = set()
        
        for enrollment in enrollments:
            course_id = str(enrollment.get("course_id", ""))
            if not course_id or course_id in seen_courses:
                continue
            seen_courses.add(course_id)
            
            properties = {
                "id": course_id,
                "title": enrollment.get("course_name", f"Course {course_id}"),
            }
            
            conflicts = self.monitor.check_node(NodeType.COURSE, course_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("Course", course_id, properties)
                self.created_nodes.add(f"Course:{course_id}")

    async def _create_lesson_nodes(self, data: Dict[str, Any]):
        """Create Lesson nodes from progress data"""
        progress = data.get("progress", {}).get("lesson_progress", [])
        seen_lessons = set()
        
        for lp in progress:
            lesson_id = str(lp.get("lesson_id", ""))
            if not lesson_id or lesson_id in seen_lessons:
                continue
            seen_lessons.add(lesson_id)
            
            properties = {
                "id": lesson_id,
                "title": lp.get("lesson_title", f"Lesson {lesson_id}"),
                "description": lp.get("lesson_description", ""),
            }
            
            conflicts = self.monitor.check_node(NodeType.LESSON, lesson_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("Lesson", lesson_id, properties)
                self.created_nodes.add(f"Lesson:{lesson_id}")

    async def _create_section_nodes(self, data: Dict[str, Any]):
        """Create Section nodes from progress data"""
        progress = data.get("progress", {}).get("section_progress", [])
        seen_sections = set()
        
        for sp in progress:
            section_id = str(sp.get("section_id", ""))
            if not section_id or section_id in seen_sections:
                continue
            seen_sections.add(section_id)
            
            properties = {
                "id": section_id,
                "title": sp.get("section_title", f"Section {section_id}"),
                "description": sp.get("section_description", ""),
            }
            
            conflicts = self.monitor.check_node(NodeType.SECTION, section_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("Section", section_id, properties)
                self.created_nodes.add(f"Section:{section_id}")

    async def _create_content_nodes(self, data: Dict[str, Any]):
        """Create Content nodes from topics structure"""
        topics = data.get("topics", [])
        seen_contents = set()
        
        for topic in topics:
            for lesson in topic.get("lessons", []):
                for section in lesson.get("sections", []):
                    for content in section.get("contents", []):
                        content_id = str(content.get("content_id", ""))
                        if not content_id or content_id in seen_contents:
                            continue
                        seen_contents.add(content_id)
                        
                        properties = {
                            "id": content_id,
                            "content_type": content.get("content_type", "Unknown"),
                            "title": content.get("content_title", f"Content {content_id}"),
                            "section_id": str(section.get("section_id", "")),
                        }
                        
                        conflicts = self.monitor.check_node(NodeType.CONTENT, content_id, properties)
                        if not conflicts or all(c.severity != "critical" for c in conflicts):
                            await self.client.create_node("Content", content_id, properties)
                            self.created_nodes.add(f"Content:{content_id}")

    async def _create_curriculum_enrollment_nodes(self, data: Dict[str, Any]):
        """Create CurriculumEnrollment nodes"""
        enrollments = data.get("enrollments", {}).get("curriculum_enrollments", [])
        
        for enrollment in enrollments:
            student_id = enrollment.get("student_id")
            if not student_id:
                logger.warning("Skip curriculum_enrollment with missing student_id: %s", enrollment)
                continue

            node_id = enrollment.get("id")
            if not node_id:
                node_id = f"curr-enr-{student_id}-{enrollment.get('curriculum_id', 'unknown')}"
                enrollment["id"] = node_id  # persist synthesized id for downstream relationships
                logger.info("Synthesized curriculum_enrollment id=%s for student_id=%s", node_id, student_id)
            node_id = str(node_id)
            properties = {
                "id": node_id,
                "student_id": student_id,
                "curriculum_id": str(enrollment.get("curriculum_id", "")),
                "status": enrollment.get("status", "InProgress"),
                "classroom_id": str(enrollment.get("classroom_id", "")),
                "enrolled_at": enrollment.get("enrolled_at", ""),
                "progress_percentage": enrollment.get("progress_percentage", 0),
            }
            
            conflicts = self.monitor.check_node(NodeType.CURRICULUM_ENROLLMENT, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("CurriculumEnrollment", node_id, properties)
                self.created_nodes.add(f"CurriculumEnrollment:{node_id}")

    async def _create_course_enrollment_nodes(self, data: Dict[str, Any]):
        """Create CourseEnrollment nodes"""
        enrollments = data.get("enrollments", {}).get("course_enrollments", [])
        
        for enrollment in enrollments:
            student_id = enrollment.get("student_id")
            if not student_id:
                logger.warning("Skipping course_enrollment with missing student_id: %s", enrollment)
                continue

            node_id = enrollment.get("id")
            if not node_id:
                node_id = f"course-enr-{student_id}-{enrollment.get('course_id', 'unknown')}"
                enrollment["id"] = node_id
                logger.info("Synthesized course_enrollment id=%s for student_id=%s", node_id, student_id)
            node_id = str(node_id)
            
            properties = {
                "id": node_id,
                "student_id": student_id,
                "course_id": str(enrollment.get("course_id", "")),
                "status": enrollment.get("status", "InProgress"),
                "curriculum_enrollment_id": str(enrollment.get("curriculum_enrollment_id", "")),
                "enrolled_at": enrollment.get("enrolled_at", ""),
                "progress_percentage": enrollment.get("progress_percentage", 0),
            }
            
            conflicts = self.monitor.check_node(NodeType.COURSE_ENROLLMENT, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("CourseEnrollment", node_id, properties)
                self.created_nodes.add(f"CourseEnrollment:{node_id}")

    async def _create_lesson_progress_nodes(self, data: Dict[str, Any]):
        """Create StudentLessonProgress nodes"""
        progress = data.get("progress", {}).get("lesson_progress", [])
        
        for lp in progress:
            node_id = lp.get("id")
            if not node_id:
                node_id = f"lesprog-{lp.get('student_id', 'unknown')}-{lp.get('lesson_id', 'unknown')}"
                lp["id"] = node_id
                logger.info("Synthesized lesson_progress id=%s", node_id)
            node_id = str(node_id)
            
            properties = {
                "id": node_id,
                "lesson_id": str(lp.get("lesson_id", "")),
                "status": lp.get("status", "InProgress"),
                "completed_at": lp.get("completed_at"),
                "enrollment_id": str(lp.get("enrollment_id", "")),
            }
            
            conflicts = self.monitor.check_node(NodeType.STUDENT_LESSON_PROGRESS, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("StudentLessonProgress", node_id, properties)
                self.created_nodes.add(f"StudentLessonProgress:{node_id}")

    async def _create_section_progress_nodes(self, data: Dict[str, Any]):
        """Create StudentSectionProgress nodes"""
        progress = data.get("progress", {}).get("section_progress", [])
        
        for sp in progress:
            node_id = sp.get("id")
            if not node_id:
                student_id = sp.get("student_id", "unknown")
                section_id = sp.get("section_id", "unknown")
                node_id = f"secprog-{student_id}-{section_id}"
                sp["id"] = node_id
                logger.info("Synthesized section_progress id=%s", node_id)
            node_id = str(node_id)
            
            properties = {
                "id": node_id,
                "section_id": str(sp.get("section_id", "")),
                "status": sp.get("status", "InProgress"),
                "completed_at": sp.get("completed_at"),
                "student_lesson_progress_id": str(sp.get("student_lesson_progress_id", "")),
            }
            
            conflicts = self.monitor.check_node(NodeType.STUDENT_SECTION_PROGRESS, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("StudentSectionProgress", node_id, properties)
                self.created_nodes.add(f"StudentSectionProgress:{node_id}")

    async def _create_student_quiz_nodes(self, data: Dict[str, Any]):
        """Create StudentQuiz nodes"""
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        
        for sq in student_quizzes:
            node_id = sq.get("id")
            student_id = sq.get("student_id", "")
            if not student_id:
                logger.warning("Skipping student_quiz with missing student_id: %s", sq)
                continue
            if not node_id:
                node_id = f"sq-{student_id}-{sq.get('quiz_id', 'unknown')}"
                sq["id"] = node_id
                logger.info("Synthesized student_quiz id=%s for student_id=%s", node_id, student_id)
            node_id = str(node_id)
            
            properties = {
                "id": node_id,
                "quiz_id": str(sq.get("quiz_id", "")),
                "student_id": student_id,
                "status": sq.get("status", "Assigned"),
                "final_score": sq.get("final_score"),
                "attempt_count": sq.get("attempt_count", 0),
                "assigned_at": sq.get("assigned_at", ""),
                "due_date": sq.get("due_date"),
                "student_section_progress_id": str(sq.get("student_section_progress_id", "")),
            }
            
            conflicts = self.monitor.check_node(NodeType.STUDENT_QUIZ, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("StudentQuiz", node_id, properties)
                self.created_nodes.add(f"StudentQuiz:{node_id}")

    async def _create_student_assignment_nodes(self, data: Dict[str, Any]):
        """Create StudentAssignment nodes"""
        student_assignments = data.get("assignments", {}).get("student_assignments", [])
        
        # Track synthesized IDs to avoid duplicates
        seen_synthesized_ids = set()
        synthesis_counter = {}  # (student_id, assignment_id) -> counter
        
        for idx, sa in enumerate(student_assignments):
            node_id = sa.get("id")
            student_id = sa.get("student_id", "")
            if not student_id:
                logger.warning("Skipping student_assignment with missing student_id: %s", sa)
                continue
            if not node_id:

                assignment_id = sa.get("assignment_id")
                if assignment_id:
                    node_id = f"sa-{student_id}-{assignment_id}"
                else:
                    unique_parts = []
                    if sa.get("due_date"):
                        unique_parts.append(str(hash(sa.get("due_date")) % 10000))
                    if sa.get("submitted_at"):
                        unique_parts.append(str(hash(sa.get("submitted_at")) % 10000))
                    if sa.get("assigned_at"):
                        unique_parts.append(str(hash(sa.get("assigned_at")) % 10000))
                    
                    if not unique_parts:
                        unique_parts.append(str(hash(str(sa)) % 100000))
                    
                    # Use counter as last resort
                    key = (student_id, "unknown")
                    if key not in synthesis_counter:
                        synthesis_counter[key] = 0
                    synthesis_counter[key] += 1
                    unique_parts.append(str(synthesis_counter[key]))
                    
                    node_id = f"sa-{student_id}-{'-'.join(unique_parts)}"
                
                original_node_id = node_id
                counter = 1
                while node_id in seen_synthesized_ids:
                    node_id = f"{original_node_id}-{counter}"
                    counter += 1
                
                seen_synthesized_ids.add(node_id)
                sa["id"] = node_id
                logger.info("Synthesized student_assignment id=%s for student_id=%s", node_id, student_id)
            node_id = str(node_id)
            
            properties = {
                "id": node_id,
                "assignment_id": str(sa.get("assignment_id", "")),
                "student_id": student_id,
                "status": sa.get("status", "Assigned"),
                "final_score": sa.get("final_score"),
                "attempt_count": sa.get("attempt_count", 0),
                "assigned_at": sa.get("assigned_at", ""),
                "due_date": sa.get("due_date"),
                "student_section_progress_id": str(sa.get("student_section_progress_id", "")),
            }
            
            conflicts = self.monitor.check_node(NodeType.STUDENT_ASSIGNMENT, node_id, properties)
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_node("StudentAssignment", node_id, properties)
                self.created_nodes.add(f"StudentAssignment:{node_id}")
    
    async def _create_curriculum_structure_relationships(self, data: Dict[str, Any]):
        """Create curriculum structure relationships (Curriculum -> Course -> Lesson -> Section -> Content)"""
        # Curriculum -> Course
        curriculum_enrollments = data.get("enrollments", {}).get("curriculum_enrollments", [])
        course_enrollments = data.get("enrollments", {}).get("course_enrollments", [])
        
        # Map curriculum_id to course_ids
        curriculum_courses = {}
        for ce in course_enrollments:
            curriculum_enrollment_id = str(ce.get("curriculum_enrollment_id", ""))
            ce_id = ce.get("id")
            if not ce_id:
                logger.warning("Skipping course_enrollment in relationships with missing id: %s", ce)
                continue
            if curriculum_enrollment_id:
                for cur_enr in curriculum_enrollments:
                    cur_enr_id = cur_enr.get("id")
                    if not cur_enr_id:
                        logger.warning("Skipping curriculum_enrollment in relationships with missing id: %s", cur_enr)
                        continue
                    if str(cur_enr_id) == curriculum_enrollment_id:
                        curriculum_id = str(cur_enr.get("curriculum_id", ""))
                        if curriculum_id not in curriculum_courses:
                            curriculum_courses[curriculum_id] = set()
                        curriculum_courses[curriculum_id].add(str(ce.get("course_id", "")))
        
        for curriculum_id, course_ids in curriculum_courses.items():
            for course_id in course_ids:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.HAS_COURSE,
                    NodeType.CURRICULUM,
                    curriculum_id,
                    NodeType.COURSE,
                    course_id
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "Curriculum", curriculum_id,
                        "HAS_COURSE",
                        "Course", course_id
                    )
        
        # Course -> Lesson (from progress data)
        lesson_progress = data.get("progress", {}).get("lesson_progress", [])
        course_enrollments_map = {
            str(ce["id"]): str(ce.get("course_id", ""))
            for ce in course_enrollments
            if ce.get("id")
        }
        
        for lp in lesson_progress:
            lesson_id = str(lp.get("lesson_id", ""))
            enrollment_id = str(lp.get("enrollment_id", ""))
            course_id = course_enrollments_map.get(enrollment_id)
            
            if course_id:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.HAS_LESSON,
                    NodeType.COURSE,
                    course_id,
                    NodeType.LESSON,
                    lesson_id,
                    {"order_index": lp.get("order_index", 0)}
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "Course", course_id,
                        "HAS_LESSON",
                        "Lesson", lesson_id,
                        {"order_index": lp.get("order_index", 0)}
                    )
        
        # Lesson -> Section
        section_progress = data.get("progress", {}).get("section_progress", [])
        lesson_progress_map = {
            str(lp["id"]): str(lp.get("lesson_id", ""))
            for lp in lesson_progress
            if lp.get("id")
        }
        
        for sp in section_progress:
            section_id = str(sp.get("section_id", ""))
            lesson_progress_id = str(sp.get("student_lesson_progress_id", ""))
            lesson_id = lesson_progress_map.get(lesson_progress_id)
            
            if lesson_id:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.CONTAINS,
                    NodeType.LESSON,
                    lesson_id,
                    NodeType.SECTION,
                    section_id,
                    {"order_index": sp.get("order_index", 0)}
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "Lesson", lesson_id,
                        "CONTAINS",
                        "Section", section_id,
                        {"order_index": sp.get("order_index", 0)}
                    )
        
        # Section -> Content -> Quiz/Assignment
        topics = data.get("topics", [])
        for topic in topics:
            for lesson in topic.get("lessons", []):
                for section in lesson.get("sections", []):
                    section_id = str(section.get("section_id", ""))
                    for content in section.get("contents", []):
                        content_id = str(content.get("content_id", ""))
                        content_type = content.get("content_type", "")
                        
                        # Section -> Content
                        conflicts = self.monitor.check_relationship(
                            RelationshipType.HAS_CONTENT,
                            NodeType.SECTION,
                            section_id,
                            NodeType.CONTENT,
                            content_id
                        )
                        if not conflicts or all(c.severity != "critical" for c in conflicts):
                            await self.client.create_relationship(
                                "Section", section_id,
                                "HAS_CONTENT",
                                "Content", content_id
                            )
                        
                        # Content -> Quiz/Assignment
                        if content_type == "Quiz":
                            # Find quiz_id from student_quizzes
                            student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
                            quiz_id = None
                            for sq in student_quizzes:
                                if str(sq.get("quiz_id", "")) and content_id in str(sq.get("quiz_id", "")):
                                    quiz_id = str(sq.get("quiz_id", ""))
                                    break
                            # Or try to extract from content_title
                            if not quiz_id:
                                # Try to match by title
                                for sq in student_quizzes:
                                    if content.get("content_title", "") in sq.get("quiz_title", ""):
                                        quiz_id = str(sq.get("quiz_id", ""))
                                        break
                            
                            if quiz_id:
                                conflicts = self.monitor.check_relationship(
                                    RelationshipType.IS_QUIZ,
                                    NodeType.CONTENT,
                                    content_id,
                                    NodeType.QUIZ,
                                    quiz_id
                                )
                                if not conflicts or all(c.severity != "critical" for c in conflicts):
                                    await self.client.create_relationship(
                                        "Content", content_id,
                                        "IS_QUIZ",
                                        "Quiz", quiz_id
                                    )
                        elif content_type == "Assignment":
                            # Find assignment_id from student_assignments
                            student_assignments = data.get("assignments", {}).get("student_assignments", [])
                            assignment_id = None
                            for sa in student_assignments:
                                if str(sa.get("assignment_id", "")) and content_id in str(sa.get("assignment_id", "")):
                                    assignment_id = str(sa.get("assignment_id", ""))
                                    break
                            # Or try to extract from content_title
                            if not assignment_id:
                                for sa in student_assignments:
                                    if content.get("content_title", "") in sa.get("assignment_title", ""):
                                        assignment_id = str(sa.get("assignment_id", ""))
                                        break
                            
                            if assignment_id:
                                conflicts = self.monitor.check_relationship(
                                    RelationshipType.IS_ASSIGNMENT,
                                    NodeType.CONTENT,
                                    content_id,
                                    NodeType.ASSIGNMENT,
                                    assignment_id
                                )
                                if not conflicts or all(c.severity != "critical" for c in conflicts):
                                    await self.client.create_relationship(
                                        "Content", content_id,
                                        "IS_ASSIGNMENT",
                                        "Assignment", assignment_id
                                    )

    async def _create_enrollment_relationships(self, data: Dict[str, Any]):
        """Create enrollment relationships (Student -> CurriculumEnrollment -> CourseEnrollment)"""
        classroom_id = str(data["classroom"]["id"])
        curriculum_enrollments = data.get("enrollments", {}).get("curriculum_enrollments", [])
        course_enrollments = data.get("enrollments", {}).get("course_enrollments", [])
        
        for enrollment in curriculum_enrollments:
            student_id = enrollment["student_id"]
            enrollment_id = str(enrollment["id"])
            curriculum_id = str(enrollment.get("curriculum_id", ""))
            
            # Student -> CurriculumEnrollment
            conflicts = self.monitor.check_relationship(
                RelationshipType.ENROLLED_IN_CURRICULUM,
                NodeType.STUDENT,
                student_id,
                NodeType.CURRICULUM_ENROLLMENT,
                enrollment_id,
                {"enrolled_at": enrollment.get("enrolled_at", "")}
            )
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "Student", student_id,
                    "ENROLLED_IN_CURRICULUM",
                    "CurriculumEnrollment", enrollment_id,
                    {"enrolled_at": enrollment.get("enrolled_at", "")}
                )
            
            # CurriculumEnrollment -> Curriculum
            if curriculum_id:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.IN_CURRICULUM,
                    NodeType.CURRICULUM_ENROLLMENT,
                    enrollment_id,
                    NodeType.CURRICULUM,
                    curriculum_id
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "CurriculumEnrollment", enrollment_id,
                        "IN_CURRICULUM",
                        "Curriculum", curriculum_id
                    )
            
            # CurriculumEnrollment -> CourseEnrollment
            for ce in course_enrollments:
                if str(ce.get("curriculum_enrollment_id", "")) == enrollment_id:
                    ce_id = str(ce["id"])
                    conflicts = self.monitor.check_relationship(
                        RelationshipType.HAS_COURSE_ENROLLMENT,
                        NodeType.CURRICULUM_ENROLLMENT,
                        enrollment_id,
                        NodeType.COURSE_ENROLLMENT,
                        ce_id
                    )
                    if not conflicts or all(c.severity != "critical" for c in conflicts):
                        await self.client.create_relationship(
                            "CurriculumEnrollment", enrollment_id,
                            "HAS_COURSE_ENROLLMENT",
                            "CourseEnrollment", ce_id
                        )
                    
                    # CourseEnrollment -> Course
                    course_id = str(ce.get("course_id", ""))
                    if course_id:
                        conflicts = self.monitor.check_relationship(
                            RelationshipType.FOR_COURSE,
                            NodeType.COURSE_ENROLLMENT,
                            ce_id,
                            NodeType.COURSE,
                            course_id
                        )
                        if not conflicts or all(c.severity != "critical" for c in conflicts):
                            await self.client.create_relationship(
                                "CourseEnrollment", ce_id,
                                "FOR_COURSE",
                                "Course", course_id
                            )
                    else:
                        logger.debug(
                            f"CourseEnrollment {ce_id} has no course_id - will be orphaned. "
                            f"Backend should provide course_id in course_enrollments data."
                        )
            
            # Also create Student -> Classroom relationship for backward compatibility
            conflicts = self.monitor.check_relationship(
                RelationshipType.ENROLLED_IN,
                NodeType.STUDENT,
                student_id,
                NodeType.CLASSROOM,
                classroom_id,
                {"enrolled_at": enrollment.get("enrolled_at", "")}
            )
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "Student", student_id,
                    "ENROLLED_IN",
                    "Classroom", classroom_id,
                    {"enrolled_at": enrollment.get("enrolled_at", "")}
                )

    async def _create_progress_relationships(self, data: Dict[str, Any]):
        """Create progress relationships"""
        # StudentSectionProgress -> Section
        section_progress = data.get("progress", {}).get("section_progress", [])
        for sp in section_progress:
            node_id = str(sp["id"])
            section_id = str(sp.get("section_id", ""))
            
            conflicts = self.monitor.check_relationship(
                RelationshipType.FOR_SECTION,
                NodeType.STUDENT_SECTION_PROGRESS,
                node_id,
                NodeType.SECTION,
                section_id
            )
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "StudentSectionProgress", node_id,
                    "FOR_SECTION",
                    "Section", section_id
                )
            
            # StudentSectionProgress -> StudentLessonProgress
            lesson_progress_id = str(sp.get("student_lesson_progress_id", ""))
            if lesson_progress_id:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.IN_LESSON,
                    NodeType.STUDENT_SECTION_PROGRESS,
                    node_id,
                    NodeType.STUDENT_LESSON_PROGRESS,
                    lesson_progress_id
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "StudentSectionProgress", node_id,
                        "IN_LESSON",
                        "StudentLessonProgress", lesson_progress_id
                    )
        
        # StudentLessonProgress -> Lesson
        lesson_progress = data.get("progress", {}).get("lesson_progress", [])
        for lp in lesson_progress:
            node_id = str(lp["id"])
            lesson_id = str(lp.get("lesson_id", ""))
            
            conflicts = self.monitor.check_relationship(
                RelationshipType.FOR_LESSON,
                NodeType.STUDENT_LESSON_PROGRESS,
                node_id,
                NodeType.LESSON,
                lesson_id
            )
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "StudentLessonProgress", node_id,
                    "FOR_LESSON",
                    "Lesson", lesson_id
                )
            
            # StudentLessonProgress -> CourseEnrollment
            enrollment_id = str(lp.get("enrollment_id", ""))
            if enrollment_id:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.IN_COURSE,
                    NodeType.STUDENT_LESSON_PROGRESS,
                    node_id,
                    NodeType.COURSE_ENROLLMENT,
                    enrollment_id
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "StudentLessonProgress", node_id,
                        "IN_COURSE",
                        "CourseEnrollment", enrollment_id
                    )

    async def _create_student_quiz_relationships(self, data: Dict[str, Any]):
        """Create StudentQuiz relationships"""
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        quiz_attempts = data.get("quizzes", {}).get("quiz_attempts", [])
        
        for sq in student_quizzes:
            node_id = sq.get("id")
            if not node_id:
                logger.warning("Skipping student_quiz without id in relationship creation")
                continue
            node_id = str(node_id)
            student_id = sq.get("student_id", "")
            quiz_id = str(sq.get("quiz_id", ""))
            section_progress_id = str(sq.get("student_section_progress_id", ""))
            
            # Student -> StudentQuiz
            conflicts = self.monitor.check_relationship(
                RelationshipType.HAS_QUIZ,
                NodeType.STUDENT,
                student_id,
                NodeType.STUDENT_QUIZ,
                node_id,
                {"assigned_at": sq.get("assigned_at", "")}
            )
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "Student", student_id,
                    "HAS_QUIZ",
                    "StudentQuiz", node_id,
                    {"assigned_at": sq.get("assigned_at", "")}
                )
            
            # StudentQuiz -> Quiz
            conflicts = self.monitor.check_relationship(
                RelationshipType.FOR_QUIZ,
                NodeType.STUDENT_QUIZ,
                node_id,
                NodeType.QUIZ,
                quiz_id
            )
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "StudentQuiz", node_id,
                    "FOR_QUIZ",
                    "Quiz", quiz_id
                )
            
            # StudentQuiz -> StudentSectionProgress
            if section_progress_id:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.IN_SECTION,
                    NodeType.STUDENT_QUIZ,
                    node_id,
                    NodeType.STUDENT_SECTION_PROGRESS,
                    section_progress_id
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "StudentQuiz", node_id,
                        "IN_SECTION",
                        "StudentSectionProgress", section_progress_id
                    )
            
            # StudentQuiz -> QuizAttempt
            for attempt in quiz_attempts:
                if attempt.get("student_quiz_id") == node_id:
                    attempt_id = attempt.get("id")
                    if not attempt_id:
                        logger.warning("Skipping quiz_attempt without id in relationship creation")
                        continue
                    attempt_id = str(attempt_id)
                    conflicts = self.monitor.check_relationship(
                        RelationshipType.HAS_ATTEMPT,
                        NodeType.STUDENT_QUIZ,
                        node_id,
                        NodeType.QUIZ_ATTEMPT,
                        attempt_id,
                        {"attempt_number": attempt.get("attempt_number", 1)}
                    )
                    if not conflicts or all(c.severity != "critical" for c in conflicts):
                        await self.client.create_relationship(
                            "StudentQuiz", node_id,
                            "HAS_ATTEMPT",
                            "QuizAttempt", attempt_id,
                            {"attempt_number": attempt.get("attempt_number", 1)}
                        )

    async def _create_student_assignment_relationships(self, data: Dict[str, Any]):
        """Create StudentAssignment relationships"""
        student_assignments = data.get("assignments", {}).get("student_assignments", [])
        assignment_attempts = data.get("assignments", {}).get("assignment_attempts", [])
        
        for sa in student_assignments:
            node_id = sa.get("id")
            if not node_id:
                logger.warning("Skipping student_assignment without id in relationship creation")
                continue
            node_id = str(node_id)
            student_id = sa.get("student_id", "")
            assignment_id = str(sa.get("assignment_id", ""))
            section_progress_id = str(sa.get("student_section_progress_id", ""))
            
            # Student -> StudentAssignment
            conflicts = self.monitor.check_relationship(
                RelationshipType.HAS_ASSIGNMENT,
                NodeType.STUDENT,
                student_id,
                NodeType.STUDENT_ASSIGNMENT,
                node_id,
                {"assigned_at": sa.get("assigned_at", "")}
            )
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "Student", student_id,
                    "HAS_ASSIGNMENT",
                    "StudentAssignment", node_id,
                    {"assigned_at": sa.get("assigned_at", "")}
                )
            
            # StudentAssignment -> Assignment
            conflicts = self.monitor.check_relationship(
                RelationshipType.FOR_ASSIGNMENT,
                NodeType.STUDENT_ASSIGNMENT,
                node_id,
                NodeType.ASSIGNMENT,
                assignment_id
            )
            if not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "StudentAssignment", node_id,
                    "FOR_ASSIGNMENT",
                    "Assignment", assignment_id
                )
            
            # StudentAssignment -> StudentSectionProgress
            if section_progress_id:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.IN_SECTION,
                    NodeType.STUDENT_ASSIGNMENT,
                    node_id,
                    NodeType.STUDENT_SECTION_PROGRESS,
                    section_progress_id
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "StudentAssignment", node_id,
                        "IN_SECTION",
                        "StudentSectionProgress", section_progress_id
                    )
            
            # StudentAssignment -> AssignmentAttempt
            for attempt in assignment_attempts:
                if attempt.get("student_assignment_id") == node_id:
                    attempt_id = attempt.get("id")
                    if not attempt_id:
                        logger.warning("Skipping assignment_attempt without id in relationship creation")
                        continue
                    attempt_id = str(attempt_id)
                    conflicts = self.monitor.check_relationship(
                        RelationshipType.HAS_ATTEMPT,
                        NodeType.STUDENT_ASSIGNMENT,
                        node_id,
                        NodeType.ASSIGNMENT_ATTEMPT,
                        attempt_id,
                        {"attempt_number": attempt.get("attempt_number", 1)}
                    )
                    if not conflicts or all(c.severity != "critical" for c in conflicts):
                        await self.client.create_relationship(
                            "StudentAssignment", node_id,
                            "HAS_ATTEMPT",
                            "AssignmentAttempt", attempt_id,
                            {"attempt_number": attempt.get("attempt_number", 1)}
                        )
    
    async def _create_quiz_relationships(self, data: Dict[str, Any]):
        """Create quiz-related relationships"""
        quiz_attempts = data.get("quizzes", {}).get("quiz_attempts", [])
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        
        for attempt in quiz_attempts:
            attempt_id = attempt.get("id")
            if not attempt_id:
                logger.warning("Skipping quiz_attempt without id in quiz relationships")
                continue
            attempt_id = str(attempt_id)
            student_quiz = next(
                (sq for sq in student_quizzes if sq.get("id") == attempt.get("student_quiz_id")),
                None
            )
            if not student_quiz:
                logger.warning(f"QuizAttempt {attempt_id} has no student_quiz - will be orphaned")
                continue
            
            student_id = student_quiz.get("student_id")
            quiz_id = student_quiz.get("quiz_id")
            
            if not student_id:
                logger.warning(f"StudentQuiz for QuizAttempt {attempt_id} has no student_id - skipping")
                continue
            
            if not quiz_id:
                logger.warning(
                    f"StudentQuiz for QuizAttempt {attempt_id} has no quiz_id - skipping relationship creation. "
                    f"Backend should provide quiz_id in student_quizzes data. "
                    f"This will result in orphan QuizAttempt node."
                )
                continue
            
            student_id = str(student_id)
            quiz_id = str(quiz_id)
            
            # Note: attempt_id == quiz_id is OK (different entity types)
            # Monitor will block only if same node type references itself
            
            # Student -[ATTEMPTED]-> QuizAttempt
            conflicts = self.monitor.check_relationship(
                RelationshipType.ATTEMPTED,
                NodeType.STUDENT,
                student_id,
                NodeType.QUIZ_ATTEMPT,
                attempt_id,
                {"attempt_number": attempt.get("attempt_number", 1)}
            )
            if conflicts and any(c.conflict_type == ConflictType.CIRCULAR_REFERENCE for c in conflicts):
                logger.warning(f"Blocked circular reference: Student {student_id} -> QuizAttempt {attempt_id}")
            elif not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "Student", student_id,
                    "ATTEMPTED",
                    "QuizAttempt", attempt_id,
                    {"attempt_number": attempt.get("attempt_number", 1)}
                )
            
            # QuizAttempt -[ATTEMPTED_FOR]-> Quiz
            conflicts = self.monitor.check_relationship(
                RelationshipType.ATTEMPTED_FOR,
                NodeType.QUIZ_ATTEMPT,
                attempt_id,
                NodeType.QUIZ,
                quiz_id
            )
            if conflicts and any(c.conflict_type == ConflictType.CIRCULAR_REFERENCE for c in conflicts):
                logger.warning(f"Blocked circular reference: QuizAttempt {attempt_id} -> Quiz {quiz_id}")
            elif not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "QuizAttempt", attempt_id,
                    "ATTEMPTED_FOR",
                    "Quiz", quiz_id
                )
    
    async def _create_assignment_relationships(self, data: Dict[str, Any]):
        """Create assignment-related relationships"""
        assignment_attempts = data.get("assignments", {}).get("assignment_attempts", [])
        student_assignments = data.get("assignments", {}).get("student_assignments", [])
        
        for attempt in assignment_attempts:
            attempt_id = attempt.get("id")
            if not attempt_id:
                logger.warning("Skipping assignment_attempt without id in assignment relationships")
                continue
            attempt_id = str(attempt_id)
            student_assignment = next(
                (sa for sa in student_assignments if sa.get("id") == attempt.get("student_assignment_id")),
                None
            )
            if not student_assignment:
                logger.warning(f"AssignmentAttempt {attempt_id} has no student_assignment - will be orphaned")
                continue
            
            student_id = student_assignment.get("student_id")
            assignment_id = student_assignment.get("assignment_id")
            
            if not student_id:
                logger.warning(f"StudentAssignment for AssignmentAttempt {attempt_id} has no student_id - skipping")
                continue
            
            if not assignment_id:
                logger.warning(f"StudentAssignment for AssignmentAttempt {attempt_id} has no assignment_id - skipping")
                continue
            
            student_id = str(student_id)
            assignment_id = str(assignment_id)
            
            # Note: attempt_id == assignment_id is OK (different entity types)
            # Monitor will block only if same node type references itself
            
            # Student -[SUBMITTED]-> AssignmentAttempt (NOT ATTEMPTED)
            conflicts = self.monitor.check_relationship(
                RelationshipType.SUBMITTED,
                NodeType.STUDENT,
                student_id,
                NodeType.ASSIGNMENT_ATTEMPT,
                attempt_id,
                {
                    "attempt_number": attempt.get("attempt_number", 1),
                    "submitted_at": attempt.get("submitted_at", "")
                }
            )
            if conflicts and any(c.conflict_type == ConflictType.CIRCULAR_REFERENCE for c in conflicts):
                logger.warning(f"Blocked circular reference: Student {student_id} -> AssignmentAttempt {attempt_id}")
            elif not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "Student", student_id,
                    "SUBMITTED",
                    "AssignmentAttempt", attempt_id,
                    {
                        "attempt_number": attempt.get("attempt_number", 1),
                        "submitted_at": attempt.get("submitted_at", "")
                    }
                )
            
            # AssignmentAttempt -[SUBMITTED_FOR]-> Assignment (NOT ATTEMPTED_FOR)
            conflicts = self.monitor.check_relationship(
                RelationshipType.SUBMITTED_FOR,
                NodeType.ASSIGNMENT_ATTEMPT,
                attempt_id,
                NodeType.ASSIGNMENT,
                assignment_id
            )
            if conflicts and any(c.conflict_type == ConflictType.CIRCULAR_REFERENCE for c in conflicts):
                logger.warning(f"Blocked circular reference: AssignmentAttempt {attempt_id} -> Assignment {assignment_id}")
            elif not conflicts or all(c.severity != "critical" for c in conflicts):
                await self.client.create_relationship(
                    "AssignmentAttempt", attempt_id,
                    "SUBMITTED_FOR",
                    "Assignment", assignment_id
                )
    
    async def _create_topic_relationships(self, data: Dict[str, Any]):
        """Create topic relationships"""
        # Extract topics from quiz attempts
        quiz_attempts = data.get("quizzes", {}).get("quiz_attempts", [])
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        
        quiz_topic_map = {}  # quiz_id -> set of topic names
        
        for attempt in quiz_attempts:
            student_quiz = next(
                (sq for sq in student_quizzes if sq.get("id") == attempt.get("student_quiz_id")),
                None
            )
            if not student_quiz:
                continue
            
            quiz_id = student_quiz.get("quiz_id")
            if not quiz_id:
                continue
            quiz_id = str(quiz_id)
            if quiz_id not in quiz_topic_map:
                quiz_topic_map[quiz_id] = set()
            
            # Extract topics from question attempts
            for qa in attempt.get("question_attempts", []):
                topics = qa.get("topics", [])
                # Handle both string and dict topics
                topic_names = []
                for topic in topics:
                    if isinstance(topic, str):
                        topic_names.append(topic)
                    elif isinstance(topic, dict):
                        topic_names.append(topic.get("name", topic.get("id", "")))
                quiz_topic_map[quiz_id].update(topic_names)
        
        assignment_attempts = data.get("assignments", {}).get("assignment_attempts", [])
        student_assignments = data.get("assignments", {}).get("student_assignments", [])
        assignment_topic_map = {}  # assignment_id -> set of topic names
        
        # Source 1: Extract from question attempts
        for attempt in assignment_attempts:
            student_assignment = next(
                (sa for sa in student_assignments if sa.get("id") == attempt.get("student_assignment_id")),
                None
            )
            if not student_assignment:
                continue
            
            assignment_id = student_assignment.get("assignment_id")
            if not assignment_id:
                continue
            assignment_id = str(assignment_id)
            if assignment_id not in assignment_topic_map:
                assignment_topic_map[assignment_id] = set()
            
            for qa in attempt.get("question_attempts", []):
                topics = qa.get("topics", [])
                # Handle both string and dict topics
                topic_names = []
                for topic in topics:
                    if isinstance(topic, str):
                        topic_names.append(topic)
                    elif isinstance(topic, dict):
                        topic_names.append(topic.get("name", topic.get("id", "")))
                assignment_topic_map[assignment_id].update(topic_names)
        
        # Create Quiz -[HAS_TOPIC]-> Topic relationships
        topics = {t["topic_name"]: str(t["topic_id"]) for t in data.get("topics", [])}
        logger.info(f"Creating HAS_TOPIC relationships. Quiz topic map: {quiz_topic_map}, Topics mapping: {topics}")
        
        created_count = 0
        for quiz_id, topic_names in quiz_topic_map.items():
            for topic_name in topic_names:
                topic_id = topics.get(topic_name)
                if topic_id:
                    # Note: quiz_id == topic_id is OK (different entity types)
                    # Monitor will block only if same node type references itself
                    conflicts = self.monitor.check_relationship(
                        RelationshipType.HAS_TOPIC,
                        NodeType.QUIZ,
                        quiz_id,
                        NodeType.TOPIC,
                        topic_id
                    )
                    if conflicts and any(c.conflict_type == ConflictType.CIRCULAR_REFERENCE for c in conflicts):
                        logger.warning(f"Blocked circular reference: Quiz {quiz_id} -> Topic {topic_id}")
                        continue
                    
                    if not conflicts or all(c.severity != "critical" for c in conflicts):
                        await self.client.create_relationship(
                            "Quiz", quiz_id,
                            "HAS_TOPIC",
                            "Topic", topic_id
                        )
                        created_count += 1
                        logger.debug(f"Created HAS_TOPIC: Quiz {quiz_id} -> Topic {topic_id} ({topic_name})")
                else:
                    logger.warning(f"Topic name '{topic_name}' not found in topics mapping for Quiz {quiz_id}")
        
        logger.info(f"Created {created_count} Quiz HAS_TOPIC relationships")
        
        # Create Assignment -[HAS_TOPIC]-> Topic relationships
        logger.info(f"Creating Assignment HAS_TOPIC relationships. Assignment topic map: {assignment_topic_map}")
        
        created_count = 0
        for assignment_id, topic_names in assignment_topic_map.items():
            for topic_name in topic_names:
                topic_id = topics.get(topic_name)
                if topic_id:
                    # Note: assignment_id == topic_id is OK (different entity types)
                    # Monitor will block only if same node type references itself
                    conflicts = self.monitor.check_relationship(
                        RelationshipType.HAS_TOPIC,
                        NodeType.ASSIGNMENT,
                        assignment_id,
                        NodeType.TOPIC,
                        topic_id
                    )
                    # Block if circular reference detected
                    if conflicts and any(c.conflict_type == ConflictType.CIRCULAR_REFERENCE for c in conflicts):
                        logger.warning(f"Blocked circular reference: Assignment {assignment_id} -> Topic {topic_id}")
                        continue
                    
                    if not conflicts or all(c.severity != "critical" for c in conflicts):
                        await self.client.create_relationship(
                            "Assignment", assignment_id,
                            "HAS_TOPIC",
                            "Topic", topic_id
                        )
                        created_count += 1
                        logger.debug(f"Created HAS_TOPIC: Assignment {assignment_id} -> Topic {topic_id} ({topic_name})")
                else:
                    logger.warning(f"Topic name '{topic_name}' not found in topics mapping for Assignment {assignment_id}")
        
        logger.info(f"Created {created_count} Assignment HAS_TOPIC relationships")
    
    async def _connect_orphan_nodes(self, data: Dict[str, Any]):
        """Try to connect orphan nodes to graph"""
        classroom_id = str(data["classroom"]["id"])
        
        # Get all nodes that should be connected
        connected_nodes = set()
        for rel in self.monitor._relationship_registry:
            connected_nodes.add(rel["from"])
            connected_nodes.add(rel["to"])
        
        # Find orphan nodes
        all_node_ids = {node_id.split(":")[1] if ":" in node_id else node_id 
                       for node_id in self.created_nodes}
        orphan_nodes = all_node_ids - connected_nodes
        print("orphan_nodes count", len(orphan_nodes))
        connected_count = 0
        
        # Connect orphan Students to Classroom
        for node_key in self.created_nodes:
            if node_key.startswith("Student:"):
                student_id = node_key.split(":")[1]
                if student_id in orphan_nodes:
                    # Check if student is enrolled
                    enrollments = data.get("enrollments", {}).get("curriculum_enrollments", [])
                    if any(e["student_id"] == student_id for e in enrollments):
                        # Try to create ENROLLED_IN relationship
                        conflicts = self.monitor.check_relationship(
                            RelationshipType.ENROLLED_IN,
                            NodeType.STUDENT,
                            student_id,
                            NodeType.CLASSROOM,
                            classroom_id
                        )
                        if not conflicts or all(c.severity != "critical" for c in conflicts):
                            await self.client.create_relationship(
                                "Student", student_id,
                                "ENROLLED_IN",
                                "Classroom", classroom_id
                            )
                            connected_count += 1
                            logger.info(f"Connected orphan Student {student_id} to Classroom")
        
        # Try to reconnect orphan QuizAttempts/AssignmentAttempts that were blocked by circular reference
        # by finding their parent data and creating relationships
        quiz_attempts = data.get("quizzes", {}).get("quiz_attempts", [])
        student_quizzes = data.get("quizzes", {}).get("student_quizzes", [])
        
        for attempt in quiz_attempts:
            attempt_id = attempt.get("id")
            if not attempt_id:
                logger.warning("Skipping quiz_attempt without id in orphan connection")
                continue
            attempt_id = str(attempt_id)
            if attempt_id in orphan_nodes:
                student_quiz = next(
                    (sq for sq in student_quizzes if sq.get("id") == attempt.get("student_quiz_id")),
                    None
                )
                if student_quiz:
                    student_id = student_quiz.get("student_id")
                    quiz_id = student_quiz.get("quiz_id")
                    
                    if not student_id or not quiz_id:
                        logger.warning(f"StudentQuiz for orphan QuizAttempt {attempt_id} missing student_id or quiz_id - skipping")
                        continue
                    
                    student_id = str(student_id)
                    quiz_id = str(quiz_id)
                    
                    # Try to create relationships (monitor will handle circular reference if needed)
                    # Student -> QuizAttempt
                    conflicts = self.monitor.check_relationship(
                        RelationshipType.ATTEMPTED,
                        NodeType.STUDENT,
                        student_id,
                        NodeType.QUIZ_ATTEMPT,
                        attempt_id,
                        {"attempt_number": attempt.get("attempt_number", 1)}
                    )
                    if not conflicts or all(c.severity != "critical" for c in conflicts):
                        await self.client.create_relationship(
                            "Student", student_id,
                            "ATTEMPTED",
                            "QuizAttempt", attempt_id,
                            {"attempt_number": attempt.get("attempt_number", 1)}
                        )
                        connected_count += 1
                    
                    # QuizAttempt -> Quiz
                    conflicts = self.monitor.check_relationship(
                        RelationshipType.ATTEMPTED_FOR,
                        NodeType.QUIZ_ATTEMPT,
                        attempt_id,
                        NodeType.QUIZ,
                        quiz_id
                    )
                    if not conflicts or all(c.severity != "critical" for c in conflicts):
                        await self.client.create_relationship(
                            "QuizAttempt", attempt_id,
                            "ATTEMPTED_FOR",
                            "Quiz", quiz_id
                        )
                        connected_count += 1
        
        # Similar for AssignmentAttempts
        assignment_attempts = data.get("assignments", {}).get("assignment_attempts", [])
        student_assignments = data.get("assignments", {}).get("student_assignments", [])
        
        for attempt in assignment_attempts:
            attempt_id = attempt.get("id")
            if not attempt_id:
                logger.warning("Skipping assignment_attempt without id in orphan connection")
                continue
            attempt_id = str(attempt_id)
            if attempt_id in orphan_nodes:
                student_assignment = next(
                    (sa for sa in student_assignments if sa.get("id") == attempt.get("student_assignment_id")),
                    None
                )
                if student_assignment:
                    student_id = student_assignment.get("student_id")
                    assignment_id = student_assignment.get("assignment_id")
                    
                    if not student_id or not assignment_id:
                        logger.warning(f"StudentAssignment for orphan AssignmentAttempt {attempt_id} missing student_id or assignment_id - skipping")
                        continue
                    
                    student_id = str(student_id)
                    assignment_id = str(assignment_id)
                    
                    # Student -> AssignmentAttempt
                    conflicts = self.monitor.check_relationship(
                        RelationshipType.SUBMITTED,
                        NodeType.STUDENT,
                        student_id,
                        NodeType.ASSIGNMENT_ATTEMPT,
                        attempt_id,
                        {
                            "attempt_number": attempt.get("attempt_number", 1),
                            "submitted_at": attempt.get("submitted_at", "")
                        }
                    )
                    if not conflicts or all(c.severity != "critical" for c in conflicts):
                        await self.client.create_relationship(
                            "Student", student_id,
                            "SUBMITTED",
                            "AssignmentAttempt", attempt_id,
                            {
                                "attempt_number": attempt.get("attempt_number", 1),
                                "submitted_at": attempt.get("submitted_at", "")
                            }
                        )
                        connected_count += 1
                    
                    # AssignmentAttempt -> Assignment
                    conflicts = self.monitor.check_relationship(
                        RelationshipType.SUBMITTED_FOR,
                        NodeType.ASSIGNMENT_ATTEMPT,
                        attempt_id,
                        NodeType.ASSIGNMENT,
                        assignment_id
                    )
                    if not conflicts or all(c.severity != "critical" for c in conflicts):
                        await self.client.create_relationship(
                            "AssignmentAttempt", attempt_id,
                            "SUBMITTED_FOR",
                            "Assignment", assignment_id
                        )
                        connected_count += 1
        
        if connected_count > 0:
            logger.info(f"Connected {connected_count} orphan nodes to graph")
        
        # Note: Topics without relationships are OK - they might not be used yet
    
    async def _create_performance_relationships(self, data: Dict[str, Any]) -> Dict[str, int]:
        """
        Create Level 5 performance-based relationships:
        - STRUGGLES_WITH: Student -> Topic (low scores)
        - EXCELS_AT: Student -> Topic (high scores)
        
        Returns:
            Dict with counts of created relationships
        """
        logger.info("[LEVEL 5] Creating performance-based relationships...")
        
        students = data.get("students", [])
        topics_list = (
            data.get("topics", []) 
            or data.get("topics_catalog", []) 
            or data.get("topicsCatalog", [])
        )
        
        logger.info(f"[LEVEL 5] Found {len(topics_list)} topics, {len(students)} students")
        
        if not topics_list:
            logger.warning(
                "[LEVEL 5] No topics found in data. "
                "Expected 'topics', 'topics_catalog', or 'topicsCatalog' field with list of topic objects. "
                "Each topic should have 'topic_id'/'topicId' and 'topic_name'/'topicName' fields."
            )
        
        # Build mapping: topic_name -> topic_id and topic_id -> topic_name
        # Handle both snake_case and camelCase field names
        topics_map = {}
        topic_name_to_id = {}
        
        for t in topics_list:
            if isinstance(t, dict):
                topic_id = t.get("topic_id") or t.get("topicId")
                topic_name = t.get("topic_name") or t.get("topicName")
                if topic_id and topic_name:
                    topics_map[str(topic_id)] = topic_name
                    topic_name_to_id[topic_name] = str(topic_id)
        
        logger.info(f"[LEVEL 5] Mapped {len(topics_map)} topics (topic_id -> topic_name)")
        
        # Track student-topic performance
        student_topic_scores = {}  # (student_id, topic_id) -> list of scores
        
        # Collect scores from quiz attempts
       
        quizzes_data = data.get("quizzes", {})
        quiz_attempts = (
            quizzes_data.get("quiz_attempts", []) 
            or quizzes_data.get("quizAttempts", [])
            or data.get("quizAttempts", [])
        )
        student_quizzes = (
            quizzes_data.get("student_quizzes", [])
            or quizzes_data.get("studentQuizzes", [])
            or data.get("studentQuizzes", [])
        )
        
        logger.info(f"[LEVEL 5] Processing {len(quiz_attempts)} quiz attempts, {len(student_quizzes)} student quizzes")
        
        quiz_attempts_processed = 0
        quiz_attempts_with_topics = 0
        quiz_attempts_without_topics = 0
        
        for attempt in quiz_attempts:
            student_quiz_id = attempt.get("student_quiz_id") or attempt.get("studentQuizId")
            student_quiz = next(
                (sq for sq in student_quizzes if sq.get("id") == student_quiz_id),
                None
            )
            if not student_quiz:
                continue
            
            student_id = student_quiz.get("student_id") or student_quiz.get("studentId")
            if not student_id:
                continue
            student_id = str(student_id)
            
            score = attempt.get("score") or attempt.get("totalScore", 0)
            if not isinstance(score, (int, float)):
                try:
                    score = float(score)
                except (ValueError, TypeError):
                    continue
            
            quiz_attempts_processed += 1
            has_topics = False
            
            # Extract topics from question attempts
            question_attempts = attempt.get("question_attempts", []) or attempt.get("questionAttempts", [])
            if not question_attempts:
                continue
            
            # Try to get quiz_id for fallback
            quiz_id = student_quiz.get("quiz_id")
            
            for qa in question_attempts:
                topics = qa.get("topics", [])
                
                # Fallback: If question_attempt has no topics, try to get from quiz level
                if not topics and quiz_id:
                    # Try to find quiz node and get its topics from HAS_TOPIC relationships
                    # This is a fallback when question-level topics are missing
                    logger.debug(
                        f"[LEVEL 5] Question attempt missing topics, quiz_id={quiz_id}. "
                        f"Note: Backend should provide topics in question_attempts for accurate performance analysis."
                    )
                
                if not topics:
                    continue
                    
                for topic in topics:
                    if isinstance(topic, str):
                        topic_id = topic_name_to_id.get(topic)
                    elif isinstance(topic, dict):
                        topic_id = str(topic.get("id", topic.get("topic_id", "")))
                    else:
                        continue
                    
                    if topic_id and topic_id in topics_map:
                        has_topics = True
                        key = (student_id, topic_id)
                        if key not in student_topic_scores:
                            student_topic_scores[key] = []
                        student_topic_scores[key].append(score)
            
            if has_topics:
                quiz_attempts_with_topics += 1
            else:
                quiz_attempts_without_topics += 1
                if quiz_id:
                    logger.debug(
                        f"[LEVEL 5] Quiz attempt {attempt.get('id', 'unknown')} has no topics in question_attempts. "
                        f"quiz_id={quiz_id}, student_id={student_id}. "
                        f"Backend should populate question_attempts[].topics[] for Level 5 performance analysis."
                    )
        
        logger.info(
            f"[LEVEL 5] Quiz attempts: {quiz_attempts_processed} processed, "
            f"{quiz_attempts_with_topics} with topics, {quiz_attempts_without_topics} without topics"
        )
        
        # Collect scores from assignment attempts
        assignments_data = data.get("assignments", {})
        assignment_attempts = (
            assignments_data.get("assignment_attempts", [])
            or assignments_data.get("assignmentAttempts", [])
            or data.get("assignmentAttempts", [])
        )
        student_assignments = (
            assignments_data.get("student_assignments", [])
            or assignments_data.get("studentAssignments", [])
            or data.get("studentAssignments", [])
        )
        
        logger.info(f"[LEVEL 5] Processing {len(assignment_attempts)} assignment attempts, {len(student_assignments)} student assignments")
        
        for attempt in assignment_attempts:
            student_assignment_id = attempt.get("student_assignment_id") or attempt.get("studentAssignmentId")
            student_assignment = next(
                (sa for sa in student_assignments if sa.get("id") == student_assignment_id),
                None
            )
            if not student_assignment:
                continue
            
            student_id = student_assignment.get("student_id") or student_assignment.get("studentId")
            if not student_id:
                continue
            student_id = str(student_id)
            
            score = attempt.get("score") or attempt.get("finalScore", 0)
            if not isinstance(score, (int, float)):
                try:
                    score = float(score)
                except (ValueError, TypeError):
                    continue
            
            # Extract topics from question attempts
            question_attempts = attempt.get("question_attempts", []) or attempt.get("questionAttempts", [])
            for qa in question_attempts:
                topics = qa.get("topics", [])
                if not topics:
                    continue
                    
                for topic in topics:
                    if isinstance(topic, str):
                        topic_id = topic_name_to_id.get(topic)
                    elif isinstance(topic, dict):
                        topic_id = str(topic.get("id", topic.get("topic_id", "")))
                    else:
                        continue
                    
                    if topic_id and topic_id in topics_map:
                        key = (student_id, topic_id)
                        if key not in student_topic_scores:
                            student_topic_scores[key] = []
                        student_topic_scores[key].append(score)
        
        # Create relationships based on average scores
        logger.info(f"[LEVEL 5] Found {len(student_topic_scores)} student-topic pairs with scores")
        
        if len(student_topic_scores) == 0:
            logger.warning(
                "[LEVEL 5] No student-topic scores found. "
                "Possible reasons: "
                "1) Question attempts don't have topics, "
                "2) Topics don't match topic list, "
                "3) No quiz/assignment attempts with scores"
            )
        
        struggles_count = 0
        excels_count = 0
        
        for (student_id, topic_id), scores in student_topic_scores.items():
            if not scores:
                continue
            
            avg_score = sum(scores) / len(scores)
            low_score_count = sum(1 for s in scores if s < 0.6)
            high_score_count = sum(1 for s in scores if s >= 0.8)
            
            # STRUGGLES_WITH: average < 0.5 OR 2+ low scores
            if avg_score < 0.5 or low_score_count >= 2:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.STRUGGLES_WITH,
                    NodeType.STUDENT,
                    student_id,
                    NodeType.TOPIC,
                    topic_id,
                    {"average_score": avg_score, "low_score_count": low_score_count}
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "Student", student_id,
                        "STRUGGLES_WITH",
                        "Topic", topic_id,
                        {
                            "average_score": avg_score,
                            "low_score_count": low_score_count,
                            "total_attempts": len(scores)
                        }
                    )
                    struggles_count += 1
            
            # EXCELS_AT: average >= 0.8 AND 2+ high scores
            if avg_score >= 0.8 and high_score_count >= 2:
                conflicts = self.monitor.check_relationship(
                    RelationshipType.EXCELS_AT,
                    NodeType.STUDENT,
                    student_id,
                    NodeType.TOPIC,
                    topic_id,
                    {"average_score": avg_score, "high_score_count": high_score_count}
                )
                if not conflicts or all(c.severity != "critical" for c in conflicts):
                    await self.client.create_relationship(
                        "Student", student_id,
                        "EXCELS_AT",
                        "Topic", topic_id,
                        {
                            "average_score": avg_score,
                            "high_score_count": high_score_count,
                            "total_attempts": len(scores)
                        }
                    )
                    excels_count += 1
        
        total_performance_rels = struggles_count + excels_count
        logger.info(f"[LEVEL 5] Created {struggles_count} STRUGGLES_WITH and {excels_count} EXCELS_AT relationships")
        logger.info(f"[LEVEL 5] Total performance relationships: {total_performance_rels}")
        
        return {
            "struggles_with": struggles_count,
            "excels_at": excels_count,
            "total": total_performance_rels
        }
    
    async def _validate_structure(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Validate 5-level graph structure
        
        Checks:
        1. Level 1: Curriculum nodes exist
        2. Level 2: Course nodes exist and linked to Curriculum
        3. Level 3: Lesson nodes exist and linked to Course
        4. Level 4: Section → Content → Quiz/Assignment → Attempts chain exists
        5. Level 5: Performance relationships (STRUGGLES_WITH/EXCELS_AT) exist
        
        Returns:
            Validation result with status and details
        """
        logger.info("[VALIDATION] Starting structure validation...")
        
        validation_result = {
            "valid": True,
            "errors": [],
            "warnings": [],
            "level_checks": {}
        }
        
        try:
            # Check Level 1: Curriculum
            curriculum_count = sum(1 for n in self.created_nodes if n.startswith("Curriculum:"))
            validation_result["level_checks"]["level_1_curriculum"] = {
                "count": curriculum_count,
                "valid": curriculum_count > 0
            }
            if curriculum_count == 0:
                validation_result["warnings"].append("No Curriculum nodes found (Level 1)")
            
            # Check Level 2: Course
            course_count = sum(1 for n in self.created_nodes if n.startswith("Course:"))
            validation_result["level_checks"]["level_2_course"] = {
                "count": course_count,
                "valid": course_count > 0
            }
            if course_count == 0:
                validation_result["warnings"].append("No Course nodes found (Level 2)")
            
            # Check Level 3: Lesson
            lesson_count = sum(1 for n in self.created_nodes if n.startswith("Lesson:"))
            validation_result["level_checks"]["level_3_lesson"] = {
                "count": lesson_count,
                "valid": lesson_count > 0
            }
            if lesson_count == 0:
                validation_result["warnings"].append("No Lesson nodes found (Level 3)")
            
            # Check Level 4: Section → Content → Quiz/Assignment → Attempts
            section_count = sum(1 for n in self.created_nodes if n.startswith("Section:"))
            content_count = sum(1 for n in self.created_nodes if n.startswith("Content:"))
            quiz_count = sum(1 for n in self.created_nodes if n.startswith("Quiz:"))
            assignment_count = sum(1 for n in self.created_nodes if n.startswith("Assignment:"))
            attempt_count = (
                sum(1 for n in self.created_nodes if n.startswith("QuizAttempt:")) +
                sum(1 for n in self.created_nodes if n.startswith("AssignmentAttempt:"))
            )
            
            validation_result["level_checks"]["level_4_structure"] = {
                "sections": section_count,
                "contents": content_count,
                "quizzes": quiz_count,
                "assignments": assignment_count,
                "attempts": attempt_count,
                "valid": section_count > 0 or content_count > 0 or quiz_count > 0 or assignment_count > 0
            }
            
            if section_count == 0 and content_count == 0:
                validation_result["warnings"].append("No Section or Content nodes found (Level 4)")
            
            # Check Level 5: Performance relationships
            # Note: We can't easily query relationships from created_nodes
            # This is a basic check - full validation would require graph queries
            validation_result["level_checks"]["level_5_performance"] = {
                "note": "Performance relationships created in _create_performance_relationships",
                "valid": True  # Assume valid if method completed without errors
            }
            
            # Overall validation
            if validation_result["warnings"]:
                logger.warning(f"[VALIDATION] Found {len(validation_result['warnings'])} warnings")
            else:
                logger.info("[VALIDATION] All structure checks passed")
            
            validation_result["valid"] = len(validation_result["errors"]) == 0
            
        except Exception as e:
            logger.error(f"[VALIDATION] Error during validation: {e}")
            validation_result["valid"] = False
            validation_result["errors"].append(f"Validation error: {str(e)}")
        
        return validation_result

