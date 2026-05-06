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

    public void ExitEditorMode() => LevelManager.Current.ExitEditMode();

    public void SaveCurrentLevel() => LevelLayout.FromLevel(LevelManager.Current, new(0, 3), new(4, 3)).SaveAsAsset();

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
                break;
        }
    }

    public bool IsBlockInSlot(Block block) => slotManager.IsBlockInSlot(block);

    public bool IsBlockSelected() => SelectedBlock != null;

    public bool TryRotateSelectedBlock(bool clockwise)
    {
        if (IsDragging && hasSavedGridPosition && grid.TryPlaceBlock(SelectedBlock, savedPivotTile, SelectedBlock.Pivot))
        {
            bool rotated = grid.TryRotateBlock(SelectedBlock, clockwise);
            RefreshSavedPivotAndLift();
            return rotated;
        }
        return grid.TryRotateBlock(SelectedBlock, clockwise);
    }

    public bool TryFlipSelectedBlock()
    {
        if (IsDragging && hasSavedGridPosition && grid.TryPlaceBlock(SelectedBlock, savedPivotTile, SelectedBlock.Pivot))
        {
            bool flipped = grid.TryFlipBlock(SelectedBlock);
            RefreshSavedPivotAndLift();
            return flipped;
        }
        return grid.TryFlipBlock(SelectedBlock);
    }

    public void SelectBlock(Block block, BlockSegment segment)
    {
        UnselectBlock();

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
            Vector2Int? tile = grid.GetTileOfBlock(SelectedBlock.Pivot);
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
            DragSelectedBlock();
    }

    public bool TryDropDraggedBlock(Vector2 worldPosition, bool moveToSlotOnFailure = false)
    {
        if (SelectedBlock == null) return false;

        Vector2Int tile = grid.WorldToTile(worldPosition);
        bool tryAllPivots = SelectedBlock.MobilityType == Block.Mobility.Free;
        bool placed = grid.TryPlaceBlock(SelectedBlock, tile, selectedSegment, tryAllPivots);

        if (!placed)
        {
            bool restored = !moveToSlotOnFailure && hasSavedGridPosition && grid.TryPlaceBlock(SelectedBlock, savedPivotTile, SelectedBlock.Pivot);
            if (!restored) slotManager.TryAsignAvailableSlot(SelectedBlock);
        }

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

    void RefreshSavedPivotAndLift()
    {
        Vector2Int? tile = grid.GetTileOfBlock(SelectedBlock.Pivot);
        if (tile.HasValue) savedPivotTile = tile.Value;
        grid.RemoveBlock(SelectedBlock);
    }
}
