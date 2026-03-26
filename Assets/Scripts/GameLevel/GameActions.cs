using UnityEngine;

[RequireComponent(typeof(SlotManager))]
[RequireComponent(typeof(BoardGrid))]
public class GameActions : MonoBehaviour
{
    public bool IsDragging => draggedBlock != null;

    Block draggedBlock;
    BlockSegment grabbedSegment;
    SlotManager slotManager;
    BoardGrid board;

    void Awake()
    {
        slotManager = GetComponent<SlotManager>();
        board = GetComponent<BoardGrid>();
    }
    
    public void SelectBlock(Block block, BlockSegment segment)
    {
        draggedBlock = block;
        grabbedSegment = segment;
        board.RemoveBlock(block);
        slotManager.FreeSlot(block);
    }

    public void MoveBlock(Vector3 targetPosition)
    {
        if (draggedBlock == null) return;
        
        Vector3 offset = draggedBlock.transform.position - grabbedSegment.transform.position;
        targetPosition += offset;
        targetPosition.z = draggedBlock.transform.position.z;
        draggedBlock.transform.position = targetPosition;
    }

    public bool TryRotateBlock()
    {
        if (draggedBlock == null) return false;

        draggedBlock.Rotate(grabbedSegment);
        return true;
    }

    public void DropBlock(Vector2Int targetTile)
    {
        if (draggedBlock == null) return;

        if (!board.TryPlaceBlock(draggedBlock, targetTile, grabbedSegment))
            slotManager.AsignAvailableSlot(draggedBlock);

        draggedBlock = null;
        grabbedSegment = null;
    }
}