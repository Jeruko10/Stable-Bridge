"""Terminal + Tkinter rendering of solutions.

Matches the layout of the reference abhishek_symmetry.py: full-block banner,
box-drawing grid, piece table with stair orientation labels, integer-tuple
path printing, and a Tkinter GUI with X/Y axis labels.
"""

import tkinter as tk
from tkinter import ttk


def _stair_label(info):
    """Map (orient, is_mirror, is_inverted) to a short legacy label."""
    orient = "VERT" if info['h'] > info['w'] else "HORIZ"
    is_mirror = info.get('is_mirror', False)
    is_inverted = info.get('is_inverted', False)
    if is_inverted:
        return "H.M.Inv" if is_mirror else "H.N.Inv"
    if is_mirror:
        return "V.Mirr" if orient == "VERT" else "H.Mirr"
    return "V.Norm" if orient == "VERT" else "H.Norm"


def print_solution_terminal(solver):
    test = solver.all_tests[solver.current_test_idx]

    print(f"\n{'█'*90}")
    print(f"🎉 SOLUTION #{solver.solution_count} - Test {solver.current_test_idx+1}: {test['name']}")
    print(f"{'█'*90}\n")

    print("┌─ GRID LAYOUT (Y-axis, IDs shown) ─────────────────────────────────────────────┐")
    print("│  X:  0    1    2    3    4    5                                               │")
    for y in range(solver.GRID_H - 1, -1, -1):
        row = f"│  Y{y}: "
        for x in range(solver.GRID_W):
            val = solver.grid[y][x]
            if (x, y) == solver.knight_pos:
                row += "[K ] "
            elif (x, y) == solver.princess_pos:
                row += "[P ] "
            else:
                row += f"[{val:2}] "
        row += " │"
        print(row)
    print("└──────────────────────────────────────────────────────────────────────────────────┘")

    print("\n🗺️  POSITIONS & PATHFINDING:")
    print(f"    Knight:    {solver.knight_pos} (start)")
    print(f"    Princess:  {solver.princess_pos} (goal)")
    if solver.final_path:
        path_str = " → ".join(str(p) for p in solver.final_path)
        print(f"    Path ({len(solver.final_path)} steps): {path_str}")

    print(f"\n📦 PIECE PLACEMENTS ({len(solver.placed_info)} pieces):")
    print("┌──────────────────────────────────────────────────────────────────────────────────┐")
    print("│ ID │ Type  │ Size  │ Orient │  Position │    Stair Type    │ Occupies Cells     │")
    print("├──────────────────────────────────────────────────────────────────────────────────┤")

    for p_id in sorted(solver.placed_info.keys()):
        info = solver.placed_info[p_id]
        p_def = next((p for p in solver.inventory if p['id'] == p_id), None)
        piece_type = "STAIR" if p_def.get('is_stair') else "FLAT "
        size_str = f"{info['w']}×{info['h']}"
        orient = "VERT" if info['h'] > info['w'] else "HORIZ"
        pos_str = f"({info['x']}, {info['y']})"
        stair_str = _stair_label(info) if p_def.get('is_stair') else " - "

        cells = [f"({info['x']+j},{info['y']+i})"
                 for i in range(info['h']) for j in range(info['w'])]
        cells_disp = ", ".join(cells[:2])
        if len(cells) > 2:
            cells_disp += f", +{len(cells)-2}"

        print(f"│ {p_id:2} │ {piece_type} │ {size_str:5} │ {orient:6} │ {pos_str:9} │ {stair_str:16} │ {cells_disp:18} │")

    print("└──────────────────────────────────────────────────────────────────────────────────┘")
    print()

    if solver.use_shapestacks:
        print("✓ ShapeStacks Gravity Validation: PASSED")
        print()


