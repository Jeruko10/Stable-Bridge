"""ShapeStacks-style stability: per-block stats, supports, force distribution."""

from .segments import SLOPE_SEG


def block_bbox(block_id, placed_info):
    tiles = placed_info[block_id]['tile_map']
    xs = [x for x, _ in tiles]
    ys = [y for _, y in tiles]
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def get_block_stats(block_id, placed_info):
    """Mass + horizontal centre of mass.

    BASIC tile  → mass 1.0, COM at tile centre.
    SLOPE tile  → mass 0.5, COM nudged toward the slope's transition midpoint.
    Other types → mass 1.0, COM at tile centre.
    """
    info = placed_info.get(block_id)
    if not info:
        return 0, 0

    total_mass = 0.0
    total_moment = 0.0
    rot = info['rotation']
    fl  = info['flipped']

    for (tx, ty), seg in info['tile_map'].items():
        if seg is SLOPE_SEG:
            ts = seg.get_transitions(rot, fl)
            if ts:
                fp, tp = ts[0].from_pt, ts[0].to_pt
                cx_off = (fp[0] + tp[0]) / 2.0
            else:
                cx_off = 0.0
            mass = 0.5
            cx = tx + 0.5 + cx_off / 2.0
        else:
            mass = 1.0
            cx = tx + 0.5

        total_mass += mass
        total_moment += mass * cx

    if total_mass <= 0:
        return 0, 0
    return total_mass, total_moment / total_mass


def get_discrete_supports(block_id, placed_info, grid, grid_w):
    """Return list of (x_left, x_right, support_id) tuples beneath this block."""
    x0, _, x1, _ = block_bbox(block_id, placed_info)
    tiles = placed_info[block_id]['tile_map']
    supports = []
    seen = set()

    for tx in range(x0, x1):
        cols = [y for (x, y) in tiles if x == tx]
        if not cols:
            continue
        ty = min(cols) - 1

        if ty < 0:
            if 'ground' not in seen:
                supports.append((x0, x1, 'ground'))
                seen.add('ground')
            continue
        if not (0 <= tx < grid_w):
            continue

        val = grid[ty][tx]
        if val == 0:
            continue
        if val == 1:
            key = f"tower_{tx}_{ty}"
            if key not in seen:
                supports.append((tx, tx + 1, 'tower'))
                seen.add(key)
        elif val >= 10 and val != block_id and val not in seen:
            bx0, _, bx1, _ = block_bbox(val, placed_info)
            supports.append((bx0, bx1, val))
            seen.add(val)
    return supports


def is_stack_stable(placed_info, grid, grid_w):
    """Top-down force distribution: each block must keep its resultant
    centre of mass inside the convex hull of its discrete supports."""
    pieces = sorted(
        placed_info.keys(),
        key=lambda p: min(y for _, y in placed_info[p]['tile_map']),
        reverse=True,
    )
    if not pieces:
        return True

    external_loads = {pid: [] for pid in pieces}

    for pid in pieces:
        m_self, cx_self = get_block_stats(pid, placed_info)

        total_weight = m_self
        total_moment = m_self * cx_self
        for force, pos in external_loads[pid]:
            total_weight += force
            total_moment += force * pos

        res_com_x = total_moment / total_weight if total_weight > 0 else cx_self

        supports = get_discrete_supports(pid, placed_info, grid, grid_w)
        if not supports:
            return False

        s_min = min(s[0] for s in supports)
        s_max = max(s[1] for s in supports)
        if not (s_min - 1e-9 <= res_com_x <= s_max + 1e-9):
            return False

        if len(supports) == 1:
            _, _, sid = supports[0]
            if sid not in ('ground', 'tower') and sid in external_loads:
                external_loads[sid].append((total_weight, res_com_x))
        else:
            supports.sort(key=lambda s: s[0])
            s1l, s1r, s1id = supports[0]
            s2l, s2r, s2id = supports[-1]
            cp1 = (s1l + s1r) / 2.0
            cp2 = (s2l + s2r) / 2.0
            span = cp2 - cp1
            if span > 0:
                w2 = total_weight * (max(0, res_com_x - cp1) / span)
                w1 = total_weight - w2
                if s1id in external_loads:
                    external_loads[s1id].append((w1, cp1))
                if s2id in external_loads:
                    external_loads[s2id].append((w2, cp2))
    return True
