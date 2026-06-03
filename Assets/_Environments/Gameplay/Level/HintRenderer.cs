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
    List<HintStep> steps;
    readonly List<Block> ghosts = new();
    int revealedCount;

    struct HintStep
    {
        public Block block;
        public Vector2 position;
        public BoardGrid.Rotation rotation;
        public bool flipped;
    }

    void Awake() => level = GetComponent<Level>();

    public void DisplayHint()
    {
        if (steps == null)
        {
            steps = BuildSteps();
            if (steps == null) return;
        }

        if (revealedCount >= steps.Count) return;

        ghosts.Add(SpawnGhost(steps[revealedCount]));
        revealedCount++;
        DataCollectionManager.Instance?.RecordHint();
        Debug.Log($"HintRenderer: revealed hint {revealedCount}/{steps.Count}.");
    }

    public void SpawnHardCodedHint() => DisplayHint();

    public void ResetHints()
    {
        foreach (Block ghost in ghosts)
            if (ghost != null) Destroy(ghost.gameObject);
        ghosts.Clear();
        revealedCount = 0;
        steps = null;
    }

    List<HintStep> BuildSteps()
    {
        LevelLayout layout = LevelManager.CurrentLayout;
        if (layout == null || layout.Solutions.Count == 0)
        {
            Debug.Log("HintRenderer: no baked solutions available for this level.");
            return null;
        }

        // Map blockId → Block instance using inventory order (matches free-block order in layout)
        var freeBlocks = layout.Blocks
            .Where(b => b.MobilityType == Block.Mobility.Free)
            .ToList();

        var idToBlock = new Dictionary<int, Block>();
        for (int i = 0; i < freeBlocks.Count && i < level.Inventory.Count; i++)
            idToBlock[freeBlocks[i].BlockId] = level.Inventory[i];

        return layout.Solutions[0].Placements
            .Where(p => idToBlock.ContainsKey(p.blockId))
            .OrderBy(p => p.tile.y).ThenBy(p => p.tile.x)
            .Select(p => new HintStep
            {
                block = idToBlock[p.blockId],
                position = new Vector2(p.tile.x, p.tile.y),
                rotation = p.rotation,
                flipped = p.flipped
            })
            .ToList();
    }

    Block SpawnGhost(HintStep step)
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
