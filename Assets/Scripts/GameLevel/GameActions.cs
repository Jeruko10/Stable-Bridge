using System;
using UnityEngine;

public class GameActions : MonoBehaviour
{
    [SerializeField] Color highlightColor;

    public bool IsDragging { get; private set; }
    public Block SelectedBlockRef => selectedBlock;

    Color defaultBlockColor;
    Block selectedBlock;
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

        switch (selectedBlock.MobilityType)
        {
            case Block.Mobility.Free:
                DragSelectedBlock();
                break;
            case Block.Mobility.RotateOnly:
                grid.TryRotateBlock(selectedBlock, true);
                break;
            case Block.Mobility.SlideOnly:
                grid.TrySlideBlock(selectedBlock);
                break;
            case Block.Mobility.Fixed:
                // Fixed blocks cannot be moved
                break;
        }
    }

    public bool IsBlockInSlot(Block block) => slotManager.IsBlockInSlot(block);

    public bool IsBlockSelected() => selectedBlock != null;

    public bool TryRotateSelectedBlock(bool clockwise) => grid.TryRotateBlock(selectedBlock, clockwise);

    public bool TryFlipSelectedBlock() => grid.TryFlipBlock(selectedBlock);

    public void SelectBlock(Block block, BlockSegment segment)
    {
        if (!baseColorPicked)
        {
            defaultBlockColor = block.Color;
            baseColorPicked = true;
        }

        selectedBlock = block;
        selectedSegment = segment;
        block.Color = highlightColor;
    }

    public void UnselectBlock()
    {
        if (selectedBlock == null) return;

        selectedBlock.Color = defaultBlockColor;
        selectedBlock = null;
        selectedSegment = null;
    }

    public bool TryRemoveSelectedBlock()
    {
        if (!slotManager.TryAsignAvailableSlot(selectedBlock))
            return false;

        IsDragging = false;
        grid.RemoveBlock(selectedBlock);
        return true;
    }

    public void DragSelectedBlock()
    {
        hasSavedGridPosition = grid.ContainsBlock(selectedBlock);
        if (hasSavedGridPosition)
        {
            Vector2Int? tile = grid.GetTileOfBlock(selectedSegment);
            hasSavedGridPosition = tile.HasValue;
            if (hasSavedGridPosition) savedPivotTile = tile.Value;
        }

        grid.RemoveBlock(selectedBlock);
        slotManager.FreeSlot(selectedBlock);
        IsDragging = true;
    }

    public void StartDragSelectedBlock()
    {
        if (selectedBlock == null || IsDragging) return;

        if (selectedBlock.MobilityType == Block.Mobility.Free)
        {
            DragSelectedBlock();
        }
    }

    // Used for click-to-move: if placement fails, restores to original grid position.
    // Returns true if block was placed at the intended position.
    public bool DropDraggedBlock(Vector2 worldPosition)
    {
        if (selectedBlock == null) return false;

        Vector2Int tile = grid.WorldToTile(worldPosition);
        bool placed = grid.TryPlaceBlock(selectedBlock, tile, selectedSegment);

        if (!placed)
        {
            bool restored = hasSavedGridPosition && grid.TryPlaceBlock(selectedBlock, savedPivotTile, selectedSegment);
            if (!restored) slotManager.TryAsignAvailableSlot(selectedBlock);
        }

        if (IsDragging) IsDragging = false;
        return placed;
    }

    // Used for hold-drag: if placement fails, sends block to slot (outside grid).
    // Returns true if block was placed at the intended position.
    public bool DropDraggedBlockToSlot(Vector2 worldPosition)
    {
        if (selectedBlock == null) return false;

        Vector2Int tile = grid.WorldToTile(worldPosition);
        bool placed = grid.TryPlaceBlock(selectedBlock, tile, selectedSegment);

        if (!placed) slotManager.TryAsignAvailableSlot(selectedBlock);

        if (IsDragging) IsDragging = false;
        return placed;
    }

    public void MoveDraggedBlock(Vector2 targetPosition)
    {
        if (!IsDragging) return;

        Vector2 offset = selectedBlock.transform.position - selectedSegment.transform.position;
        targetPosition += offset;
        selectedBlock.Position2D = targetPosition;
    }
}
