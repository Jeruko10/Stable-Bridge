using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BoardGrid))]
public class HintManager : MonoBehaviour
{
    BoardGrid grid;
    Block[] blockPrefabs;
    Block currentHint;

    void Awake() => grid = GetComponent<BoardGrid>();

    void Start()
    {
        blockPrefabs = Resources.LoadAll<Block>("Blocks");
    }

    public void DisplayTestHint()
    {
        DisplayHint(blockPrefabs[1], new(2, 1), BoardGrid.Rotation.Deg0, false, 0);
    }

    public void DisplayHint(Block block, Vector2Int cell, BoardGrid.Rotation rotation, bool flipped, int pivotIndex)
    {
        Debug.Log($"Displaying hint for block {block.name} at cell {cell} with rotation {rotation} and flipped {flipped}");
        HideHint();

        Block prefab = block.Prefab != null ? block.Prefab : block;

        currentHint = Instantiate(prefab, transform);
        currentHint.Initialize(prefab, pivotIndex, Block.Mobility.Fixed);
        currentHint.SetRotation(currentHint.Pivot, rotation);
        if (flipped) currentHint.Flip(currentHint.Pivot);

        Vector3 pivotTarget = grid.TileToWorld(cell);
        Vector3 delta = pivotTarget - currentHint.Pivot.transform.position;
        Vector3 placedPos = currentHint.transform.position + delta;
        currentHint.transform.position = placedPos;
        currentHint.Position2D = placedPos;
        currentHint.DepthOffset = 0.1f;
        currentHint.Color = Color.cyan;
    }

    public void HighlightBlock(Block block)
    {
        block.Color = Color.cyan;
    }

    public void HideHint()
    {
        if (currentHint == null) return;
        Destroy(currentHint.gameObject);
        currentHint = null;
    }
}
