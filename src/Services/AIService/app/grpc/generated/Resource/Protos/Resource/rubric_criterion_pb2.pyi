from google.protobuf import empty_pb2 as _empty_pb2
from google.protobuf import wrappers_pb2 as _wrappers_pb2
from google.api import annotations_pb2 as _annotations_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class RubricCriterion(_message.Message):
    __slots__ = ("id", "assignmentQuestionId", "criterionName", "description", "maxPoints")
    ID_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTQUESTIONID_FIELD_NUMBER: _ClassVar[int]
    CRITERIONNAME_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    MAXPOINTS_FIELD_NUMBER: _ClassVar[int]
    id: int
    assignmentQuestionId: int
    criterionName: str
    description: _wrappers_pb2.StringValue
    maxPoints: float
    def __init__(self, id: _Optional[int] = ..., assignmentQuestionId: _Optional[int] = ..., criterionName: _Optional[str] = ..., description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., maxPoints: _Optional[float] = ...) -> None: ...

class CreateRubricCriterionRequest(_message.Message):
    __slots__ = ("assignmentQuestionId", "criterionName", "description", "maxPoints")
    ASSIGNMENTQUESTIONID_FIELD_NUMBER: _ClassVar[int]
    CRITERIONNAME_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    MAXPOINTS_FIELD_NUMBER: _ClassVar[int]
    assignmentQuestionId: int
    criterionName: str
    description: _wrappers_pb2.StringValue
    maxPoints: float
    def __init__(self, assignmentQuestionId: _Optional[int] = ..., criterionName: _Optional[str] = ..., description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., maxPoints: _Optional[float] = ...) -> None: ...

class UpdateRubricCriterionRequest(_message.Message):
    __slots__ = ("id", "criterionName", "description", "maxPoints")
    ID_FIELD_NUMBER: _ClassVar[int]
    CRITERIONNAME_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    MAXPOINTS_FIELD_NUMBER: _ClassVar[int]
    id: int
    criterionName: str
    description: _wrappers_pb2.StringValue
    maxPoints: float
    def __init__(self, id: _Optional[int] = ..., criterionName: _Optional[str] = ..., description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., maxPoints: _Optional[float] = ...) -> None: ...

class GetRubricCriterionRequest(_message.Message):
    __slots__ = ("id",)
    ID_FIELD_NUMBER: _ClassVar[int]
    id: int
    def __init__(self, id: _Optional[int] = ...) -> None: ...

class DeleteRubricCriterionRequest(_message.Message):
    __slots__ = ("id",)
    ID_FIELD_NUMBER: _ClassVar[int]
    id: int
    def __init__(self, id: _Optional[int] = ...) -> None: ...

class RubricCriterionList(_message.Message):
    __slots__ = ("rubricCriterions",)
    RUBRICCRITERIONS_FIELD_NUMBER: _ClassVar[int]
    rubricCriterions: _containers.RepeatedCompositeFieldContainer[RubricCriterionResponse]
    def __init__(self, rubricCriterions: _Optional[_Iterable[_Union[RubricCriterionResponse, _Mapping]]] = ...) -> None: ...

class PagedRubricCriterionList(_message.Message):
    __slots__ = ("items", "totalCount", "pageNumber", "pageSize", "totalPages")
    ITEMS_FIELD_NUMBER: _ClassVar[int]
    TOTALCOUNT_FIELD_NUMBER: _ClassVar[int]
    PAGENUMBER_FIELD_NUMBER: _ClassVar[int]
    PAGESIZE_FIELD_NUMBER: _ClassVar[int]
    TOTALPAGES_FIELD_NUMBER: _ClassVar[int]
    items: _containers.RepeatedCompositeFieldContainer[RubricCriterionResponse]
    totalCount: int
    pageNumber: int
    pageSize: int
    totalPages: int
    def __init__(self, items: _Optional[_Iterable[_Union[RubricCriterionResponse, _Mapping]]] = ..., totalCount: _Optional[int] = ..., pageNumber: _Optional[int] = ..., pageSize: _Optional[int] = ..., totalPages: _Optional[int] = ...) -> None: ...

class RubricCriterionResponse(_message.Message):
    __slots__ = ("id", "assignmentQuestionId", "criterionName", "description", "maxPoints")
    ID_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTQUESTIONID_FIELD_NUMBER: _ClassVar[int]
    CRITERIONNAME_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    MAXPOINTS_FIELD_NUMBER: _ClassVar[int]
    id: int
    assignmentQuestionId: int
    criterionName: str
    description: _wrappers_pb2.StringValue
    maxPoints: float
    def __init__(self, id: _Optional[int] = ..., assignmentQuestionId: _Optional[int] = ..., criterionName: _Optional[str] = ..., description: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., maxPoints: _Optional[float] = ...) -> None: ...

class QueryRubricCriterionsRequest(_message.Message):
    __slots__ = ("search", "pageNumber", "pageSize", "orderBy", "assignmentQuestionId")
    SEARCH_FIELD_NUMBER: _ClassVar[int]
    PAGENUMBER_FIELD_NUMBER: _ClassVar[int]
    PAGESIZE_FIELD_NUMBER: _ClassVar[int]
    ORDERBY_FIELD_NUMBER: _ClassVar[int]
    ASSIGNMENTQUESTIONID_FIELD_NUMBER: _ClassVar[int]
    search: _wrappers_pb2.StringValue
    pageNumber: _wrappers_pb2.Int32Value
    pageSize: _wrappers_pb2.Int32Value
    orderBy: _wrappers_pb2.StringValue
    assignmentQuestionId: _wrappers_pb2.Int32Value
    def __init__(self, search: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., pageNumber: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., pageSize: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ..., orderBy: _Optional[_Union[_wrappers_pb2.StringValue, _Mapping]] = ..., assignmentQuestionId: _Optional[_Union[_wrappers_pb2.Int32Value, _Mapping]] = ...) -> None: ...
