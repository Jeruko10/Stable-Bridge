using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelSolver
{
    public struct HintStep
    {
        public Block block;
        public Vector2 position;
        public BoardGrid.Rotation rotation;
        public bool flipped;
        public Vector2Int tile;
    }

    struct BlockState
    {
        public Vector3 position;
        public Vector2 position2D;
        public BoardGrid.Rotation rotation;
        public bool flipped;
    }

    static readonly BoardGrid.Rotation[] AllRotations =
    {
        BoardGrid.Rotation.Deg0,
        BoardGrid.Rotation.Deg90,
        BoardGrid.Rotation.Deg180,
        BoardGrid.Rotation.Deg270
    };

    readonly BoardGrid grid;
    readonly Vector2Int start, end;
    int iterations;
    int maxIterations;
    bool aborted;

    public LevelSolver(BoardGrid grid, Vector2Int start, Vector2Int end)
    {
        this.grid = grid;
        this.start = start;
        this.end = end;
    }

    public List<HintStep> Solve(List<Block> blocks, int maxIterations = 100000)
    {
        this.maxIterations = maxIterations;
        iterations = 0;
        aborted = false;

        var saved = blocks.ToDictionary(b => b, SaveState);
        var result = new List<HintStep>();
        bool found = Backtrack(blocks, 0, result);

        foreach (Block block in blocks)
        {
            grid.RemoveBlock(block);
            RestoreState(block, saved[block]);
        }

        return found ? result : null;
    }

    bool Backtrack(List<Block> blocks, int index, List<HintStep> current)
    {
        if (aborted) return false;

        if (++iterations > maxIterations)
        {
            Debug.LogWarning($"LevelSolver: hit iteration limit ({maxIterations}), aborting search.");
            aborted = true;
            return false;
        }

        if (index >= blocks.Count) return PathExists();

        Block block = blocks[index];
        BlockState saved = SaveState(block);

        foreach (bool flip in new[] { false, true })
        {
            foreach (BoardGrid.Rotation rotation in AllRotations)
            {
                RestoreState(block, saved);
                ApplyTransform(block, rotation, flip);

                for (int x = 0; x < grid.Size.x; x++)
                {
                    for (int y = 0; y < grid.Size.y; y++)
                    {
                        Vector2Int tile = new(x, y);
                        foreach (BlockSegment pivot in block.Segments)
                        {
                            if (!grid.TryPlaceBlock(block, tile, pivot)) continue;

                            current.Add(new HintStep
                            {
                                block = block,
                                position = block.Position2D,
                                rotation = rotation,
                                flipped = flip,
                                tile = tile
                            });

                            if (Backtrack(blocks, index + 1, current)) return true;

                            if (aborted) return false;

                            current.RemoveAt(current.Count - 1);
                            grid.RemoveBlock(block);
                        }
                    }
                }
            }
        }

        RestoreState(block, saved);
        return false;
    }

    bool PathExists()
    {
        grid.AddRow(false);
        Graph graph = PathSolver.GridToGraph(grid);
        grid.RemoveRow();
        IEnumerable<Vector2> path = PathSolver.GetPath(start, end, graph);
        return path.LastOrDefault() == (Vector2)end + BlockSegment.BottomRight;
    }

    void ApplyTransform(Block block, BoardGrid.Rotation rotation, bool flip)
    {
        if (block.IsFlipped) block.Flip(block.Pivot);
        block.SetRotation(block.Pivot, rotation);
        if (flip) block.Flip(block.Pivot);
    }

    BlockState SaveState(Block block) => new()
    {
        position = block.transform.position,
        position2D = block.Position2D,
        rotation = block.Rotation,
        flipped = block.IsFlipped
    };

    void RestoreState(Block block, BlockState state)
    {
        if (block.IsFlipped != state.flipped) block.Flip(block.Pivot);
        block.SetRotation(block.Pivot, state.rotation);
        block.transform.position = state.position;
        block.Position2D = state.position2D;
    }
}
