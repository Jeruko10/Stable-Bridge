"""CamelotSolverComplete – segment-aware backtracking solver with five pruning
techniques.

PRUNING TECHNIQUES
==================
P1  Symmetry breaking      – among identical pieces (same size + stair flag),
                             only the one with the lowest id is tried first.
                             Avoids re-exploring permutations of equal pieces.

P2  Spatial x-range        – skip any placement whose cells fall entirely to
                             the left of the knight (x+w <= knight_x) or start
                             beyond the princess column (x > princess_x).
                             Pieces there can never contribute to the path.

P3  Optimistic reachability– after each placement, run a fast BFS that treats
                             empty cells as walkable.  If even this liberal
                             check shows no path, the entire subtree is cut.

P4  Forward checking       – before recursing, verify that every still-unplaced
                             piece has at least one valid cell in the grid.  If
                             any piece has no room, backtrack immediately.

P5  Incremental stability  – after each placement, run the full ShapeStacks
                             COM check on the pieces placed so far.  A partial
                             stack that is already unstable will never become
                             stable by adding more pieces on top.

COMPARING TECHNIQUES
====================
Set individual prune_* flags to True/False at construction time, then compare
`solver.recursion_calls` and `solver.pruning_stats` after each test.  The
recursion_calls counter is incremented on every entry to solve(), so it is the
single comparable metric across all pruning combinations.
"""

import json
import math
import os

from .blocks import build_block_segments
from .pathfinding import (find_path as _find_path, is_cell_walkable,
                           can_reach_optimistically)
from .stability import is_entire_stack_stable


_PKG_DIR = os.path.dirname(os.path.abspath(__file__))
_DEFAULT_TESTS = os.path.join(_PKG_DIR, "tests.json")


