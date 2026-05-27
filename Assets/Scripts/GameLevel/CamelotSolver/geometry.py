"""Local tile-space geometry: corner constants + LocalTransition.

Mirrors `BlockSegment.cs` constants and `LocalTransition.cs`.
"""

APOTHEM      = 0.5
TOP_LEFT     = (-APOTHEM,  APOTHEM)
TOP_RIGHT    = ( APOTHEM,  APOTHEM)
BOTTOM_LEFT  = (-APOTHEM, -APOTHEM)
BOTTOM_RIGHT = ( APOTHEM, -APOTHEM)


class LocalTransition:
    """A walkability edge in a tile's local space, from one corner to another."""
    __slots__ = ('from_pt', 'to_pt')

    def __init__(self, from_pt, to_pt):
        self.from_pt = from_pt
        self.to_pt = to_pt

    def flipped(self):
        return LocalTransition(
            (-self.from_pt[0], self.from_pt[1]),
            (-self.to_pt[0],   self.to_pt[1]),
        )
