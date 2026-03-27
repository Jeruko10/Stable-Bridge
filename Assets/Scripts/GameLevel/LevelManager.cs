using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoardGrid))]
[RequireComponent(typeof(SlotManager))]
[RequireComponent(typeof(GameActions))]
public class LevelManager : MonoBehaviour
{
    [field: SerializeField] public float CameraDistance { get; set; } = 1f;
    [field: SerializeField] public float BackgroundDistance { get; set; } = 1f;
    [field: SerializeField] GameObject backgroundPrefab;
    [field: SerializeField] GameObject baseBlockPrefab;
    [field: SerializeField] GameObject basicSegmentPrefab;

    GameObject blocksFolder;
    BoardGrid board;
    SlotManager slotManager;
    List<LevelData> levels;
    readonly HashSet<Block> activeBlocks = new();
    int currentLevelIndex = 0;

    void Awake()
    {
        board = GetComponent<BoardGrid>();
        slotManager = GetComponent<SlotManager>();
        blocksFolder = new("Blocks");
        
        levels = Resources.LoadAll<LevelData>("Levels").ToList();
    }

    void Start()
    {
        LoadLevel(currentLevelIndex);
    }

    void LoadLevel(int levelIndex)
    {
        // actions.DropBlock(Vector2Int.zero); TODO: Fixes a bug where the player could be dragging a block while loading a new level, causing it to be lost in the scene. This is a temporary solution and should be replaced with a more robust handling of dragging state during level transitions.
        DestroyAllBlocks();

        LevelData level = levels[levelIndex];

        board.Initialize(level.LevelSize);
        slotManager.GenerateSlots(level.Blocks.Count);
        SetLevelAesthetic();
        
        foreach (BlockPlacement data in level.Blocks)
            LoadPlacementData(data);

        CreateGround();
    }

    void LoadPlacementData(BlockPlacement data)
    {
        Block block = Instantiate(data.BlockPrefab, blocksFolder.transform);
        
        activeBlocks.Add(block);
        block.MobilityType = data.MobilityType;

        if (data.Mirrored) block.Mirror();

        if (data.PivotIndex >= 0 && data.PivotIndex < block.Segments.Length)
            block.Pivot = block.Segments[data.PivotIndex];
        else
            Debug.LogWarning($"Invalid PivotIndex {data.PivotIndex} for block {block.name}. It should be between 0 and {block.Segments.Length - 1}.");

        if (data.MobilityType != Block.Mobility.Free)
            if (board.TryPlaceBlock(block, data.StartingTile, block.Segments.First()))
                return;
            else
                Debug.LogWarning($"Failed to place block {block.name} at {data.StartingTile} during level load. Check if the tile is valid and unoccupied.");
        
        slotManager.AsignAvailableSlot(block);
    }

    void DestroyAllBlocks()
    {
        foreach (Block block in activeBlocks)
            Destroy(block.gameObject);
        
        activeBlocks.Clear();
    }

    void SetLevelAesthetic()
    {
        // Camera
        Vector3 center = board.GetGridCenter();
        float boardSize = board.Size.x * board.Size.y;
        Camera.main.transform.position = center + new Vector3(0f, 0f, boardSize * -CameraDistance);

        // Background
        if (!Camera.main.orthographic) return;
        GameObject background = Instantiate(backgroundPrefab);
        background.transform.position = center + new Vector3(0f, 0f, boardSize * BackgroundDistance);
    }

    void CreateGround()
    {
        Vector3 startPos = board.TileToWorld(new(0, -1));
        GameObject groundObj = Instantiate(baseBlockPrefab, startPos, Quaternion.identity, blocksFolder.transform);
        
        groundObj.name = "Ground";
        groundObj.GetComponent<Rigidbody>().isKinematic = true;

        for (int x = 0; x < board.Size.x; x++)
        {
            GameObject segmentObj = Instantiate(basicSegmentPrefab, groundObj.transform);
            segmentObj.transform.localPosition = new Vector3(x * board.TileSize, 0, 0);
        }

        Block groundBlock = groundObj.GetComponent<Block>();
        groundBlock.FetchSegments();
        groundBlock.MobilityType = Block.Mobility.Fixed;
    }
}
