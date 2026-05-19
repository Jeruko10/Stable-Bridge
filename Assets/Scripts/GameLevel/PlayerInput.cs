using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [field: SerializeField] public LayerMask BlockLayer { get; private set; }
    [field: SerializeField] float RayDistance { get; set; } = 100f;
    [field: SerializeField] float DragThresholdPixels { get; set; } = 10f;
    [field: SerializeField] float FlipHoldTime { get; set; } = 0.4f;
    [field: SerializeField] BlockInventory blockInventory;

    Block ActiveBlock => activeSegment.GetParent();
    BlockSegment activeSegment;
    Camera mainCamera;
    readonly Plane interactionPlane = new(Vector3.forward, Vector3.zero);
    BoardGrid grid;
    Vector2 pressStartPosition;
    Vector2Int savedPivotTile;
    bool isDragging, flipTriggered;
    float pressStartTime;

    void Awake()
    {
        mainCamera = Camera.main;
        LevelManager.LevelLoaded += OnLevelLoaded;
    }

    void OnLevelLoaded(Level level)
    {
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
        AudioManager.Play(AudioManager.Instance.Blocks);
        if (block == null || block.MobilityType == Block.Mobility.Fixed || block.MobilityType == Block.Mobility.Ground) return;

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
            grid.TryFlipBlock(ActiveBlock, activeSegment);
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
            grid.TryRotateBlock(ActiveBlock, activeSegment, clockwise: true);
    }

    void StartDrag()
    {
        if (ActiveBlock.MobilityType != Block.Mobility.Free) return;

        Vector2Int? savedTile = grid.GetTileOfBlock(ActiveBlock.Pivot);
        if (savedTile.HasValue) savedPivotTile = savedTile.Value;

        AudioManager.Play(AudioManager.Instance.GridSnap);
        grid.RemoveBlock(ActiveBlock);
        isDragging = true;
    }

    void MoveDrag(Vector2 targetPosition)
    {
        Block block = ActiveBlock;
        Vector2 offset = block.transform.position - activeSegment.transform.position;
        block.Position2D = targetPosition + offset;
    }

    void DropDrag(Vector2 worldPosition, bool moveToSlotOnFailure = false)
    {
        Vector2Int tile = grid.WorldToTile(worldPosition);
        bool placed = grid.TryPlaceBlock(ActiveBlock, tile, activeSegment, tryAllPivots: true);

        if (!placed)
        {
            bool restored = !moveToSlotOnFailure && grid.TryPlaceBlock(ActiveBlock, savedPivotTile, ActiveBlock.Pivot);
            if (!restored) blockInventory.AddBlock(ActiveBlock);
        }
        else AudioManager.Play(AudioManager.Instance.GridSnap);

        isDragging = false;
        activeSegment = null;
    }

    public void BeginDragFromInventory(Block block)
    {
        blockInventory.RemoveBlock(block);

        if (TryGetWorldPosition(out Vector3 worldPos))
        {
            // Teleport to cursor immediately so MoveDrag offset is correct from the first frame
            block.transform.position = new Vector3(worldPos.x, worldPos.y, block.transform.position.z);
            block.Position2D = worldPos;
        }

        activeSegment = block.Pivot;
        savedPivotTile = default;
        pressStartPosition = Pointer.current.position.ReadValue();
        flipTriggered = true;
        isDragging = true;
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
