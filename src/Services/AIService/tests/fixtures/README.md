# Test Fixtures - Mock Classroom Data

## Overview

Mock data được tạo để test RAG pipeline trước khi tích hợp với database thực. Tất cả fields đều được include để đảm bảo schema đầy đủ và **phản ánh đúng cấu trúc real-world** từ Classroom Service và Resource Service.

## Files

- `mock_classroom_data.py`: Complete mock data với đầy đủ fields, **bao gồm tất cả entities cần thiết cho graph**
- `sample_data.py`: Re-export functions để dễ import

## Usage

### Full Mock Data

```python
from tests.fixtures.mock_classroom_data import get_mock_classroom_data

data = get_mock_classroom_data()
# Contains all fields: classroom, students, enrollments, progress, quizzes, assignments, topics, time_metrics
```

### Minimal Mock Data

```python
from tests.fixtures.mock_classroom_data import get_minimal_classroom_data

data = get_minimal_classroom_data()
# Contains only essential fields for quick testing
```

### Topic-Focused Data

```python
from tests.fixtures.mock_classroom_data import get_topic_focused_data

data = get_topic_focused_data()
# Contains additional quiz attempts with different topics for testing weak topics detection
```

## Data Structure

### Complete Schema

```
{
  "classroom": {...},           # Classroom metadata
  "students": [...],            # Student information
  "enrollments": {
    "curriculum_enrollments": [...],  # Links Student -> Curriculum
    "course_enrollments": [...]       # Links CurriculumEnrollment -> Course
  },
  "progress": {
    "lesson_progress": [...],         # StudentLessonProgress (links CourseEnrollment -> Lesson)
    "section_progress": [...]         # StudentSectionProgress (links LessonProgress -> Section)
  },
  "quizzes": {
    "student_quizzes": [...],         # StudentQuiz (CRITICAL: links Student -> Quiz, includes student_section_progress_id)
    "quiz_attempts": [...]            # QuizAttempt (links StudentQuiz -> QuizAttempt)
                                      # With question_attempts, answer_attempts, topics
  },
  "assignments": {
    "student_assignments": [...],     # StudentAssignment (CRITICAL: links Student -> Assignment, includes student_section_progress_id)
    "assignment_attempts": [...]       # AssignmentAttempt (links StudentAssignment -> AssignmentAttempt)
                                      # With question_attempts, rubric_scores, topics
  },
  "topics": [...],                    # Topic mapping from Resource Service
                                      # Includes: Topic -> Lesson -> Section -> Content -> Quiz/Assignment structure
  "time_metrics": {...},              # Calculated time-based metrics
  "analysis_period": {...}            # Date range for analysis
}
```

### Key Entities for Graph

#### 1. **StudentQuiz & StudentAssignment** (CRITICAL)
- **Purpose**: Track quiz/assignment status, scores, attempts per student
- **Key Fields**:
  - `id`, `quiz_id`/`assignment_id`, `student_id`
  - `status`, `final_score`, `attempt_count`
  - `assigned_at`, `due_date`
  - **`student_section_progress_id`**: Links quiz/assignment to section progress

#### 2. **Progress Tracking** (CRITICAL)
- **StudentSectionProgress**: Tracks section completion
  - `id`, `section_id`, `status`, `completed_at`
  - `student_lesson_progress_id`: Links to lesson progress
- **StudentLessonProgress**: Tracks lesson completion
  - `id`, `lesson_id`, `status`, `completed_at`
  - `enrollment_id`: Links to course enrollment

#### 3. **Enrollment Hierarchy**
- **CurriculumEnrollment**: Student enrollment in curriculum
  - `id`, `student_id`, `curriculum_id`, `classroom_id`
  - `status`, `progress_percentage`
- **CourseEnrollment**: Student enrollment in course
  - `id`, `student_id`, `course_id`
  - `curriculum_enrollment_id`: Links to curriculum enrollment
  - `status`, `progress_percentage`

#### 4. **Curriculum Structure**
- **Topics Structure**: Includes full hierarchy
  ```
  Topic
    └─ Lessons
        └─ Sections
            └─ Contents
                ├─ Quiz (content_type: "Quiz")
                └─ Assignment (content_type: "Assignment")
  ```

## Key Fields

### StudentQuiz (NEW - CRITICAL)
- `id`: Unique identifier
- `quiz_id`: Links to Quiz entity
- `student_id`: Links to Student
- `student_section_progress_id`: **CRITICAL** - Links to StudentSectionProgress
- `status`: "Assigned", "Passed", "Failed", etc.
- `final_score`: Best score across all attempts
- `attempt_count`: Number of attempts made
- `assigned_at`, `due_date`: Assignment timestamps

