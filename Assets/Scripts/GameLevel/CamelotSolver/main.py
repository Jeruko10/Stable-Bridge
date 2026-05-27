"""Runnable entry point: `python -m CamelotSolver.main` or `python main.py`."""

import sys

# Windows consoles default to cp1252 — re-encode stdout/stderr as UTF-8 so
# box-drawing characters and emojis in the visualization don't crash.
try:
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')
except AttributeError:
    pass

from .solver import CamelotSolverComplete


def main():
    print("\n" + "="*90)
    print("CAMELOT SOLVER - COMPLETE SOLUTION (Terminal + GUI)")
    print("="*90)
    print("\nFEATURES:")
    print("  ✓ Cell-based stair detection (sloped vs flat)")
    print("  ✓ Diagonal moves only onto sloped stair cells")
    print("  ✓ Terminal grid visualization for each solution")
    print("  ✓ Interactive Tkinter GUI with path visualization")
    print("  ✓ Detailed piece and path information")
    print("="*90 + "\n")

    solver = CamelotSolverComplete(
        use_shapestacks=True,
        grid_w=6,
        grid_h=6,
        enable_gui=False,
    )

    solver.run_tests()  # run every level in tests.json

    print("\n" + "="*90)
    print("Done.")
    print("="*90 + "\n")


if __name__ == "__main__":
    main()
