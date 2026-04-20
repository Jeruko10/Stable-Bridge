using System;
using UnityEngine;

public class GameActions : MonoBehaviour
{
    public bool IsDragging { get; private set; }

    Block selectedBlock;
    BlockSegment selectedSegment;
    SlotManager slotManager;
    BoardGrid board;

    void Awake()
    {
        LevelManager.LevelLoaded += OnLevelLoaded;
        UnselectBlock();
    }

    void Update()
    {
        
    }

    void OnLevelLoaded(Level level)
    {
        slotManager = level.Slots;
        board = level.Grid;
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
                board.TryRotateBlock(selectedBlock, true);
                break;
            case Block.Mobility.SlideOnly:
                board.TrySlideBlock(selectedBlock);
                break;
            case Block.Mobility.Fixed:
                // Fixed blocks cannot be moved
                break;
        }
    }

    public bool IsBlockSelected() => selectedBlock != null;

    public void RotateSelectedBlock(bool clockwise) => selectedBlock.Rotate(selectedSegment, clockwise);

    public void FlipSelectedBlock() => selectedBlock.Flip();

    public void SelectBlock(Block block, BlockSegment segment)
    {
        selectedBlock = block;
        selectedSegment = segment;
    }

    public void UnselectBlock()
    {
        selectedBlock = null;
        selectedSegment = null;
    }

    public void RemoveSelectedBlock()
    {
        IsDragging = false;
        board.RemoveBlock(selectedBlock);
        slotManager.AsignAvailableSlot(selectedBlock);
    }

    public void DragSelectedBlock()
    {
        board.RemoveBlock(selectedBlock);
        slotManager.FreeSlot(selectedBlock);
        IsDragging = true;
    }

    public void DropDraggedBlock(Vector2 worldPosition)
    {
        if (!IsDragging) return;

        Vector2Int tile = board.WorldToTile(worldPosition);

        if (!board.TryPlaceBlock(selectedBlock, tile, selectedSegment))
            slotManager.AsignAvailableSlot(selectedBlock);
        
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