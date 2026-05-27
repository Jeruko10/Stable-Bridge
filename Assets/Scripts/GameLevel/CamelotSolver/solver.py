"""CamelotSolverComplete — orchestrates inventory, placement, search, output.

Segment-aware backtracking solver:
  - `self.grid`        stores integer occupancy (0 empty, 1 tower, >=10 piece id)
  - `self.segment_map` stores the per-cell Segment for every placed piece
  - `self.placed_info` stores the legacy flag-based placement record used by
    the stability checker and the terminal/GUI output

`can_place` and `find_path` consult `segment_map` (per-cell Segment behaviour)
instead of inspecting block-level flags directly.
"""

import json
import math
import os

from .blocks import build_block_segments
from .pathfinding import find_path as _find_path, is_cell_walkable
from .stability import is_entire_stack_stable


_PKG_DIR = os.path.dirname(os.path.abspath(__file__))
_DEFAULT_TESTS = os.path.join(_PKG_DIR, "tests.json")


class CamelotSolverComplete:
    """Cell- and segment-based Camelot solver."""

    def __init__(self, test_file=_DEFAULT_TESTS, use_shapestacks=True,
                 grid_w=6, grid_h=6, enable_gui=True):
        self.use_shapestacks = use_shapestacks
        self.GRID_W = grid_w
        self.GRID_H = grid_h
        self.enable_gui = enable_gui

        self.grid = [[0 for _ in range(self.GRID_W)] for _ in range(self.GRID_H)]
        self.segment_map = {}  # (x, y) -> Segment

        self.used_ids = set()
        self.found_solutions = set()
        self.placed_info = {}  # block_id -> legacy flag record
        self.final_path = []
        self.inventory = []
        self.knight_pos = (0, 0)
        self.princess_pos = (0, 0)
        self.towers_list = []

        try:
            with open(test_file, 'r', encoding='utf-8') as f:
                self.all_tests = json.load(f)
        except FileNotFoundError:
            print(f"Error: {test_file} not found.")
            self.all_tests = []
            return

        self.current_test_idx = 0
        self.solution_count = 0
        self.stable_configs_count = 0

    # -----------------------------------------------------------------
    # Symmetry breaking
    # -----------------------------------------------------------------
    @staticmethod
    def _piece_key(p):
        w, h = p['w'], p['h']
        nw, nh = (w, h) if w <= h else (h, w)
        return (nw, nh, bool(p.get('is_stair')))

    # -----------------------------------------------------------------
    # Placement check (uses Segment.provides_top_surface)
    # -----------------------------------------------------------------
    def can_place(self, x, y, w, h):
        if x + w > self.GRID_W or y + h > self.GRID_H:
            return False

        for i in range(h):
            for j in range(w):
                if self.grid[y + i][x + j] != 0:
                    return False
                if (x + j, y + i) in (self.knight_pos, self.princess_pos):
                    return False

        if y == 0:
            return True

        # Need at least one column whose below-cell provides a top surface.
        for j in range(w):
            below_val = self.grid[y - 1][x + j]
            if below_val == 0:
                continue
            if below_val == 1:  # tower top is supportive
                return True
            below_seg = self.segment_map.get((x + j, y - 1))
            if below_seg is not None and below_seg.provides_top_surface:
                return True
        return False

    # -----------------------------------------------------------------
    # Place / remove
    # -----------------------------------------------------------------
    def _place(self, pid, x, y, w, h, is_stair, is_mirror, is_inverted):
        for r in range(h):
            for c in range(w):
                self.grid[y + r][x + c] = pid
        seg_list = build_block_segments(x, y, w, h, is_stair, is_mirror, is_inverted)
        for cell, seg in seg_list:
            self.segment_map[cell] = seg
        self.placed_info[pid] = {
            'x': x, 'y': y, 'w': w, 'h': h,
            'is_stair': is_stair,
            'is_mirror': is_mirror,
            'is_inverted': is_inverted,
            'segments': seg_list,
        }

    def _remove(self, pid):
        info = self.placed_info.pop(pid, None)
        if info is None:
            return
        x, y, w, h = info['x'], info['y'], info['w'], info['h']
        for r in range(h):
            for c in range(w):
                self.grid[y + r][x + c] = 0
        for cell, _ in info['segments']:
            self.segment_map.pop(cell, None)

    # -----------------------------------------------------------------
    # Pathfinding
    # -----------------------------------------------------------------
    def find_path(self):
        return _find_path(self.grid, self.segment_map,
                          self.knight_pos, self.princess_pos,
                          self.GRID_W, self.GRID_H)

    # -----------------------------------------------------------------
    # Solution fingerprint
    # -----------------------------------------------------------------
    def get_solution_hash(self):
        items = []
        for pid in sorted(self.placed_info.keys()):
            info = self.placed_info[pid]
            items.append(
                f"{pid}:{info['x']},{info['y']},{info['w']},{info['h']},"
                f"{info.get('is_mirror', False)},{info.get('is_inverted', False)}"
            )
        return "|".join(items)

    # -----------------------------------------------------------------
    # Test loading
    # -----------------------------------------------------------------
    def load_test_case(self, index):
        if not (0 <= index < len(self.all_tests)):
            return False
        test = self.all_tests[index]

        print(f"\n{'='*90}")
        print(f">>> LOADING TEST {index+1}: {test['name']}")
        print(f">>> Grid: {self.GRID_W}×{self.GRID_H} | Solver: Complete")
        print(f"{'='*90}\n")

        self.grid = [[0 for _ in range(self.GRID_W)] for _ in range(self.GRID_H)]
        self.segment_map = {}
        self.placed_info = {}
        self.final_path = []
        self.found_solutions = set()
        # Accept both the new (miner/goal) and legacy (knight/princess) keys.
        self.knight_pos = tuple(test.get('knight', test.get('miner')))
        self.princess_pos = tuple(test.get('princess', test.get('goal')))
        self.solution_count = 0
        self.towers_list = test.get('towers', [])

        for tx, ty in self.towers_list:
            if 0 <= tx < self.GRID_W and 0 <= ty < self.GRID_H:
                self.grid[ty][tx] = 1

        kx, ky = self.knight_pos
        if self.grid[ky][kx] != 0:
            print(f"WARNING: Knight starting cell ({kx},{ky}) is not empty. Skipping.")
            return False
        if not is_cell_walkable(self.grid, kx, ky, self.GRID_W, self.GRID_H):
            print(f"WARNING: Knight starting cell ({kx},{ky}) is not walkable. Skipping.")
            return False

        self.inventory = test['inventory']
        self.used_ids = set()
        self.current_test_idx = index
        return True

    # -----------------------------------------------------------------
    # Backtracking solver with symmetry breaking
    # -----------------------------------------------------------------
    def solve(self):
        from .visualization import print_solution_terminal, show_solution_gui

        if len(self.used_ids) == len(self.inventory):
            sol_hash = self.get_solution_hash()
            if sol_hash in self.found_solutions:
                return

            if self.use_shapestacks:
                if not is_entire_stack_stable(self.placed_info, self.grid,
                                              self.GRID_W, self.GRID_H,
                                              self.inventory):
                    return
                self.stable_configs_count += 1

            res = self.find_path()
            if res:
                self.final_path = res
                self.found_solutions.add(sol_hash)
                self.solution_count += 1
                print_solution_terminal(self)
                if self.enable_gui:
                    show_solution_gui(self)
            return

        unused = [p for p in self.inventory if p['id'] not in self.used_ids]
        unused.sort(key=lambda p: (self._piece_key(p), p['id']))

        for i, p in enumerate(unused):
            key = self._piece_key(p)
            if any(self._piece_key(q) == key and q['id'] not in self.used_ids
                   for q in unused[:i]):
                continue

            self.used_ids.add(p['id'])
            is_stair = bool(p.get('is_stair'))

            for w, h in {(p['w'], p['h']), (p['h'], p['w'])}:
                if is_stair:
                    if h > w:
                        stair_configs = [(False, False), (True, False)]
                    else:
                        stair_configs = [(False, False), (True, False),
                                         (False, True), (True, True)]
                else:
                    stair_configs = [(False, False)]

                for y in range(self.GRID_H):
                    for x in range(self.GRID_W):
                        if not self.can_place(x, y, w, h):
                            continue
                        for is_mirror, is_inverted in stair_configs:
                            self._place(p['id'], x, y, w, h,
                                        is_stair, is_mirror, is_inverted)
                            self.solve()
                            self._remove(p['id'])

            self.used_ids.remove(p['id'])

    # -----------------------------------------------------------------
    # Driver
    # -----------------------------------------------------------------
    def run_tests(self, test_indices=None):
        if test_indices is None:
            test_indices = range(len(self.all_tests))
        with open("test_results.txt", "w", encoding="utf-8") as f:
            def dual_print(msg):
                print(msg)
                print(msg, file=f)
            for idx in test_indices:
                if self.load_test_case(idx):
                    dual_print(f"Solving Test {idx+1}...\n")
                    self.solve()
                    dual_print(
                        f"✗ No more solution for Test {idx+1}.\n"
                        f"                         Total solutions: {self.solution_count}\n"
                        f"                         Total Stable Configs: {self.stable_configs_count}\n"
                        f"                         FrustrationScore: {self.stable_configs_count/max(1,self.solution_count)}.\n"
                        f"                        Static Entanglement: {self.get_static_entanglement():.2f} bits.\n"
                    )

    def get_static_entanglement(self):
        """Estimate placement freedom per piece on the (currently-empty) board."""
        total_bits = 0
        for p in self.inventory:
            bi = 0
            is_stair = bool(p.get('is_stair'))
            for w, h in {(p['w'], p['h']), (p['h'], p['w'])}:
                configs = ([(False, False), (True, False),
                            (False, True), (True, True)]
                           if is_stair else [(False, False)])
                for _m, _inv in configs:
                    for y in range(self.GRID_H):
                        for x in range(self.GRID_W):
                            if self.can_place(x, y, w, h):
                                bi += 1
            total_bits += math.log2(bi + 1)
        return total_bits
