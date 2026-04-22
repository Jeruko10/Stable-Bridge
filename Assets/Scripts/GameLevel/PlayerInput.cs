using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GameActions))]
public class PlayerInput : MonoBehaviour
{
    [field: SerializeField] public LayerMask BlockLayer { get; private set; }
    [field: SerializeField] float rayDistance = 100f;
    [field: SerializeField] UserInterfaceManager uiManager;

    GameActions actions;
    Camera mainCamera;
    readonly Plane interactionPlane = new(Vector3.forward, Vector3.zero);

    void Awake()
    {
        actions = GetComponent<GameActions>();
        mainCamera = Camera.main;

        uiManager.RotateButton.clicked += OnRotateButtonClicked;
        uiManager.FlipButton.clicked += OnFlipButtonClicked;
    }

    void Update()
    {
        HandleKeyboardInputs();
        HandleMouseInputs();
    }

    void HandleKeyboardInputs()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame)
            LevelManager.Current.ExitEditMode();

        if (Keyboard.current.sKey.wasPressedThisFrame)
            LevelLayout.FromLevel(LevelManager.Current, new(0, 3), new(4, 3)).SaveAsAsset();
    }

    void HandleMouseInputs()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) HandleLeftClick();
        else if (Mouse.current.rightButton.wasPressedThisFrame) HandleRightClick();

        if (actions.IsDragging) UpdateDragging();
    }

    void HandleLeftClick()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (actions.IsDragging)
        {
            if (TryGetMouseWorldPosition(out Vector3 pos))
                actions.DropDraggedBlock(pos);
        }
        else
        {
            if (!TryRaycastToBlock(out BlockSegment segment)) return;

            Block block = segment.GetParent();

            if (block == null || block.MobilityType == Block.Mobility.Fixed) return;

            actions.SelectBlock(block, segment);
            actions.TriggerSelectedBlockInteraction();
        }
    }

    void HandleRightClick()
    {
        if (!actions.IsDragging)
        {
            if (!TryRaycastToBlock(out BlockSegment segment)) return;
            
            Block block = segment.GetParent();

            if (block == null || actions.IsBlockInSlot(block) || block.MobilityType != Block.Mobility.Free) return;

            actions.SelectBlock(block, segment);
        }

        actions.TryRemoveSelectedBlock();
        actions.UnselectBlock();
    }

    void OnFlipButtonClicked()
    {
        if (actions.IsBlockSelected())
            actions.TryFlipSelectedBlock();
    }

    void OnRotateButtonClicked()
    {
        if (actions.IsBlockSelected())
            actions.TryRotateSelectedBlock(clockwise: true);
    }

    Ray GetMouseRay() => mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

    bool TryGetMouseWorldPosition(out Vector3 worldPos)
    {
        Ray ray = GetMouseRay();
        if (interactionPlane.Raycast(ray, out float distance))
        {
            worldPos = ray.GetPoint(distance);
            return true;
        }
        
        worldPos = Vector3.zero;
        return false;
    }

    bool TryRaycastToBlock(out BlockSegment clickedSegment)
    {
        actions.UnselectBlock();
        clickedSegment = null;
        bool debugData = false;

        if (!Physics.Raycast(GetMouseRay(), out RaycastHit hit, rayDistance, BlockLayer))
        {
            if (debugData) Debug.Log("Click ray missed anything");
            return false;
        }

        BlockSegment segment = hit.collider.GetComponentInParent<BlockSegment>();
        if (segment == null)
        {
            if (debugData) Debug.Log($"Hit {hit.collider.name} but no BlockSegment");
            return false;
        }
        
        if (debugData) Debug.Log($"Hit block {segment.GetParent().name} segment {segment.name}");

        clickedSegment = segment;
        return true;
    }

    void UpdateDragging()
    {
        if (TryGetMouseWorldPosition(out Vector3 pos)) actions.MoveDraggedBlock(pos);
    }
}