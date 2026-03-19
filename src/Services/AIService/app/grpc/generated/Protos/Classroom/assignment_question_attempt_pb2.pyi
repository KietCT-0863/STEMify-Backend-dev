from google.protobuf import wrappers_pb2 as _wrappers_pb2
from Protos.Classroom import rubric_score_pb2 as _rubric_score_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class GrpcAssignmentQuestionAttemptResponse(_message.Message):
    __slots__ = ("id", "assignmentAttemptId", "assignmentQuestionId", "answerText", "answerFileUrl", "points", "rubricScore")
    ID_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTATTEMPTID_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTQUESTIONID_FIELD_NUMBER: _ClassVar[int]
    ANSWERTEXT_FIELD_NUMBER: _ClassVar[int]
    ANSWERFILEURL_FIELD_NUMBER: _ClassVar[int]
    POINTS_FIELD_NUMBER: _ClassVar[int]
    RUBRICSCORE_FIELD_NUMBER: _ClassVar[int]
    id: int
    assignmentAttemptId: int
    assignmentQuestionId: int
    answerText: str
    answerFileUrl: str
    points: float
    rubricScore: _containers.RepeatedCompositeFieldContainer[_rubric_score_pb2.GrpcRubricScoreModel]
    def __init__(self, id: _Optional[int] = ..., assignmentAttemptId: _Optional[int] = ..., assignmentQuestionId: _Optional[int] = ..., answerText: _Optional[str] = ..., answerFileUrl: _Optional[str] = ..., points: _Optional[float] = ..., rubricScore: _Optional[_Iterable[_Union[_rubric_score_pb2.GrpcRubricScoreModel, _Mapping]]] = ...) -> None: ...

class CreateAssignmentQuestionAttemptRequest(_message.Message):
    __slots__ = ("assignmentQuestionId", "answerText", "answerFile")
    ASSIGNMENTQUESTIONID_FIELD_NUMBER: _ClassVar[int]
    ANSWERTEXT_FIELD_NUMBER: _ClassVar[int]
    ANSWERFILE_FIELD_NUMBER: _ClassVar[int]
    assignmentQuestionId: int
    answerText: _wrappers_pb2.StringValue
    answerFile: _wrappers_pb2.BytesValue
    def __init__(self, assignmentQuestionId: _Optional[int] = ..., answerText: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., answerFile: _Optional[_Union[_wrappers_pb2.BytesValue, _Mapping]] = ...) -> None: ...
