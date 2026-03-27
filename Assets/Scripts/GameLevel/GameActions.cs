using UnityEngine;

public class GameActions : MonoBehaviour
{
    public bool IsDragging => draggedBlock != null;

    Block draggedBlock;
    BlockSegment grabbedSegment;
    SlotManager slotManager;
    BoardGrid board;

    void Start()
    {
        slotManager = LevelManager.Current.Slots;
        board = LevelManager.Current.Grid;
    }

    public void TriggerBlockInteraction(Block block, BlockSegment segment)
    {
        if (IsDragging) return;

        switch (block.MobilityType)
        {
            case Block.Mobility.Free:
                SelectBlock(block, segment);
                break;
            case Block.Mobility.RotateOnly:
                board.TryRotateBlock(block, true);
                break;
            case Block.Mobility.SlideOnly:
                board.TrySlideBlock(block);
                break;
            case Block.Mobility.Fixed:
                // Fixed blocks cannot be moved
                break;
        }
    }

    public void MoveSelectedBlock(Vector2 targetPosition)
    {
        if (!IsDragging) return;
        
        Vector2 offset = draggedBlock.transform.position - grabbedSegment.transform.position;
        targetPosition += offset;
        draggedBlock.Position2D = targetPosition;
    }

    public void RotateSelectedBlock(bool clockwise)
    {
        if (!IsDragging) return;

        draggedBlock.Rotate(grabbedSegment, clockwise);
    }

    public void FlipSelectedBlock()
    {
        if (!IsDragging) return;

        draggedBlock.Mirror();
    }

    public void DropSelectedBlock(Vector2 worldPosition)
    {
        if (!IsDragging) return;

        Vector2Int tile = board.WorldToTile(worldPosition);

        if (!board.TryPlaceBlock(draggedBlock, tile, grabbedSegment))
            slotManager.AsignAvailableSlot(draggedBlock);

        draggedBlock = null;
        grabbedSegment = null;
    }

    void SelectBlock(Block block, BlockSegment segment)
    {
        draggedBlock = block;
        grabbedSegment = segment;
        board.RemoveBlock(block);
        slotManager.FreeSlot(block);
    }
}