def show_solution_gui(solver):
    root = tk.Tk()
    root.title(f"Camelot Solution - Test {solver.current_test_idx+1}: "
               f"{solver.all_tests[solver.current_test_idx]['name']}")
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

    for y in range(solver.GRID_H):
        for x in range(solver.GRID_W):
            x1 = margin + x * cell_size
            y1 = margin + (solver.GRID_H - 1 - y) * cell_size
            x2 = x1 + cell_size
            y2 = y1 + cell_size
            val = solver.grid[y][x]
            if (x, y) == solver.knight_pos:
                color, text = '#FFD700', 'K'
            elif (x, y) == solver.princess_pos:
                color, text = '#FF69B4', 'P'
            elif val == 1:
                color, text = '#808080', '1'
            elif val >= 10:
                color, text = '#87CEEB', str(val)
            else:
                color, text = '#F0F0F0', '·'
            canvas.create_rectangle(x1, y1, x2, y2, fill=color, outline='black', width=2)
            canvas.create_text(x1 + cell_size//2, y1 + cell_size//2,
                               text=text, font=('Arial', 14, 'bold'))

    if solver.final_path:
        for i in range(len(solver.final_path) - 1):
            x1, y1 = solver.final_path[i]
            x2, y2 = solver.final_path[i+1]
            cx1 = margin + x1 * cell_size + cell_size // 2
            cy1 = margin + (solver.GRID_H - 1 - y1) * cell_size + cell_size // 2
            cx2 = margin + x2 * cell_size + cell_size // 2
            cy2 = margin + (solver.GRID_H - 1 - y2) * cell_size + cell_size // 2
            canvas.create_line(cx1, cy1, cx2, cy2, fill='red', width=3, arrow=tk.LAST)

    for i in range(solver.GRID_W + 1):
        gx = margin + i * cell_size
        canvas.create_line(gx, margin, gx, margin + solver.GRID_H * cell_size, fill='black', width=1)
    for i in range(solver.GRID_H + 1):
        gy = margin + i * cell_size
        canvas.create_line(margin, gy, margin + solver.GRID_W * cell_size, gy, fill='black', width=1)
    for x in range(solver.GRID_W):
        canvas.create_text(margin + x * cell_size + cell_size // 2, 20, text=str(x), font=('Arial', 10))
    for y in range(solver.GRID_H):
        canvas.create_text(20, margin + (solver.GRID_H - 1 - y) * cell_size + cell_size // 2,
                           text=str(y), font=('Arial', 10))


def _draw_info_gui(solver, parent):
    text = tk.Text(parent, wrap=tk.WORD, font=('Courier', 9))
    text.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

    info = f"Solution #{solver.solution_count}\n"
    info += f"Test: {solver.all_tests[solver.current_test_idx]['name']}\n\n"
    info += f"Knight: {solver.knight_pos}\n"
    info += f"Princess: {solver.princess_pos}\n\n"
    if solver.final_path:
        info += f"Path Length: {len(solver.final_path)} steps\n"
        info += "Path: " + " → ".join(str(p) for p in solver.final_path) + "\n\n"
    info += f"{'='*70}\nPIECE INFORMATION\n{'='*70}\n\n"
    for p_id in sorted(solver.placed_info.keys()):
        pinfo = solver.placed_info[p_id]
        p_def = next((p for p in solver.inventory if p['id'] == p_id), None)
        piece_type = "STAIR" if p_def.get('is_stair') else "FLAT"
        orient = "VERTICAL" if pinfo['h'] > pinfo['w'] else "HORIZONTAL"
        info += f"ID {p_id}: {piece_type} {pinfo['w']}×{pinfo['h']} ({orient})\n"
        info += f"  Position: ({pinfo['x']}, {pinfo['y']})\n"
        cells = [f"({pinfo['x']+j},{pinfo['y']+i})"
                 for i in range(pinfo['h']) for j in range(pinfo['w'])]
        info += f"  Cells: {', '.join(cells)}\n\n"

    text.insert("1.0", info)
    text.config(state=tk.DISABLED)