### StudentAssignment (NEW - CRITICAL)
- `id`: Unique identifier
- `assignment_id`: Links to Assignment entity
- `student_id`: Links to Student
- `student_section_progress_id`: **CRITICAL** - Links to StudentSectionProgress
- `status`: "Assigned", "Passed", "Failed", "UnderReview", etc.
- `final_score`: Best score across all attempts
- `attempt_count`: Number of attempts made
- `assigned_at`, `due_date`: Assignment timestamps

### Progress Tracking (NEW - CRITICAL)

#### StudentSectionProgress
- `id`: Unique identifier
- `section_id`: Links to Section entity
- `student_lesson_progress_id`: Links to StudentLessonProgress
- `status`: "InProgress", "Completed"
- `completed_at`: Completion timestamp

#### StudentLessonProgress
- `id`: Unique identifier
- `lesson_id`: Links to Lesson entity
- `enrollment_id`: Links to CourseEnrollment
- `status`: "InProgress", "Completed"
- `completed_at`: Completion timestamp

### Enrollment (NEW)

#### CurriculumEnrollment
- `id`: Unique identifier
- `student_id`: Links to Student
- `curriculum_id`: Links to Curriculum
- `classroom_id`: Links to Classroom
- `status`: "InProgress", "Completed"
- `progress_percentage`: Overall progress
- `enrolled_at`, `completed_at`: Timestamps

#### CourseEnrollment
- `id`: Unique identifier
- `student_id`: Links to Student
- `course_id`: Links to Course
- `curriculum_enrollment_id`: Links to CurriculumEnrollment
- `status`: "InProgress", "Completed"
- `progress_percentage`: Course progress
- `enrolled_at`, `completed_at`: Timestamps

### Quiz Attempts
- `id`: Unique identifier
- `student_quiz_id`: **CRITICAL** - Links to StudentQuiz (not directly to Student)
- `attempt_number`: Sequential attempt number
- `started_at`, `completed_at`: Timestamps
- `time_spent_minutes`: Calculated from timestamps
- `status`: "Passed", "Failed", etc.
- `total_score`: Score for this attempt
- `question_attempts`: Array with:
  - `question_id`, `question_content`, `question_type`, `question_points`
  - `is_correct`, `score`
  - `answer_attempts`: Array with `answer_id`, `answer_content`, `is_correct`, `is_selected`
  - `topics`: Array of topic names

### Assignment Attempts
- `id`: Unique identifier
- `student_assignment_id`: **CRITICAL** - Links to StudentAssignment (not directly to Student)
- `attempt_number`: Sequential attempt number
- `submitted_at`: Timestamp
- `status`: "Passed", "Failed", "UnderReview", etc.
- `total_score`: Score for this attempt
- `feedback`: Teacher feedback text
- `question_attempts`: Array with:
  - `assignment_question_id`, `question_content`, `question_type`, `question_points`
  - `answer_text`, `answer_file_url`
  - `points`: Scored points
  - `rubric_scores`: Array with `rubric_criterion_id`, `criterion_name`, `max_points`, `points`
  - `topics`: Array of topic names

### Topics Structure (ENHANCED)
- `topic_id`, `topic_name`
- `lessons`: Array with full hierarchy:
  ```json
  {
    "lesson_id": 10,
    "lesson_title": "Lực và Chuyển động",
    "sections": [
      {
        "section_id": 25,
        "section_title": "Khái niệm về Lực",
        "contents": [
          {
            "content_id": 50,
            "content_type": "Quiz",
            "content_title": "Quiz: Lực và Chuyển động"
          },
          {
            "content_id": 52,
            "content_type": "Assignment",
            "content_title": "Bài tập: Thiết kế mạch điện"
          }
        ]
      }
    ]
  }
  ```
- Maps to questions/assignments for topic-level analysis
- **Full curriculum structure**: Topic → Lesson → Section → Content → Quiz/Assignment

## Graph Structure

Mock data được design để build **complete knowledge graph** với đầy đủ relationships:

### Graph Hierarchy

```
Student
  └─[ENROLLED_IN_CURRICULUM]─> CurriculumEnrollment ─[IN_CURRICULUM]─> Curriculum
      └─[HAS_COURSE_ENROLLMENT]─> CourseEnrollment ─[FOR_COURSE]─> Course
          └─[HAS_LESSON]─> Lesson
              └─[CONTAINS]─> Section
                  └─[HAS_CONTENT]─> Content
                      ├─[IS_QUIZ]─> Quiz
                      └─[IS_ASSIGNMENT]─> Assignment

Student
  └─[HAS_QUIZ]─> StudentQuiz ─[FOR_QUIZ]─> Quiz
      └─[IN_SECTION]─> StudentSectionProgress ─[FOR_SECTION]─> Section
          └─[IN_LESSON]─> StudentLessonProgress ─[FOR_LESSON]─> Lesson
              └─[IN_COURSE]─> CourseEnrollment
      └─[HAS_ATTEMPT]─> QuizAttempt

Student
  └─[HAS_ASSIGNMENT]─> StudentAssignment ─[FOR_ASSIGNMENT]─> Assignment
      └─[IN_SECTION]─> StudentSectionProgress
      └─[HAS_ATTEMPT]─> AssignmentAttempt

Quiz ─[HAS_TOPIC]─> Topic
Assignment ─[HAS_TOPIC]─> Topic
```

