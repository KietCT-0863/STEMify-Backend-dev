"""
Graph Schema for Classroom Data
Defines node types, relationships, and validation rules
"""

from typing import Dict, List, Any
from dataclasses import dataclass
from enum import Enum


class NodeType(str, Enum):
    """Graph node types"""
    CLASSROOM = "Classroom"
    STUDENT = "Student"
    TEACHER = "Teacher"
    QUIZ = "Quiz"
    ASSIGNMENT = "Assignment"
    QUIZ_ATTEMPT = "QuizAttempt"
    ASSIGNMENT_ATTEMPT = "AssignmentAttempt"
    STUDENT_QUIZ = "StudentQuiz"
    STUDENT_ASSIGNMENT = "StudentAssignment"
    STUDENT_SECTION_PROGRESS = "StudentSectionProgress"
    STUDENT_LESSON_PROGRESS = "StudentLessonProgress"
    CURRICULUM_ENROLLMENT = "CurriculumEnrollment"
    COURSE_ENROLLMENT = "CourseEnrollment"
    TOPIC = "Topic"
    LESSON = "Lesson"
    SECTION = "Section"
    CONTENT = "Content"
    QUESTION = "Question"
    CURRICULUM = "Curriculum"
    COURSE = "Course"


class RelationshipType(str, Enum):
    """Graph relationship types"""
    ENROLLED_IN = "ENROLLED_IN"
    ATTEMPTED = "ATTEMPTED"  # Student -> QuizAttempt only
    ATTEMPTED_FOR = "ATTEMPTED_FOR"  # QuizAttempt -> Quiz
    SUBMITTED = "SUBMITTED"  # Student -> AssignmentAttempt
    SUBMITTED_FOR = "SUBMITTED_FOR"  # AssignmentAttempt -> Assignment
    HAS_QUIZ = "HAS_QUIZ"  # Student -> StudentQuiz
    HAS_ASSIGNMENT = "HAS_ASSIGNMENT"  # Student -> StudentAssignment
    FOR_QUIZ = "FOR_QUIZ"  # StudentQuiz -> Quiz
    FOR_ASSIGNMENT = "FOR_ASSIGNMENT"  # StudentAssignment -> Assignment
    HAS_ATTEMPT = "HAS_ATTEMPT"  # StudentQuiz -> QuizAttempt, StudentAssignment -> AssignmentAttempt
    IN_SECTION = "IN_SECTION"  # StudentQuiz/StudentAssignment -> StudentSectionProgress
    FOR_SECTION = "FOR_SECTION"  # StudentSectionProgress -> Section
    IN_LESSON = "IN_LESSON"  # StudentSectionProgress -> StudentLessonProgress
    FOR_LESSON = "FOR_LESSON"  # StudentLessonProgress -> Lesson
    IN_COURSE = "IN_COURSE"  # StudentLessonProgress -> CourseEnrollment
    FOR_COURSE = "FOR_COURSE"  # CourseEnrollment -> Course
    ENROLLED_IN_CURRICULUM = "ENROLLED_IN_CURRICULUM"  # Student -> CurriculumEnrollment
    IN_CURRICULUM = "IN_CURRICULUM"  # CurriculumEnrollment -> Curriculum
    HAS_COURSE_ENROLLMENT = "HAS_COURSE_ENROLLMENT"  # CurriculumEnrollment -> CourseEnrollment
    HAS_TOPIC = "HAS_TOPIC"  # Quiz/Assignment -> Topic
    COVERS = "COVERS"  # Topic -> Quiz/Assignment (reverse of HAS_TOPIC)
    APPEARS_IN = "APPEARS_IN"
    BELONGS_TO = "BELONGS_TO"
    CONTAINS = "CONTAINS"  # Lesson -> Section
    HAS_CONTENT = "HAS_CONTENT"  # Section -> Content
    IS_QUIZ = "IS_QUIZ"  # Content -> Quiz
    IS_ASSIGNMENT = "IS_ASSIGNMENT"  # Content -> Assignment
    HAS_LESSON = "HAS_LESSON"  # Course -> Lesson
    HAS_COURSE = "HAS_COURSE"  # Curriculum -> Course
    MADE_ERROR = "MADE_ERROR"
    RELATED_TO = "RELATED_TO"
    STRUGGLES_WITH = "STRUGGLES_WITH"  # Student -> Topic (low performance)
    EXCELS_AT = "EXCELS_AT"  # Student -> Topic (high performance)


