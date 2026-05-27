"""Terminal + Tkinter rendering of solutions.

Output layout mirrors the original abhishek_symmetry.py: box-drawing grid,
piece table, full GUI with grid lines and X/Y labels.
"""

import tkinter as tk
from tkinter import ttk

from .segments import SLOPE_SEG
from .stability import block_bbox


# ---------------------------------------------------------------------
# Helpers to bridge the new data model to the old display fields
# ---------------------------------------------------------------------
def _is_stair(info):
    return any(seg is SLOPE_SEG for seg in info['tile_map'].values())


def _wh(info):
    bx0, by0, bx1, by1 = _bbox(info)
    return bx1 - bx0, by1 - by0


def _bbox(info):
    tiles = info['tile_map']
    xs = [x for x, _ in tiles]
    ys = [y for _, y in tiles]
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def _stair_label(info):
    """Map (rotation, flipped) of a stair block to the legacy short label."""
    if not _is_stair(info):
        return " - "
    rot = info['rotation']
    fl  = info['flipped']
    # Horizontal orientation: SLOPE transitions go slanted (rot 0) or flat-top (rot 180).
    # Vertical orientation: rot 90/270.
    if rot == 0:
        return "H.Mirr" if fl else "H.Norm"
    if rot == 180:
        return "H.M.Inv" if fl else "H.N.Inv"
    if rot == 90:
        return "V.Mirr" if fl else "V.Norm"
    if rot == 270:
        return "V.M.Inv" if fl else "V.N.Inv"
    return "?"


# ---------------------------------------------------------------------
# Terminal output
# ---------------------------------------------------------------------
def print_solution_terminal(solver):
    test = solver.all_tests[solver.current_test_idx]

    print(f"\n{'█'*90}")
    print(f"🎉 SOLUTION #{solver.solution_count} - Test {solver.current_test_idx+1}: {test['name']}")
    print(f"{'█'*90}\n")

    # Grid visualisation
    print("┌─ GRID LAYOUT (Y-axis, IDs shown) ─────────────────────────────────────────────┐")
    header = "│  X:  " + "    ".join(str(x) for x in range(solver.GRID_W))
    header += " " * max(0, 80 - len(header)) + "│"
    print(header)
    for y in range(solver.GRID_H - 1, -1, -1):
        row = f"│  Y{y}: "
        for x in range(solver.GRID_W):
            val = solver.grid[y][x]
            if (x, y) == solver.miner_pos:
                row += "[M ] "
            elif (x, y) == solver.goal_pos:
                row += "[G ] "
            else:
                row += f"[{val:2}] "
        row += " " * max(0, 80 - len(row)) + "│"
        print(row)
    print("└──────────────────────────────────────────────────────────────────────────────────┘")

    # Path info
    print("\n🗺️  POSITIONS & PATHFINDING:")
    print(f"    Miner:  {solver.miner_pos} (start)")
    print(f"    Goal:   {solver.goal_pos} (end)")
    if solver.final_path:
        path_str = " → ".join(f"({p[0]:.1f},{p[1]:.1f})" for p in solver.final_path)
        print(f"    Path ({len(solver.final_path)} waypoints): {path_str}")

    # Pieces table
    print(f"\n📦 PIECE PLACEMENTS ({len(solver.placed_info)} pieces):")
    print("┌──────────────────────────────────────────────────────────────────────────────────┐")
    print("│ ID │ Type  │ Size  │ Orient │  Position │    Stair Type    │ Occupies Cells     │")
    print("├──────────────────────────────────────────────────────────────────────────────────┤")

    for p_id in sorted(solver.placed_info.keys()):
        info = solver.placed_info[p_id]
        is_stair = _is_stair(info)
        w, h = _wh(info)
        bx0, by0, _, _ = _bbox(info)

        piece_type = "STAIR" if is_stair else "FLAT "
        size_str = f"{w}×{h}"
        orient = "VERT" if h > w else "HORIZ"
        pos_str = f"({bx0}, {by0})"
        stair_str = _stair_label(info)

        cells = sorted(info['tile_map'].keys())
        cells_disp = ", ".join(f"({x},{y})" for x, y in cells[:2])
        if len(cells) > 2:
            cells_disp += f", +{len(cells)-2}"

        print(f"│ {p_id:2} │ {piece_type} │ {size_str:5} │ {orient:6} │ {pos_str:9} │ {stair_str:16} │ {cells_disp:18} │")

    print("└──────────────────────────────────────────────────────────────────────────────────┘")
    print()

    if solver.use_shapestacks:
        print("✓ ShapeStacks Gravity Validation: PASSED")
        print()


# ---------------------------------------------------------------------
# Tkinter GUI
# ---------------------------------------------------------------------
def show_solution_gui(solver):
    root = tk.Tk()
    root.title(
        f"Camelot Solution - Test {solver.current_test_idx+1}: "
        f"{solver.all_tests[solver.current_test_idx]['name']}"
    )
    root.geometry("900x700")

    notebook = ttk.Notebook(root)
    notebook.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

    grid_frame = ttk.Frame(notebook)
    notebook.add(grid_frame, text="Grid Visualization")
    _draw_grid_gui(solver, grid_frame)

    info_frame = ttk.Frame(notebook)
    notebook.add(info_frame, text="Path & Pieces")
    _draw_info_gui(solver, info_frame)

    root.mainloop()


