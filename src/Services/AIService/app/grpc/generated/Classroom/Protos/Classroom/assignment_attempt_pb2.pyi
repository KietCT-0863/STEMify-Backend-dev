import datetime

from google.protobuf import timestamp_pb2 as _timestamp_pb2
from google.protobuf import wrappers_pb2 as _wrappers_pb2
from google.api import annotations_pb2 as _annotations_pb2
from Protos.Classroom import assignment_question_attempt_pb2 as _assignment_question_attempt_pb2
from Protos.Classroom import rubric_score_pb2 as _rubric_score_pb2
from Protos.Resource import assignment_pb2 as _assignment_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class CreateAssignmentAttemptRequest(_message.Message):
    __slots__ = ("studentAssignmentId", "questionAttempts")
    STUDENTASSIGNMENTID_FIELD_NUMBER: _ClassVar[int]
    QUESTIONATTEMPTS_FIELD_NUMBER: _ClassVar[int]
    studentAssignmentId: int
    questionAttempts: _containers.RepeatedCompositeFieldContainer[_assignment_question_attempt_pb2.CreateAssignmentQuestionAttemptRequest]
    def __init__(self, studentAssignmentId: _Optional[int] = ..., questionAttempts: _Optional[_Iterable[_Union[_assignment_question_attempt_pb2.CreateAssignmentQuestionAttemptRequest, _Mapping]]] = ...) -> None: ...

class UpdateAssignmentAttemptRequest(_message.Message):
    __slots__ = ("id", "feedback", "questionGrades")
    ID_FIELD_NUMBER: _ClassVar[int]
    FEEDBACK_FIELD_NUMBER: _ClassVar[int]
    QUESTIONGRADES_FIELD_NUMBER: _ClassVar[int]
    id: int
    feedback: _wrappers_pb2.StringValue
    questionGrades: _containers.RepeatedCompositeFieldContainer[CreateQuestionGradeRequest]
    def __init__(self, id: _Optional[int] = ..., feedback: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., questionGrades: _Optional[_Iterable[_Union[CreateQuestionGradeRequest, _Mapping]]] = ...) -> None: ...

class CreateQuestionGradeRequest(_message.Message):
    __slots__ = ("assignmentQuestionAttemptId", "rubricScores")
    ASSIGNMENTQUESTIONATTEMPTID_FIELD_NUMBER: _ClassVar[int]
    RUBRICSCORES_FIELD_NUMBER: _ClassVar[int]
    assignmentQuestionAttemptId: int
    rubricScores: _containers.RepeatedCompositeFieldContainer[_rubric_score_pb2.CreateRubricScoreRequest]
    def __init__(self, assignmentQuestionAttemptId: _Optional[int] = ..., rubricScores: _Optional[_Iterable[_Union[_rubric_score_pb2.CreateRubricScoreRequest, _Mapping]]] = ...) -> None: ...

class GetAssignmentAttemptByIdRequest(_message.Message):
    __slots__ = ("id",)
    ID_FIELD_NUMBER: _ClassVar[int]
    id: int
    def __init__(self, id: _Optional[int] = ...) -> None: ...

class GetAssignmentAttemptParams(_message.Message):
    __slots__ = ("pageNumber", "pageSize", "search", "orderBy", "studentId", "status", "courseId", "classroomId", "assignmentId", "fromDate", "toDate")
    PAGENUMBER_FIELD_NUMBER: _ClassVar[int]
    PAGESIZE_FIELD_NUMBER: _ClassVar[int]
    SEARCH_FIELD_NUMBER: _ClassVar[int]
    ORDERBY_FIELD_NUMBER: _ClassVar[int]
    STUDENTID_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    COURSEID_FIELD_NUMBER: _ClassVar[int]
    CLASSROOMID_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTID_FIELD_NUMBER: _ClassVar[int]
    FROMDATE_FIELD_NUMBER: _ClassVar[int]
    TODATE_FIELD_NUMBER: _ClassVar[int]
    pageNumber: int
    pageSize: int
    search: _wrappers_pb2.StringValue
    orderBy: _wrappers_pb2.StringValue
    studentId: _wrappers_pb2.StringValue
    status: str
    courseId: _wrappers_pb2.Int32Value
    classroomId: _wrappers_pb2.Int32Value
    assignmentId: _wrappers_pb2.Int32Value
    fromDate: _timestamp_pb2.Timestamp
    toDate: _timestamp_pb2.Timestamp
    def __init__(self, pageNumber: _Optional[int] = ..., pageSize: _Optional[int] = ..., search: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., orderBy: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., studentId: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., status: _Optional[str] = ..., courseId: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., classroomId: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., assignmentId: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., fromDate: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., toDate: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ...) -> None: ...

