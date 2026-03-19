"""Expose generated Resource protobuf modules under a stable package name."""

from __future__ import annotations

import importlib
import sys
from typing import Tuple

_BASE_PACKAGE = "app.grpc.generated.Resource"
_MODULES: Tuple[str, ...] = (
    "lesson_pb2",
    "lesson_pb2_grpc",
    "section_pb2",
    "section_pb2_grpc",
)

for _name in _MODULES:
    _module = importlib.import_module(f"{_BASE_PACKAGE}.{_name}")
    sys.modules[f"{__name__}.{_name}"] = _module
    globals()[_name] = _module

__all__ = _MODULES


