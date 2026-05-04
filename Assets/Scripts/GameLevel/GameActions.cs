using System;
using UnityEngine;

public class GameActions : MonoBehaviour
{
    [SerializeField] Color highlightColor;

    public bool IsDragging { get; private set; }
    public Block SelectedBlock { get; private set; }

    Color defaultBlockColor;
    BlockSegment selectedSegment;
    SlotManager slotManager;
    BoardGrid grid;
    bool baseColorPicked = false;

    Vector2Int savedPivotTile;
    bool hasSavedGridPosition;

    void Awake()
    {
        LevelManager.LevelLoaded += OnLevelLoaded;
        UnselectBlock();
    }

    void OnLevelLoaded(Level level)
    {
        slotManager = level.Slots;
        grid = level.Grid;
    }

    public void TriggerSelectedBlockInteraction()
    {
        if (IsDragging) return;

        switch (SelectedBlock.MobilityType)
        {
            case Block.Mobility.Free:
                DragSelectedBlock();
                break;
            case Block.Mobility.RotateOnly:
                grid.TryRotateBlock(SelectedBlock, true);
                break;
            case Block.Mobility.SlideOnly:
                grid.TrySlideBlock(SelectedBlock);
                break;
            case Block.Mobility.Fixed:
                // Fixed blocks cannot be moved
                break;
        }
    }

    public bool IsBlockInSlot(Block block) => slotManager.IsBlockInSlot(block);

    public bool IsBlockSelected() => SelectedBlock != null;

    public bool TryRotateSelectedBlock(bool clockwise) => grid.TryRotateBlock(SelectedBlock, clockwise);

    public bool TryFlipSelectedBlock() => grid.TryFlipBlock(SelectedBlock);

    public void SelectBlock(Block block, BlockSegment segment)
    {
        if (!baseColorPicked)
        {
            defaultBlockColor = block.Color;
            baseColorPicked = true;
        }

        SelectedBlock = block;
        selectedSegment = segment;
        block.Color = highlightColor;
    }

    public void UnselectBlock()
    {
        if (SelectedBlock == null) return;

        SelectedBlock.Color = defaultBlockColor;
        SelectedBlock = null;
        selectedSegment = null;
    }

    public bool TryRemoveSelectedBlock()
    {
        if (!slotManager.TryAsignAvailableSlot(SelectedBlock))
            return false;

        IsDragging = false;
        grid.RemoveBlock(SelectedBlock);
        return true;
    }

    public void DragSelectedBlock()
    {
        hasSavedGridPosition = grid.ContainsBlock(SelectedBlock);
        if (hasSavedGridPosition)
        {
            Vector2Int? tile = grid.GetTileOfBlock(selectedSegment);
            hasSavedGridPosition = tile.HasValue;
            if (hasSavedGridPosition) savedPivotTile = tile.Value;
        }

        grid.RemoveBlock(SelectedBlock);
        slotManager.FreeSlot(SelectedBlock);
        IsDragging = true;
    }

    public void StartDragSelectedBlock()
    {
        if (SelectedBlock == null || IsDragging) return;

        if (SelectedBlock.MobilityType == Block.Mobility.Free)
        {
            DragSelectedBlock();
        }
    }

    // Used for click-to-move: if placement fails, restores to original grid position.
    // Returns true if block was placed at the intended position.
    public bool DropDraggedBlock(Vector2 worldPosition)
    {
        if (SelectedBlock == null) return false;

        Vector2Int tile = grid.WorldToTile(worldPosition);
        bool placed = grid.TryPlaceBlock(SelectedBlock, tile, selectedSegment);

        if (!placed)
        {
            bool restored = hasSavedGridPosition && grid.TryPlaceBlock(SelectedBlock, savedPivotTile, selectedSegment);
            if (!restored) slotManager.TryAsignAvailableSlot(SelectedBlock);
        }

        if (IsDragging) IsDragging = false;
        return placed;
    }

    // Used for hold-drag: if placement fails, sends block to slot (outside grid).
    // Returns true if block was placed at the intended position.
    public bool DropDraggedBlockToSlot(Vector2 worldPosition)
    {
        if (SelectedBlock == null) return false;

        Vector2Int tile = grid.WorldToTile(worldPosition);
        bool placed = grid.TryPlaceBlock(SelectedBlock, tile, selectedSegment);

        if (!placed) slotManager.TryAsignAvailableSlot(SelectedBlock);

        if (IsDragging) IsDragging = false;
        return placed;
    }

    public void MoveDraggedBlock(Vector2 targetPosition)
    {
        if (!IsDragging) return;

        Vector2 offset = SelectedBlock.transform.position - selectedSegment.transform.position;
        targetPosition += offset;
        SelectedBlock.Position2D = targetPosition;
    }
}
