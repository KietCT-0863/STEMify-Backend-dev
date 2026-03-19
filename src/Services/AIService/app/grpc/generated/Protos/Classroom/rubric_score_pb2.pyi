from google.protobuf import wrappers_pb2 as _wrappers_pb2
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class GrpcRubricScoreModel(_message.Message):
    __slots__ = ("rubricCriterionId", "criterionName", "description", "maxPoints", "currentPoints")
    RUBRICCRITERIONID_FIELD_NUMBER: _ClassVar[int]
    CRITERIONNAME_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    MAXPOINTS_FIELD_NUMBER: _ClassVar[int]
    CURRENTPOINTS_FIELD_NUMBER: _ClassVar[int]
    rubricCriterionId: int
    criterionName: str
    description: _wrappers_pb2.StringValue
    maxPoints: float
    currentPoints: _wrappers_pb2.DoubleValue
    def __init__(self, rubricCriterionId: _Optional[int] = ..., criterionName: _Optional[str] = ..., description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., maxPoints: _Optional[float] = ..., currentPoints: _Optional[_Union[_wrappers_pb2.DoubleValue, _Mapping]] = ...) -> None: ...

class CreateRubricScoreRequest(_message.Message):
    __slots__ = ("rubricCriterionId", "points")
    RUBRICCRITERIONID_FIELD_NUMBER: _ClassVar[int]
    POINTS_FIELD_NUMBER: _ClassVar[int]
    rubricCriterionId: int
    points: float
    def __init__(self, rubricCriterionId: _Optional[int] = ..., points: _Optional[float] = ...) -> None: ...
