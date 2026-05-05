using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GameActions))]
public class PlayerInput : MonoBehaviour
{
    [field: SerializeField] public LayerMask BlockLayer { get; private set; }
    [field: SerializeField] float RayDistance { get; set; } = 100f;
    [field: SerializeField] UserInterfaceManager UiManager { get; set; }
    [field: SerializeField] float DragThresholdPixels { get; set; } = 10f;

    GameActions actions;
    Camera mainCamera;
    readonly Plane interactionPlane = new(Vector3.forward, Vector3.zero);

    bool isHoldDragging;
    bool pressedOnBlock;
    Vector2 pressStartPosition;

    void Awake()
    {
        actions = GetComponent<GameActions>();
        mainCamera = Camera.main;

        UiManager.RotateButton.clicked += OnRotateButtonClicked;
        UiManager.FlipButton.clicked += OnFlipButtonClicked;
    }

    void Update()
    {
        HandleKeyboardInputs();
        HandlePointerInputs();
    }

    void HandleKeyboardInputs()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame)
            LevelManager.Current.ExitEditMode();

        if (Keyboard.current.sKey.wasPressedThisFrame)
            LevelLayout.FromLevel(LevelManager.Current, new(0, 3), new(4, 3)).SaveAsAsset();
    }

    void HandlePointerInputs()
    {
        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame)  OnPointerPressed();
        if (Pointer.current.press.isPressed)            OnPointerHeld();
        if (Pointer.current.press.wasReleasedThisFrame) OnPointerReleased();
    }

    void OnPointerPressed()
    {
        pressStartPosition = Pointer.current.position.ReadValue();
        isHoldDragging = false;
        pressedOnBlock = false;

        if (IsPointerOverUI()) return;

        if (actions.IsDragging)
        {
            if (TryRaycastToBlock(out BlockSegment segment) && segment.GetParent() == actions.SelectedBlock)
            {
                pressedOnBlock = true;
                return;
            }

            if (TryGetWorldPosition(out Vector3 dropPos) && actions.DropDraggedBlock(dropPos))
            {
                actions.StartDragSelectedBlock();
                return;
            }

            Block hitBlock = segment != null ? segment.GetParent() : null;
            if (hitBlock != null && hitBlock.MobilityType != Block.Mobility.Fixed)
            {
                actions.SelectBlock(hitBlock, segment);
                pressedOnBlock = true;
            }
            else
            {
                actions.UnselectBlock();
            }
            return;
        }

        if (!TryRaycastToBlock(out BlockSegment hit)) return;

        Block block = hit.GetParent();
        if (block == null || block.MobilityType == Block.Mobility.Fixed) return;

        actions.SelectBlock(block, hit);
        pressedOnBlock = true;
    }

    void OnPointerHeld()
    {
        if (!pressedOnBlock || !actions.IsBlockSelected()) return;
        if (actions.SelectedBlock.MobilityType != Block.Mobility.Free) return;

        if (!isHoldDragging)
        {
            if ((Pointer.current.position.ReadValue() - pressStartPosition).magnitude < DragThresholdPixels) return;

            actions.StartDragSelectedBlock();
            isHoldDragging = true;
        }

        if (actions.IsDragging && TryGetWorldPosition(out Vector3 pos))
            actions.MoveDraggedBlock(pos);
    }

    void OnPointerReleased()
    {
        if (isHoldDragging)
        {
            bool placed = TryGetWorldPosition(out Vector3 pos) && actions.DropDraggedBlockToSlot(pos);
            if (!placed) actions.UnselectBlock();
            isHoldDragging = false;
            return;
        }

        if (actions.IsDragging) return;

        if (IsPointerOverUI() || !actions.IsBlockSelected()) return;

        if (!pressedOnBlock)
        {
            actions.StartDragSelectedBlock();
            if (TryGetWorldPosition(out Vector3 pos) && actions.DropDraggedBlock(pos))
            {
                actions.StartDragSelectedBlock();
                return;
            }
            actions.UnselectBlock();
            return;
        }

        if (actions.SelectedBlock.MobilityType == Block.Mobility.Free)
        {
            actions.StartDragSelectedBlock();
            return;
        }

        actions.TriggerSelectedBlockInteraction();
    }

    void OnFlipButtonClicked()
    {
        if (actions.IsBlockSelected()) actions.TryFlipSelectedBlock();
    }

    void OnRotateButtonClicked()
    {
        if (actions.IsBlockSelected()) actions.TryRotateSelectedBlock(clockwise: true);
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
