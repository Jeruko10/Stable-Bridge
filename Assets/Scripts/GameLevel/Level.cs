using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoardGrid))]
[RequireComponent(typeof(SlotManager))]
[RequireComponent(typeof(PathSolver))]
public class Level : MonoBehaviour
{
    [field: SerializeField] public float CameraDistance { get; set; } = 1f;
    [field: SerializeField] public float BackgroundDistance { get; set; } = 1f;
    [field: SerializeField] GameObject backgroundPrefab;
    [field: SerializeField] GameObject baseBlockPrefab;
    [field: SerializeField] GameObject basicSegmentPrefab;

    public BoardGrid Grid { get; private set; }
    public SlotManager Slots { get; private set; }
    public PathSolver PathSolver { get; private set; }
    
    GameObject blocksFolder;

    public void Initialize(LevelLayout layout)
    {
        blocksFolder = new GameObject("Blocks");

        Grid = GetComponent<BoardGrid>();
        Slots = GetComponent<SlotManager>();
        PathSolver = GetComponent<PathSolver>();

        Grid.Initialize(layout.LevelSize);
        Slots.Initialize(layout.Blocks.Count);

        SetLevelAesthetic();
        
        foreach (BlockPlacementData blockData in layout.Blocks)
            InterpretBlockData(blockData);

        CreateGround();
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

    void CreateGround()
    {
        Vector3 startPos = Grid.TileToWorld(new(0, -1));
        GameObject groundObj = Instantiate(baseBlockPrefab, startPos, Quaternion.identity, blocksFolder.transform);
        
        groundObj.name = "Ground";
        groundObj.GetComponent<Rigidbody>().isKinematic = true;

        for (int x = 0; x < Grid.Size.x; x++)
        {
            GameObject segmentObj = Instantiate(basicSegmentPrefab, groundObj.transform);
            segmentObj.transform.localPosition = new Vector3(x * Grid.TileSize, 0, 0);
        }

        Block groundBlock = groundObj.GetComponent<Block>();
        groundBlock.Initialize(0, Block.Mobility.Fixed);
        groundBlock.Position2D = startPos;
    }
}