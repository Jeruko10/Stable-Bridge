using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [field: SerializeField] public Camera MainCamera { get; set; }
    [field: SerializeField] public SlotManager SlotManager { get; private set; }
    [field: SerializeField] public BoardGrid Board { get; set; }
    [field: SerializeField] public LayerMask BlockLayer { get; set; }

    Block draggedBlock;
    BlockSegment grabbedSegment;

    const float rayDistance = 10000f;

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (draggedBlock == null) TryGrab();
            else TryDrop();
        }

        if (draggedBlock != null)
        {
            Drag();

            if (Mouse.current.rightButton.wasPressedThisFrame)
                draggedBlock.Rotate();
        }
    }

    void TryGrab()
    {
        Ray ray = MainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, BlockLayer)) return;
        if (!hit.collider.TryGetComponent(out BlockSegment segment)) return;
        
        Block block = segment.GetComponentInParent<Block>();

        if (block == null) return;

        if (block.MobilityType == Block.Mobility.Pinned)
        {
            draggedBlock = block;
            grabbedSegment = segment;
            draggedBlock.Snapped = false;
            Board.RemoveBlock(draggedBlock);
            Debug.Log("This block is Free.");
        }
        else if (block.MobilityType == Block.Mobility.RotateOnly)
        {
            block.Rotate();
            Debug.Log("This block is RotateOnly.");
        }
        else if (block.MobilityType == Block.Mobility.SlideOnly)
        {
            Debug.Log("This block is SlideOnly.");
        }
        else if (block.MobilityType == Block.Mobility.Pinned)
        {
            Debug.Log("This block is pinned and cannot be moved.");
        }
    }

    void Drag()
    {
        Ray ray = MainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new(Vector3.forward, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 targetPos = ray.GetPoint(distance);
            Vector3 offset = draggedBlock.transform.position - grabbedSegment.transform.position;
            targetPos += offset;
            targetPos.z = draggedBlock.transform.position.z;
            draggedBlock.transform.position = targetPos;
        }
    }

    void TryDrop()
    {
        Vector2Int tile = Board.WorldToTile(grabbedSegment.transform.position);

        if (Board.CanPlaceBlock(draggedBlock, tile, grabbedSegment))
        {
            Board.PlaceBlock(draggedBlock, tile, grabbedSegment);
            draggedBlock.Snapped = true;
        }
        else
        {
            Vector3? sloat = SlotManager.GetAvailableSlot();

            if (sloat.HasValue)
            {
                draggedBlock.transform.position = sloat.Value;
            }
            else
            {
                Debug.Log("No available slots! Returning block to original position.");
                draggedBlock.transform.position = grabbedSegment.transform.position;
                draggedBlock.Snapped = true;
            }
        }
        
        draggedBlock = null;
        grabbedSegment = null;
    }
}