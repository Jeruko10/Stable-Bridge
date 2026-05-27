"""CamelotSolverComplete — orchestrates inventory, placement, search, and output."""

import json
import math
import os

from .blocks import make_flat_block, make_stair_block
from .pathfinding import build_graph, find_path
from .stability import is_stack_stable

_PKG_DIR = os.path.dirname(os.path.abspath(__file__))
_DEFAULT_TESTS = os.path.join(_PKG_DIR, "tests.json")


class CamelotSolverComplete:
    """Segment-based Camelot solver."""

    _ALL_ROTATIONS = (0, 90, 180, 270)
    _ALL_FLIPS = (False, True)

    def __init__(self, test_file=_DEFAULT_TESTS, use_shapestacks=True,
                 grid_w=6, grid_h=6, enable_gui=True):
        self.use_shapestacks = use_shapestacks
        self.GRID_W = grid_w
        self.GRID_H = grid_h
        self.enable_gui = enable_gui

        self.grid = [[0 for _ in range(self.GRID_W)] for _ in range(self.GRID_H)]
        # (x, y) -> (block_id, SegmentDef, rotation, flipped)
        self.tile_segment_map = {}

        self.used_ids = set()
        self.found_solutions = set()
        self.placed_info = {}
        self.final_path = []
        self.inventory = []
        self.miner_pos = (0, 0)
        self.goal_pos = (0, 0)
        self.towers_list = []

        try:
            with open(test_file, 'r') as f:
                self.all_tests = json.load(f)
        except FileNotFoundError:
            print(f"Error: {test_file} not found.")
            self.all_tests = []
            return

        self.current_test_idx = 0
        self.solution_count = 0
        self.stable_configs_count = 0

    # -----------------------------------------------------------------
    # Inventory conversion
    # -----------------------------------------------------------------
    @staticmethod
    def _build_block_def(piece):
        w, h = piece['w'], piece['h']
        if piece.get('is_stair'):
            return make_stair_block(w, h)
        return make_flat_block(w, h)

    @staticmethod
    def _shape_key(piece):
        w, h = piece['w'], piece['h']
        nw, nh = (w, h) if w <= h else (h, w)
        return (nw, nh, bool(piece.get('is_stair')))

    # -----------------------------------------------------------------
    # Placement primitives
    # -----------------------------------------------------------------
    def _resolved_tiles(self, block_def, pivot, rotation, flipped):
        px, py = pivot
        for (dx, dy), seg, _, _ in block_def.resolve(rotation, flipped):
            yield (px + dx, py + dy), seg

    def can_place(self, block_def, pivot, rotation, flipped):
        tiles = list(self._resolved_tiles(block_def, pivot, rotation, flipped))

        for (x, y), _ in tiles:
            if not (0 <= x < self.GRID_W and 0 <= y < self.GRID_H):
                return False
            if self.grid[y][x] != 0:
                return False
            if (x, y) == self.miner_pos or (x, y) == self.goal_pos:
                return False

        bottoms = {}
        for (x, y), _ in tiles:
            if x not in bottoms or y < bottoms[x]:
                bottoms[x] = y

        if any(y == 0 for y in bottoms.values()):
            return True

        for x, y in bottoms.items():
            # Tower below acts as a rigid pillar support (matches the original code).
            if 0 <= y - 1 < self.GRID_H and self.grid[y - 1][x] == 1:
                return True
            below = self.tile_segment_map.get((x, y - 1))
            if below is None:
                continue
            _, seg, rot, fl = below
            if seg.provides_top_surface(rot, fl):
                return True
        return False

    def _place_block(self, block_id, block_def, pivot, rotation, flipped):
        tile_map = {}
        for (x, y), seg in self._resolved_tiles(block_def, pivot, rotation, flipped):
            self.grid[y][x] = block_id
            self.tile_segment_map[(x, y)] = (block_id, seg, rotation, flipped)
            tile_map[(x, y)] = seg
        self.placed_info[block_id] = {
            'block_def': block_def,
            'pivot':     pivot,
            'rotation':  rotation,
            'flipped':   flipped,
            'tile_map':  tile_map,
        }

    def _remove_block(self, block_id):
        info = self.placed_info.pop(block_id, None)
        if not info:
            return
        for (x, y) in info['tile_map']:
            self.grid[y][x] = 0
            self.tile_segment_map.pop((x, y), None)

    # -----------------------------------------------------------------
    # Pathfinding hook (uses pathfinding module)
    # -----------------------------------------------------------------
    def grid_to_graph(self):
        return build_graph(self.tile_segment_map, self.grid, self.GRID_H)

    def find_path(self):
        return find_path(self.grid_to_graph(), self.miner_pos, self.goal_pos)

    # -----------------------------------------------------------------
    # Solution fingerprint
    # -----------------------------------------------------------------
    def get_solution_hash(self):
        items = []
        for pid in sorted(self.placed_info.keys()):
            info = self.placed_info[pid]
            items.append(
                f"{pid}:{info['pivot']},{info['rotation']},{info['flipped']}"
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
        print(f">>> Grid: {self.GRID_W}x{self.GRID_H} | Solver: Segment-based")
        print(f"{'='*90}\n")

        self.grid = [[0 for _ in range(self.GRID_W)] for _ in range(self.GRID_H)]
        self.tile_segment_map = {}
        self.placed_info = {}
        self.final_path = []
        self.found_solutions = set()
        # Accept both new (miner/goal) and legacy (knight/princess) JSON keys.
        self.miner_pos = tuple(test.get('miner', test.get('knight')))
        self.goal_pos  = tuple(test.get('goal',  test.get('princess')))
        self.solution_count = 0
        self.towers_list = test.get('towers', [])

        for tx, ty in self.towers_list:
            if 0 <= tx < self.GRID_W and 0 <= ty < self.GRID_H:
                self.grid[ty][tx] = 1

        mx, my = self.miner_pos
        if self.grid[my][mx] != 0:
            print(f"WARNING: Miner cell ({mx},{my}) not empty. Skipping.")
            return False

        self.inventory = []
        for piece in test['inventory']:
            self.inventory.append({
                'id':        piece['id'],
                'block_def': self._build_block_def(piece),
                'shape_key': self._shape_key(piece),
                'raw':       piece,
            })
        self.used_ids = set()
        self.current_test_idx = index
        return True

    # -----------------------------------------------------------------
    # Backtracking solver
    # -----------------------------------------------------------------
    def solve(self):
        # Lazy import so visualization (tkinter) is only loaded when needed.
        from .visualization import print_solution_terminal, show_solution_gui

        if len(self.used_ids) == len(self.inventory):
            sol_hash = self.get_solution_hash()
            if sol_hash in self.found_solutions:
                return

            if self.use_shapestacks:
                if not is_stack_stable(self.placed_info, self.grid, self.GRID_W):
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
        unused.sort(key=lambda p: (p['shape_key'], p['id']))

        for i, piece in enumerate(unused):
            key = piece['shape_key']
            skip = any(q['shape_key'] == key and q['id'] not in self.used_ids
                       for q in unused[:i])
            if skip:
                continue

            self.used_ids.add(piece['id'])
            block_def = piece['block_def']

            seen_configs = set()
            for rotation in self._ALL_ROTATIONS:
                for flipped in self._ALL_FLIPS:
                    resolved_key = tuple(
                        (off, id(seg)) for off, seg, _, _ in block_def.resolve(rotation, flipped)
                    )
                    if resolved_key in seen_configs:
                        continue
                    seen_configs.add(resolved_key)

                    for y in range(self.GRID_H):
                        for x in range(self.GRID_W):
                            pivot = (x, y)
                            if not self.can_place(block_def, pivot, rotation, flipped):
                                continue
                            self._place_block(piece['id'], block_def, pivot, rotation, flipped)
                            self.solve()
                            self._remove_block(piece['id'])

            self.used_ids.remove(piece['id'])

    # -----------------------------------------------------------------
    # Diagnostics
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
                        f"Test {idx+1} done. Solutions: {self.solution_count}. "
                        f"Stable configs: {self.stable_configs_count}. "
                        f"Frustration: {self.stable_configs_count/max(1,self.solution_count):.3f}. "
                        f"Static Entanglement: {self.get_static_entanglement():.2f} bits.\n"
                    )

    def get_static_entanglement(self):
        total_bits = 0
        for piece in self.inventory:
            bi = 0
            block_def = piece['block_def']
            seen_configs = set()
            for rotation in self._ALL_ROTATIONS:
                for flipped in self._ALL_FLIPS:
                    resolved_key = tuple(
                        (off, id(seg)) for off, seg, _, _ in block_def.resolve(rotation, flipped)
                    )
                    if resolved_key in seen_configs:
                        continue
                    seen_configs.add(resolved_key)
                    for y in range(self.GRID_H):
                        for x in range(self.GRID_W):
                            if self.can_place(block_def, (x, y), rotation, flipped):
                                bi += 1
            total_bits += math.log2(bi + 1)
        return total_bits
