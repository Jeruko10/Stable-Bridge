using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerInput : MonoBehaviour
{
    [Header("Click Interaction")]
    [field: SerializeField] public LayerMask BlockLayer { get; private set; }
    [field: SerializeField] float RayDistance { get; set; } = 100f;
    [field: SerializeField] float DragThresholdPixels { get; set; } = 10f;
    [field: SerializeField] float FlipHoldTime { get; set; } = 0.6f;

    [Header("Block Snapping")]
    [field: SerializeField] int SnapLimitLeft { get; set; } = 2;
    [field: SerializeField] int SnapLimitRight { get; set; } = 2;
    [field: SerializeField] int SnapLimitDown { get; set; } = 2;
    [field: SerializeField] int SnapLimitUp { get; set; } = 2;

    [Header("References")]
    [field: SerializeField] BlockInventory blockInventory;

    const int NoTouch = -1;

    // A finger contact jitters by several pixels during a "still" tap, and on a dense display
    // DragThresholdPixels is a tiny physical distance - so taps get misread as drags. Enforce
    // a physical floor via DPI so a tap stays a tap while a deliberate drag still registers.
    const float MinDragThresholdInches = 0.2f;
    float DragDistanceThreshold => Mathf.Max(DragThresholdPixels, Screen.dpi * MinDragThresholdInches);

    Block ActiveBlock => activeSegment.GetParent();
    BlockSegment activeSegment;
    Camera mainCamera;
    readonly Plane interactionPlane = new(Vector3.forward, Vector3.zero);
    BoardGrid grid;
    Vector2 pressStartPosition;
    Vector2Int savedPivotTile;
    bool isDragging, flipTriggered;
    float pressStartTime;

    // On a touchscreen we lock onto the first finger that pressed and follow only it until it
    // lifts, so a second finger can't hijack the position or fire extra press/release events.
    int activeTouchId = NoTouch;
    Vector2 pointerPosition;

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
        if (!ResolvePointer(out bool pressed, out bool held, out bool released)) return;

        if (pressed) OnPointerPressed();
        if (held) OnPointerHeld();
        if (released) OnPointerReleased();
    }

    // Resolves a single, stable pointer for this frame and caches its position. Touch uses the
    // locked-finger path; mouse and pen use the normal aggregated pointer.
    bool ResolvePointer(out bool pressed, out bool held, out bool released)
    {
        pressed = held = released = false;

        if (Pointer.current is Touchscreen touchscreen)
            return ResolveTouch(touchscreen, out pressed, out held, out released);

        if (Pointer.current == null) return false;

        pointerPosition = Pointer.current.position.ReadValue();
        pressed = Pointer.current.press.wasPressedThisFrame;
        held = Pointer.current.press.isPressed;
        released = Pointer.current.press.wasReleasedThisFrame;
        return true;
    }

    bool ResolveTouch(Touchscreen touchscreen, out bool pressed, out bool held, out bool released)
    {
        pressed = held = released = false;

        if (activeTouchId != NoTouch)
        {
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (touch.touchId.ReadValue() != activeTouchId) continue;

                pointerPosition = touch.position.ReadValue();
                if (touch.press.isPressed) held = true;
                else { released = true; activeTouchId = NoTouch; }
                return true;
            }

            // The tracked finger disappeared without a release phase - end the interaction.
            released = true;
            activeTouchId = NoTouch;
            return true;
        }

        foreach (TouchControl touch in touchscreen.touches)
        {
            if (!touch.press.wasPressedThisFrame) continue;

            activeTouchId = touch.touchId.ReadValue();
            pointerPosition = touch.position.ReadValue();
            pressed = held = true;
            return true;
        }

        return false;
    }

    void OnPointerPressed()
    {
        pressStartPosition = pointerPosition;
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

        bool dragThresholdExceeded = (pointerPosition - pressStartPosition).magnitude >= DragDistanceThreshold;

        if (dragThresholdExceeded)
        {
            flipTriggered = true;
            if (!isDragging) StartDrag();
        }
        else if (!flipTriggered && Time.time - pressStartTime >= FlipHoldTime)
        {
            grid.TryFlipBlock(ActiveBlock, activeSegment);
            flipTriggered = true;
            DataCollectionManager.Instance?.RecordMove();
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
        {
            grid.TryRotateBlock(ActiveBlock, activeSegment, clockwise: true);
            DataCollectionManager.Instance?.RecordMove();
        }
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
        bool outsideGrid = !grid.IsValidTile(tile);
        bool withinSnapLimit = tile.x >= grid.MinTile.x - SnapLimitLeft && tile.x < grid.MinTile.x + grid.Size.x + SnapLimitRight &&
                               tile.y >= grid.MinTile.y - SnapLimitDown && tile.y < grid.MinTile.y + grid.Size.y + SnapLimitUp;
        bool placed = !outsideGrid
            ? grid.TryPlaceBlock(ActiveBlock, tile, activeSegment, tryAllPivots: true)
            : withinSnapLimit && grid.TryPlaceBlockClosest(ActiveBlock, tile, activeSegment);

        if (!placed)
        {
            bool restored = !moveToSlotOnFailure && grid.TryPlaceBlock(ActiveBlock, savedPivotTile, ActiveBlock.Pivot);
            if (!restored) blockInventory.AddBlock(ActiveBlock);
            else ActiveBlock.SnapToTarget();
        }
        else
        {
            // Settle the block on its tile immediately so a follow-up tap rotates around the
            // real position instead of the still-gliding one.
            ActiveBlock.SnapToTarget();
            AudioManager.Play(AudioManager.Instance.GridSnap);
            DataCollectionManager.Instance?.RecordMove();
        }

        isDragging = false;
        activeSegment = null;
    }

    public void BeginDragFromInventory(Block block)
    {
        blockInventory.RemoveBlock(block);

        // A drag started from a UI slot, outside the normal press flow - lock onto the finger
        // that grabbed it so it's followed and other fingers stay ignored.
        LockToCurrentTouch();

        if (TryGetWorldPosition(out Vector3 worldPos))
        {
            // Teleport to cursor immediately so MoveDrag offset is correct from the first frame
            block.transform.position = new Vector3(worldPos.x, worldPos.y, block.transform.position.z);
            block.Position2D = worldPos;
        }

        activeSegment = block.Pivot;
        savedPivotTile = default;
        pressStartPosition = pointerPosition;
        flipTriggered = true;
        isDragging = true;
    }

    void LockToCurrentTouch()
    {
        if (Pointer.current is Touchscreen touchscreen)
        {
            foreach (TouchControl touch in touchscreen.touches)
                if (touch.press.isPressed)
                {
                    activeTouchId = touch.touchId.ReadValue();
                    pointerPosition = touch.position.ReadValue();
                    return;
                }
        }
        else if (Pointer.current != null)
            pointerPosition = Pointer.current.position.ReadValue();
    }

    bool IsPointerOverUI() => Pointer.current is Touchscreen
        ? EventSystem.current.IsPointerOverGameObject(Pointer.current.deviceId)
        : EventSystem.current.IsPointerOverGameObject();

    Ray GetPointerRay() => mainCamera.ScreenPointToRay(pointerPosition);

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
