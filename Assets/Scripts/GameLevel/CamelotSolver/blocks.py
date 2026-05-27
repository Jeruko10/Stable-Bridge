"""Block composition: build the list of segments for a placed piece.

The mapping from `(w, h, is_stair, is_mirror, is_inverted)` to segments
follows the six stair types defined for the game:

    | Type                    | w vs h | mirror | Slope          | Movement |
    |-------------------------|--------|--------|----------------|----------|
    | HORIZ NORMAL            | w > h  | NO     | right edge     | DOWN     |
    | HORIZ MIRROR            | w > h  | YES    | left edge      | UP       |
    | VERT  NORMAL            | h > w  | NO     | top cell       | UP       |
    | VERT  MIRROR            | h > w  | YES    | top cell       | DOWN     |
    | HORIZ NORMAL INVERTED   | w > h  | NO     | none (flat)    | -        |
    | HORIZ MIRROR INVERTED   | w > h  | YES    | none (flat)    | -        |

For `w == h` the piece is treated as horizontal (mirrors the reference).
"""

from .segments import (
    BASIC, SLOPE_DOWN_R, SLOPE_UP_L, SLOPE_UP_T, SLOPE_DOWN_T, VERT_INTERIOR,
)


def build_block_segments(x, y, w, h, is_stair, is_mirror, is_inverted):
    """Return [((abs_x, abs_y), Segment), ...] for this placement."""
    segments = []

    # Flat block or inverted stair: every cell is BASIC.
    if not is_stair or is_inverted:
        for i in range(h):
            for j in range(w):
                segments.append(((x + j, y + i), BASIC))
        return segments

    if h > w:
        # Vertical stair: top cell is the slope, everything below is interior.
        slope_seg = SLOPE_DOWN_T if is_mirror else SLOPE_UP_T
        for i in range(h):
            for j in range(w):
                seg = slope_seg if i == h - 1 else VERT_INTERIOR
                segments.append(((x + j, y + i), seg))
        return segments

    # Horizontal stair (w > h, or w == h treated as horizontal).
    slope_seg = SLOPE_UP_L if is_mirror else SLOPE_DOWN_R
    slope_col = 0 if is_mirror else w - 1
    for i in range(h):
        for j in range(w):
            seg = slope_seg if j == slope_col else BASIC
            segments.append(((x + j, y + i), seg))
    return segments
