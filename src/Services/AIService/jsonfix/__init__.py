"""
Lightweight JSON repair utility.

This module provides a minimal `fix` function that attempts to repair
common truncation/formatting issues in JSON text produced by LLMs.

It is NOT a full replacement for the external `jsonfix` library, but it
matches the import path `from jsonfix import fix` and keeps the codebase
dependency-free while improving robustness.
"""

from __future__ import annotations

from typing import Final


def fix(text: str) -> str:

    if not text:
        return text

    start = text.find("{")
    if start == -1:
        # No object start found, return as-is
        return text

    depth: int = 0
    end_index: int | None = None

    for i, ch in enumerate(text[start:], start=start):
        if ch == "{":
            depth += 1
        elif ch == "}":
            if depth > 0:
                depth -= 1
            # When depth returns to zero, we've closed the outermost object
            if depth == 0:
                end_index = i + 1
                break

    if end_index is not None:
        return text[start:end_index]

    # Fallback: return original if we couldn't find a balanced object
    return text


__all__: Final = ["fix"]


