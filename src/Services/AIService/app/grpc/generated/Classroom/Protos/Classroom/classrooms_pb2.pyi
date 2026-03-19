import datetime

from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf import wrappers_pb2 as _wrappers_pb2
from google.api import annotations_pb2 as _annotations_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class CreateClassroomRequest(_message.Message):
    __slots__ = ("description", "startDate", "endDate", "coverImageUrl", "courseId", "organizationSubscriptionOrderId", "studentGroups")
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    STARTDATE_FIELD_NUMBER: _ClassVar[int]
    ENDDATE_FIELD_NUMBER: _ClassVar[int]
    COVERIMAGEURL_FIELD_NUMBER: _ClassVar[int]
    COURSEID_FIELD_NUMBER: _ClassVar[int]
    ORGANIZATIONSUBSCRIPTIONORDERID_FIELD_NUMBER: _ClassVar[int]
    STUDENTGROUPS_FIELD_NUMBER: _ClassVar[int]
    description: _wrappers_pb2.StringValue
    startDate: _timestamp_pb2.Timestamp
    endDate: _timestamp_pb2.Timestamp
    coverImageUrl: _wrappers_pb2.StringValue
    courseId: int
    organizationSubscriptionOrderId: int
    studentGroups: _containers.RepeatedCompositeFieldContainer[StudentGroup]
    def __init__(self, description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., startDate: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., endDate: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., coverImageUrl: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., courseId: _Optional[int] = ..., organizationSubscriptionOrderId: _Optional[int] = ..., studentGroups: _Optional[_Iterable[_Union[StudentGroup, _Mapping]]] = ...) -> None: ...

class StudentGroup(_message.Message):
    __slots__ = ("groupCode", "studentIds", "teacherId", "groupName", "grade")
    GROUPCODE_FIELD_NUMBER: _ClassVar[int]
    STUDENTIDS_FIELD_NUMBER: _ClassVar[int]
    TEACHERID_FIELD_NUMBER: _ClassVar[int]
    GROUPNAME_FIELD_NUMBER: _ClassVar[int]
    GRADE_FIELD_NUMBER: _ClassVar[int]
    groupCode: str
    studentIds: _containers.RepeatedScalarFieldContainer[str]
    teacherId: str
    groupName: str
    grade: str
    def __init__(self, groupCode: _Optional[str] = ..., studentIds: _Optional[_Iterable[str]] = ..., teacherId: _Optional[str] = ..., groupName: _Optional[str] = ..., grade: _Optional[str] = ...) -> None: ...

class GrpcCreateClassroomResponse(_message.Message):
    __slots__ = ("classrooms",)
    CLASSROOMS_FIELD_NUMBER: _ClassVar[int]
    classrooms: _containers.RepeatedCompositeFieldContainer[GrpcCreateClassroomModel]
    def __init__(self, classrooms: _Optional[_Iterable[_Union[GrpcCreateClassroomModel, _Mapping]]] = ...) -> None: ...

class GrpcCreateClassroomModel(_message.Message):
    __slots__ = ("id", "classCode", "className")
    ID_FIELD_NUMBER: _ClassVar[int]
    CLASSCODE_FIELD_NUMBER: _ClassVar[int]
    CLASSNAME_FIELD_NUMBER: _ClassVar[int]
    id: int
    classCode: str
    className: str
    def __init__(self, id: _Optional[int] = ..., classCode: _Optional[str] = ..., className: _Optional[str] = ...) -> None: ...

class UpdateClassroomRequest(_message.Message):
    __slots__ = ("id", "name", "grade", "description", "startDate", "endDate", "coverImageUrl", "teacherId", "courseId", "classCode")
    ID_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    GRADE_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    STARTDATE_FIELD_NUMBER: _ClassVar[int]
    ENDDATE_FIELD_NUMBER: _ClassVar[int]
    COVERIMAGEURL_FIELD_NUMBER: _ClassVar[int]
    TEACHERID_FIELD_NUMBER: _ClassVar[int]
    COURSEID_FIELD_NUMBER: _ClassVar[int]
    CLASSCODE_FIELD_NUMBER: _ClassVar[int]
    id: int
    name: _wrappers_pb2.StringValue
    grade: _wrappers_pb2.StringValue
    description: _wrappers_pb2.StringValue
    startDate: _timestamp_pb2.Timestamp
    endDate: _timestamp_pb2.Timestamp
    coverImageUrl: _wrappers_pb2.StringValue
    teacherId: _wrappers_pb2.StringValue
    courseId: _wrappers_pb2.Int32Value
    classCode: _wrappers_pb2.StringValue
    def __init__(self, id: _Optional[int] = ..., name: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., grade: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., startDate: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., endDate: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., coverImageUrl: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., teacherId: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., courseId: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., classCode: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ...) -> None: ...

