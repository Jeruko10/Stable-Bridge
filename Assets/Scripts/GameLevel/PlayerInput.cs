using System;
using UnityEngine;
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
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) HandleLeftClick();
        else if (Mouse.current.rightButton.wasPressedThisFrame) HandleRightClick();

        if (actions.IsDragging) UpdateDragging();
    }

    void HandleLeftClick()
    {
        Debug.Log(actions.IsDragging);
        if (actions.IsDragging)
        {
            if (TryGetMouseWorldPosition(out Vector3 pos))
                actions.DropDraggedBlock(pos);
        }
        else
        {
            var blockData = ThrowClickRaycast();

            if (blockData.Item1 == null) return;

            actions.SelectBlock(blockData.Item1, blockData.Item2);
            actions.TriggerSelectedBlockInteraction();
        }
    }

    void HandleRightClick()
    {
        if (!actions.IsDragging)
        {
            var blockData = ThrowClickRaycast();
            if (blockData.Item1 == null) return;
            actions.SelectBlock(blockData.Item1, blockData.Item2);
        }

        actions.RemoveSelectedBlock();
        actions.UnselectBlock();
    }

    void OnFlipButtonClicked()
    {
        if (actions.IsBlockSelected())
            actions.FlipSelectedBlock();
    }

    void OnRotateButtonClicked()
    {
        Debug.Log("Rotate button clicked");
        if (actions.IsBlockSelected())
            actions.RotateSelectedBlock(clockwise: true);
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

    (Block, BlockSegment) ThrowClickRaycast()
    {
        actions.UnselectBlock();

        if (!Physics.Raycast(GetMouseRay(), out RaycastHit hit, rayDistance, BlockLayer))
        {
            Debug.Log("Click ray missed anything");
            return (null, null);
        }

        BlockSegment segment = hit.collider.GetComponentInParent<BlockSegment>();
        if (segment == null)
        {
            Debug.Log($"Hit {hit.collider.name} but no BlockSegment");
            return (null, null);
        }
        
        Block block = segment.GetParent();
        Debug.Log($"Hit block {block.name} segment {segment.name}");
        return (block, segment);
    }

    void UpdateDragging()
    {
        if (TryGetMouseWorldPosition(out Vector3 pos)) actions.MoveDraggedBlock(pos);
    }
}