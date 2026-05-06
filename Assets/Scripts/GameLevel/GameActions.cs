using UnityEngine;

public class GameActions : MonoBehaviour
{
    public bool IsDragging { get; private set; }

    Block draggedBlock;
    BlockSegment draggedSegment;
    SlotManager slotManager;
    BoardGrid grid;
    Vector2Int savedPivotTile;
    bool hasSavedGridPosition;

    void Awake() => LevelManager.LevelLoaded += OnLevelLoaded;

    void OnLevelLoaded(Level level)
    {
        slotManager = level.Slots;
        grid = level.Grid;
    }

    public bool TryRotateBlock(Block block, bool clockwise) => grid.TryRotateBlock(block, clockwise);

    public bool TryFlipBlock(Block block) => grid.TryFlipBlock(block);

    public void StartDragBlock(Block block, BlockSegment segment)
    {
        if (block == null || IsDragging || block.MobilityType != Block.Mobility.Free) return;

        draggedBlock = block;
        draggedSegment = segment;

        Vector2Int? savedTile = grid.ContainsBlock(draggedBlock) ? grid.GetTileOfBlock(draggedBlock.Pivot) : null;
        hasSavedGridPosition = savedTile.HasValue;
        if (hasSavedGridPosition) savedPivotTile = savedTile.Value;

        grid.RemoveBlock(draggedBlock);
        slotManager.FreeSlot(draggedBlock);
        IsDragging = true;
    }

    public bool TryDropDraggedBlock(Vector2 worldPosition, bool moveToSlotOnFailure = false)
    {
        if (draggedBlock == null) return false;

        Vector2Int tile = grid.WorldToTile(worldPosition);
        bool placed = grid.TryPlaceBlock(draggedBlock, tile, draggedSegment, tryAllPivots: true);

        if (!placed)
        {
            bool restored = !moveToSlotOnFailure && hasSavedGridPosition && grid.TryPlaceBlock(draggedBlock, savedPivotTile, draggedBlock.Pivot);
            if (!restored) slotManager.TryAsignAvailableSlot(draggedBlock);
        }

        IsDragging = false;
        draggedBlock = null;
        draggedSegment = null;
        return placed;
    }

    public void MoveDraggedBlock(Vector2 targetPosition)
    {
        if (!IsDragging) return;

        Vector2 offset = draggedBlock.transform.position - draggedSegment.transform.position;
        targetPosition += offset;
        draggedBlock.Position2D = targetPosition;
    }
}
