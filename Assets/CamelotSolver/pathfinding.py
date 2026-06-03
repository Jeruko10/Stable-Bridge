"""Cell-based pathfinding over the segment graph.

Two path functions are provided:
  find_path               – exact BFS used for solution verification.
  can_reach_optimistically – fast BFS used as a pruning oracle: treats every
                             currently-empty cell as potentially walkable so it
                             never produces a false negative (never prunes a
                             solvable state), but cuts branches where towers or
                             existing piece placements make the goal provably
                             unreachable.
"""

from collections import deque


# ---------------------------------------------------------------------------
# Exact BFS (solution verification)
# ---------------------------------------------------------------------------

def find_path(grid, segment_map, knight_pos, princess_pos, grid_w, grid_h):
    """BFS from knight to princess.  Returns the path list or None."""
    queue = deque([knight_pos])
    visited = {knight_pos}
    parent = {knight_pos: None}

    while queue:
        cx, cy = queue.popleft()
        if (cx, cy) == princess_pos:
            return _reconstruct(parent, princess_pos)

        for nx, ny in _moves_from(cx, cy, grid, segment_map, grid_w, grid_h):
            if 0 <= nx < grid_w and 0 <= ny < grid_h and (nx, ny) not in visited:
                visited.add((nx, ny))
                parent[(nx, ny)] = (cx, cy)
                queue.append((nx, ny))

    return None


def _reconstruct(parent, goal):
    path = []
    node = goal
    while node is not None:
        path.append(node)
        node = parent[node]
    path.reverse()
    return path


def _moves_from(cx, cy, grid, segment_map, grid_w, grid_h):
    """Exact movement rules from (cx, cy)."""
    here = segment_map.get((cx, cy))
    if here is not None:
        return here.moves_when_inside(cx, cy)

    if grid[cy][cx] != 0:      # tower – dead end
        return []

    if cy == 0:
        return [(cx + 1, cy)]  # ground level: walk right

    below = segment_map.get((cx, cy - 1))
    if below is not None:
        return below.moves_when_above(cx, cy)

    if grid[cy - 1][cx] == 1:  # tower top – walk right
        return [(cx + 1, cy)]

    return []                   # unsupported – no move


# ---------------------------------------------------------------------------
# Optimistic BFS (pruning oracle – P3)
# ---------------------------------------------------------------------------

def can_reach_optimistically(grid, segment_map, knight_pos, princess_pos,
                              grid_w, grid_h):
    """Return False only when the path is provably impossible.

    Unsupported empty cells are treated as if a future piece could fill them
    with any movement (right, up-right, down-right).  This guarantees the
    function never cuts a solvable branch; it only fires when towers or existing
    pieces create an impassable barrier or the princess is to the left.
    """
    kx = knight_pos[0]
    px = princess_pos[0]

    if px < kx:          # can never move left
        return False

    queue = deque([knight_pos])
    visited = {knight_pos}

    while queue:
        cx, cy = queue.popleft()
        if (cx, cy) == princess_pos:
            return True

        for nx, ny in _moves_from_optimistic(cx, cy, grid, segment_map,
                                              grid_w, grid_h):
            if 0 <= nx < grid_w and 0 <= ny < grid_h and (nx, ny) not in visited:
                visited.add((nx, ny))
                queue.append((nx, ny))

    return False


def _moves_from_optimistic(cx, cy, grid, segment_map, grid_w, grid_h):
    here = segment_map.get((cx, cy))
    if here is not None:
        return here.moves_when_inside(cx, cy)

    if grid[cy][cx] != 0:      # tower – dead end
        return []

    # Empty cell: start with whatever the current configuration actually
    # allows, then add the optimistic moves a future piece could provide.
    moves = set()

    if cy == 0:
        moves.add((cx + 1, cy))        # ground: walk right (actual)
    else:
        below = segment_map.get((cx, cy - 1))
        if below is not None:
            moves.update(below.moves_when_above(cx, cy))   # actual
        elif grid[cy - 1][cx] == 1:    # tower below: walk right (actual)
            moves.add((cx + 1, cy))
        else:
            # No support yet: future piece could be flat or slope-down
            moves.add((cx + 1, cy))
            moves.add((cx + 1, cy - 1))

    # From ANY empty cell a future slope piece could allow climbing.
    moves.add((cx + 1, cy + 1))

    return list(moves)


# ---------------------------------------------------------------------------
# Walkability helper (used by solver.load_test_case)
# ---------------------------------------------------------------------------

def is_cell_walkable(grid, x, y, grid_w, grid_h):
    if y == 0:
        return True
    if y < 0 or y >= grid_h or x < 0 or x >= grid_w:
        return False
    if grid[y][x] == 1:
        return False
    return grid[y - 1][x] != 0