class GrpcAssignmentAttemptResponse(_message.Message):
    __slots__ = ("id", "studentAssignmentId", "teacherId", "submittedAt", "totalScore", "status", "feedback", "attemptNumber", "questionAttempts")
    ID_FIELD_NUMBER: _ClassVar[int]
    STUDENTASSIGNMENTID_FIELD_NUMBER: _ClassVar[int]
    TEACHERID_FIELD_NUMBER: _ClassVar[int]
    SUBMITTEDAT_FIELD_NUMBER: _ClassVar[int]
    TOTALSCORE_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    FEEDBACK_FIELD_NUMBER: _ClassVar[int]
    ATTEMPTNUMBER_FIELD_NUMBER: _ClassVar[int]
    QUESTIONATTEMPTS_FIELD_NUMBER: _ClassVar[int]
    id: int
    studentAssignmentId: int
    teacherId: str
    submittedAt: str
    totalScore: float
    status: str
    feedback: str
    attemptNumber: int
    questionAttempts: _containers.RepeatedCompositeFieldContainer[_assignment_question_attempt_pb2.GrpcAssignmentQuestionAttemptResponse]
    def __init__(self, id: _Optional[int] = ..., studentAssignmentId: _Optional[int] = ..., teacherId: _Optional[str] = ..., submittedAt: _Optional[str] = ..., totalScore: _Optional[float] = ..., status: _Optional[str] = ..., feedback: _Optional[str] = ..., attemptNumber: _Optional[int] = ..., questionAttempts: _Optional[_Iterable[_Union[_assignment_question_attempt_pb2.GrpcAssignmentQuestionAttemptResponse, _Mapping]]] = ...) -> None: ...

class GrpcAssignmentAttemptModel(_message.Message):
    __slots__ = ("id", "studentAssignmentId", "teacherId", "submittedAt", "totalScore", "status", "feedback", "attemptNumber", "assignment")
    ID_FIELD_NUMBER: _ClassVar[int]
    STUDENTASSIGNMENTID_FIELD_NUMBER: _ClassVar[int]
    TEACHERID_FIELD_NUMBER: _ClassVar[int]
    SUBMITTEDAT_FIELD_NUMBER: _ClassVar[int]
    TOTALSCORE_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    FEEDBACK_FIELD_NUMBER: _ClassVar[int]
    ATTEMPTNUMBER_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENT_FIELD_NUMBER: _ClassVar[int]
    id: int
    studentAssignmentId: int
    teacherId: str
    submittedAt: _timestamp_pb2.Timestamp
    totalScore: float
    status: str
    feedback: str
    attemptNumber: int
    assignment: _assignment_pb2.GrpcAssignment
    def __init__(self, id: _Optional[int] = ..., studentAssignmentId: _Optional[int] = ..., teacherId: _Optional[str] = ..., submittedAt: _Optional[_Union[datetime.datetime, _timestamp_pb2.Timestamp, _Mapping]] = ..., totalScore: _Optional[float] = ..., status: _Optional[str] = ..., feedback: _Optional[str] = ..., attemptNumber: _Optional[int] = ..., assignment: _Optional[_Union[_assignment_pb2.GrpcAssignment, _Mapping]] = ...) -> None: ...

class GrpcPagedAssignmentAttemptsResponse(_message.Message):
    __slots__ = ("items", "totalCount", "pageNumber", "pageSize", "totalPages")
    ITEMS_FIELD_NUMBER: _ClassVar[int]
    TOTALCOUNT_FIELD_NUMBER: _ClassVar[int]
    PAGENUMBER_FIELD_NUMBER: _ClassVar[int]
    PAGESIZE_FIELD_NUMBER: _ClassVar[int]
    TOTALPAGES_FIELD_NUMBER: _ClassVar[int]
    items: _containers.RepeatedCompositeFieldContainer[GrpcAssignmentAttemptModel]
    totalCount: int
    pageNumber: int
    pageSize: int
    totalPages: int
    def __init__(self, items: _Optional[_Iterable[_Union[GrpcAssignmentAttemptModel, _Mapping]]] = ..., totalCount: _Optional[int] = ..., pageNumber: _Optional[int] = ..., pageSize: _Optional[int] = ..., totalPages: _Optional[int] = ...) -> None: ...
