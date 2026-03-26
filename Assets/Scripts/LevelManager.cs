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

    BoardGrid board;
    SlotManager slotManager;
    GameActions blockController;
    List<LevelData> levels;
    readonly HashSet<Block> activeBlocks = new();
    int currentLevelIndex = 0;

    void Awake()
    {
        board = GetComponent<BoardGrid>();
        slotManager = GetComponent<SlotManager>();
        blockController = GetComponent<GameActions>();
        
        levels = Resources.LoadAll<LevelData>("Levels").ToList();
    }

    void Start()
    {
        LoadLevel(currentLevelIndex);
    }

    void LoadLevel(int levelIndex)
    {
        blockController.DropBlock(Vector2Int.zero);
        DestroyAllBlocks();

        LevelData level = levels[levelIndex];

        board.Initialize(level.LevelSize);
        slotManager.GenerateSlots(level.Blocks.Count);
        SetLevelAesthetic();
        
        foreach (BlockPlacement bplace in level.Blocks)
        {
            Block newBlock = Instantiate(bplace.BlockPrefab);
            newBlock.transform.position = slotManager.GetAvailableSlot().Value;
            newBlock.GetComponent<Rigidbody>().isKinematic = true;
        }

        CreateGround();
    }

    void DestroyAllBlocks()
    {
        foreach (Block block in activeBlocks) Destroy(block.gameObject);
        activeBlocks.Clear();
    }

    void SetLevelAesthetic()
    {
        GameObject background = Instantiate(backgroundPrefab);
        Vector3 center = board.GetGridCenter();
        float boardSize = board.Size.x * board.Size.y;

        Camera.main.transform.position = center + new Vector3(0f, 0f, boardSize * -CameraDistance);
        background.transform.position = center + new Vector3(0f, 0f, boardSize * BackgroundDistance);
    }

    void CreateGround()
    {
        Vector3 startPos = board.TileToWorld(new(0, -1));
        GameObject groundObj = Instantiate(baseBlockPrefab, startPos, Quaternion.identity);
        
        groundObj.name = "Ground";
        groundObj.GetComponent<Rigidbody>().isKinematic = true;

        for (int x = 0; x < board.Size.x; x++)
        {
            GameObject segmentObj = Instantiate(basicSegmentPrefab, groundObj.transform);
            segmentObj.transform.localPosition = new Vector3(x * board.TileSize, 0, 0);
        }

        Block groundBlock = groundObj.GetComponent<Block>();
        groundBlock.FetchSegments();
        groundBlock.MobilityType = Block.Mobility.Pinned;
    }
}
