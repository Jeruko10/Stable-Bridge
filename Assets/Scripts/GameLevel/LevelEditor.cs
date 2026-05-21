using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class LevelEditor : MonoBehaviour
{
    [SerializeField] LayerMask blockLayer;
    [SerializeField] float rayDistance = 100f;

    BoardGrid grid;

    void Awake()
    {
        LevelManager.LevelLoaded += OnLevelLoaded;
    }

    void OnLevelLoaded(Level level)
    {
        grid = level.Grid;
    }

    void Update()
    {
#if UNITY_EDITOR

        if (grid == null || Mouse.current == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame) TryClickBlock();

        if (Keyboard.current.upArrowKey.wasPressedThisFrame) grid.AddRow(true);
        if (Keyboard.current.downArrowKey.wasPressedThisFrame) grid.RemoveRow();
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) grid.AddColumn(true);
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) grid.RemoveColumn();

        if (Keyboard.current.sKey.wasPressedThisFrame) SaveLevel();
        if (Keyboard.current.rKey.wasPressedThisFrame) LevelManager.RestartLevel();
#endif
    }

    void TryClickBlock()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, blockLayer)) return;

        BlockSegment segment = hit.collider.GetComponentInParent<BlockSegment>();
        if (segment == null) return;

        Block block = segment.GetParent();
        if (block.MobilityType == Block.Mobility.Ground) return;

        Block.Mobility[] cycle = { Block.Mobility.Free, Block.Mobility.RotateOnly, Block.Mobility.SlideOnly, Block.Mobility.Fixed };
        int current = System.Array.IndexOf(cycle, block.MobilityType);
        Block.Mobility nextMobility = cycle[(current + 1) % cycle.Length];

        int pivotIndex = System.Array.IndexOf(block.Segments, block.Pivot);
        Vector2Int? tile = grid.GetTileOfBlock(block.Pivot);

        BlockPlacementData data = new(
            blockPrefab: block.Prefab,
            mobilityType: nextMobility,
            pivotIndex: pivotIndex,
            flipped: block.IsFlipped,
            rotation: block.Rotation,
            startingTile: tile ?? Vector2Int.zero,
            slideTiles: block.SlidePositions != null ? block.SlidePositions.ToList() : new List<Vector2Int>()
        );

        grid.RemoveBlock(block);
        Destroy(block.gameObject);

        Debug.Log($"Block mobility changed to {nextMobility}");
        LevelManager.Current.CreateBlock(data);
    }

    void SaveLevel()
    {
        Level level = LevelManager.Current;
        LevelLayout.FromLevel(level, level.StartPosition, level.EndPosition).SaveAsAsset();
    }
}
