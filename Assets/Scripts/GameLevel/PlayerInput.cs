using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GameActions))]
public class PlayerInput : MonoBehaviour
{
    [field: SerializeField] public LayerMask BlockLayer { get; private set; }
    [field: SerializeField] float RayDistance { get; set; } = 100f;
    [field: SerializeField] float DragThresholdPixels { get; set; } = 10f;
    [field: SerializeField] float FlipHoldTime { get; set; } = 0.4f;

    GameActions actions;
    Camera mainCamera;
    Vector2 pressStartPosition;
    readonly Plane interactionPlane = new(Vector3.forward, Vector3.zero);
    bool isHoldDragging, pressedOnBlock, reselecting, flipTriggered;
    float pressStartTime;

    void Awake()
    {
        actions = GetComponent<GameActions>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame) OnPointerPressed();
        if (Pointer.current.press.isPressed) OnPointerHeld();
        if (Pointer.current.press.wasReleasedThisFrame) OnPointerReleased();
    }

    void OnPointerPressed()
    {
        pressStartPosition = Pointer.current.position.ReadValue();
        pressStartTime = Time.time;
        isHoldDragging = false;
        pressedOnBlock = false;
        reselecting = false;
        flipTriggered = false;

        if (IsPointerOverUI()) return;

        if (!TryRaycastToBlock(out BlockSegment segment)) return;

        Block block = segment.GetParent();
        if (block == null || block.MobilityType == Block.Mobility.Fixed) return;

        reselecting = actions.SelectedBlock == block;
        actions.SelectBlock(block, segment);
        pressedOnBlock = true;
    }

    void OnPointerHeld()
    {
        if (!pressedOnBlock) return;

        bool dragThresholdExceeded = (Pointer.current.position.ReadValue() - pressStartPosition).magnitude >= DragThresholdPixels;

        if (dragThresholdExceeded)
        {
            flipTriggered = true;
            if (!isHoldDragging)
            {
                actions.StartDragSelectedBlock();
                isHoldDragging = actions.IsDragging;
            }
        }
        else if (!flipTriggered && Time.time - pressStartTime >= FlipHoldTime)
        {
            actions.TryFlipSelectedBlock();
            flipTriggered = true;
        }

        if (actions.IsDragging && TryGetWorldPosition(out Vector3 pos))
            actions.MoveDraggedBlock(pos);
    }

    void OnPointerReleased()
    {
        if (isHoldDragging)
        {
            bool placed = TryGetWorldPosition(out Vector3 pos) && actions.TryDropDraggedBlock(pos, true);
            if (!placed) actions.UnselectBlock();
            isHoldDragging = false;
            return;
        }

        if (IsPointerOverUI()) return;

        if (!pressedOnBlock)
        {
            actions.UnselectBlock();
            return;
        }

        if (!flipTriggered && reselecting)
            actions.TryRotateSelectedBlock(clockwise: true);
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
