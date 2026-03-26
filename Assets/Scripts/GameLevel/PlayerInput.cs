using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GameActions))]
[RequireComponent(typeof(BoardGrid))]
public class PlayerInput : MonoBehaviour
{
    [field: SerializeField] public LayerMask BlockLayer { get; private set; }
    [field: SerializeField] float rayDistance = 100f;

    BoardGrid board;
    GameActions actions;

    void Awake()
    {
        board = GetComponent<BoardGrid>();
        actions = GetComponent<GameActions>();
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) HandleLeftClick();
        else if (Mouse.current.rightButton.wasPressedThisFrame) actions.TryRotateBlock();

        if (actions.IsDragging) UpdateDragging();
    }

    void HandleLeftClick()
    {
        if (actions.IsDragging)
        {
            Vector2Int clickedTile = board.WorldToTile(Mouse.current.position.ReadValue());
            actions.DropBlock(clickedTile);
        }
        else
        {
            ThrowClickRaycast();
        }
    }

    void ThrowClickRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, BlockLayer)) return;
        if (!hit.collider.TryGetComponent(out BlockSegment segment)) return;
        
        Block block = segment.GetComponentInParent<Block>();

        if (block == null) return;

        if (block.MobilityType == Block.Mobility.Free)
            actions.SelectBlock(block, segment);
        else if (block.MobilityType == Block.Mobility.RotateOnly)
            block.Rotate(segment);
    }

    void UpdateDragging()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new(Vector3.forward, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
            actions.MoveBlock(ray.GetPoint(distance));
    }
}