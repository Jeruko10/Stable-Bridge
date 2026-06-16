using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoardGrid))]
[RequireComponent(typeof(SimulationObserver))]
[RequireComponent(typeof(HintRenderer))]
public class Level : MonoBehaviour
{
    [field: SerializeField] public float CameraDistance { get; set; } = 1f;
    [field: SerializeField] public float BackgroundDistance { get; set; } = 1f;
    [field: SerializeField] public float CharactersHeightOffset { get; set; } = 0f;
    [field: SerializeField] public Vector2 CameraOffset { get; set; } = new(1f, 0f);

    [Header("References")]
    [field: SerializeField] Miner minerPrefab;
    [field: SerializeField] Goal goalPrefab;
    [field: SerializeField] Block baseBlockPrefab;
    [field: SerializeField] BasicSegment basicSegmentPrefab;

    public BoardGrid Grid { get; private set; }
    public SimulationObserver SimulationObserver { get; private set; }
    public HintRenderer HintRenderer { get; private set; }
    public Vector2Int StartPosition { get; private set; }
    public Vector2Int EndPosition { get; private set; }
    public bool IsEditing { get; private set; } = true;
    public List<Block> Inventory { get; private set; } = new();

    public event Action<bool> LevelComplete;
    public event Action<bool> SuccessKnown;

    public bool SkipProgression { get; set; }

    readonly Vector2Int gridExtraBoundaries = new(3, 2);
    bool success, fastGameplay;
    IEnumerable<Vector2> minerPath;
    GameObject blocksFolder;
    Miner miner;
    BlockInventory blockInventory;

    public void Initialize(LevelLayout layout, bool fastGameplay, BlockInventory blockInventory)
    {
        this.fastGameplay = fastGameplay;
        this.blockInventory = blockInventory;

        blocksFolder = new GameObject("Blocks");
        blocksFolder.transform.SetParent(transform);

        Grid = GetComponent<BoardGrid>();
        SimulationObserver = GetComponent<SimulationObserver>();
        HintRenderer = GetComponent<HintRenderer>();

        Grid.Initialize(layout.LevelSize);

        Grid.AddColumn(atRight: false, count: gridExtraBoundaries.x);
        Grid.AddColumn(atRight: true, count: gridExtraBoundaries.x);
        Grid.AddRow(atTop: true, count: gridExtraBoundaries.y);

        StartPosition = layout.StartPosition;
        EndPosition = layout.EndPosition;
        Grid.SetReservedTiles(new[] { StartPosition, EndPosition });

        Inventory.Clear();
        SetCamera();

        foreach (BlockPlacementData blockData in layout.Blocks)
            CreateBlock(blockData);

        CreateGround(layout.LevelSize.x);
        CreateCharacters();

        SimulationObserver.SimulationEnded += OnSimulationEnded;
        SimulationObserver.StabilityKnown += OnStabilityKnown;
        miner.PathEnded += OnPathEnded;
    }

    public void ExitEditMode()
    {
        if (!IsEditing) return;

        IsEditing = false;

        if (Grid == null)
        {
            Debug.LogError("Level.ExitEditMode: Grid is null.");
            return;
        }
        if (SimulationObserver == null)
        {
            Debug.LogError("Level.ExitEditMode: SimulationObserver is null.");
            return;
        }
        if (HintRenderer == null)
        {
            Debug.LogError("Level.ExitEditMode: HintRenderer is null.");
            return;
        }

        HintRenderer.ResetHints();
        Camera.main.GetComponent<CameraController>().DoCinematic();
        SimulationObserver.Initialize(Grid.GetAllBlocks(), fastGameplay);
    }

    void OnStabilityKnown(IEnumerable<Block> unstableBlocks)
    {
        foreach (Block block in unstableBlocks)
            Grid.RemoveBlock(block);

        Graph graph = PathSolver.GridToGraph(Grid);

        minerPath = PathSolver.GetPath(StartPosition, EndPosition, graph);
        success = minerPath.LastOrDefault() == EndPosition + BlockSegment.BottomRight;
        SuccessKnown?.Invoke(success);
    }

