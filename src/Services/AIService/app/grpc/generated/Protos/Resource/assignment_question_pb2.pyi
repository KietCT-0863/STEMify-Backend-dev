from google.protobuf import wrappers_pb2 as _wrappers_pb2
from Protos.Resource import rubric_criterion_pb2 as _rubric_criterion_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class CreateAssignmentQuestionRequest(_message.Message):
    __slots__ = ("type", "orderIndex", "points", "content", "rubricCriterion")
    TYPE_FIELD_NUMBER: _ClassVar[int]
    ORDERINDEX_FIELD_NUMBER: _ClassVar[int]
    POINTS_FIELD_NUMBER: _ClassVar[int]
    CONTENT_FIELD_NUMBER: _ClassVar[int]
    RUBRICCRITERION_FIELD_NUMBER: _ClassVar[int]
    type: str
    orderIndex: int
    points: float
    content: str
    rubricCriterion: _containers.RepeatedCompositeFieldContainer[_rubric_criterion_pb2.CreateRubricCriterionRequest]
    def __init__(self, type: _Optional[str] = ..., orderIndex: _Optional[int] = ..., points: _Optional[float] = ..., content: _Optional[str] = ..., rubricCriterion: _Optional[_Iterable[_Union[_rubric_criterion_pb2.CreateRubricCriterionRequest, _Mapping]]] = ...) -> None: ...

class UpdateAssignmentQuestionRequest(_message.Message):
    __slots__ = ("id", "type", "orderIndex", "points", "content")
    ID_FIELD_NUMBER: _ClassVar[int]
    TYPE_FIELD_NUMBER: _ClassVar[int]
    ORDERINDEX_FIELD_NUMBER: _ClassVar[int]
    POINTS_FIELD_NUMBER: _ClassVar[int]
    CONTENT_FIELD_NUMBER: _ClassVar[int]
    id: _wrappers_pb2.Int32Value
    type: str
    orderIndex: int
    points: float
    content: str
    def __init__(self, id: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., type: _Optional[str] = ..., orderIndex: _Optional[int] = ..., points: _Optional[float] = ..., content: _Optional[str] = ...) -> None: ...

class GrpcAssignmentQuestionModel(_message.Message):
    __slots__ = ("id", "type", "orderIndex", "points", "content", "rubricCriterion")
    ID_FIELD_NUMBER: _ClassVar[int]
    TYPE_FIELD_NUMBER: _ClassVar[int]
    ORDERINDEX_FIELD_NUMBER: _ClassVar[int]
    POINTS_FIELD_NUMBER: _ClassVar[int]
    CONTENT_FIELD_NUMBER: _ClassVar[int]
    RUBRICCRITERION_FIELD_NUMBER: _ClassVar[int]
    id: int
    type: str
    orderIndex: int
    points: float
    content: str
    rubricCriterion: _containers.RepeatedCompositeFieldContainer[_rubric_criterion_pb2.RubricCriterionResponse]
    def __init__(self, id: _Optional[int] = ..., type: _Optional[str] = ..., orderIndex: _Optional[int] = ..., points: _Optional[float] = ..., content: _Optional[str] = ..., rubricCriterion: _Optional[_Iterable[_Union[_rubric_criterion_pb2.RubricCriterionResponse, _Mapping]]] = ...) -> None: ...
