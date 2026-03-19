from google.protobuf import wrappers_pb2 as _wrappers_pb2
from google.api import annotations_pb2 as _annotations_pb2
from google.protobuf import empty_pb2 as _empty_pb2
from Protos.Resource import assignment_question_pb2 as _assignment_question_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class CreateAssignmentRequest(_message.Message):
    __slots__ = ("sectionId", "passingScore", "durationDays", "questions", "title", "cooldownHours", "maxAttemptAllowed")
    SECTIONID_FIELD_NUMBER: _ClassVar[int]
    PASSINGSCORE_FIELD_NUMBER: _ClassVar[int]
    DURATIONDAYS_FIELD_NUMBER: _ClassVar[int]
    QUESTIONS_FIELD_NUMBER: _ClassVar[int]
    TITLE_FIELD_NUMBER: _ClassVar[int]
    COOLDOWNHOURS_FIELD_NUMBER: _ClassVar[int]
    MAXATTEMPTALLOWED_FIELD_NUMBER: _ClassVar[int]
    sectionId: int
    passingScore: float
    durationDays: _wrappers_pb2.Int32Value
    questions: _containers.RepeatedCompositeFieldContainer[_assignment_question_pb2.CreateAssignmentQuestionRequest]
    title: str
    cooldownHours: _wrappers_pb2.Int32Value
    maxAttemptAllowed: _wrappers_pb2.Int32Value
    def __init__(self, sectionId: _Optional[int] = ..., passingScore: _Optional[float] = ..., durationDays: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., questions: _Optional[_Iterable[_Union[_assignment_question_pb2.CreateAssignmentQuestionRequest, _Mapping]]] = ..., title: _Optional[str] = ..., cooldownHours: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., maxAttemptAllowed: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...

class UpdateAssignmentRequest(_message.Message):
    __slots__ = ("id", "passingScore", "durationDays", "questions", "title", "cooldownHours", "maxAttemptAllowed")
    ID_FIELD_NUMBER: _ClassVar[int]
    PASSINGSCORE_FIELD_NUMBER: _ClassVar[int]
    DURATIONDAYS_FIELD_NUMBER: _ClassVar[int]
    QUESTIONS_FIELD_NUMBER: _ClassVar[int]
    TITLE_FIELD_NUMBER: _ClassVar[int]
    COOLDOWNHOURS_FIELD_NUMBER: _ClassVar[int]
    MAXATTEMPTALLOWED_FIELD_NUMBER: _ClassVar[int]
    id: int
    passingScore: _wrappers_pb2.DoubleValue
    durationDays: _wrappers_pb2.Int32Value
    questions: _containers.RepeatedCompositeFieldContainer[_assignment_question_pb2.UpdateAssignmentQuestionRequest]
    title: _wrappers_pb2.StringValue
    cooldownHours: _wrappers_pb2.Int32Value
    maxAttemptAllowed: _wrappers_pb2.Int32Value
    def __init__(self, id: _Optional[int] = ..., passingScore: _Optional[_Union[_wrappers_pb2.DoubleValue, _Mapping]] = ..., durationDays: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., questions: _Optional[_Iterable[_Union[_assignment_question_pb2.UpdateAssignmentQuestionRequest, _Mapping]]] = ..., title: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., cooldownHours: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., maxAttemptAllowed: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...

class GetAssignmentRequest(_message.Message):
    __slots__ = ("id",)
    ID_FIELD_NUMBER: _ClassVar[int]
    id: int
    def __init__(self, id: _Optional[int] = ...) -> None: ...

class DeleteAssignmentRequest(_message.Message):
    __slots__ = ("id",)
    ID_FIELD_NUMBER: _ClassVar[int]
    id: int
    def __init__(self, id: _Optional[int] = ...) -> None: ...

class GrpcAssignmentModel(_message.Message):
    __slots__ = ("id", "contentId", "title", "totalScore", "passingScore", "durationDays", "questions", "cooldownHours", "maxAttemptAllowed")
    ID_FIELD_NUMBER: _ClassVar[int]
    CONTENTID_FIELD_NUMBER: _ClassVar[int]
    TITLE_FIELD_NUMBER: _ClassVar[int]
    TOTALSCORE_FIELD_NUMBER: _ClassVar[int]
    PASSINGSCORE_FIELD_NUMBER: _ClassVar[int]
    DURATIONDAYS_FIELD_NUMBER: _ClassVar[int]
    QUESTIONS_FIELD_NUMBER: _ClassVar[int]
    COOLDOWNHOURS_FIELD_NUMBER: _ClassVar[int]
    MAXATTEMPTALLOWED_FIELD_NUMBER: _ClassVar[int]
    id: int
    contentId: int
    title: str
    totalScore: float
    passingScore: float
    durationDays: _wrappers_pb2.Int32Value
    questions: _containers.RepeatedCompositeFieldContainer[_assignment_question_pb2.GrpcAssignmentQuestionModel]
    cooldownHours: _wrappers_pb2.Int32Value
    maxAttemptAllowed: _wrappers_pb2.Int32Value
    def __init__(self, id: _Optional[int] = ..., contentId: _Optional[int] = ..., title: _Optional[str] = ..., totalScore: _Optional[float] = ..., passingScore: _Optional[float] = ..., durationDays: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., questions: _Optional[_Iterable[_Union[_assignment_question_pb2.GrpcAssignmentQuestionModel, _Mapping]]] = ..., cooldownHours: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., maxAttemptAllowed: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...

class GrpcAssignment(_message.Message):
    __slots__ = ("id", "contentId", "totalScore", "passingScore", "durationDays")
    ID_FIELD_NUMBER: _ClassVar[int]
    CONTENTID_FIELD_NUMBER: _ClassVar[int]
    TOTALSCORE_FIELD_NUMBER: _ClassVar[int]
    PASSINGSCORE_FIELD_NUMBER: _ClassVar[int]
    DURATIONDAYS_FIELD_NUMBER: _ClassVar[int]
    id: int
    contentId: int
    totalScore: float
    passingScore: float
    durationDays: _wrappers_pb2.Int32Value
    def __init__(self, id: _Optional[int] = ..., contentId: _Optional[int] = ..., totalScore: _Optional[float] = ..., passingScore: _Optional[float] = ..., durationDays: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...

class AssignmentQuestionsTemplate(_message.Message):
    __slots__ = ("csvFile", "fileName")
    CSVFILE_FIELD_NUMBER: _ClassVar[int]
    FILENAME_FIELD_NUMBER: _ClassVar[int]
    csvFile: _wrappers_pb2.BytesValue
    fileName: str
    def __init__(self, csvFile: _Optional[_Union[_wrappers_pb2.BytesValue, _Mapping]] = ..., fileName: _Optional[str] = ...) -> None: ...

class ImportAssignmentQuestionsRequest(_message.Message):
    __slots__ = ("id", "csvFile")
    ID_FIELD_NUMBER: _ClassVar[int]
    CSVFILE_FIELD_NUMBER: _ClassVar[int]
    id: int
    csvFile: _wrappers_pb2.BytesValue
    def __init__(self, id: _Optional[int] = ..., csvFile: _Optional[_Union[_wrappers_pb2.BytesValue, _Mapping]] = ...) -> None: ...

class AssignmentImportResult(_message.Message):
    __slots__ = ("totalRows", "successCount", "failureCount", "errors")
    TOTALROWS_FIELD_NUMBER: _ClassVar[int]
    SUCCESSCOUNT_FIELD_NUMBER: _ClassVar[int]
    FAILURECOUNT_FIELD_NUMBER: _ClassVar[int]
    ERRORS_FIELD_NUMBER: _ClassVar[int]
    totalRows: int
    successCount: int
    failureCount: int
    errors: _containers.RepeatedCompositeFieldContainer[AssignmentImportError]
    def __init__(self, totalRows: _Optional[int] = ..., successCount: _Optional[int] = ..., failureCount: _Optional[int] = ..., errors: _Optional[_Iterable[_Union[AssignmentImportError, _Mapping]]] = ...) -> None: ...

class AssignmentImportError(_message.Message):
    __slots__ = ("rowNumber", "field", "errorMessage", "rowData")
    ROWNUMBER_FIELD_NUMBER: _ClassVar[int]
    FIELD_FIELD_NUMBER: _ClassVar[int]
    ERRORMESSAGE_FIELD_NUMBER: _ClassVar[int]
    ROWDATA_FIELD_NUMBER: _ClassVar[int]
    rowNumber: int
    field: str
    errorMessage: str
    rowData: str
    def __init__(self, rowNumber: _Optional[int] = ..., field: _Optional[str] = ..., errorMessage: _Optional[str] = ..., rowData: _Optional[str] = ...) -> None: ...
