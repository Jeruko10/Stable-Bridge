"""Graph construction + path search.

Direct port of `PathSolver.GridToGraph` / `PathSolver.GetPath`.
"""

from .geometry import BOTTOM_RIGHT


def build_graph(tile_segment_map, grid, grid_h):
    """Walk every tile with a segment, skip those covered above, and emit
    waypoint-to-waypoint edges based on each segment's transitions."""
    adjacency = {}
    for (x, y), (_block_id, seg, rotation, flipped) in tile_segment_map.items():
        if y + 1 < grid_h and grid[y + 1][x] != 0:
            continue
        for t in seg.get_transitions(rotation, flipped):
            wp_from = (x + t.from_pt[0], y + t.from_pt[1])
            wp_to   = (x + t.to_pt[0],   y + t.to_pt[1])
            adjacency.setdefault(wp_from, set()).add(wp_to)
            adjacency.setdefault(wp_to,   set()).add(wp_from)
    return adjacency


def find_path(graph, miner_pos, goal_pos):
    """Greedy best-first search by Manhattan distance to the goal.

    Mirrors PathSolver.GetPath: start at miner + BottomRight, terminate at
    goal + BottomRight, return the full path on success or None otherwise.
    """
    mx, my = miner_pos
    gx, gy = goal_pos
    start = (mx + BOTTOM_RIGHT[0], my + BOTTOM_RIGHT[1])
    end   = (gx + BOTTOM_RIGHT[0], gy + BOTTOM_RIGHT[1])

    if start not in graph:
        return None

    visited = {start}
    current = start
    path = [current]

    while True:
        options = [n for n in graph.get(current, ()) if n not in visited]
        if not options:
            break
        best = min(options, key=lambda v: abs(v[0] - gx) + abs(v[1] - gy))
        current = best
        visited.add(current)
        path.append(current)
        if current == end:
            break

    return path if path and path[-1] == end else None
