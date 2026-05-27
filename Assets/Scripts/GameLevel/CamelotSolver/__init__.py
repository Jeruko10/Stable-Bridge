"""Segment-and-block Camelot solver — exports the public API."""

from .segments import (
    Segment, BASIC, SLOPE_DOWN_R, SLOPE_UP_L, SLOPE_UP_T, SLOPE_DOWN_T,
    VERT_INTERIOR,
)
from .blocks import build_block_segments
from .solver import CamelotSolverComplete

__all__ = [
    "Segment", "BASIC", "SLOPE_DOWN_R", "SLOPE_UP_L", "SLOPE_UP_T",
    "SLOPE_DOWN_T", "VERT_INTERIOR",
    "build_block_segments",
    "CamelotSolverComplete",
]
