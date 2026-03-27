using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GameActions))]
public class PlayerInput : MonoBehaviour
{
    [field: SerializeField] public LayerMask BlockLayer { get; private set; }
    [field: SerializeField] float rayDistance = 100f;

    GameActions actions;
    Camera mainCamera;
    readonly Plane interactionPlane = new(Vector3.forward, Vector3.zero);

    void Awake()
    {
        actions = GetComponent<GameActions>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        bool scrollUp = scrollY > 0f;
        bool scrollDown = scrollY < 0f;

        if (Mouse.current.leftButton.wasPressedThisFrame) HandleLeftClick();
        else if (Mouse.current.rightButton.wasPressedThisFrame) actions.FlipSelectedBlock();

        if (scrollUp || scrollDown) actions.RotateSelectedBlock(scrollUp);
        if (actions.IsDragging) UpdateDragging();
    }

    Ray GetMouseRay() => mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

    bool TryGetWorldPosition(out Vector3 worldPos)
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

    void HandleLeftClick()
    {
        if (actions.IsDragging)
        {
            if (TryGetWorldPosition(out Vector3 pos))
                actions.DropSelectedBlock(pos);
        }
        else ThrowClickRaycast();
    }

    void ThrowClickRaycast()
    {
        if (!Physics.Raycast(GetMouseRay(), out RaycastHit hit, rayDistance, BlockLayer)) return;
        if (!hit.collider.TryGetComponent(out BlockSegment segment)) return;
        
        Block block = segment.GetComponentInParent<Block>();
        if (block == null) return;

        actions.TriggerBlockInteraction(block, segment);
    }

    void UpdateDragging()
    {
        if (TryGetWorldPosition(out Vector3 pos)) actions.MoveSelectedBlock(pos);
    }
}