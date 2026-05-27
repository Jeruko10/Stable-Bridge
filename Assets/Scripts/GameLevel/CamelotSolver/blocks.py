"""BlockDef + factories.

A block is a collection of (local_tile_offset, SegmentDef) pairs — mirroring
how Unity's Block prefabs hold BlockSegment children at relative positions.
"""

from .segments import BASIC_SEG, SLOPE_SEG


def _rotate_offset(dx, dy, rotation):
    if rotation == 0:   return (dx, dy)
    if rotation == 90:  return (-dy, dx)
    if rotation == 180: return (-dx, -dy)
    if rotation == 270: return (dy, -dx)
    raise ValueError(rotation)


class BlockDef:
    """A block as a collection of (local_offset, SegmentDef) pairs."""

    def __init__(self, segments):
        self.segments = tuple((tuple(off), seg) for off, seg in segments)
        self._signature = tuple(sorted(
            ((off, id(seg)) for off, seg in self.segments)
        ))

    def resolve(self, rotation=0, flipped=False):
        """
        Return [(tile_offset, SegmentDef, rotation, flipped), ...] with offsets
        normalised so min(x)==0 and min(y)==0.
        """
        entries = []
        for (dx, dy), seg in self.segments:
            rx, ry = _rotate_offset(dx, dy, rotation)
            if flipped:
                rx = -rx
            entries.append([rx, ry, seg])

        if not entries:
            return []

        min_x = min(e[0] for e in entries)
        min_y = min(e[1] for e in entries)
        return [
            ((e[0] - min_x, e[1] - min_y), e[2], rotation, flipped)
            for e in entries
        ]

    def bounding_box(self, rotation=0, flipped=False):
        resolved = self.resolve(rotation, flipped)
        if not resolved:
            return (0, 0)
        max_x = max(off[0] for off, *_ in resolved)
        max_y = max(off[1] for off, *_ in resolved)
        return (max_x + 1, max_y + 1)

    def signature(self):
        return self._signature


def make_flat_block(w, h):
    return BlockDef([((x, y), BASIC_SEG) for x in range(w) for y in range(h)])


def make_stair_block(w, h):
    """Rectangular block whose top-right corner is SLOPE; rest are BASIC."""
    segs = []
    for x in range(w):
        for y in range(h):
            segs.append(((x, y), SLOPE_SEG if (x == w - 1 and y == h - 1) else BASIC_SEG))
    return BlockDef(segs)
