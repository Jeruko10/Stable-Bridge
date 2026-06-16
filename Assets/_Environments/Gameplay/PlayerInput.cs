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
    [field: SerializeField] float FlipHoldTime { get; set; } = 0.4f;

    [Header("Block Snapping")]
    [field: SerializeField] int SnapLimitLeft { get; set; } = 2;
    [field: SerializeField] int SnapLimitRight { get; set; } = 2;
    [field: SerializeField] int SnapLimitDown { get; set; } = 2;
    [field: SerializeField] int SnapLimitUp { get; set; } = 2;
    
    [Header("References")]
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

    // While a touch is locked in, every other finger touching the screen is ignored
    // until this one is released, so a second finger can't hijack or reset the interaction.
    int activeTouchId = -1;
    Vector2 currentPointerPosition;
    int currentPointerIdForUI;

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
        if (!TryGetPointerState(out bool pressedThisFrame, out bool isPressed, out bool releasedThisFrame)) return;

        if (pressedThisFrame) OnPointerPressed();
        if (isPressed) OnPointerHeld();
        if (releasedThisFrame) OnPointerReleased();
    }

    // Resolves a single, consistent pointer for this frame. On touchscreens, once a finger is
    // locked in via activeTouchId, every other finger is ignored until that one is released -
    // this is what prevents a second finger from resetting or hijacking an in-progress interaction.
    bool TryGetPointerState(out bool pressedThisFrame, out bool isPressed, out bool releasedThisFrame)
    {
        pressedThisFrame = isPressed = releasedThisFrame = false;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            if (activeTouchId != -1)
            {
                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (touch.touchId.ReadValue() != activeTouchId) continue;

                    currentPointerPosition = touch.position.ReadValue();
                    currentPointerIdForUI = activeTouchId;
                    isPressed = touch.press.isPressed;
                    releasedThisFrame = !isPressed;
                    if (releasedThisFrame) activeTouchId = -1;
                    return true;
                }

                // The tracked touch disappeared without going through Ended/Canceled - release it now.
                currentPointerIdForUI = activeTouchId;
                releasedThisFrame = true;
                activeTouchId = -1;
                return true;
            }

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.wasPressedThisFrame) continue;

                activeTouchId = touch.touchId.ReadValue();
                currentPointerPosition = touch.position.ReadValue();
                currentPointerIdForUI = activeTouchId;
                pressedThisFrame = true;
                isPressed = true;
                return true;
            }

            return false;
        }

        Pointer pointer = Pointer.current;
        if (pointer == null) return false;

        currentPointerPosition = pointer.position.ReadValue();
        currentPointerIdForUI = pointer.deviceId;
        pressedThisFrame = pointer.press.wasPressedThisFrame;
        isPressed = pointer.press.isPressed;
        releasedThisFrame = pointer.press.wasReleasedThisFrame;
        return true;
    }

    void OnPointerPressed()
    {
        pressStartPosition = currentPointerPosition;
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

        bool dragThresholdExceeded = (currentPointerPosition - pressStartPosition).magnitude >= DragThresholdPixels;

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
        }
        else
        {
            AudioManager.Play(AudioManager.Instance.GridSnap);
            DataCollectionManager.Instance?.RecordMove();
        }

        isDragging = false;
        activeSegment = null;
    }

    public void BeginDragFromInventory(Block block)
    {
        blockInventory.RemoveBlock(block);

        // Lock onto whichever finger is currently down so Update() doesn't treat it as a
        // fresh press, and so any other finger touching the screen keeps being ignored.
        LockCurrentTouch();

        if (TryGetWorldPosition(out Vector3 worldPos))
        {
            // Teleport to cursor immediately so MoveDrag offset is correct from the first frame
            block.transform.position = new Vector3(worldPos.x, worldPos.y, block.transform.position.z);
            block.Position2D = worldPos;
        }

        activeSegment = block.Pivot;
        savedPivotTile = default;
        pressStartPosition = currentPointerPosition;
        flipTriggered = true;
        isDragging = true;
    }

    void LockCurrentTouch()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            Pointer pointer = Pointer.current;
            if (pointer != null) currentPointerPosition = pointer.position.ReadValue();
            return;
        }

        foreach (TouchControl touch in touchscreen.touches)
        {
            if (!touch.press.isPressed) continue;

            activeTouchId = touch.touchId.ReadValue();
            currentPointerPosition = touch.position.ReadValue();
            currentPointerIdForUI = activeTouchId;
            return;
        }
    }

    bool IsPointerOverUI() => Touchscreen.current != null
        ? EventSystem.current.IsPointerOverGameObject(currentPointerIdForUI)
        : EventSystem.current.IsPointerOverGameObject();

    Ray GetPointerRay() => mainCamera.ScreenPointToRay(currentPointerPosition);

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
