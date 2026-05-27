"""Segment-level walkability for the cell graph.

Each cell of a placed block carries one Segment. A segment knows:
  - moves_when_above(cx, cy): targets the knight can reach when standing in
    the empty cell directly above this one (the knight is at (cx, cy), this
    segment is at (cx, cy - 1)).
  - moves_when_inside(cx, cy): targets the knight can reach when standing
    inside this segment's cell (only meaningful for slope cells the knight
    actually enters).
  - provides_top_surface: True if another piece may rest on top of this cell.

This file defines the six segment singletons used by the block builder.
"""


class Segment:
    __slots__ = ("name", "_above", "_inside", "provides_top_surface")

    def __init__(self, name, above_fn, inside_fn, supports):
        self.name = name
        self._above = above_fn
        self._inside = inside_fn
        self.provides_top_surface = supports

    def moves_when_above(self, cx, cy):
        return self._above(cx, cy)

    def moves_when_inside(self, cx, cy):
        return self._inside(cx, cy)

    def __repr__(self):
        return f"Segment({self.name})"


# Flat walkable top. Knight above walks straight right; supports stacking.
BASIC = Segment(
    "BASIC",
    above_fn=lambda cx, cy: [(cx + 1, cy)],
    inside_fn=lambda cx, cy: [],
    supports=True,
)

# Horizontal NORMAL stair: slope on the RIGHT cell.
# Knight standing above slides diagonally DOWN to the right.
SLOPE_DOWN_R = Segment(
    "SLOPE_DOWN_R",
    above_fn=lambda cx, cy: [(cx + 1, cy - 1)],
    inside_fn=lambda cx, cy: [],
    supports=False,
)

# Horizontal MIRROR stair: slope on the LEFT cell.
# Knight cannot walk above the slope; entering the cell exits UP-right.
SLOPE_UP_L = Segment(
    "SLOPE_UP_L",
    above_fn=lambda cx, cy: [],
    inside_fn=lambda cx, cy: [(cx + 1, cy + 1)],
    supports=False,
)

# Vertical NORMAL stair: slope on the TOP cell.
# Knight above cannot walk; entering the cell exits UP-right.
SLOPE_UP_T = Segment(
    "SLOPE_UP_T",
    above_fn=lambda cx, cy: [],
    inside_fn=lambda cx, cy: [(cx + 1, cy + 1)],
    supports=False,
)

# Vertical MIRROR stair: slope on the TOP cell, going DOWN.
# Knight above slides diagonally DOWN to the right.
SLOPE_DOWN_T = Segment(
    "SLOPE_DOWN_T",
    above_fn=lambda cx, cy: [(cx + 1, cy - 1)],
    inside_fn=lambda cx, cy: [],
    supports=False,
)

# Interior cell of a vertical stair (every cell below the top).
# Acts as a wall: not walkable from above, not entered from the side,
# never supports a piece on top.
VERT_INTERIOR = Segment(
    "VERT_INTERIOR",
    above_fn=lambda cx, cy: [],
    inside_fn=lambda cx, cy: [],
    supports=False,
)
