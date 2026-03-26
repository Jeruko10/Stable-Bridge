using UnityEngine;

[RequireComponent(typeof(SlotManager))]
[RequireComponent(typeof(BoardGrid))]
public class GameActions : MonoBehaviour
{
    public bool IsDragging => draggedBlock != null;

    Block draggedBlock;
    BlockSegment grabbedSegment;
    SlotManager outsideSlots;
    BoardGrid board;

    void Awake()
    {
        outsideSlots = GetComponent<SlotManager>();
        board = GetComponent<BoardGrid>();
    }
    
    public void SelectBlock(Block block, BlockSegment segment)
    {
        draggedBlock = block;
        grabbedSegment = segment;
        board.RemoveBlock(draggedBlock);
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

        if (board.TryPlaceBlock(draggedBlock, targetTile, grabbedSegment))
        {
            // Snapping block into the grid
            board.TryPlaceBlock(draggedBlock, targetTile, grabbedSegment);
        }
        else
        {
            // Returning block to an outside slot
            Vector3? slot = outsideSlots.GetAvailableSlot();
            if (slot.HasValue) draggedBlock.transform.position = slot.Value;
            else
            {
                draggedBlock.transform.position = grabbedSegment.transform.position;
            }
        }
        
        draggedBlock = null;
        grabbedSegment = null;
    }
}