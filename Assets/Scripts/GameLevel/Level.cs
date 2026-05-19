using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoardGrid))]
[RequireComponent(typeof(SimulationObserver))]
public class Level : MonoBehaviour
{
    [field: SerializeField] public float CameraDistance { get; set; } = 1f;
    [field: SerializeField] public float BackgroundDistance { get; set; } = 1f;
    [field: SerializeField] public float CharactersHeightOffset { get; set; } = 0f;
    [field: SerializeField] Miner minerPrefab;
    [field: SerializeField] Goal goalPrefab;
    [field: SerializeField] Block baseBlockPrefab;
    [field: SerializeField] BasicSegment basicSegmentPrefab;

    public BoardGrid Grid { get; private set; }
    public SimulationObserver SimulationObserver { get; private set; }
    public Vector2Int StartPosition { get; private set; }
    public Vector2Int EndPosition { get; private set; }
    public bool IsEditing { get; private set; } = true;
    public List<Block> Inventory { get; private set; } = new();

    public event Action<bool> LevelComplete;
    public event Action<bool> SuccessKnown;

    bool success, trainModeEnabled;
    IEnumerable<Vector2> minerPath;
    GameObject blocksFolder;
    Miner miner;
    BlockInventory blockInventory;

    public void Initialize(LevelLayout layout, bool trainModeEnabled, BlockInventory blockInventory)
    {
        this.trainModeEnabled = trainModeEnabled;
        this.blockInventory = blockInventory;

        blocksFolder = new GameObject("Blocks");
        blocksFolder.transform.SetParent(transform);

        Grid = GetComponent<BoardGrid>();
        SimulationObserver = GetComponent<SimulationObserver>();

        Grid.Initialize(layout.LevelSize);
        
        StartPosition = layout.StartPosition;
        EndPosition = layout.EndPosition;

        Inventory.Clear();
        SetCamera();
        
        foreach (BlockPlacementData blockData in layout.Blocks)
            InterpretBlockData(blockData);

        CreateGround();
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
            Debug.LogError("Level.ExitEditMode: Grid es null.");
            return;
        }

        if (SimulationObserver == null)
        {
            Debug.LogError("Level.ExitEditMode: SimulationObserver es null.");
            return;
        }

        Camera.main.GetComponent<CameraController>().DoCinematic();
        SimulationObserver.Initialize(Grid.GetAllBlocks(), trainModeEnabled);
    }

    void OnStabilityKnown(IEnumerable<Block> unstableBlocks)
    {
        foreach (Block block in unstableBlocks)
            Grid.RemoveBlock(block);

        // Start pathfinding with the remaining blocks
        Grid.AddRow(false);
        Graph graph = PathSolver.GridToGraph(Grid);

        minerPath = PathSolver.GetPath(StartPosition, EndPosition, graph);
        success = minerPath.LastOrDefault() == EndPosition + BlockSegment.BottomRight;
        SuccessKnown?.Invoke(success);
    }

    void OnSimulationEnded()
    {
        if (trainModeEnabled)
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

        if (success)
        {
            AudioManager.Play(AudioManager.Instance.Success);
            if (!trainModeEnabled) yield return new WaitForSeconds(1f);
            LevelManager.PassLevel();
        }
        else
        {
            AudioManager.Play(AudioManager.Instance.Failure);
            if (!trainModeEnabled) yield return new WaitForSeconds(3f);
            LevelManager.RestartLevel();
        }
    }

    void InterpretBlockData(BlockPlacementData data)
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
                startingTile = data.SlideTiles.FirstOrDefault();
            }

            if (Grid.TryPlaceBlock(block, startingTile, block.Pivot)) return;
            else Debug.LogWarning($"Failed to place block {block.name} at {data.StartingTile} during level load. Check if the tile is valid and unoccupied.");
        }
        
        // Block has Free Mobility or failed to place: send to inventory
        blockInventory.AddBlock(block);
        Inventory.Add(block);
    }

    void SetCamera()
    {
        // Camera
        Vector3 center = Grid.GetGridCenter();
        float boardSize = Grid.Size.x * Grid.Size.y;
        Camera.main.transform.position = center + new Vector3(0f, 0f, boardSize * -CameraDistance);
        Camera.main.GetComponent<CameraController>().LookTarget = center;
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

        ground.Initialize(null, 0, Block.Mobility.Ground);
        Grid.TryPlaceBlock(ground, Vector2Int.zero, ground.Segments.FirstOrDefault());
    }
}