"""SegmentDef + predefined segment types.

Direct port of `BasicSegment` and `SlopeSegment` from the Unity project,
expressed as data instead of MonoBehaviours.
"""

from .geometry import (
    APOTHEM, TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT,
    LocalTransition,
)  # BOTTOM_LEFT kept available for future segment types


class SegmentDef:
    """A tile's walkability rules, indexed by rotation. Flipping inverts X."""

    def __init__(self, name, transitions_by_rotation):
        self.name = name
        self._t = transitions_by_rotation

    def get_transitions(self, rotation=0, flipped=False):
        ts = self._t.get(rotation, self._t[0])
        return [t.flipped() for t in ts] if flipped else list(ts)

    def provides_top_surface(self, rotation=0, flipped=False):
        # True if a transition traverses the top edge (both endpoints at y=+APOTHEM).
        # Used by can_place() — a flat top supports a piece above it.
        for t in self.get_transitions(rotation, flipped):
            if t.from_pt[1] == APOTHEM and t.to_pt[1] == APOTHEM:
                return True
        return False

    def __repr__(self):
        return f"SegmentDef({self.name})"


BASIC_SEG = SegmentDef("Basic", {
    0:   [LocalTransition(TOP_LEFT, TOP_RIGHT)],
    90:  [LocalTransition(TOP_LEFT, TOP_RIGHT)],
    180: [LocalTransition(TOP_LEFT, TOP_RIGHT)],
    270: [LocalTransition(TOP_LEFT, TOP_RIGHT)],
})

SLOPE_SEG = SegmentDef("Slope", {
    0:   [LocalTransition(TOP_LEFT, BOTTOM_RIGHT)],
    90:  [LocalTransition(BOTTOM_LEFT, TOP_RIGHT)],
    180: [LocalTransition(TOP_LEFT, TOP_RIGHT)],
    270: [LocalTransition(TOP_LEFT, TOP_RIGHT)],
})
