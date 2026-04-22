using System;
using UnityEngine;

public class GameActions : MonoBehaviour
{
    [SerializeField] Color highlightColor;

    public bool IsDragging { get; private set; }

    Color defaultBlockColor;
    Block selectedBlock;
    BlockSegment selectedSegment;
    SlotManager slotManager;
    BoardGrid grid;
    bool baseColorPicked = false;

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
        grid.RemoveBlock(selectedBlock);
        slotManager.FreeSlot(selectedBlock);
        IsDragging = true;
    }

    public void DropDraggedBlock(Vector2 worldPosition)
    {
        if (!IsDragging) return;

        Vector2Int tile = grid.WorldToTile(worldPosition);

        if (!grid.TryPlaceBlock(selectedBlock, tile, selectedSegment))
            slotManager.TryAsignAvailableSlot(selectedBlock);
        
        IsDragging = false;
    }

    public void MoveDraggedBlock(Vector2 targetPosition)
    {
        if (!IsDragging) return;
        
        Vector2 offset = selectedBlock.transform.position - selectedSegment.transform.position;
        targetPosition += offset;
        selectedBlock.Position2D = targetPosition;
    }
}