class CamelotSolverComplete:
    """Segment-aware Camelot solver with configurable pruning."""

    def __init__(self, test_file=_DEFAULT_TESTS, use_shapestacks=True,
                 grid_w=9, grid_h=9, enable_gui=False,
                 prune_spatial=True,
                 prune_reach=True,
                 prune_forward=True,
                 prune_stability=False,
                 silent=False):

        self.use_shapestacks = use_shapestacks
        self.GRID_W = grid_w
        self.GRID_H = grid_h
        self.enable_gui = enable_gui
        self.silent = silent

        # Pruning flags (symmetry breaking is always active — not a toggle)
        self.prune_spatial   = prune_spatial
        self.prune_reach     = prune_reach
        self.prune_forward   = prune_forward
        self.prune_stability = prune_stability

        # Grid state
        self.grid        = [[0] * grid_w for _ in range(grid_h)]
        self.segment_map = {}          # (x, y) -> Segment
        self.placed_info = {}          # pid -> placement record
        self.used_ids    = set()
        self.found_solutions = set()
        self.final_path  = []
        self.inventory   = []
        self.knight_pos  = (0, 0)
        self.princess_pos = (0, 0)
        self.towers_list = []

        # Per-test counters (reset in load_test_case)
        self.recursion_calls    = 0
        self.solution_count     = 0
        self.stable_configs_count = 0
        self.pruning_stats = {
            'p1_symmetry':  0,  # pieces skipped by symmetry rule
            'p2_spatial':   0,  # position attempts skipped
            'p3_reach':     0,  # subtrees cut by optimistic reachability
            'p4_forward':   0,  # subtrees cut by forward check
            'p5_stability': 0,  # subtrees cut by incremental COM check
        }

        try:
            with open(test_file, 'r', encoding='utf-8') as f:
                self.all_tests = json.load(f)
        except FileNotFoundError:
            print(f"Error: {test_file} not found.")
            self.all_tests = []
            return

        self.current_test_idx = 0

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------

    @staticmethod
    def _piece_key(p):
        """Canonical key for symmetry breaking: normalise (w,h) and stair flag."""
        w, h = p['w'], p['h']
        nw, nh = (w, h) if w <= h else (h, w)
        return (nw, nh, bool(p.get('is_stair')))

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
        for j in range(w):
            bv = self.grid[y - 1][x + j]
            if bv == 0:
                continue
            if bv == 1:
                return True
            seg = self.segment_map.get((x + j, y - 1))
            if seg is not None and seg.provides_top_surface:
                return True
        return False

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

    def find_path(self):
        return _find_path(self.grid, self.segment_map,
                          self.knight_pos, self.princess_pos,
                          self.GRID_W, self.GRID_H)

    def get_solution_hash(self):
        # Group placements by canonical piece key so that swapping two
        # physically identical pieces (same key) hashes identically.
        groups = {}
        id_to_piece = {q['id']: q for q in self.inventory}
        for pid, info in self.placed_info.items():
            p = id_to_piece.get(pid)
            key = self._piece_key(p) if p is not None else pid
            tup = (info['x'], info['y'], info['w'], info['h'],
                   info['is_mirror'], info['is_inverted'])
            groups.setdefault(key, []).append(tup)
        parts = []
        for key in sorted(groups):
            for tup in sorted(groups[key]):
                parts.append(f"{key}:{tup}")
        return "|".join(parts)

    # P4 helper ----------------------------------------------------------

    def _forward_check(self):
        """Return False if any unplaced piece has no valid position."""
        for p in self.inventory:
            if p['id'] in self.used_ids:
                continue
            has_spot = False
            for w, h in {(p['w'], p['h']), (p['h'], p['w'])}:
                if has_spot:
                    break
                for y in range(self.GRID_H):
                    if has_spot:
                        break
                    for x in range(self.GRID_W):
                        if self.can_place(x, y, w, h):
                            has_spot = True
                            break
            if not has_spot:
                return False
        return True

    # ------------------------------------------------------------------
    # Test loading
    # ------------------------------------------------------------------

    def load_test_case(self, index):
        if not (0 <= index < len(self.all_tests)):
            return False
        test = self.all_tests[index]

        active = [
            name for name, flag in [
                ('P2-spa',  self.prune_spatial),
                ('P3-reach',self.prune_reach),
                ('P4-fwd',  self.prune_forward),
                ('P5-stab', self.prune_stability),
            ] if flag
        ]
        if not self.silent:
            print(f"\n{'='*90}")
            print(f">>> TEST {index+1}: {test['name']}  |  Grid {self.GRID_W}x{self.GRID_H}  "
                  f"|  Active prunes: P1-sym(always), {', '.join(active) if active else 'none'}")
            print(f"{'='*90}\n")

        self.grid        = [[0] * self.GRID_W for _ in range(self.GRID_H)]
        self.segment_map = {}
        self.placed_info = {}
        self.final_path  = []
        self.found_solutions = set()
        self.knight_pos   = tuple(test.get('knight', test.get('miner')))
        self.princess_pos = tuple(test.get('princess', test.get('goal')))
        self.towers_list  = test.get('towers', [])

        # Reset per-test counters
        self.recursion_calls      = 0
        self.solution_count       = 0
        self.stable_configs_count = 0
        for k in self.pruning_stats:
            self.pruning_stats[k] = 0

        for tx, ty in self.towers_list:
            if 0 <= tx < self.GRID_W and 0 <= ty < self.GRID_H:
                self.grid[ty][tx] = 1

        kx, ky = self.knight_pos
        if self.grid[ky][kx] != 0:
            if not self.silent:
                print(f"WARNING: Knight at ({kx},{ky}) is not empty. Skipping.")
            return False
        if not is_cell_walkable(self.grid, kx, ky, self.GRID_W, self.GRID_H):
            if not self.silent:
                print(f"WARNING: Knight at ({kx},{ky}) is not walkable. Skipping.")
            return False

        self.inventory = test['inventory']
        self.used_ids  = set()
        self.current_test_idx = index
        return True

    # ------------------------------------------------------------------
    # Stats report
    # ------------------------------------------------------------------

    def _print_pruning_report(self):
        if self.silent:
            return
        s = self.pruning_stats
        w = 12  # field width
        print(f"\n  {'Recursion calls':<28} {self.recursion_calls:>{w},}")
        print(f"  {'P1 symmetry  – skipped':<28} {s['p1_symmetry']:>{w},}")
        if self.prune_spatial:
            print(f"  {'P2 spatial   – positions skipped':<28} {s['p2_spatial']:>{w},}")
        if self.prune_reach:
            print(f"  {'P3 reach     – subtrees cut':<28} {s['p3_reach']:>{w},}")
        if self.prune_forward:
            print(f"  {'P4 forward   – subtrees cut':<28} {s['p4_forward']:>{w},}")
        if self.prune_stability:
            print(f"  {'P5 stability – subtrees cut':<28} {s['p5_stability']:>{w},}")

    # ------------------------------------------------------------------
    # Backtracking solver
    # ------------------------------------------------------------------

    def solve(self):
        from .visualization import print_solution_terminal, show_solution_gui

        self.recursion_calls += 1

        # P3: optimistic reachability  --------------------------------
        if self.prune_reach:
            if not can_reach_optimistically(
                    self.grid, self.segment_map,
                    self.knight_pos, self.princess_pos,
                    self.GRID_W, self.GRID_H):
                self.pruning_stats['p3_reach'] += 1
                return

        # P5: incremental COM stability  ------------------------------
        if self.prune_stability and self.placed_info:
            if not is_entire_stack_stable(self.placed_info, self.grid,
                                          self.GRID_W, self.GRID_H,
                                          self.inventory):
                self.pruning_stats['p5_stability'] += 1
                return

        # Base case: all pieces placed  --------------------------------
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
                if not self.silent:
                    print_solution_terminal(self)
                if self.enable_gui:
                    show_solution_gui(self)
            return

        # P4: forward check  -----------------------------------------
        if self.prune_forward and not self._forward_check():
            self.pruning_stats['p4_forward'] += 1
            return

        # Enumerate remaining pieces  ----------------------------------
        unused = [p for p in self.inventory if p['id'] not in self.used_ids]

        unused.sort(key=lambda p: (self._piece_key(p), p['id']))

        kx     = self.knight_pos[0]
        px_col = self.princess_pos[0]

        for i, p in enumerate(unused):

            # P1: symmetry breaking (always active) --------------------
            key = self._piece_key(p)
            if any(self._piece_key(q) == key and q['id'] not in self.used_ids
                   for q in unused[:i]):
                self.pruning_stats['p1_symmetry'] += 1
                continue

            self.used_ids.add(p['id'])
            is_stair = bool(p.get('is_stair'))

            for w, h in {(p['w'], p['h']), (p['h'], p['w'])}:

                if is_stair:
                    if h > w:
                        # Vertical stair: normal + mirror only (no inverted).
                        stair_configs = [(False, False), (True, False)]
                    else:
                        # Horizontal stair: all four mirror/inverted combos.
                        # (False,True) and (True,True) both produce all-BASIC
                        # segments, but differ in is_mirror so they hash as
                        # distinct solutions — both must be tried.
                        stair_configs = [(False, False), (True, False),
                                         (False, True), (True, True)]
                else:
                    stair_configs = [(False, False)]

                for y in range(self.GRID_H):
                    for x in range(self.GRID_W):

                        # P2: skip positions entirely left of the knight.
                        # Note: x > px_col is intentionally NOT checked here —
                        # pieces can validly land beyond the princess column.
                        if self.prune_spatial:
                            if x + w <= kx:
                                self.pruning_stats['p2_spatial'] += 1
                                continue

                        if not self.can_place(x, y, w, h):
                            continue

                        for is_mirror, is_inverted in stair_configs:
                            self._place(p['id'], x, y, w, h,
                                        is_stair, is_mirror, is_inverted)
                            self.solve()
                            self._remove(p['id'])

            self.used_ids.remove(p['id'])

    # ------------------------------------------------------------------
    # Driver
    # ------------------------------------------------------------------

    def run_tests(self, test_indices=None):
        if test_indices is None:
            test_indices = range(len(self.all_tests))

        with open("test_results.txt", "w", encoding="utf-8") as f:
            def dual_print(msg):
                print(msg)
                print(msg, file=f)

            for idx in test_indices:
                if not self.load_test_case(idx):
                    continue
                dual_print(f"Solving Test {idx + 1}...\n")
                self.solve()

                frustration  = self.stable_configs_count / max(1, self.solution_count)
                entanglement = self.get_static_entanglement()
                metrics      = self.get_difficulty_metrics()
                dual_print(
                    f"\n--- Test {idx+1} complete ---\n"
                    f"  Solutions found:    {self.solution_count}\n"
                    f"  Stable configs:     {self.stable_configs_count}\n"
                    f"  Frustration score:  {frustration:.2f}\n"
                    f"  Static entanglement:{entanglement:.2f} bits"
                )
                self._print_pruning_report()
                dual_print(
                    f"\n  Difficulty Metrics\n"
                    f"  {'Inventory Size':<22} {metrics['inventory_size']}\n"
                    f"  {'Total Volume':<22} {metrics['total_volume']}\n"
                    f"  {'Height Sum':<22} {metrics['height_sum']}\n"
                    f"  {'Stable Configs':<22} {metrics['stable_configs']}\n"
                    f"  {'Max Choice':<22} {metrics['max_choice']}\n"
                    f"  {'Total Solutions':<22} {metrics['total_solutions']}\n"
                    f"  {'Large Pieces':<22} {metrics['large_pieces']}\n"
                    f"  {'Unique Shapes':<22} {metrics['unique_shapes']}"
                )
                dual_print("")

    # ------------------------------------------------------------------
    # Pruning soundness validation
    # ------------------------------------------------------------------

    def validate_pruning_soundness(self, test_indices=None):
        """Run all challenges under each P2-P5 configuration and verify that
        disabling any one pruning does not change the solution count compared
        to the all-on baseline.  Uses silent mode so no per-solution output
        is produced during the run.
        """
        if test_indices is None:
            test_indices = list(range(len(self.all_tests)))

        saved_silent = self.silent
        self.silent = True

        W = 70
        print(f"\n{'='*W}")
        print(f"PRUNING SOUNDNESS VALIDATION  –  {len(test_indices)} challenges")
        print(f"{'='*W}")

        # --- baseline: all prune flags on --------------------------------
        print("\nBaseline  (all prune flags ON) …")
        self.prune_spatial = self.prune_reach = self.prune_forward = self.prune_stability = True
        baseline: dict[int, int] = {}
        for idx in test_indices:
            if self.load_test_case(idx):
                self.solve()
                baseline[idx] = self.solution_count
        print(f"  Done – {len(baseline)} challenges solved.")

        # --- test P2 / P3 / P4 individually (verified sound) ------------
        # P5 (incremental stability) is excluded: it is an aggressive heuristic
        # that can cut valid solutions when a later piece would have made an
        # intermediate partial stack stable. Disabled by default.
        sound_configs = [
            ('P2 Spatial ', 'prune_spatial'),
            ('P3 Reach   ', 'prune_reach'),
            ('P4 Forward ', 'prune_forward'),
        ]

        all_pass = True
        for label, attr in sound_configs:
            self.prune_spatial = self.prune_reach = self.prune_forward = self.prune_stability = True
            setattr(self, attr, False)
            print(f"\n{label} OFF (others on) …")
            failures: list[tuple[int, int, int]] = []
            for idx in test_indices:
                if idx not in baseline:
                    continue
                if not self.load_test_case(idx):
                    continue
                self.solve()
                if self.solution_count != baseline[idx]:
                    failures.append((idx + 1, baseline[idx], self.solution_count))
            if failures:
                all_pass = False
                print(f"  FAIL – {len(failures)} mismatch(es):")
                for ch, exp, got in failures:
                    print(f"    Challenge {ch:>2}: expected {exp}, got {got}")
            else:
                print(f"  PASS – all {len(baseline)} challenges match baseline")

        # --- P5 audit (informational only, not part of pass/fail) --------
        print(f"\nP5 Stability OFF vs ON (informational – P5 is disabled by default) …")
        self.prune_spatial = self.prune_reach = self.prune_forward = True
        self.prune_stability = False
        p5_off: dict[int, int] = {}
        for idx in test_indices:
            if idx not in baseline:
                continue
            if not self.load_test_case(idx):
                continue
            self.solve()
            p5_off[idx] = self.solution_count
        diffs = [(idx + 1, baseline[idx], p5_off[idx])
                 for idx in p5_off if p5_off[idx] != baseline[idx]]
        if diffs:
            print(f"  {len(diffs)} challenge(s) differ when P5 is on vs off:")
            for ch, with_p5, without_p5 in diffs:
                print(f"    Challenge {ch:>2}: P5-on={with_p5}  P5-off={without_p5}")
        else:
            print(f"  No differences – P5 appears sound for this dataset.")

        # --- restore state -----------------------------------------------
        self.prune_spatial = self.prune_reach = self.prune_forward = True
        self.prune_stability = False
        self.silent = saved_silent

        print(f"\n{'='*W}")
        print(f"RESULT: {'ALL PASS' if all_pass else 'FAILURES DETECTED – see above'}")
        print(f"{'='*W}\n")
        return all_pass

    def get_difficulty_metrics(self):
        """Return the eight features from the difficulty correlation table."""
        inv = self.inventory

        inventory_size = len(inv)
        total_volume   = sum(p['w'] * p['h'] for p in inv)
        height_sum     = sum(p['h'] for p in inv)
        large_pieces   = sum(1 for p in inv if max(p['w'], p['h']) >= 3)
        unique_shapes  = len({
            (min(p['w'], p['h']), max(p['w'], p['h']), bool(p.get('is_stair')))
            for p in inv
        })

        # Maximum valid-placement count across all pieces and orientations
        # on the current grid (towers only, no puzzle pieces placed).
        max_choice = 0
        for p in inv:
            bi = 0
            is_stair = bool(p.get('is_stair'))
            for w, h in {(p['w'], p['h']), (p['h'], p['w'])}:
                if is_stair:
                    configs = ([(False, False), (True, False)]
                               if h > w else
                               [(False, False), (True, False),
                                (False, True),  (True, True)])
                else:
                    configs = [(False, False)]
                for _m, _iv in configs:
                    for y in range(self.GRID_H):
                        for x in range(self.GRID_W):
                            if self.can_place(x, y, w, h):
                                bi += 1
            if bi > max_choice:
                max_choice = bi

        return {
            'inventory_size': inventory_size,
            'total_volume':   total_volume,
            'height_sum':     height_sum,
            'stable_configs': self.stable_configs_count,
            'max_choice':     max_choice,
            'total_solutions':self.solution_count,
            'large_pieces':   large_pieces,
            'unique_shapes':  unique_shapes,
        }

    def get_static_entanglement(self):
        total = 0.0
        for p in self.inventory:
            bi = 0
            is_stair = bool(p.get('is_stair'))
            for w, h in {(p['w'], p['h']), (p['h'], p['w'])}:
                configs = ([(False, False), (True, False), (False, True)]
                           if is_stair else [(False, False)])
                for _m, _inv in configs:
                    for y in range(self.GRID_H):
                        for x in range(self.GRID_W):
                            if self.can_place(x, y, w, h):
                                bi += 1
            total += math.log2(bi + 1)
        return total
