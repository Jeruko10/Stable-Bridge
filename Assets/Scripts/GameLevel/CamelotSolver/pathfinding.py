"""Cell-based BFS over the segment graph.

The graph is implicit: each cell is either empty, a tower (grid value 1) or
contains a Segment from a placed block. Movement rules are delegated to:
  - the Segment at the knight's current cell (if any), OR
  - the Segment in the cell directly below (when the knight stands on an
    empty cell on top of a piece), OR
  - hardcoded ground / tower-top movement otherwise.

This replaces the old monolithic if/elif tree with per-segment behaviour.
"""

from collections import deque


def find_path(grid, segment_map, knight_pos, princess_pos, grid_w, grid_h):
    """BFS from knight to princess across the segment graph.

    Returns the list of (x, y) cells visited, or None if no path exists.
    """
    queue = deque([(knight_pos, [knight_pos])])
    visited = {knight_pos}

    while queue:
        (cx, cy), path = queue.popleft()
        if (cx, cy) == princess_pos:
            return path

        for (nx, ny) in _moves_from(cx, cy, grid, segment_map, grid_w, grid_h):
            if not (0 <= nx < grid_w and 0 <= ny < grid_h):
                continue
            if (nx, ny) in visited:
                continue
            visited.add((nx, ny))
            queue.append(((nx, ny), path + [(nx, ny)]))

    return None


def _moves_from(cx, cy, grid, segment_map, grid_w, grid_h):
    """Outgoing edges from (cx, cy) according to the segment graph."""
    here = segment_map.get((cx, cy))
    if here is not None:
        # The knight is *inside* a segment — only slope cells produce moves.
        return here.moves_when_inside(cx, cy)

    # Any non-empty cell without a Segment (a tower) is a dead end — the
    # knight may be queued there by an earlier move but cannot proceed.
    if grid[cy][cx] != 0:
        return []

    # Empty cell: behaviour depends on what's directly below.
    if cy == 0:
        return [(cx + 1, cy)]  # walking along the ground

    below = segment_map.get((cx, cy - 1))
    if below is not None:
        return below.moves_when_above(cx, cy)

    if grid[cy - 1][cx] == 1:  # towers are walkable on top
        return [(cx + 1, cy)]

    return []  # nothing under our feet, no move


def is_cell_walkable(grid, x, y, grid_w, grid_h):
    """Knight may stand at (x, y) only if supported (ground, tower, block)."""
    if y == 0:
        return True
    if y < 0 or y >= grid_h or x < 0 or x >= grid_w:
        return False
    if grid[y][x] == 1:  # tower itself isn't walkable
        return False
    return grid[y - 1][x] != 0
