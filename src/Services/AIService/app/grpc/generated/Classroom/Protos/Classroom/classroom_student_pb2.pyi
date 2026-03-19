from google.api import annotations_pb2 as _annotations_pb2
from google.protobuf import empty_pb2 as _empty_pb2
from google.protobuf import wrappers_pb2 as _wrappers_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class CreateClassroomStudentRequest(_message.Message):
    __slots__ = ("classroomId", "studentIds", "studentEmails")
    CLASSROOMID_FIELD_NUMBER: _ClassVar[int]
    STUDENTIDS_FIELD_NUMBER: _ClassVar[int]
    STUDENTEMAILS_FIELD_NUMBER: _ClassVar[int]
    classroomId: int
    studentIds: _containers.RepeatedScalarFieldContainer[str]
    studentEmails: _containers.RepeatedScalarFieldContainer[str]
    def __init__(self, classroomId: _Optional[int] = ..., studentIds: _Optional[_Iterable[str]] = ..., studentEmails: _Optional[_Iterable[str]] = ...) -> None: ...

class DeleteClassroomStudentRequest(_message.Message):
    __slots__ = ("classroomId", "studentIds")
    CLASSROOMID_FIELD_NUMBER: _ClassVar[int]
    STUDENTIDS_FIELD_NUMBER: _ClassVar[int]
    classroomId: int
    studentIds: _containers.RepeatedScalarFieldContainer[str]
    def __init__(self, classroomId: _Optional[int] = ..., studentIds: _Optional[_Iterable[str]] = ...) -> None: ...

class GetClassroomStudentByIdRequest(_message.Message):
    __slots__ = ("classroomId", "studentId")
    CLASSROOMID_FIELD_NUMBER: _ClassVar[int]
    STUDENTID_FIELD_NUMBER: _ClassVar[int]
    classroomId: int
    studentId: str
    def __init__(self, classroomId: _Optional[int] = ..., studentId: _Optional[str] = ...) -> None: ...

class GrpcClassroomStudentResponse(_message.Message):
    __slots__ = ("studentId", "studentName", "studentImageUrl", "studentEmail", "courseEnrollmentStatus", "averageQuizScore", "averageAssignmentScore", "totalQuizzesTaken", "totalAssignmentsSubmitted")
    STUDENTID_FIELD_NUMBER: _ClassVar[int]
    STUDENTNAME_FIELD_NUMBER: _ClassVar[int]
    STUDENTIMAGEURL_FIELD_NUMBER: _ClassVar[int]
    STUDENTEMAIL_FIELD_NUMBER: _ClassVar[int]
    COURSEENROLLMENTSTATUS_FIELD_NUMBER: _ClassVar[int]
    AVERAGEQUIZSCORE_FIELD_NUMBER: _ClassVar[int]
    AVERAGEASSIGNMENTSCORE_FIELD_NUMBER: _ClassVar[int]
    TOTALQUIZZESTAKEN_FIELD_NUMBER: _ClassVar[int]
    TOTALASSIGNMENTSSUBMITTED_FIELD_NUMBER: _ClassVar[int]
    studentId: str
    studentName: str
    studentImageUrl: _wrappers_pb2.StringValue
    studentEmail: str
    courseEnrollmentStatus: str
    averageQuizScore: float
    averageAssignmentScore: float
    totalQuizzesTaken: int
    totalAssignmentsSubmitted: int
    def __init__(self, studentId: _Optional[str] = ..., studentName: _Optional[str] = ..., studentImageUrl: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., studentEmail: _Optional[str] = ..., courseEnrollmentStatus: _Optional[str] = ..., averageQuizScore: _Optional[float] = ..., averageAssignmentScore: _Optional[float] = ..., totalQuizzesTaken: _Optional[int] = ..., totalAssignmentsSubmitted: _Optional[int] = ...) -> None: ...