    void OnSimulationEnded()
    {
        if (fastGameplay)
        {
            StartCoroutine(EndLevel(success));
            return;
        }
        
        Vector3[] worldPath = minerPath.Select(tile => Grid.TileToWorld(tile)).ToArray();
        miner.StartPath(worldPath, success);
    }

    void OnPathEnded() => StartCoroutine(EndLevel(success));

    IEnumerator EndLevel(bool success)
    {
        LevelComplete?.Invoke(success);

        if (SkipProgression) yield break;

        if (success)
        {
            AudioManager.Play(AudioManager.Instance.Success);
            if (!fastGameplay) yield return new WaitForSeconds(1f);
            LevelManager.PassLevel();
        }
        else
        {
            AudioManager.Play(AudioManager.Instance.Failure);
            if (!fastGameplay) yield return new WaitForSeconds(3f);
            LevelManager.RestartLevel();
        }
    }

    public Block CreateBlock(BlockPlacementData data, bool forcePosition = false)
    {
        Block block = Instantiate(data.BlockPrefab, blocksFolder.transform);
        block.Initialize(data.BlockPrefab, data.PivotIndex, data.MobilityType);
        block.SetRotation(block.Pivot, data.StartingRotation);

        if (data.Flipped) block.Flip(block.Pivot);

        if (data.MobilityType != Block.Mobility.Free)
        {
            Vector2Int startingTile = data.StartingTile;

            if (data.MobilityType == Block.Mobility.SlideOnly)
            {
                block.SlidePositions = data.SlideTiles.ToArray();
                startingTile = block.SlidePositions.FirstOrDefault();
            }

            if (Grid.TryPlaceBlock(block, startingTile, block.Pivot)) return block;
            Debug.LogWarning($"Failed to place block {block.name} at {data.StartingTile}. Check if the tile is valid and unoccupied.");
        }
        
        blockInventory.AddBlock(block);
        Inventory.Add(block);
        return block;
    }

    void SetCamera()
    {
        // Camera
        Vector3 center = Grid.GetGridCenter();
        float boardSize = Grid.Size.x * Grid.Size.y;
        Camera.main.transform.position = center + new Vector3(CameraOffset.x, CameraOffset.y, boardSize * -CameraDistance);
        Camera.main.GetComponent<CameraController>().LookTarget = center + (Vector3)CameraOffset;
        Camera.main.orthographic = true;
    }

    void CreateCharacters()
    {
        Vector2 playerPos = Grid.TileToWorld(StartPosition) + (CharactersHeightOffset - BlockSegment.Apothem) * Vector3.up;
        Vector2 goalPos = Grid.TileToWorld(EndPosition) + (CharactersHeightOffset - BlockSegment.Apothem) * Vector3.up;

        miner = Instantiate(minerPrefab, transform);
        miner.transform.position = playerPos;
        miner.name = "Player";
        miner.HeightOffset = CharactersHeightOffset;

        Goal goal = Instantiate(goalPrefab, transform);
        goal.transform.position = goalPos;
        goal.name = "Goal";

        Vector2Int playerGround = StartPosition + Vector2Int.down;
        Vector2Int goalGround = EndPosition + Vector2Int.down;

        if (StartPosition == EndPosition)
            Debug.LogWarning("Player and Goal are at the same position.");

        if (!Grid.IsValidTile(StartPosition))
            Debug.LogWarning($"Player's position {StartPosition} is not valid.");

        if (!Grid.IsValidTile(EndPosition))
            Debug.LogWarning($"Goal's position {EndPosition} is not valid.");
    }

    void CreateGround(int levelWidth)
    {
        Vector2Int groundTile = new(0, 0);
        Vector3 startPos = Grid.TileToWorld(groundTile);
        Block ground = Instantiate(baseBlockPrefab, startPos, Quaternion.identity, blocksFolder.transform);
        ground.name = "Ground";

        for (int x = 0; x < levelWidth; x++)
        {
            BlockSegment segmentObj = Instantiate(basicSegmentPrefab, ground.transform);
            segmentObj.transform.localPosition = new Vector3(x * Grid.TileSize, 0, 0);
        }

        ground.Initialize(null, 0, Block.Mobility.Ground);
        Grid.TryPlaceBlock(ground, groundTile, ground.Segments.FirstOrDefault());
    }
}