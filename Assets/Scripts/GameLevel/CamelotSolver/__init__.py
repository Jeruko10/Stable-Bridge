"""Segment-based Camelot solver, organised after the Unity project's architecture."""

from .geometry import (
    APOTHEM, TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT,
    LocalTransition,
)
from .segments import SegmentDef, BASIC_SEG, SLOPE_SEG
from .blocks import BlockDef, make_flat_block, make_stair_block
from .solver import CamelotSolverComplete

__all__ = [
    "APOTHEM", "TOP_LEFT", "TOP_RIGHT", "BOTTOM_LEFT", "BOTTOM_RIGHT",
    "LocalTransition",
    "SegmentDef", "BASIC_SEG", "SLOPE_SEG",
    "BlockDef", "make_flat_block", "make_stair_block",
    "CamelotSolverComplete",
]