def _draw_grid_gui(solver, parent):
    canvas = tk.Canvas(parent, bg='white', width=700, height=700)
    canvas.pack(padx=10, pady=10)
    cell_size = 100
    margin = 40

    # Draw cells
    for y in range(solver.GRID_H):
        for x in range(solver.GRID_W):
            x1 = margin + x * cell_size
            y1 = margin + (solver.GRID_H - 1 - y) * cell_size
            x2 = x1 + cell_size
            y2 = y1 + cell_size

            val = solver.grid[y][x]
            if (x, y) == solver.miner_pos:
                color, text = '#FFD700', 'M'    # Gold (miner)
            elif (x, y) == solver.goal_pos:
                color, text = '#FF69B4', 'G'    # Pink (goal)
            elif val == 1:
                color, text = '#808080', '1'    # Gray tower
            elif val >= 10:
                color, text = '#87CEEB', str(val)  # Sky blue piece
            else:
                color, text = '#F0F0F0', '·'    # Light gray empty

            canvas.create_rectangle(x1, y1, x2, y2, fill=color, outline='black', width=2)
            canvas.create_text(x1 + cell_size//2, y1 + cell_size//2,
                               text=text, font=('Arial', 14, 'bold'))

    # Draw path (waypoints are half-integer; scale into pixel space)
    if solver.final_path:
        for i in range(len(solver.final_path) - 1):
            wx1, wy1 = solver.final_path[i]
            wx2, wy2 = solver.final_path[i+1]
            cx1 = margin + wx1 * cell_size + cell_size // 2
            cy1 = margin + (solver.GRID_H - 1 - wy1) * cell_size + cell_size // 2
            cx2 = margin + wx2 * cell_size + cell_size // 2
            cy2 = margin + (solver.GRID_H - 1 - wy2) * cell_size + cell_size // 2
            canvas.create_line(cx1, cy1, cx2, cy2, fill='red', width=3, arrow=tk.LAST)

    # Grid lines
    for i in range(solver.GRID_W + 1):
        gx = margin + i * cell_size
        canvas.create_line(gx, margin, gx, margin + solver.GRID_H * cell_size, fill='black', width=1)
    for i in range(solver.GRID_H + 1):
        gy = margin + i * cell_size
        canvas.create_line(margin, gy, margin + solver.GRID_W * cell_size, gy, fill='black', width=1)

    # X labels (top)
    for x in range(solver.GRID_W):
        canvas.create_text(margin + x * cell_size + cell_size // 2, 20,
                           text=str(x), font=('Arial', 10))
    # Y labels (left)
    for y in range(solver.GRID_H):
        canvas.create_text(20, margin + (solver.GRID_H - 1 - y) * cell_size + cell_size // 2,
                           text=str(y), font=('Arial', 10))


def _draw_info_gui(solver, parent):
    text_widget = tk.Text(parent, wrap=tk.WORD, font=('Courier', 9))
    text_widget.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

    info = f"Solution #{solver.solution_count}\n"
    info += f"Test: {solver.all_tests[solver.current_test_idx]['name']}\n\n"
    info += f"Miner: {solver.miner_pos}\n"
    info += f"Goal:  {solver.goal_pos}\n\n"

    if solver.final_path:
        info += f"Path Length: {len(solver.final_path)} waypoints\n"
        info += "Path: " + " → ".join(f"({p[0]:.1f},{p[1]:.1f})" for p in solver.final_path)
        info += "\n\n"

    info += f"{'='*70}\n"
    info += "PIECE INFORMATION\n"
    info += f"{'='*70}\n\n"

    for p_id in sorted(solver.placed_info.keys()):
        pinfo = solver.placed_info[p_id]
        is_stair = _is_stair(pinfo)
        w, h = _wh(pinfo)
        bx0, by0, _, _ = _bbox(pinfo)

        piece_type = "STAIR" if is_stair else "FLAT"
        orient = "VERTICAL" if h > w else "HORIZONTAL"

        info += f"ID {p_id}: {piece_type} {w}×{h} ({orient})\n"
        info += f"  Position: ({bx0}, {by0})\n"
        info += f"  Rotation: {pinfo['rotation']}°  Flipped: {'Yes' if pinfo['flipped'] else 'No'}\n"
        info += f"  Stair Type: {_stair_label(pinfo)}\n"

        cells = sorted(pinfo['tile_map'].keys())
        cells_str = ", ".join(f"({x},{y})" for x, y in cells)
        info += f"  Cells: {cells_str}\n"

        seg_breakdown = ", ".join(f"({x},{y}):{seg.name}" for (x, y), seg in sorted(pinfo['tile_map'].items()))
        info += f"  Segments: {seg_breakdown}\n\n"

    text_widget.insert("1.0", info)
    text_widget.config(state=tk.DISABLED)