### Key Relationships

1. **StudentQuiz/StudentAssignment** → Links student với quiz/assignment, tracks status và scores
2. **Progress Tracking** → Links quiz/assignment với section/lesson progress
3. **Enrollment Hierarchy** → Links student với curriculum/course structure
4. **Curriculum Structure** → Full hierarchy từ Curriculum → Course → Lesson → Section → Content → Quiz/Assignment

## Testing Scenarios

### 1. Basic Insights
```python
data = get_mock_classroom_data()
# Test: Generate class insights with full data
# Includes: StudentQuiz, StudentAssignment, Progress nodes, Curriculum structure
```

### 2. Weak Topics Detection
```python
data = get_topic_focused_data()
# Test: Identify topics with high error rates
# Uses: Quiz/Assignment → Topic relationships
# Graph query: Find topics with low average scores across all attempts
```

### 3. Student Performance
```python
data = get_mock_classroom_data()
# Test: Analyze individual student performance
# Student 1: 85.5% (improved from 65%) - via StudentQuiz.final_score
# Student 2: 92% (excellent) - via StudentQuiz.final_score
# Student 3: 55% (struggling, 3 failed attempts) - via StudentQuiz.attempt_count
# Graph query: Student → StudentQuiz → QuizAttempt (track improvement)
```

### 4. Engagement Analysis
```python
data = get_mock_classroom_data()
# Test: Calculate engagement metrics
# Student 1: Last activity today, 5 activities in 7 days
# Student 3: Last activity 2 days ago, 3 activities (low engagement)
# Graph query: Student → StudentSectionProgress (track completion)
```

### 5. Progress Tracking (NEW)
```python
data = get_mock_classroom_data()
# Test: Track student progress through curriculum
# Graph query: Student → CurriculumEnrollment → CourseEnrollment → LessonProgress → SectionProgress
# Identify: Which sections/lessons are bottlenecks
```

### 6. Section-Level Analysis (NEW)
```python
data = get_mock_classroom_data()
# Test: Analyze performance by section
# Graph query: StudentSectionProgress → StudentQuiz/StudentAssignment
# Identify: Which sections have low quiz/assignment scores
```

## Notes

- All timestamps are in ISO 8601 format (UTC)
- All scores are percentages (0-100)
- Topics are mapped from Resource Service with full curriculum structure
- Time spent is calculated from `started_at` and `completed_at`
- Mock data includes realistic Vietnamese names and content

### Critical Fields for Graph Building

1. **`student_section_progress_id`** in StudentQuiz/StudentAssignment:
   - **MUST** be present to link quiz/assignment to section progress
   - Links to `progress.section_progress[].id`

2. **`student_lesson_progress_id`** in StudentSectionProgress:
   - **MUST** be present to link section progress to lesson progress
   - Links to `progress.lesson_progress[].id`

3. **`enrollment_id`** in StudentLessonProgress:
   - **MUST** be present to link lesson progress to course enrollment
   - Links to `enrollments.course_enrollments[].id`

4. **`curriculum_enrollment_id`** in CourseEnrollment:
   - Links course enrollment to curriculum enrollment
   - Links to `enrollments.curriculum_enrollments[].id`

5. **Topics structure**:
   - Includes full hierarchy: Topic → Lesson → Section → Content
   - Content has `content_type` ("Quiz" or "Assignment")
   - Content links to Quiz/Assignment via title matching

### Graph Accuracy

Mock data được design để **100% match** với real-world structure từ:
- **Classroom Service**: StudentQuiz, StudentAssignment, Progress, Enrollment entities
- **Resource Service**: Curriculum, Course, Lesson, Section, Content, Topic structure

Graph builder sẽ tạo:
- ✅ All core entities (Student, Quiz, Assignment, Topic)
- ✅ All intermediate entities (StudentQuiz, StudentAssignment)
- ✅ All progress tracking (StudentSectionProgress, StudentLessonProgress)
- ✅ All enrollment tracking (CurriculumEnrollment, CourseEnrollment)
- ✅ Full curriculum structure (Curriculum → Course → Lesson → Section → Content)

## Schema Validation

Use `validate_schema()` function to ensure data matches expected structure:

```python
from tests.fixtures.mock_classroom_data import validate_schema

data = get_mock_classroom_data()
is_valid, errors = validate_schema(data)
if not is_valid:
    print(f"Schema errors: {errors}")
```