class DeleteClassroomRequest(_message.Message):
    __slots__ = ("id",)
    ID_FIELD_NUMBER: _ClassVar[int]
    id: int
    def __init__(self, id: _Optional[int] = ...) -> None: ...

class GetClassroomRequest(_message.Message):
    __slots__ = ("id",)
    ID_FIELD_NUMBER: _ClassVar[int]
    id: int
    def __init__(self, id: _Optional[int] = ...) -> None: ...

class GetClassroomsRequest(_message.Message):
    __slots__ = ("pageNumber", "pageSize", "search", "orderBy", "teacherId", "courseId", "status", "organizationId", "fromDate", "toDate", "studentId", "organizationSubscriptionOrderId")
    PAGENUMBER_FIELD_NUMBER: _ClassVar[int]
    PAGESIZE_FIELD_NUMBER: _ClassVar[int]
    SEARCH_FIELD_NUMBER: _ClassVar[int]
    ORDERBY_FIELD_NUMBER: _ClassVar[int]
    TEACHERID_FIELD_NUMBER: _ClassVar[int]
    COURSEID_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    ORGANIZATIONID_FIELD_NUMBER: _ClassVar[int]
    FROMDATE_FIELD_NUMBER: _ClassVar[int]
    TODATE_FIELD_NUMBER: _ClassVar[int]
    STUDENTID_FIELD_NUMBER: _ClassVar[int]
    ORGANIZATIONSUBSCRIPTIONORDERID_FIELD_NUMBER: _ClassVar[int]
    pageNumber: int
    pageSize: int
    search: _wrappers_pb2.StringValue
    orderBy: _wrappers_pb2.StringValue
    teacherId: _wrappers_pb2.StringValue
    courseId: _wrappers_pb2.Int32Value
    status: _wrappers_pb2.StringValue
    organizationId: _wrappers_pb2.Int32Value
    fromDate: _timestamp_pb2.Timestamp
    toDate: _timestamp_pb2.Timestamp
    studentId: _wrappers_pb2.StringValue
    organizationSubscriptionOrderId: _wrappers_pb2.Int32Value
    def __init__(self, pageNumber: _Optional[int] = ..., pageSize: _Optional[int] = ..., search: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., orderBy: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., teacherId: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., courseId: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., status: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., organizationId: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., fromDate: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., toDate: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., studentId: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., organizationSubscriptionOrderId: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...

class GrpcClassroomResponse(_message.Message):
    __slots__ = ("id", "name", "grade", "description", "createdAt", "updatedAt", "startDate", "endDate", "teacher", "classCode", "coverImageUrl", "status", "numberOfStudents", "students", "course", "organizationSubscriptionOrderId", "organizationId")
    ID_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    GRADE_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    CREATEDAT_FIELD_NUMBER: _ClassVar[int]
    UPDATEDAT_FIELD_NUMBER: _ClassVar[int]
    STARTDATE_FIELD_NUMBER: _ClassVar[int]
    ENDDATE_FIELD_NUMBER: _ClassVar[int]
    TEACHER_FIELD_NUMBER: _ClassVar[int]
    CLASSCODE_FIELD_NUMBER: _ClassVar[int]
    COVERIMAGEURL_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    NUMBEROFSTUDENTS_FIELD_NUMBER: _ClassVar[int]
    STUDENTS_FIELD_NUMBER: _ClassVar[int]
    COURSE_FIELD_NUMBER: _ClassVar[int]
    ORGANIZATIONSUBSCRIPTIONORDERID_FIELD_NUMBER: _ClassVar[int]
    ORGANIZATIONID_FIELD_NUMBER: _ClassVar[int]
    id: int
    name: str
    grade: str
    description: _wrappers_pb2.StringValue
    createdAt: str
    updatedAt: _wrappers_pb2.StringValue
    startDate: str
    endDate: str
    teacher: GrpcUserModel
    classCode: str
    coverImageUrl: _wrappers_pb2.StringValue
    status: str
    numberOfStudents: int
    students: _containers.RepeatedCompositeFieldContainer[GrpcUserModel]
    course: GrpcCourseModel
    organizationSubscriptionOrderId: int
    organizationId: int
    def __init__(self, id: _Optional[int] = ..., name: _Optional[str] = ..., grade: _Optional[str] = ..., description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., createdAt: _Optional[str] = ..., updatedAt: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., startDate: _Optional[str] = ..., endDate: _Optional[str] = ..., teacher: _Optional[_Union[GrpcUserModel, _Mapping]] = ..., classCode: _Optional[str] = ..., coverImageUrl: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., status: _Optional[str] = ..., numberOfStudents: _Optional[int] = ..., students: _Optional[_Iterable[_Union[GrpcUserModel, _Mapping]]] = ..., course: _Optional[_Union[GrpcCourseModel, _Mapping]] = ..., organizationSubscriptionOrderId: _Optional[int] = ..., organizationId: _Optional[int] = ...) -> None: ...

class GrpcPagedClassroomsResponse(_message.Message):
    __slots__ = ("items", "totalCount", "pageNumber", "pageSize", "totalPages")
    ITEMS_FIELD_NUMBER: _ClassVar[int]
    TOTALCOUNT_FIELD_NUMBER: _ClassVar[int]
    PAGENUMBER_FIELD_NUMBER: _ClassVar[int]
    PAGESIZE_FIELD_NUMBER: _ClassVar[int]
    TOTALPAGES_FIELD_NUMBER: _ClassVar[int]
    items: _containers.RepeatedCompositeFieldContainer[GrpcClassroomResponse]
    totalCount: int
    pageNumber: int
    pageSize: int
    totalPages: int
    def __init__(self, items: _Optional[_Iterable[_Union[GrpcClassroomResponse, _Mapping]]] = ..., totalCount: _Optional[int] = ..., pageNumber: _Optional[int] = ..., pageSize: _Optional[int] = ..., totalPages: _Optional[int] = ...) -> None: ...

class DeleteClassroomResponse(_message.Message):
    __slots__ = ("success",)
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    success: bool
    def __init__(self, success: bool = ...) -> None: ...

class GrpcUserModel(_message.Message):
    __slots__ = ("id", "name", "email", "imageUrl")
    ID_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    EMAIL_FIELD_NUMBER: _ClassVar[int]
    IMAGEURL_FIELD_NUMBER: _ClassVar[int]
    id: str
    name: str
    email: str
    imageUrl: _wrappers_pb2.StringValue
    def __init__(self, id: _Optional[str] = ..., name: _Optional[str] = ..., email: _Optional[str] = ..., imageUrl: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ...) -> None: ...

class GrpcCourseModel(_message.Message):
    __slots__ = ("id", "title", "description", "imageUrl", "lessonCount", "totalDuration", "code")
    ID_FIELD_NUMBER: _ClassVar[int]
    TITLE_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    IMAGEURL_FIELD_NUMBER: _ClassVar[int]
    LESSONCOUNT_FIELD_NUMBER: _ClassVar[int]
    TOTALDURATION_FIELD_NUMBER: _ClassVar[int]
    CODE_FIELD_NUMBER: _ClassVar[int]
    id: int
    title: str
    description: str
    imageUrl: _wrappers_pb2.StringValue
    lessonCount: int
    totalDuration: int
    code: str
    def __init__(self, id: _Optional[int] = ..., title: _Optional[str] = ..., description: _Optional[str] = ..., imageUrl: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., lessonCount: _Optional[int] = ..., totalDuration: _Optional[int] = ..., code: _Optional[str] = ...) -> None: ...

class GrpcClassroomScheduleResponse(_message.Message):
    __slots__ = ("minutesPerWeek", "totalWeeks", "courseId", "courseTitle", "scheduleItems")
    MINUTESPERWEEK_FIELD_NUMBER: _ClassVar[int]
    TOTALWEEKS_FIELD_NUMBER: _ClassVar[int]
    COURSEID_FIELD_NUMBER: _ClassVar[int]
    COURSETITLE_FIELD_NUMBER: _ClassVar[int]
    SCHEDULEITEMS_FIELD_NUMBER: _ClassVar[int]
    minutesPerWeek: int
    totalWeeks: int
    courseId: int
    courseTitle: str
    scheduleItems: _containers.RepeatedCompositeFieldContainer[GrpcCourseScheduleItem]
    def __init__(self, minutesPerWeek: _Optional[int] = ..., totalWeeks: _Optional[int] = ..., courseId: _Optional[int] = ..., courseTitle: _Optional[str] = ..., scheduleItems: _Optional[_Iterable[_Union[GrpcCourseScheduleItem, _Mapping]]] = ...) -> None: ...

class GrpcCourseScheduleItem(_message.Message):
    __slots__ = ("weekNumber", "lessonSchedule")
    WEEKNUMBER_FIELD_NUMBER: _ClassVar[int]
    LESSONSCHEDULE_FIELD_NUMBER: _ClassVar[int]
    weekNumber: int
    lessonSchedule: _containers.RepeatedCompositeFieldContainer[GrpcLessonSchedule]
    def __init__(self, weekNumber: _Optional[int] = ..., lessonSchedule: _Optional[_Iterable[_Union[GrpcLessonSchedule, _Mapping]]] = ...) -> None: ...

class GrpcLessonSchedule(_message.Message):
    __slots__ = ("lessonId", "lessonTitle", "duration")
    LESSONID_FIELD_NUMBER: _ClassVar[int]
    LESSONTITLE_FIELD_NUMBER: _ClassVar[int]
    DURATION_FIELD_NUMBER: _ClassVar[int]
    lessonId: int
    lessonTitle: str
    duration: int
    def __init__(self, lessonId: _Optional[int] = ..., lessonTitle: _Optional[str] = ..., duration: _Optional[int] = ...) -> None: ...

class GrpcClassroomStatisticResponse(_message.Message):
    __slots__ = ("quizStatistic", "assignmentStatistic", "ungradedAssignments", "courseStats")
    QUIZSTATISTIC_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTSTATISTIC_FIELD_NUMBER: _ClassVar[int]
    UNGRADEDASSIGNMENTS_FIELD_NUMBER: _ClassVar[int]
    COURSESTATS_FIELD_NUMBER: _ClassVar[int]
    quizStatistic: GrpcQuizStatistic
    assignmentStatistic: GrpcAssignmentStatistic
    ungradedAssignments: _containers.RepeatedCompositeFieldContainer[GrpcUngradedAssignment]
    courseStats: GrpcCourseStatistic
    def __init__(self, quizStatistic: _Optional[_Union[GrpcQuizStatistic, _Mapping]] = ..., assignmentStatistic: _Optional[_Union[GrpcAssignmentStatistic, _Mapping]] = ..., ungradedAssignments: _Optional[_Iterable[_Union[GrpcUngradedAssignment, _Mapping]]] = ..., courseStats: _Optional[_Union[GrpcCourseStatistic, _Mapping]] = ...) -> None: ...

class GrpcQuizStatistic(_message.Message):
    __slots__ = ("averageScore", "submissions", "passRate")
    AVERAGESCORE_FIELD_NUMBER: _ClassVar[int]
    SUBMISSIONS_FIELD_NUMBER: _ClassVar[int]
    PASSRATE_FIELD_NUMBER: _ClassVar[int]
    averageScore: float
    submissions: int
    passRate: float
    def __init__(self, averageScore: _Optional[float] = ..., submissions: _Optional[int] = ..., passRate: _Optional[float] = ...) -> None: ...

class GrpcAssignmentStatistic(_message.Message):
    __slots__ = ("averageScore", "submissions", "passRate", "failedRate")
    AVERAGESCORE_FIELD_NUMBER: _ClassVar[int]
    SUBMISSIONS_FIELD_NUMBER: _ClassVar[int]
    PASSRATE_FIELD_NUMBER: _ClassVar[int]
    FAILEDRATE_FIELD_NUMBER: _ClassVar[int]
    averageScore: float
    submissions: int
    passRate: float
    failedRate: float
    def __init__(self, averageScore: _Optional[float] = ..., submissions: _Optional[int] = ..., passRate: _Optional[float] = ..., failedRate: _Optional[float] = ...) -> None: ...

class GrpcUngradedAssignment(_message.Message):
    __slots__ = ("studentAssignmentId", "studentName", "assignmentTitle", "assignmentAttemptId")
    STUDENTASSIGNMENTID_FIELD_NUMBER: _ClassVar[int]
    STUDENTNAME_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTTITLE_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTATTEMPTID_FIELD_NUMBER: _ClassVar[int]
    studentAssignmentId: int
    studentName: str
    assignmentTitle: str
    assignmentAttemptId: int
    def __init__(self, studentAssignmentId: _Optional[int] = ..., studentName: _Optional[str] = ..., assignmentTitle: _Optional[str] = ..., assignmentAttemptId: _Optional[int] = ...) -> None: ...

class GrpcCourseStatistic(_message.Message):
    __slots__ = ("id", "name", "quizStats", "assignmentStats", "studentScoreHistogram")
    ID_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    QUIZSTATS_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTSTATS_FIELD_NUMBER: _ClassVar[int]
    STUDENTSCOREHISTOGRAM_FIELD_NUMBER: _ClassVar[int]
    id: int
    name: str
    quizStats: GrpcDetailedQuizStatistic
    assignmentStats: GrpcDetailedAssignmentStatistic
    studentScoreHistogram: GrpcStudentScoreHistogramResponse
    def __init__(self, id: _Optional[int] = ..., name: _Optional[str] = ..., quizStats: _Optional[_Union[GrpcDetailedQuizStatistic, _Mapping]] = ..., assignmentStats: _Optional[_Union[GrpcDetailedAssignmentStatistic, _Mapping]] = ..., studentScoreHistogram: _Optional[_Union[GrpcStudentScoreHistogramResponse, _Mapping]] = ...) -> None: ...

class GrpcDetailedQuizStatistic(_message.Message):
    __slots__ = ("mean", "median", "min", "max", "q1", "q3", "outliers")
    MEAN_FIELD_NUMBER: _ClassVar[int]
    MEDIAN_FIELD_NUMBER: _ClassVar[int]
    MIN_FIELD_NUMBER: _ClassVar[int]
    MAX_FIELD_NUMBER: _ClassVar[int]
    Q1_FIELD_NUMBER: _ClassVar[int]
    Q3_FIELD_NUMBER: _ClassVar[int]
    OUTLIERS_FIELD_NUMBER: _ClassVar[int]
    mean: float
    median: float
    min: float
    max: float
    q1: float
    q3: float
    outliers: _containers.RepeatedScalarFieldContainer[float]
    def __init__(self, mean: _Optional[float] = ..., median: _Optional[float] = ..., min: _Optional[float] = ..., max: _Optional[float] = ..., q1: _Optional[float] = ..., q3: _Optional[float] = ..., outliers: _Optional[_Iterable[float]] = ...) -> None: ...

class GrpcDetailedAssignmentStatistic(_message.Message):
    __slots__ = ("mean", "median", "min", "max", "q1", "q3", "outliers")
    MEAN_FIELD_NUMBER: _ClassVar[int]
    MEDIAN_FIELD_NUMBER: _ClassVar[int]
    MIN_FIELD_NUMBER: _ClassVar[int]
    MAX_FIELD_NUMBER: _ClassVar[int]
    Q1_FIELD_NUMBER: _ClassVar[int]
    Q3_FIELD_NUMBER: _ClassVar[int]
    OUTLIERS_FIELD_NUMBER: _ClassVar[int]
    mean: float
    median: float
    min: float
    max: float
    q1: float
    q3: float
    outliers: _containers.RepeatedScalarFieldContainer[float]
    def __init__(self, mean: _Optional[float] = ..., median: _Optional[float] = ..., min: _Optional[float] = ..., max: _Optional[float] = ..., q1: _Optional[float] = ..., q3: _Optional[float] = ..., outliers: _Optional[_Iterable[float]] = ...) -> None: ...

class GrpcStudentScoreHistogramResponse(_message.Message):
    __slots__ = ("bins", "totalStudents")
    BINS_FIELD_NUMBER: _ClassVar[int]
    TOTALSTUDENTS_FIELD_NUMBER: _ClassVar[int]
    bins: _containers.RepeatedCompositeFieldContainer[HistogramBin]
    totalStudents: int
    def __init__(self, bins: _Optional[_Iterable[_Union[HistogramBin, _Mapping]]] = ..., totalStudents: _Optional[int] = ...) -> None: ...

class HistogramBin(_message.Message):
    __slots__ = ("rangeStart", "rangeEnd", "count")
    RANGESTART_FIELD_NUMBER: _ClassVar[int]
    RANGEEND_FIELD_NUMBER: _ClassVar[int]
    COUNT_FIELD_NUMBER: _ClassVar[int]
    rangeStart: float
    rangeEnd: float
    count: int
    def __init__(self, rangeStart: _Optional[float] = ..., rangeEnd: _Optional[float] = ..., count: _Optional[int] = ...) -> None: ...

class GetClassroomLearningSnapshotRequest(_message.Message):
    __slots__ = ("classroom_id", "student_id", "days_back")
    CLASSROOM_ID_FIELD_NUMBER: _ClassVar[int]
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    DAYS_BACK_FIELD_NUMBER: _ClassVar[int]
    classroom_id: int
    student_id: _wrappers_pb2.StringValue
    days_back: _wrappers_pb2.Int32Value
    def __init__(self, classroom_id: _Optional[int] = ..., student_id: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., days_back: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...

class GrpcClassroomLearningSnapshotResponse(_message.Message):
    __slots__ = ("classroom", "students", "student_quizzes", "quiz_attempts", "student_assignments", "engagement_metrics", "section_progress", "topics_catalog", "analysis_period")
    CLASSROOM_FIELD_NUMBER: _ClassVar[int]
    STUDENTS_FIELD_NUMBER: _ClassVar[int]
    STUDENT_QUIZZES_FIELD_NUMBER: _ClassVar[int]
    QUIZ_ATTEMPTS_FIELD_NUMBER: _ClassVar[int]
    STUDENT_ASSIGNMENTS_FIELD_NUMBER: _ClassVar[int]
    ENGAGEMENT_METRICS_FIELD_NUMBER: _ClassVar[int]
    SECTION_PROGRESS_FIELD_NUMBER: _ClassVar[int]
    TOPICS_CATALOG_FIELD_NUMBER: _ClassVar[int]
    ANALYSIS_PERIOD_FIELD_NUMBER: _ClassVar[int]
    classroom: GrpcClassroomBasicInfo
    students: _containers.RepeatedCompositeFieldContainer[GrpcStudentLearningData]
    student_quizzes: _containers.RepeatedCompositeFieldContainer[GrpcStudentQuizData]
    quiz_attempts: _containers.RepeatedCompositeFieldContainer[GrpcQuizAttemptData]
    student_assignments: _containers.RepeatedCompositeFieldContainer[GrpcStudentAssignmentData]
    engagement_metrics: _containers.RepeatedCompositeFieldContainer[GrpcEngagementMetricData]
    section_progress: _containers.RepeatedCompositeFieldContainer[GrpcSectionProgressData]
    topics_catalog: _containers.RepeatedCompositeFieldContainer[GrpcTopicCatalogItem]
    analysis_period: GrpcAnalysisPeriod
    def __init__(self, classroom: _Optional[_Union[GrpcClassroomBasicInfo, _Mapping]] = ..., students: _Optional[_Iterable[_Union[GrpcStudentLearningData, _Mapping]]] = ..., student_quizzes: _Optional[_Iterable[_Union[GrpcStudentQuizData, _Mapping]]] = ..., quiz_attempts: _Optional[_Iterable[_Union[GrpcQuizAttemptData, _Mapping]]] = ..., student_assignments: _Optional[_Iterable[_Union[GrpcStudentAssignmentData, _Mapping]]] = ..., engagement_metrics: _Optional[_Iterable[_Union[GrpcEngagementMetricData, _Mapping]]] = ..., section_progress: _Optional[_Iterable[_Union[GrpcSectionProgressData, _Mapping]]] = ..., topics_catalog: _Optional[_Iterable[_Union[GrpcTopicCatalogItem, _Mapping]]] = ..., analysis_period: _Optional[_Union[GrpcAnalysisPeriod, _Mapping]] = ...) -> None: ...

class GrpcClassroomBasicInfo(_message.Message):
    __slots__ = ("id", "name")
    ID_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    id: int
    name: str
    def __init__(self, id: _Optional[int] = ..., name: _Optional[str] = ...) -> None: ...

class GrpcStudentLearningData(_message.Message):
    __slots__ = ("student_id", "student_name", "joined_at", "enrollments")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    STUDENT_NAME_FIELD_NUMBER: _ClassVar[int]
    JOINED_AT_FIELD_NUMBER: _ClassVar[int]
    ENROLLMENTS_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    student_name: str
    joined_at: _wrappers_pb2.StringValue
    enrollments: _containers.RepeatedCompositeFieldContainer[GrpcEnrollmentData]
    def __init__(self, student_id: _Optional[str] = ..., student_name: _Optional[str] = ..., joined_at: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., enrollments: _Optional[_Iterable[_Union[GrpcEnrollmentData, _Mapping]]] = ...) -> None: ...

class GrpcEnrollmentData(_message.Message):
    __slots__ = ("student_id", "curriculum_name", "progress_percentage", "enrollment_type", "course_id", "course_name", "curriculum_id", "status")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    CURRICULUM_NAME_FIELD_NUMBER: _ClassVar[int]
    PROGRESS_PERCENTAGE_FIELD_NUMBER: _ClassVar[int]
    ENROLLMENT_TYPE_FIELD_NUMBER: _ClassVar[int]
    COURSE_ID_FIELD_NUMBER: _ClassVar[int]
    COURSE_NAME_FIELD_NUMBER: _ClassVar[int]
    CURRICULUM_ID_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    curriculum_name: _wrappers_pb2.StringValue
    progress_percentage: float
    enrollment_type: _wrappers_pb2.StringValue
    course_id: int
    course_name: _wrappers_pb2.StringValue
    curriculum_id: int
    status: _wrappers_pb2.StringValue
    def __init__(self, student_id: _Optional[str] = ..., curriculum_name: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., progress_percentage: _Optional[float] = ..., enrollment_type: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., course_id: _Optional[int] = ..., course_name: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., curriculum_id: _Optional[int] = ..., status: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ...) -> None: ...

class GrpcStudentQuizData(_message.Message):
    __slots__ = ("id", "student_id", "quiz_title", "quiz_description", "attempt_count", "final_score", "status", "assigned_at", "completed_at", "max_attempt_allowed")
    ID_FIELD_NUMBER: _ClassVar[int]
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    QUIZ_TITLE_FIELD_NUMBER: _ClassVar[int]
    QUIZ_DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    ATTEMPT_COUNT_FIELD_NUMBER: _ClassVar[int]
    FINAL_SCORE_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    ASSIGNED_AT_FIELD_NUMBER: _ClassVar[int]
    COMPLETED_AT_FIELD_NUMBER: _ClassVar[int]
    MAX_ATTEMPT_ALLOWED_FIELD_NUMBER: _ClassVar[int]
    id: int
    student_id: str
    quiz_title: str
    quiz_description: _wrappers_pb2.StringValue
    attempt_count: int
    final_score: float
    status: _wrappers_pb2.StringValue
    assigned_at: _wrappers_pb2.StringValue
    completed_at: _wrappers_pb2.StringValue
    max_attempt_allowed: _wrappers_pb2.Int32Value
    def __init__(self, id: _Optional[int] = ..., student_id: _Optional[str] = ..., quiz_title: _Optional[str] = ..., quiz_description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., attempt_count: _Optional[int] = ..., final_score: _Optional[float] = ..., status: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., assigned_at: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., completed_at: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., max_attempt_allowed: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...

class GrpcQuizAttemptData(_message.Message):
    __slots__ = ("student_quiz_id", "attempt_number", "total_score", "status", "time_spent_minutes", "started_at", "completed_at", "question_attempts")
    STUDENT_QUIZ_ID_FIELD_NUMBER: _ClassVar[int]
    ATTEMPT_NUMBER_FIELD_NUMBER: _ClassVar[int]
    TOTAL_SCORE_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    TIME_SPENT_MINUTES_FIELD_NUMBER: _ClassVar[int]
    STARTED_AT_FIELD_NUMBER: _ClassVar[int]
    COMPLETED_AT_FIELD_NUMBER: _ClassVar[int]
    QUESTION_ATTEMPTS_FIELD_NUMBER: _ClassVar[int]
    student_quiz_id: int
    attempt_number: int
    total_score: float
    status: str
    time_spent_minutes: float
    started_at: _wrappers_pb2.StringValue
    completed_at: _wrappers_pb2.StringValue
    question_attempts: _containers.RepeatedCompositeFieldContainer[GrpcQuestionAttemptData]
    def __init__(self, student_quiz_id: _Optional[int] = ..., attempt_number: _Optional[int] = ..., total_score: _Optional[float] = ..., status: _Optional[str] = ..., time_spent_minutes: _Optional[float] = ..., started_at: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., completed_at: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., question_attempts: _Optional[_Iterable[_Union[GrpcQuestionAttemptData, _Mapping]]] = ...) -> None: ...

class GrpcQuestionAttemptData(_message.Message):
    __slots__ = ("question_id", "question_content", "question_type", "is_correct", "answer_content", "is_selected", "topics")
    QUESTION_ID_FIELD_NUMBER: _ClassVar[int]
    QUESTION_CONTENT_FIELD_NUMBER: _ClassVar[int]
    QUESTION_TYPE_FIELD_NUMBER: _ClassVar[int]
    IS_CORRECT_FIELD_NUMBER: _ClassVar[int]
    ANSWER_CONTENT_FIELD_NUMBER: _ClassVar[int]
    IS_SELECTED_FIELD_NUMBER: _ClassVar[int]
    TOPICS_FIELD_NUMBER: _ClassVar[int]
    question_id: int
    question_content: str
    question_type: str
    is_correct: bool
    answer_content: str
    is_selected: bool
    topics: _containers.RepeatedScalarFieldContainer[str]
    def __init__(self, question_id: _Optional[int] = ..., question_content: _Optional[str] = ..., question_type: _Optional[str] = ..., is_correct: bool = ..., answer_content: _Optional[str] = ..., is_selected: bool = ..., topics: _Optional[_Iterable[str]] = ...) -> None: ...

class GrpcStudentAssignmentData(_message.Message):
    __slots__ = ("student_id", "final_score", "submission_count", "submitted_at", "due_date", "question_attempts")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    FINAL_SCORE_FIELD_NUMBER: _ClassVar[int]
    SUBMISSION_COUNT_FIELD_NUMBER: _ClassVar[int]
    SUBMITTED_AT_FIELD_NUMBER: _ClassVar[int]
    DUE_DATE_FIELD_NUMBER: _ClassVar[int]
    QUESTION_ATTEMPTS_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    final_score: float
    submission_count: int
    submitted_at: _wrappers_pb2.StringValue
    due_date: _wrappers_pb2.StringValue
    question_attempts: _containers.RepeatedCompositeFieldContainer[GrpcAssignmentQuestionAttemptData]
    def __init__(self, student_id: _Optional[str] = ..., final_score: _Optional[float] = ..., submission_count: _Optional[int] = ..., submitted_at: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., due_date: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., question_attempts: _Optional[_Iterable[_Union[GrpcAssignmentQuestionAttemptData, _Mapping]]] = ...) -> None: ...

class GrpcAssignmentQuestionAttemptData(_message.Message):
    __slots__ = ("question_id", "question_content", "answer_text", "points", "feedback", "rubric_scores", "topics")
    QUESTION_ID_FIELD_NUMBER: _ClassVar[int]
    QUESTION_CONTENT_FIELD_NUMBER: _ClassVar[int]
    ANSWER_TEXT_FIELD_NUMBER: _ClassVar[int]
    POINTS_FIELD_NUMBER: _ClassVar[int]
    FEEDBACK_FIELD_NUMBER: _ClassVar[int]
    RUBRIC_SCORES_FIELD_NUMBER: _ClassVar[int]
    TOPICS_FIELD_NUMBER: _ClassVar[int]
    question_id: int
    question_content: str
    answer_text: str
    points: float
    feedback: _wrappers_pb2.StringValue
    rubric_scores: _containers.RepeatedCompositeFieldContainer[GrpcRubricScoreData]
    topics: _containers.RepeatedScalarFieldContainer[str]
    def __init__(self, question_id: _Optional[int] = ..., question_content: _Optional[str] = ..., answer_text: _Optional[str] = ..., points: _Optional[float] = ..., feedback: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., rubric_scores: _Optional[_Iterable[_Union[GrpcRubricScoreData, _Mapping]]] = ..., topics: _Optional[_Iterable[str]] = ...) -> None: ...

class GrpcRubricScoreData(_message.Message):
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

class GrpcEngagementMetricData(_message.Message):
    __slots__ = ("student_id", "completion_rate", "days_since_last_activity", "active_days_last_7_days", "avg_session_duration_minutes")
    STUDENT_ID_FIELD_NUMBER: _ClassVar[int]
    COMPLETION_RATE_FIELD_NUMBER: _ClassVar[int]
    DAYS_SINCE_LAST_ACTIVITY_FIELD_NUMBER: _ClassVar[int]
    ACTIVE_DAYS_LAST_7_DAYS_FIELD_NUMBER: _ClassVar[int]
    AVG_SESSION_DURATION_MINUTES_FIELD_NUMBER: _ClassVar[int]
    student_id: str
    completion_rate: float
    days_since_last_activity: int
    active_days_last_7_days: int
    avg_session_duration_minutes: float
    def __init__(self, student_id: _Optional[str] = ..., completion_rate: _Optional[float] = ..., days_since_last_activity: _Optional[int] = ..., active_days_last_7_days: _Optional[int] = ..., avg_session_duration_minutes: _Optional[float] = ...) -> None: ...

class GrpcSectionProgressData(_message.Message):
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
    last_activity_at: _wrappers_pb2.StringValue
    def __init__(self, student_id: _Optional[str] = ..., section_id: _Optional[int] = ..., section_name: _Optional[str] = ..., status: _Optional[str] = ..., last_activity_at: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ...) -> None: ...

class GrpcTopicCatalogItem(_message.Message):
    __slots__ = ("topic_id", "topic_name", "parent_topic_id", "lessons", "sections")
    TOPIC_ID_FIELD_NUMBER: _ClassVar[int]
    TOPIC_NAME_FIELD_NUMBER: _ClassVar[int]
    PARENT_TOPIC_ID_FIELD_NUMBER: _ClassVar[int]
    LESSONS_FIELD_NUMBER: _ClassVar[int]
    SECTIONS_FIELD_NUMBER: _ClassVar[int]
    topic_id: int
    topic_name: str
    parent_topic_id: _wrappers_pb2.Int32Value
    lessons: _containers.RepeatedCompositeFieldContainer[GrpcLessonData]
    sections: _containers.RepeatedCompositeFieldContainer[GrpcSectionCatalogData]
    def __init__(self, topic_id: _Optional[int] = ..., topic_name: _Optional[str] = ..., parent_topic_id: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., lessons: _Optional[_Iterable[_Union[GrpcLessonData, _Mapping]]] = ..., sections: _Optional[_Iterable[_Union[GrpcSectionCatalogData, _Mapping]]] = ...) -> None: ...

class GrpcLessonData(_message.Message):
    __slots__ = ("lesson_title", "lesson_description")
    LESSON_TITLE_FIELD_NUMBER: _ClassVar[int]
    LESSON_DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    lesson_title: str
    lesson_description: _wrappers_pb2.StringValue
    def __init__(self, lesson_title: _Optional[str] = ..., lesson_description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ...) -> None: ...

class GrpcSectionCatalogData(_message.Message):
    __slots__ = ("section_id", "section_title", "contents")
    SECTION_ID_FIELD_NUMBER: _ClassVar[int]
    SECTION_TITLE_FIELD_NUMBER: _ClassVar[int]
    CONTENTS_FIELD_NUMBER: _ClassVar[int]
    section_id: int
    section_title: str
    contents: _containers.RepeatedCompositeFieldContainer[GrpcContentItem]
    def __init__(self, section_id: _Optional[int] = ..., section_title: _Optional[str] = ..., contents: _Optional[_Iterable[_Union[GrpcContentItem, _Mapping]]] = ...) -> None: ...

class GrpcContentItem(_message.Message):
    __slots__ = ("content_type", "content_title")
    CONTENT_TYPE_FIELD_NUMBER: _ClassVar[int]
    CONTENT_TITLE_FIELD_NUMBER: _ClassVar[int]
    content_type: str
    content_title: str
    def __init__(self, content_type: _Optional[str] = ..., content_title: _Optional[str] = ...) -> None: ...

class GrpcAnalysisPeriod(_message.Message):
    __slots__ = ("from_date", "to_date", "days_back")
    FROM_DATE_FIELD_NUMBER: _ClassVar[int]
    TO_DATE_FIELD_NUMBER: _ClassVar[int]
    DAYS_BACK_FIELD_NUMBER: _ClassVar[int]
    from_date: str
    to_date: str
    days_back: int
    def __init__(self, from_date: _Optional[str] = ..., to_date: _Optional[str] = ..., days_back: _Optional[int] = ...) -> None: ...
