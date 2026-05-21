using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(BoardGrid))]

[RequireComponent(typeof(Level))]

public class HintRenderer : MonoBehaviour
{
    [SerializeField] float depthOffset = -0.5f;
    [SerializeField] Color color = Color.cyan;
    [SerializeField] int maxSolverIterations = 100000;

    BoardGrid grid;
    Level level;
    List<LevelSolver.HintStep> steps;
    readonly List<Block> ghosts = new();
    int revealedCount;

    void Awake()
    {
        grid = GetComponent<BoardGrid>();
        level = GetComponent<Level>();
    }

    public void DisplayHint()
    {
        Debug.Log(level.Inventory.Count);

        if (steps == null)
        {
            List<LevelSolver.HintStep> solution =
                new LevelSolver(grid, level.StartPosition, level.EndPosition).Solve(level.Inventory, maxSolverIterations);

            if (solution == null)
            {
                Debug.Log("HintRenderer: no solution found for current board state.");
                return;
            }

            steps = solution.OrderBy(s => s.tile.y).ThenBy(s => s.tile.x).ToList();
        }

        if (revealedCount >= steps.Count) return;

        ghosts.Add(SpawnGhost(steps[revealedCount]));
        revealedCount++;
        Debug.Log($"HintRenderer: revealed hint {revealedCount}/{steps.Count}.");
    }

    public void SpawnHardCodedHint()
    {
        LevelSolver.HintStep step = new()
        {
            block = level.Inventory[0],
            position = new Vector2(2f, 1f),
            rotation = BoardGrid.Rotation.Deg0,
            flipped = false
        };

        ghosts.Add(SpawnGhost(step));
    }

    public void ResetHints()
    {
        Debug.Log("HintRenderer: resetting hints.");
        foreach (Block ghost in ghosts)
            if (ghost != null) Destroy(ghost.gameObject);
        ghosts.Clear();
        revealedCount = 0;
        steps = null;
    }

    Block SpawnGhost(LevelSolver.HintStep step)
    {
        Block source = step.block;
        Block prefab = source.Prefab != null ? source.Prefab : source;
        int pivotIndex = System.Array.IndexOf(source.Segments, source.Pivot);

        Block ghost = Instantiate(prefab, transform);
        ghost.Initialize(prefab, pivotIndex, Block.Mobility.Fixed);
        ghost.SetRotation(ghost.Pivot, step.rotation);
        if (step.flipped) ghost.Flip(ghost.Pivot);

        ghost.transform.position = new Vector3(step.position.x, step.position.y, depthOffset);
        ghost.Position2D = step.position;
        ghost.DepthOffset = depthOffset;
        ghost.Color = color;

        return ghost;
    }
}
