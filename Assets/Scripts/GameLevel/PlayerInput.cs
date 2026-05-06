using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [field: SerializeField] public LayerMask BlockLayer { get; private set; }
    [field: SerializeField] float RayDistance { get; set; } = 100f;
    [field: SerializeField] float DragThresholdPixels { get; set; } = 10f;
    [field: SerializeField] float FlipHoldTime { get; set; } = 0.4f;

    Camera mainCamera;
    readonly Plane interactionPlane = new(Vector3.forward, Vector3.zero);
    SlotManager slotManager;
    BoardGrid grid;
    Vector2 pressStartPosition;
    BlockSegment activeSegment;
    Vector2Int savedPivotTile;
    bool isDragging, flipTriggered, hasSavedGridPosition;
    float pressStartTime;

    void Awake()
    {
        mainCamera = Camera.main;
        LevelManager.LevelLoaded += OnLevelLoaded;
    }

    void OnLevelLoaded(Level level)
    {
        slotManager = level.Slots;
        grid = level.Grid;
    }

    void Update()
    {
        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame)  OnPointerPressed();
        if (Pointer.current.press.isPressed)            OnPointerHeld();
        if (Pointer.current.press.wasReleasedThisFrame) OnPointerReleased();
    }

    void OnPointerPressed()
    {
        pressStartPosition = Pointer.current.position.ReadValue();
        pressStartTime = Time.time;
        flipTriggered = false;
        activeSegment = null;

        if (IsPointerOverUI()) return;
        if (!TryRaycastToBlock(out BlockSegment segment)) return;

        Block block = segment.GetParent();
        if (block == null || block.MobilityType == Block.Mobility.Fixed) return;

        activeSegment = segment;
    }

    void OnPointerHeld()
    {
        if (activeSegment == null) return;

        bool dragThresholdExceeded = (Pointer.current.position.ReadValue() - pressStartPosition).magnitude >= DragThresholdPixels;

        if (dragThresholdExceeded)
        {
            flipTriggered = true;
            if (!isDragging) StartDrag();
        }
        else if (!flipTriggered && Time.time - pressStartTime >= FlipHoldTime)
        {
            grid.TryFlipBlock(activeSegment.GetParent(), activeSegment);
            flipTriggered = true;
        }

        if (isDragging && TryGetWorldPosition(out Vector3 pos))
            MoveDrag(pos);
    }

    void OnPointerReleased()
    {
        if (isDragging)
        {
            TryGetWorldPosition(out Vector3 pos);
            DropDrag(pos, moveToSlotOnFailure: true);
            return;
        }

        if (IsPointerOverUI()) return;

        if (activeSegment != null && !flipTriggered)
            grid.TryRotateBlock(activeSegment.GetParent(), activeSegment, clockwise: true);
    }

    void StartDrag()
    {
        Block block = activeSegment.GetParent();
        if (block.MobilityType != Block.Mobility.Free) return;

        Vector2Int? savedTile = grid.ContainsBlock(block) ? grid.GetTileOfBlock(block.Pivot) : null;
        hasSavedGridPosition = savedTile.HasValue;
        if (hasSavedGridPosition) savedPivotTile = savedTile.Value;

        grid.RemoveBlock(block);
        slotManager.FreeSlot(block);
        isDragging = true;
    }

    void MoveDrag(Vector2 targetPosition)
    {
        Block block = activeSegment.GetParent();
        Vector2 offset = block.transform.position - activeSegment.transform.position;
        block.Position2D = targetPosition + offset;
    }

    void DropDrag(Vector2 worldPosition, bool moveToSlotOnFailure = false)
    {
        Block block = activeSegment.GetParent();
        Vector2Int tile = grid.WorldToTile(worldPosition);
        bool placed = grid.TryPlaceBlock(block, tile, activeSegment, tryAllPivots: true);

        if (!placed)
        {
            bool restored = !moveToSlotOnFailure && hasSavedGridPosition && grid.TryPlaceBlock(block, savedPivotTile, block.Pivot);
            if (!restored) slotManager.TryAsignAvailableSlot(block);
        }

        isDragging = false;
        activeSegment = null;
    }

    bool IsPointerOverUI() => Pointer.current is Touchscreen
        ? EventSystem.current.IsPointerOverGameObject(Pointer.current.deviceId)
        : EventSystem.current.IsPointerOverGameObject();

    Ray GetPointerRay() => mainCamera.ScreenPointToRay(Pointer.current.position.ReadValue());

    bool TryGetWorldPosition(out Vector3 worldPos)
    {
        Ray ray = GetPointerRay();
        if (interactionPlane.Raycast(ray, out float distance))
        {
            worldPos = ray.GetPoint(distance);
            return true;
        }

        worldPos = Vector3.zero;
        return false;
    }

    bool TryRaycastToBlock(out BlockSegment segment)
    {
        segment = null;
        if (!Physics.Raycast(GetPointerRay(), out RaycastHit hit, RayDistance, BlockLayer)) return false;

        segment = hit.collider.GetComponentInParent<BlockSegment>();
        return segment != null;
    }
}
