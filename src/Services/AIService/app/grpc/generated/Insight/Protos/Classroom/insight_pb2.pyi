import datetime

from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf import wrappers_pb2 as _wrappers_pb2
from google.api import annotations_pb2 as _annotations_pb2
from Protos.Classroom import quiz_attempt_pb2 as _quiz_attempt_pb2
from Protos.Classroom import assignment_attempt_pb2 as _assignment_attempt_pb2
from Protos.Classroom import rubric_score_pb2 as _rubric_score_pb2
from Protos.Classroom import student_progress_pb2 as _student_progress_pb2
from Protos.Classroom import course_enrollments_pb2 as _course_enrollments_pb2
from Protos.Classroom import curriculum_enrollments_pb2 as _curriculum_enrollments_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class GetClassroomLearningSnapshotRequest(_message.Message):
    __slots__ = ("classroom_id", "student_id", "days_back")
    CLASSROOM_ID_FIELD_NUMBER: _ClassVar[int]
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    DAYS_BACK_FIELD_NUMBER: _ClassVar[int]
    classroom_id: int
    student_id: _wrappers_pb2.StringValue
    days_back: _wrappers_pb2.Int32Value
    def __init__(self, classroom_id: _Optional[int] = ..., student_id: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., days_back: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...

class ClassroomLearningSnapshotResponse(_message.Message):
    __slots__ = ("classroom", "students", "enrollments", "quizzes", "assignments", "engagement_metrics", "progress", "topics_catalog", "analysis_period")
    CLASSROOM_FIELD_NUMBER: _ClassVar[int]
    STUDENTS_FIELD_NUMBER: _ClassVar[int]
    ENROLLMENTS_FIELD_NUMBER: _ClassVar[int]
    QUIZZES_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTS_FIELD_NUMBER: _ClassVar[int]
    ENGAGEMENT_METRICS_FIELD_NUMBER: _ClassVar[int]
    PROGRESS_FIELD_NUMBER: _ClassVar[int]
    TOPICS_CATALOG_FIELD_NUMBER: _ClassVar[int]
    ANALYSIS_PERIOD_FIELD_NUMBER: _ClassVar[int]
    classroom: ClassroomInfo
    students: _containers.RepeatedCompositeFieldContainer[StudentData]
    enrollments: EnrollmentsData
    quizzes: QuizzesData
    assignments: AssignmentsData
    engagement_metrics: EngagementMetricsData
    progress: ProgressData
    topics_catalog: TopicsCatalogData
    analysis_period: AnalysisPeriod
    def __init__(self, classroom: _Optional[_Union[ClassroomInfo, _Mapping]] = ..., students: _Optional[_Iterable[_Union[StudentData, _Mapping]]] = ..., enrollments: _Optional[_Union[EnrollmentsData, _Mapping]] = ..., quizzes: _Optional[_Union[QuizzesData, _Mapping]] = ..., assignments: _Optional[_Union[AssignmentsData, _Mapping]] = ..., engagement_metrics: _Optional[_Union[EngagementMetricsData, _Mapping]] = ..., progress: _Optional[_Union[ProgressData, _Mapping]] = ..., topics_catalog: _Optional[_Union[TopicsCatalogData, _Mapping]] = ..., analysis_period: _Optional[_Union[AnalysisPeriod, _Mapping]] = ...) -> None: ...

class ClassroomInfo(_message.Message):
    __slots__ = ("id", "name", "description", "grade")
    ID_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    GRADE_FIELD_NUMBER: _ClassVar[int]
    id: int
    name: str
    description: _wrappers_pb2.StringValue
    grade: _wrappers_pb2.StringValue
    def __init__(self, id: _Optional[int] = ..., name: _Optional[str] = ..., description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., grade: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ...) -> None: ...

class StudentData(_message.Message):
    __slots__ = ("student_id", "student_name", "image_url", "email", "joined_at")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    STUDENT_NAME_FIELD_NUMBER: _ClassVar[int]
    IMAGE_URL_FIELD_NUMBER: _ClassVar[int]
    EMAIL_FIELD_NUMBER: _ClassVar[int]
    JOINED_AT_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    student_name: str
    image_url: _wrappers_pb2.StringValue
    email: _wrappers_pb2.StringValue
    joined_at: _timestamp_pb2.Timestamp
    def __init__(self, student_id: _Optional[str] = ..., student_name: _Optional[str] = ..., image_url: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., email: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., joined_at: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ...) -> None: ...

class EnrollmentsData(_message.Message):
    __slots__ = ("curriculum_enrollments", "course_enrollments")
    CURRICULUM_ENROLLMENTS_FIELD_NUMBER: _ClassVar[int]
    COURSE_ENROLLMENTS_FIELD_NUMBER: _ClassVar[int]
    curriculum_enrollments: _containers.RepeatedCompositeFieldContainer[CurriculumEnrollmentData]
    course_enrollments: _containers.RepeatedCompositeFieldContainer[CourseEnrollmentData]
    def __init__(self, curriculum_enrollments: _Optional[_Iterable[_Union[CurriculumEnrollmentData, _Mapping]]] = ..., course_enrollments: _Optional[_Iterable[_Union[CourseEnrollmentData, _Mapping]]] = ...) -> None: ...

class CurriculumEnrollmentData(_message.Message):
    __slots__ = ("student_id", "progress_percentage", "curriculum_name")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    PROGRESS_PERCENTAGE_FIELD_NUMBER: _ClassVar[int]
    CURRICULUM_NAME_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    progress_percentage: int
    curriculum_name: _wrappers_pb2.StringValue
    def __init__(self, student_id: _Optional[str] = ..., progress_percentage: _Optional[int] = ..., curriculum_name: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ...) -> None: ...

class CourseEnrollmentData(_message.Message):
    __slots__ = ("student_id", "progress_percentage", "course_name")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    PROGRESS_PERCENTAGE_FIELD_NUMBER: _ClassVar[int]
    COURSE_NAME_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    progress_percentage: int
    course_name: _wrappers_pb2.StringValue
    def __init__(self, student_id: _Optional[str] = ..., progress_percentage: _Optional[int] = ..., course_name: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ...) -> None: ...

class QuizzesData(_message.Message):
    __slots__ = ("student_quizzes", "quiz_attempts")
    STUDENT_QUIZZES_FIELD_NUMBER: _ClassVar[int]
    QUIZ_ATTEMPTS_FIELD_NUMBER: _ClassVar[int]
    student_quizzes: _containers.RepeatedCompositeFieldContainer[StudentQuizData]
    quiz_attempts: _containers.RepeatedCompositeFieldContainer[QuizAttemptData]
    def __init__(self, student_quizzes: _Optional[_Iterable[_Union[StudentQuizData, _Mapping]]] = ..., quiz_attempts: _Optional[_Iterable[_Union[QuizAttemptData, _Mapping]]] = ...) -> None: ...

class StudentQuizData(_message.Message):
    __slots__ = ("id", "student_id", "final_score", "quiz_title", "quiz_description", "attempt_count")
    ID_FIELD_NUMBER: _ClassVar[int]
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    FINAL_SCORE_FIELD_NUMBER: _ClassVar[int]
    QUIZ_TITLE_FIELD_NUMBER: _ClassVar[int]
    QUIZ_DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    ATTEMPT_COUNT_FIELD_NUMBER: _ClassVar[int]
    id: int
    student_id: str
    final_score: _wrappers_pb2.DoubleValue
    quiz_title: _wrappers_pb2.StringValue
    quiz_description: _wrappers_pb2.StringValue
    attempt_count: _wrappers_pb2.Int32Value
    def __init__(self, id: _Optional[int] = ..., student_id: _Optional[str] = ..., final_score: _Optional[_Union[_wrappers_pb2.DoubleValue, _Mapping]] = ..., quiz_title: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., quiz_description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., attempt_count: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...

class QuizAttemptData(_message.Message):
    __slots__ = ("student_quiz_id", "attempt_number", "time_spent_minutes", "total_score", "status", "question_attempts")
    STUDENT_QUIZ_ID_FIELD_NUMBER: _ClassVar[int]
    ATTEMPT_NUMBER_FIELD_NUMBER: _ClassVar[int]
    TIME_SPENT_MINUTES_FIELD_NUMBER: _ClassVar[int]
    TOTAL_SCORE_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    QUESTION_ATTEMPTS_FIELD_NUMBER: _ClassVar[int]
    student_quiz_id: int
    attempt_number: _wrappers_pb2.Int32Value
    time_spent_minutes: _wrappers_pb2.DoubleValue
    total_score: _wrappers_pb2.DoubleValue
    status: _wrappers_pb2.StringValue
    question_attempts: _containers.RepeatedCompositeFieldContainer[QuestionAttemptData]
    def __init__(self, student_quiz_id: _Optional[int] = ..., attempt_number: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., time_spent_minutes: _Optional[_Union[_wrappers_pb2.DoubleValue, _Mapping]] = ..., total_score: _Optional[_Union[_wrappers_pb2.DoubleValue, _Mapping]] = ..., status: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., question_attempts: _Optional[_Iterable[_Union[QuestionAttemptData, _Mapping]]] = ...) -> None: ...

class QuestionAttemptData(_message.Message):
    __slots__ = ("question_id", "question_content", "question_type", "is_correct", "topics", "answer_content", "is_selected")
    QUESTION_ID_FIELD_NUMBER: _ClassVar[int]
    QUESTION_CONTENT_FIELD_NUMBER: _ClassVar[int]
    QUESTION_TYPE_FIELD_NUMBER: _ClassVar[int]
    IS_CORRECT_FIELD_NUMBER: _ClassVar[int]
    TOPICS_FIELD_NUMBER: _ClassVar[int]
    ANSWER_CONTENT_FIELD_NUMBER: _ClassVar[int]
    IS_SELECTED_FIELD_NUMBER: _ClassVar[int]
    question_id: int
    question_content: _wrappers_pb2.StringValue
    question_type: _wrappers_pb2.StringValue
    is_correct: bool
    topics: _containers.RepeatedScalarFieldContainer[str]
    answer_content: _wrappers_pb2.StringValue
    is_selected: _wrappers_pb2.BoolValue
    def __init__(self, question_id: _Optional[int] = ..., question_content: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., question_type: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., is_correct: bool = ..., topics: _Optional[_Iterable[str]] = ..., answer_content: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., is_selected: _Optional[_Union[_wrappers_pb2.BoolValue, _Mapping]] = ...) -> None: ...

class AssignmentsData(_message.Message):
    __slots__ = ("student_assignments",)
    STUDENT_ASSIGNMENTS_FIELD_NUMBER: _ClassVar[int]
    student_assignments: _containers.RepeatedCompositeFieldContainer[StudentAssignmentData]
    def __init__(self, student_assignments: _Optional[_Iterable[_Union[StudentAssignmentData, _Mapping]]] = ...) -> None: ...

class StudentAssignmentData(_message.Message):
    __slots__ = ("student_id", "final_score", "submission_count", "submitted_at", "due_date", "question_attempts")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    FINAL_SCORE_FIELD_NUMBER: _ClassVar[int]
    SUBMISSION_COUNT_FIELD_NUMBER: _ClassVar[int]
    SUBMITTED_AT_FIELD_NUMBER: _ClassVar[int]
    DUE_DATE_FIELD_NUMBER: _ClassVar[int]
    QUESTION_ATTEMPTS_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    final_score: _wrappers_pb2.DoubleValue
    submission_count: _wrappers_pb2.Int32Value
    submitted_at: _timestamp_pb2.Timestamp
    due_date: _timestamp_pb2.Timestamp
    question_attempts: _containers.RepeatedCompositeFieldContainer[AssignmentQuestionAttemptData]
    def __init__(self, student_id: _Optional[str] = ..., final_score: _Optional[_Union[_wrappers_pb2.DoubleValue, _Mapping]] = ..., submission_count: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., submitted_at: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., due_date: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., question_attempts: _Optional[_Iterable[_Union[AssignmentQuestionAttemptData, _Mapping]]] = ...) -> None: ...

class AssignmentQuestionAttemptData(_message.Message):
    __slots__ = ("answer_text", "points", "feedback", "rubric_scores")
    ANSWER_TEXT_FIELD_NUMBER: _ClassVar[int]
    POINTS_FIELD_NUMBER: _ClassVar[int]
    FEEDBACK_FIELD_NUMBER: _ClassVar[int]
    RUBRIC_SCORES_FIELD_NUMBER: _ClassVar[int]
    answer_text: _wrappers_pb2.StringValue
    points: _wrappers_pb2.DoubleValue
    feedback: _wrappers_pb2.StringValue
    rubric_scores: _containers.RepeatedCompositeFieldContainer[RubricScoreData]
    def __init__(self, answer_text: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., points: _Optional[_Union[_wrappers_pb2.DoubleValue, _Mapping]] = ..., feedback: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., rubric_scores: _Optional[_Iterable[_Union[RubricScoreData, _Mapping]]] = ...) -> None: ...

class RubricScoreData(_message.Message):
    __slots__ = ("id", "rubric_criterion_id", "criterion_name", "criterion_description", "max_points", "points")
    ID_FIELD_NUMBER: _ClassVar[int]
    RUBRIC_CRITERION_ID_FIELD_NUMBER: _ClassVar[int]
    CRITERION_NAME_FIELD_NUMBER: _ClassVar[int]
    CRITERION_DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    MAX_POINTS_FIELD_NUMBER: _ClassVar[int]
    POINTS_FIELD_NUMBER: _ClassVar[int]
    id: int
    rubric_criterion_id: int
    criterion_name: str
    criterion_description: _wrappers_pb2.StringValue
    max_points: float
    points: float
    def __init__(self, id: _Optional[int] = ..., rubric_criterion_id: _Optional[int] = ..., criterion_name: _Optional[str] = ..., criterion_description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., max_points: _Optional[float] = ..., points: _Optional[float] = ...) -> None: ...

class EngagementMetricsData(_message.Message):
    __slots__ = ("engagement_metrics",)
    ENGAGEMENT_METRICS_FIELD_NUMBER: _ClassVar[int]
    engagement_metrics: _containers.RepeatedCompositeFieldContainer[EngagementMetricData]
    def __init__(self, engagement_metrics: _Optional[_Iterable[_Union[EngagementMetricData, _Mapping]]] = ...) -> None: ...

class EngagementMetricData(_message.Message):
    __slots__ = ("student_id", "completion_rate", "days_since_last_activity", "active_days_last_7_days", "avg_session_duration_minutes")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    COMPLETION_RATE_FIELD_NUMBER: _ClassVar[int]
    DAYS_SINCE_LAST_ACTIVITY_FIELD_NUMBER: _ClassVar[int]
    ACTIVE_DAYS_LAST_7_DAYS_FIELD_NUMBER: _ClassVar[int]
    AVG_SESSION_DURATION_MINUTES_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    completion_rate: float
    days_since_last_activity: int
    active_days_last_7_days: _wrappers_pb2.Int32Value
    avg_session_duration_minutes: _wrappers_pb2.DoubleValue
    def __init__(self, student_id: _Optional[str] = ..., completion_rate: _Optional[float] = ..., days_since_last_activity: _Optional[int] = ..., active_days_last_7_days: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., avg_session_duration_minutes: _Optional[_Union[_wrappers_pb2.DoubleValue, _Mapping]] = ...) -> None: ...

class ProgressData(_message.Message):
    __slots__ = ("section_progress",)
    SECTION_PROGRESS_FIELD_NUMBER: _ClassVar[int]
    section_progress: _containers.RepeatedCompositeFieldContainer[SectionProgressData]
    def __init__(self, section_progress: _Optional[_Iterable[_Union[SectionProgressData, _Mapping]]] = ...) -> None: ...

class SectionProgressData(_message.Message):
    __slots__ = ("student_id", "section_id", "section_name", "status", "last_activity_at")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    SECTION_ID_FIELD_NUMBER: _ClassVar[int]
    SECTION_NAME_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    LAST_ACTIVITY_AT_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    section_id: int
    section_name: str
    status: str
    last_activity_at: _timestamp_pb2.Timestamp
    def __init__(self, student_id: _Optional[str] = ..., section_id: _Optional[int] = ..., section_name: _Optional[str] = ..., status: _Optional[str] = ..., last_activity_at: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ...) -> None: ...

class TopicsCatalogData(_message.Message):
    __slots__ = ("topics",)
    TOPICS_FIELD_NUMBER: _ClassVar[int]
    topics: _containers.RepeatedCompositeFieldContainer[TopicCatalogItem]
    def __init__(self, topics: _Optional[_Iterable[_Union[TopicCatalogItem, _Mapping]]] = ...) -> None: ...

class TopicCatalogItem(_message.Message):
    __slots__ = ("topic_id", "topic_name", "parent_topic_id", "sections", "lessons")
    TOPIC_ID_FIELD_NUMBER: _ClassVar[int]
    TOPIC_NAME_FIELD_NUMBER: _ClassVar[int]
    PARENT_TOPIC_ID_FIELD_NUMBER: _ClassVar[int]
    SECTIONS_FIELD_NUMBER: _ClassVar[int]
    LESSONS_FIELD_NUMBER: _ClassVar[int]
    topic_id: int
    topic_name: str
    parent_topic_id: _wrappers_pb2.Int32Value
    sections: _containers.RepeatedCompositeFieldContainer[SectionData]
    lessons: _containers.RepeatedCompositeFieldContainer[LessonData]
    def __init__(self, topic_id: _Optional[int] = ..., topic_name: _Optional[str] = ..., parent_topic_id: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., sections: _Optional[_Iterable[_Union[SectionData, _Mapping]]] = ..., lessons: _Optional[_Iterable[_Union[LessonData, _Mapping]]] = ...) -> None: ...

class SectionData(_message.Message):
    __slots__ = ("section_title", "contents")
    SECTION_TITLE_FIELD_NUMBER: _ClassVar[int]
    CONTENTS_FIELD_NUMBER: _ClassVar[int]
    section_title: str
    contents: _containers.RepeatedCompositeFieldContainer[ContentData]
    def __init__(self, section_title: _Optional[str] = ..., contents: _Optional[_Iterable[_Union[ContentData, _Mapping]]] = ...) -> None: ...

class ContentData(_message.Message):
    __slots__ = ("content_type", "content_title")
    CONTENT_TYPE_FIELD_NUMBER: _ClassVar[int]
    CONTENT_TITLE_FIELD_NUMBER: _ClassVar[int]
    content_type: str
    content_title: str
    def __init__(self, content_type: _Optional[str] = ..., content_title: _Optional[str] = ...) -> None: ...

class LessonData(_message.Message):
    __slots__ = ("lesson_title", "lesson_description")
    LESSON_TITLE_FIELD_NUMBER: _ClassVar[int]
    LESSON_DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    lesson_title: str
    lesson_description: _wrappers_pb2.StringValue
    def __init__(self, lesson_title: _Optional[str] = ..., lesson_description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ...) -> None: ...

class AnalysisPeriod(_message.Message):
    __slots__ = ("from_date", "to_date", "days_back")
    FROM_DATE_FIELD_NUMBER: _ClassVar[int]
    TO_DATE_FIELD_NUMBER: _ClassVar[int]
    DAYS_BACK_FIELD_NUMBER: _ClassVar[int]
    from_date: _timestamp_pb2.Timestamp
    to_date: _timestamp_pb2.Timestamp
    days_back: int
    def __init__(self, from_date: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., to_date: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., days_back: _Optional[int] = ...) -> None: ...
