"""ShapeStacks-style stability: per-block stats, supports, force distribution.

Port from the reference abhishek_symmetry.py. Uses the flag-based placed_info
format: each piece stored as {'x','y','w','h','is_stair','is_mirror','is_inverted'}.
"""


def get_block_stats(p_id, placed_info, inventory):
    """Mass + horizontal centre of mass for a placed piece.

    Stair pieces are modelled as a rectangle (w-1)x h plus a triangle of mass
    0.5 * h; flat pieces are uniform w x h.
    """
    info = placed_info.get(p_id)
    p_def = next((p for p in inventory if p['id'] == p_id), None)
    if not info or not p_def:
        return 0, 0

    w, h = info['w'], info['h']
    x_start = info['x']

    if p_def.get('is_stair'):
        m_rect = (w - 1) * h
        m_tri = 0.5 * h
        total_mass = m_rect + m_tri
        is_mirror = info.get('is_mirror', False)
        if is_mirror:
            cx_tri = x_start + (2.0 / 3.0)
            cx_rect = x_start + 1 + (w - 1) / 2.0
        else:
            cx_rect = x_start + (w - 1) / 2.0
            cx_tri = (x_start + w) - (2.0 / 3.0)
        cx = (m_rect * cx_rect + m_tri * cx_tri) / total_mass
        return total_mass, cx

    mass = w * h
    cx = x_start + (w / 2.0)
    return mass, cx


def get_discrete_supports(pid, placed_info, grid, grid_w, grid_h):
    """List of (x_left, x_right, support_id) entities directly under a block."""
    info = placed_info[pid]
    supports = []
    seen = set()

    for dx in range(info['w']):
        tx, ty = info['x'] + dx, info['y'] - 1

        if ty < 0:
            if 'ground' not in seen:
                supports.append((info['x'], info['x'] + info['w'], 'ground'))
                seen.add('ground')
            continue

        if 0 <= ty < grid_h and 0 <= tx < grid_w:
            val = grid[ty][tx]
            if val == 0:
                continue
            if val == 1:
                key = f"tower_{tx}_{ty}"
                if key not in seen:
                    supports.append((tx, tx + 1, 'tower'))
                    seen.add(key)
            elif val >= 10 and val != pid:
                if val not in seen:
                    s_info = placed_info[val]
                    supports.append((s_info['x'], s_info['x'] + s_info['w'], val))
                    seen.add(val)
    return supports


def is_entire_stack_stable(placed_info, grid, grid_w, grid_h, inventory):
    """Force-distribution check across the whole tower of placed pieces."""
    pieces = sorted(placed_info.keys(),
                    key=lambda p: placed_info[p]['y'], reverse=True)
    if not pieces:
        return True

    external_loads = {pid: [] for pid in pieces}

    for pid in pieces:
        m_self, cx_self = get_block_stats(pid, placed_info, inventory)

        total_weight = m_self
        total_moment = m_self * cx_self
        for force, pos in external_loads[pid]:
            total_weight += force
            total_moment += force * pos

        res_com_x = total_moment / total_weight if total_weight > 0 else cx_self

        supports = get_discrete_supports(pid, placed_info, grid, grid_w, grid_h)
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
