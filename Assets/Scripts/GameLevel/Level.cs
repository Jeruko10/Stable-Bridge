using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoardGrid))]
[RequireComponent(typeof(SlotManager))]
public class Level : MonoBehaviour
{
    [field: SerializeField] public float CameraDistance { get; set; } = 1f;
    [field: SerializeField] public float BackgroundDistance { get; set; } = 1f;
    [field: SerializeField] public float CharactersOffsetY { get; set; } = 0f;
    [field: SerializeField] KnightBehaviour knightPrefab;
    [field: SerializeField] GoalBehaviour goalPrefab;
    [field: SerializeField] GameObject backgroundPrefab;
    [field: SerializeField] Block baseBlockPrefab;
    [field: SerializeField] BasicSegment basicSegmentPrefab;

    public BoardGrid Grid { get; private set; }
    public SlotManager Slots { get; private set; }
    public Vector2Int StartPosition { get; private set; }
    public Vector2Int EndPosition { get; private set; }
    public bool IsEditing { get; private set; } = true;

    PathSolver pathSolver;
    GameObject blocksFolder;

    public void Initialize(LevelLayout layout)
    {
        blocksFolder = new GameObject("Blocks");

        Grid = GetComponent<BoardGrid>();
        Slots = GetComponent<SlotManager>();

        Grid.Initialize(layout.LevelSize + new Vector2Int(0, 1)); // One extra row for the ground
        Slots.Initialize(layout.Blocks.Count);

        StartPosition = layout.StartPosition + Vector2Int.up; // Adjust for extra ground row
        EndPosition = layout.EndPosition + Vector2Int.up;

        SetLevelAesthetic();
        
        foreach (BlockPlacementData blockData in layout.Blocks)
            InterpretBlockData(blockData);

        pathSolver = new(this);
        CreateGround();
        CreateCharacters();
    }

    void Update()
    {
        if (IsEditing && Keyboard.current.enterKey.wasPressedThisFrame)
            ExitEditMode();
    }

    void ExitEditMode()
    {
        IsEditing = false;

        IEnumerable<Vector2Int> path = pathSolver.FindShortestPath();

        Debug.Log("Shortest path from " + StartPosition + " to " + EndPosition + ": " + string.Join(" -> ", path));
    }

    void InterpretBlockData(BlockPlacementData data)
    {
        Block block = Instantiate(data.BlockPrefab, blocksFolder.transform);
        block.Initialize(data.PivotIndex, data.MobilityType);

        if (data.Mirrored) block.Mirror();

        if (data.MobilityType != Block.Mobility.Free)
        {
            if (data.MobilityType == Block.Mobility.SlideOnly)
            {
                block.SlidePositions = data.SlideTiles.ToArray();
                data.StartingTile = data.SlideTiles.FirstOrDefault();
            }

            if (Grid.TryPlaceBlock(block, data.StartingTile, block.Segments.FirstOrDefault()))
                return;
            else
                Debug.LogWarning($"Failed to place block {block.name} at {data.StartingTile} during level load. Check if the tile is valid and unoccupied.");
        }
        
        // Block has Free Mobility or failed to place: assign to slot
        Slots.AsignAvailableSlot(block);
    }

    void SetLevelAesthetic()
    {
        // Camera
        Vector3 center = Grid.GetGridCenter();
        float boardSize = Grid.Size.x * Grid.Size.y;
        Camera.main.transform.position = center + new Vector3(0f, 0f, boardSize * -CameraDistance);

        // Background
        if (!Camera.main.orthographic) return;

        GameObject background = Instantiate(backgroundPrefab);
        background.transform.position = center + new Vector3(0f, 0f, boardSize * BackgroundDistance);
    }

    void CreateCharacters()
    {
        Vector2 playerPos = Grid.TileToWorld(StartPosition) + new Vector3(0, CharactersOffsetY, 0);
        Vector2 goalPos = Grid.TileToWorld(EndPosition) + new Vector3(0, CharactersOffsetY, 0);

        KnightBehaviour player = Instantiate(knightPrefab, transform);
        player.transform.position = playerPos;
        player.name = "Player";

        GoalBehaviour goal = Instantiate(goalPrefab, transform);
        goal.transform.position = goalPos;
        goal.name = "Goal";

        Vector2Int playerGround = StartPosition + Vector2Int.down;
        Vector2Int goalGround = EndPosition + Vector2Int.down;

        if (StartPosition == EndPosition)
            Debug.LogWarning("Player and Goal are at the same position.");

        if (!Grid.IsValidTile(StartPosition))
            Debug.LogWarning($"Player's position {StartPosition} is not valid.");

        if (Grid.IsValidTile(playerGround) && (Grid.GetBlockAtTile(playerGround) == null || Grid.GetBlockAtTile(playerGround).GetParent().MobilityType != Block.Mobility.Fixed))
            Debug.LogWarning($"Player at {StartPosition} does not have a fixed ground.");

        if (Grid.IsValidTile(goalGround) && (Grid.GetBlockAtTile(goalGround) == null || Grid.GetBlockAtTile(goalGround).GetParent().MobilityType != Block.Mobility.Fixed))
            Debug.LogWarning($"Goal at {EndPosition} does not have a fixed ground.");

        if (!Grid.IsValidTile(EndPosition))
            Debug.LogWarning($"Goal's position {EndPosition} is not valid.");
    }

    void CreateGround()
    {
        Vector3 startPos = Grid.TileToWorld(Vector2Int.zero);
        Block ground = Instantiate(baseBlockPrefab, startPos, Quaternion.identity, blocksFolder.transform);
        ground.name = "Ground";

        for (int x = 0; x < Grid.Size.x; x++)
        {
            BlockSegment segmentObj = Instantiate(basicSegmentPrefab, ground.transform);
            segmentObj.transform.localPosition = new Vector3(x * Grid.TileSize, 0, 0);
        }

        ground.Initialize(0, Block.Mobility.Fixed);
        Grid.TryPlaceBlock(ground, Vector2Int.zero, ground.Segments.FirstOrDefault());
    }
}