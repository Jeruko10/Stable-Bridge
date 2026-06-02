"""Entry point: `python -m CamelotSolver.main`

HOW TO COMPARE PRUNING TECHNIQUES
===================================
P1 symmetry breaking is always active.  Toggle P2-P5 to compare performance.
Run the solver with different prune_* combinations and read the "Recursion calls"
line in each test report.  Lower = less work = better pruning.

Quick comparison matrix (set the flags below):

  P1 only  : all False  (default behaviour without optional pruning)
  P1+P2    : prune_spatial=True,   rest False
  P1+P3    : prune_reach=True,     rest False
  P1+P4    : prune_forward=True,   rest False
  P1+P5    : prune_stability=True, rest False
  All on   : all True   (default)
"""

import sys
try:
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')
except AttributeError:
    pass

from .solver import CamelotSolverComplete


def main():
    print("\n" + "=" * 90)
    print("CAMELOT SOLVER  –  9x9 grid  –  five pruning techniques")
    print("=" * 90)
    print("""
PRUNING LEGEND
  P1  Symmetry breaking      – always active; skip duplicate piece orderings
  P2  Spatial x-range        – skip positions outside [knight_x, princess_x]
  P3  Optimistic reachability – cut subtree when BFS (empty cells walkable) fails
  P4  Forward checking       – cut subtree when any remaining piece has no valid cell
  P5  Incremental stability  – cut subtree when partial COM check already fails
""")
    print("=" * 90 + "\n")

    solver = CamelotSolverComplete(
        use_shapestacks=True,
        grid_w=9,
        grid_h=9,
        enable_gui=False,
        # ---- toggle individual techniques here (symmetry is always on) ----
        prune_spatial=True,
        prune_reach=True,
        prune_forward=True,
        prune_stability=False,  # unsound heuristic — disabled by default
    )

    solver.validate_pruning_soundness()
    solver.run_tests()

    print("\n" + "=" * 90)
    print("Done.")
    print("=" * 90 + "\n")


if __name__ == "__main__":
    main()
