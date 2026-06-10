using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoardGrid))]
[RequireComponent(typeof(Level))]
public class HintRenderer : MonoBehaviour
{
    [SerializeField] float depthOffset = -0.5f;
    [SerializeField] Color color = Color.cyan;

    Level level;
    BoardGrid grid;
    readonly List<Block> ghosts = new();
    readonly HashSet<int> ghostedBlockIds = new();
    int? chosenSolutionIndex;

    struct HintStep
    {
        public Block block;
        public int blockId;
        public Vector2 position;
        public BoardGrid.Rotation rotation;
        public bool flipped;
    }

    void Awake()
    {
        level = GetComponent<Level>();
        grid = GetComponent<BoardGrid>();
    }

    public void DisplayHint()
    {
        LevelLayout layout = LevelManager.CurrentLayout;
        if (layout == null || layout.Solutions.Count == 0)
        {
            Debug.Log("HintRenderer: no baked solutions available for this level.");
            return;
        }

        var idToBlock = BuildIdToBlock(layout);

        chosenSolutionIndex ??= SelectSolution(layout, idToBlock);

        HintStep? step = FindNextStep(layout.Solutions[chosenSolutionIndex.Value], idToBlock);
        if (step == null) return;

        ghostedBlockIds.Add(step.Value.blockId);
        ghosts.Add(SpawnGhost(step.Value));
        DataCollectionManager.Instance?.RecordHint();
        Debug.Log($"HintRenderer: revealed hint for block {step.Value.block.name} (solution {chosenSolutionIndex}).");
    }

    public void SpawnHardCodedHint() => DisplayHint();

    public void ResetHints()
    {
        foreach (Block ghost in ghosts)
            if (ghost != null) Destroy(ghost.gameObject);
        ghosts.Clear();
        ghostedBlockIds.Clear();
        chosenSolutionIndex = null;
    }

    Dictionary<int, Block> BuildIdToBlock(LevelLayout layout)
    {
        var freeBlocks = layout.Blocks
            .Where(b => b.MobilityType == Block.Mobility.Free)
            .ToList();

        var idToBlock = new Dictionary<int, Block>();
        for (int i = 0; i < freeBlocks.Count && i < level.Inventory.Count; i++)
            idToBlock[freeBlocks[i].BlockId] = level.Inventory[i];
        return idToBlock;
    }

    int SelectSolution(LevelLayout layout, Dictionary<int, Block> idToBlock)
    {
        bool anyPlaced = grid.GetAllBlocks().Any(b => b.MobilityType == Block.Mobility.Free);
        if (!anyPlaced)
            return Random.Range(0, layout.Solutions.Count);

        int maxPossible = idToBlock.Values.Sum(b => b.Segments.Length);
        int bestScore = -1;
        var bestIndices = new List<int>();

        for (int i = 0; i < layout.Solutions.Count; i++)
        {
            int score = ScoreSolution(layout.Solutions[i], idToBlock);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndices.Clear();
                bestIndices.Add(i);
                if (score == maxPossible) break; // all blocks match — level is solved
            }
            else if (score == bestScore)
            {
                bestIndices.Add(i);
            }
        }

        return bestIndices[Random.Range(0, bestIndices.Count)];
    }

    int ScoreSolution(LevelSolution solution, Dictionary<int, Block> idToBlock)
    {
        int score = 0;
        foreach (var p in solution.Placements)
        {
            if (idToBlock.TryGetValue(p.blockId, out Block block) && IsCorrectlyPlaced(block, p))
                score += block.Segments.Length;
        }
        return score;
    }

    bool IsCorrectlyPlaced(Block block, LevelSolution.Placement p)
    {
        if (!grid.ContainsBlock(block)) return false;
        return block.ShapeMatchesPlacement(grid.TileToWorld(p.tile), p.rotation, p.flipped);
    }

    HintStep? FindNextStep(LevelSolution solution, Dictionary<int, Block> idToBlock)
    {
        LevelSolution.Placement? best = null;

        foreach (var p in solution.Placements)
        {
            if (!idToBlock.ContainsKey(p.blockId)) continue;
            if (ghostedBlockIds.Contains(p.blockId)) continue;
            if (IsCorrectlyPlaced(idToBlock[p.blockId], p)) continue;

            if (best == null
                || p.tile.y < best.Value.tile.y
                || (p.tile.y == best.Value.tile.y && p.tile.x < best.Value.tile.x))
                best = p;
        }

        if (best == null) return null;

        var placement = best.Value;
        return new HintStep
        {
            block    = idToBlock[placement.blockId],
            blockId  = placement.blockId,
            position = grid.TileToWorld(placement.tile),
            rotation = placement.rotation,
            flipped  = placement.flipped
        };
    }

    Block SpawnGhost(HintStep step)
    {
        Block source = step.block;
        Block prefab = source.Prefab != null ? source.Prefab : source;
        int pivotIndex = System.Array.IndexOf(source.Segments, source.Pivot);

        Block ghost = Instantiate(prefab, transform);
        ghost.Initialize(prefab, pivotIndex, Block.Mobility.Fixed);

        // Reset to world origin so pivot position is clean before applying transforms
        ghost.transform.position = Vector3.zero;
        ghost.Position2D = Vector2.zero;

        ghost.SetRotation(ghost.Pivot, step.rotation);
        if (step.flipped) ghost.Flip(ghost.Pivot);

        // Baker tile = world position of the block's local-origin cell.
        // Set root directly; pivot offset must NOT be subtracted because the
        // baker already computes tile relative to the root, not the pivot segment.
        ghost.transform.position = new Vector3(step.position.x, step.position.y, depthOffset);
        ghost.Position2D = step.position;
        ghost.DepthOffset = depthOffset;
        ghost.Color = color;

        return ghost;
    }
}