@dataclass
class NodeSchema:
    """Schema for a graph node"""
    node_type: NodeType
    required_properties: List[str]
    optional_properties: List[str]
    validation_rules: Dict[str, Any] = None


@dataclass
class RelationshipSchema:
    """Schema for a graph relationship"""
    relationship_type: RelationshipType
    from_node: NodeType
    to_node: NodeType
    properties: List[str] = None
    cardinality: str = "many-to-many"  # one-to-one, one-to-many, many-to-many


# Graph Schema Definition
GRAPH_SCHEMA = {
    "nodes": {
        NodeType.CLASSROOM: NodeSchema(
            node_type=NodeType.CLASSROOM,
            required_properties=["id", "name", "grade"],
            optional_properties=["teacher_id", "curriculum_id", "status", "start_date", "end_date"]
        ),
        NodeType.STUDENT: NodeSchema(
            node_type=NodeType.STUDENT,
            required_properties=["id", "name"],
            optional_properties=["email", "joined_at"]
        ),
        NodeType.QUIZ: NodeSchema(
            node_type=NodeType.QUIZ,
            required_properties=["id", "title"],
            optional_properties=["description", "time_limit_minutes"]
        ),
        NodeType.ASSIGNMENT: NodeSchema(
            node_type=NodeType.ASSIGNMENT,
            required_properties=["id", "title"],
            optional_properties=["description", "duration_days"]
        ),
        NodeType.QUIZ_ATTEMPT: NodeSchema(
            node_type=NodeType.QUIZ_ATTEMPT,
            required_properties=["id", "score", "status"],
            optional_properties=["started_at", "completed_at", "attempt_number"]
        ),
        NodeType.ASSIGNMENT_ATTEMPT: NodeSchema(
            node_type=NodeType.ASSIGNMENT_ATTEMPT,
            required_properties=["id", "score", "status"],
            optional_properties=["submitted_at", "feedback", "attempt_number"]
        ),
        NodeType.TOPIC: NodeSchema(
            node_type=NodeType.TOPIC,
            required_properties=["id", "name"],
            optional_properties=[]
        ),
        NodeType.LESSON: NodeSchema(
            node_type=NodeType.LESSON,
            required_properties=["id", "title"],
            optional_properties=["description", "duration"]
        ),
        NodeType.SECTION: NodeSchema(
            node_type=NodeType.SECTION,
            required_properties=["id", "title"],
            optional_properties=["description", "order_index"]
        ),
        NodeType.STUDENT_QUIZ: NodeSchema(
            node_type=NodeType.STUDENT_QUIZ,
            required_properties=["id", "quiz_id", "student_id", "status"],
            optional_properties=["final_score", "attempt_count", "assigned_at", "due_date", "student_section_progress_id"]
        ),
        NodeType.STUDENT_ASSIGNMENT: NodeSchema(
            node_type=NodeType.STUDENT_ASSIGNMENT,
            required_properties=["id", "assignment_id", "student_id", "status"],
            optional_properties=["final_score", "attempt_count", "assigned_at", "due_date", "student_section_progress_id"]
        ),
        NodeType.STUDENT_SECTION_PROGRESS: NodeSchema(
            node_type=NodeType.STUDENT_SECTION_PROGRESS,
            required_properties=["id", "section_id", "status"],
            optional_properties=["completed_at", "student_lesson_progress_id"]
        ),
        NodeType.STUDENT_LESSON_PROGRESS: NodeSchema(
            node_type=NodeType.STUDENT_LESSON_PROGRESS,
            required_properties=["id", "lesson_id", "status"],
            optional_properties=["completed_at", "enrollment_id"]
        ),
        NodeType.CURRICULUM_ENROLLMENT: NodeSchema(
            node_type=NodeType.CURRICULUM_ENROLLMENT,
            required_properties=["id", "student_id", "curriculum_id", "status"],
            optional_properties=["classroom_id", "enrolled_at", "progress_percentage", "completed_at"]
        ),
        NodeType.COURSE_ENROLLMENT: NodeSchema(
            node_type=NodeType.COURSE_ENROLLMENT,
            required_properties=["id", "student_id", "course_id", "status"],
            optional_properties=["curriculum_enrollment_id", "enrolled_at", "progress_percentage", "completed_at", "final_score"]
        ),
        NodeType.CONTENT: NodeSchema(
            node_type=NodeType.CONTENT,
            required_properties=["id", "content_type", "section_id"],
            optional_properties=["title", "content_body"]
        ),
        NodeType.CURRICULUM: NodeSchema(
            node_type=NodeType.CURRICULUM,
            required_properties=["id", "title"],
            optional_properties=["code", "description", "status"]
        ),
        NodeType.COURSE: NodeSchema(
            node_type=NodeType.COURSE,
            required_properties=["id", "title"],
            optional_properties=["code", "description", "status", "curriculum_id"]
        ),
    },
    "relationships": [
        RelationshipSchema(
            relationship_type=RelationshipType.ENROLLED_IN,
            from_node=NodeType.STUDENT,
            to_node=NodeType.CLASSROOM,
            properties=["enrolled_at"],
            cardinality="many-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.ATTEMPTED,
            from_node=NodeType.STUDENT,
            to_node=NodeType.QUIZ_ATTEMPT,
            properties=["attempt_number"],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.ATTEMPTED_FOR,
            from_node=NodeType.QUIZ_ATTEMPT,
            to_node=NodeType.QUIZ,
            properties=[],
            cardinality="many-to-one"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.SUBMITTED,
            from_node=NodeType.STUDENT,
            to_node=NodeType.ASSIGNMENT_ATTEMPT,
            properties=["attempt_number", "submitted_at"],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.SUBMITTED_FOR,
            from_node=NodeType.ASSIGNMENT_ATTEMPT,
            to_node=NodeType.ASSIGNMENT,
            properties=[],
            cardinality="many-to-one"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_TOPIC,
            from_node=NodeType.QUIZ,
            to_node=NodeType.TOPIC,
            properties=[],
            cardinality="many-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_TOPIC,
            from_node=NodeType.ASSIGNMENT,
            to_node=NodeType.TOPIC,
            properties=[],
            cardinality="many-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.COVERS,
            from_node=NodeType.TOPIC,
            to_node=NodeType.QUIZ,
            properties=[],
            cardinality="many-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.COVERS,
            from_node=NodeType.TOPIC,
            to_node=NodeType.ASSIGNMENT,
            properties=[],
            cardinality="many-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.CONTAINS,
            from_node=NodeType.LESSON,
            to_node=NodeType.SECTION,
            properties=["order_index"],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_CONTENT,
            from_node=NodeType.SECTION,
            to_node=NodeType.QUIZ,
            properties=[],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_CONTENT,
            from_node=NodeType.SECTION,
            to_node=NodeType.ASSIGNMENT,
            properties=[],
            cardinality="one-to-many"
        ),
        # StudentQuiz relationships
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_QUIZ,
            from_node=NodeType.STUDENT,
            to_node=NodeType.STUDENT_QUIZ,
            properties=["assigned_at"],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.FOR_QUIZ,
            from_node=NodeType.STUDENT_QUIZ,
            to_node=NodeType.QUIZ,
            properties=[],
            cardinality="many-to-one"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_ATTEMPT,
            from_node=NodeType.STUDENT_QUIZ,
            to_node=NodeType.QUIZ_ATTEMPT,
            properties=["attempt_number"],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.IN_SECTION,
            from_node=NodeType.STUDENT_QUIZ,
            to_node=NodeType.STUDENT_SECTION_PROGRESS,
            properties=[],
            cardinality="many-to-one"
        ),
        # StudentAssignment relationships
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_ASSIGNMENT,
            from_node=NodeType.STUDENT,
            to_node=NodeType.STUDENT_ASSIGNMENT,
            properties=["assigned_at"],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.FOR_ASSIGNMENT,
            from_node=NodeType.STUDENT_ASSIGNMENT,
            to_node=NodeType.ASSIGNMENT,
            properties=[],
            cardinality="many-to-one"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_ATTEMPT,
            from_node=NodeType.STUDENT_ASSIGNMENT,
            to_node=NodeType.ASSIGNMENT_ATTEMPT,
            properties=["attempt_number"],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.IN_SECTION,
            from_node=NodeType.STUDENT_ASSIGNMENT,
            to_node=NodeType.STUDENT_SECTION_PROGRESS,
            properties=[],
            cardinality="many-to-one"
        ),
        # Progress relationships
        RelationshipSchema(
            relationship_type=RelationshipType.FOR_SECTION,
            from_node=NodeType.STUDENT_SECTION_PROGRESS,
            to_node=NodeType.SECTION,
            properties=[],
            cardinality="many-to-one"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.IN_LESSON,
            from_node=NodeType.STUDENT_SECTION_PROGRESS,
            to_node=NodeType.STUDENT_LESSON_PROGRESS,
            properties=[],
            cardinality="many-to-one"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.FOR_LESSON,
            from_node=NodeType.STUDENT_LESSON_PROGRESS,
            to_node=NodeType.LESSON,
            properties=[],
            cardinality="many-to-one"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.IN_COURSE,
            from_node=NodeType.STUDENT_LESSON_PROGRESS,
            to_node=NodeType.COURSE_ENROLLMENT,
            properties=[],
            cardinality="many-to-one"
        ),
        # Enrollment relationships
        RelationshipSchema(
            relationship_type=RelationshipType.ENROLLED_IN_CURRICULUM,
            from_node=NodeType.STUDENT,
            to_node=NodeType.CURRICULUM_ENROLLMENT,
            properties=["enrolled_at"],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.IN_CURRICULUM,
            from_node=NodeType.CURRICULUM_ENROLLMENT,
            to_node=NodeType.CURRICULUM,
            properties=[],
            cardinality="many-to-one"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_COURSE_ENROLLMENT,
            from_node=NodeType.CURRICULUM_ENROLLMENT,
            to_node=NodeType.COURSE_ENROLLMENT,
            properties=[],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.FOR_COURSE,
            from_node=NodeType.COURSE_ENROLLMENT,
            to_node=NodeType.COURSE,
            properties=[],
            cardinality="many-to-one"
        ),
        # Curriculum structure relationships
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_COURSE,
            from_node=NodeType.CURRICULUM,
            to_node=NodeType.COURSE,
            properties=[],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_LESSON,
            from_node=NodeType.COURSE,
            to_node=NodeType.LESSON,
            properties=["order_index"],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.HAS_CONTENT,
            from_node=NodeType.SECTION,
            to_node=NodeType.CONTENT,
            properties=[],
            cardinality="one-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.IS_QUIZ,
            from_node=NodeType.CONTENT,
            to_node=NodeType.QUIZ,
            properties=[],
            cardinality="one-to-one"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.IS_ASSIGNMENT,
            from_node=NodeType.CONTENT,
            to_node=NodeType.ASSIGNMENT,
            properties=[],
            cardinality="one-to-one"
        ),
        # Performance-based relationships
        RelationshipSchema(
            relationship_type=RelationshipType.STRUGGLES_WITH,
            from_node=NodeType.STUDENT,
            to_node=NodeType.TOPIC,
            properties=["average_score", "low_score_count", "total_attempts"],
            cardinality="many-to-many"
        ),
        RelationshipSchema(
            relationship_type=RelationshipType.EXCELS_AT,
            from_node=NodeType.STUDENT,
            to_node=NodeType.TOPIC,
            properties=["average_score", "high_score_count", "total_attempts"],
            cardinality="many-to-many"
        ),
    ]
}


def validate_node(node_type: NodeType, properties: Dict[str, Any]) -> tuple[bool, List[str]]:
    """Validate node properties against schema"""
    errors = []
    
    if node_type not in GRAPH_SCHEMA["nodes"]:
        errors.append(f"Unknown node type: {node_type}")
        return False, errors
    
    schema = GRAPH_SCHEMA["nodes"][node_type]
    
    # Check required properties
    for prop in schema.required_properties:
        if prop not in properties:
            errors.append(f"Missing required property: {prop}")
    
    return len(errors) == 0, errors


def validate_relationship(
    rel_type: RelationshipType,
    from_node_type: NodeType,
    to_node_type: NodeType
) -> tuple[bool, List[str]]:
    """Validate relationship against schema"""
    errors = []
    
    # Find matching relationship schema
    matching = [
        r for r in GRAPH_SCHEMA["relationships"]
        if r.relationship_type == rel_type
        and r.from_node == from_node_type
        and r.to_node == to_node_type
    ]
    
    if not matching:
        errors.append(
            f"Invalid relationship: {rel_type} from {from_node_type} to {to_node_type}"
        )
    
    return len(errors) == 0, errors

