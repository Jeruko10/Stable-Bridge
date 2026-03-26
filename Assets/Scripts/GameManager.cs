using UnityEngine;

public class GameManager : MonoBehaviour
{
    [field: SerializeField] public float CameraDistance { get; set; } = 1f;
    [field: SerializeField] public float BackgroundDistance { get; set; } = 1f;
    [field: SerializeField] public BoardGrid Board { get; private set; }
    [field: SerializeField] public SlotManager SlotManager { get; private set; }
    [field: SerializeField] public Camera MainCamera { get; private set; }
    [field: SerializeField] public GameObject BackgroundPrefab { get; private set; }
    [field: SerializeField] public GameObject BaseBlockPrefab { get; private set; }
    [field: SerializeField] public GameObject BasicSegmentPrefab { get; private set; }
    
    static GameManager instance;

    void Awake()
    {
        if (instance != null && instance != this) Destroy(gameObject);
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        Board.Initialized += OnBoardInitialized;

        SlotManager.GenerateSlots(1);

        RunTest();
    }

    void OnBoardInitialized()
    {
        GameObject background = Instantiate(BackgroundPrefab);
        Vector3 center = Board.GetGridCenter();
        float boardSize = Board.Width * Board.Height;

        MainCamera.transform.position = center + new Vector3(0f, 0f, boardSize * -CameraDistance);
        background.transform.position = center + new Vector3(0f, 0f, boardSize * BackgroundDistance);

        CreateGround();
    }

    void CreateGround()
    {
        Vector3 startPos = Board.TileToWorld(new(0, -1));
        GameObject groundObj = Instantiate(BaseBlockPrefab, startPos, Quaternion.identity);
        
        groundObj.name = "Ground";
        groundObj.GetComponent<Rigidbody>().isKinematic = true;

        for (int x = 0; x < Board.Width; x++)
        {
            GameObject segmentObj = Instantiate(BasicSegmentPrefab, groundObj.transform);
            segmentObj.transform.localPosition = new Vector3(x * Board.TileSize, 0, 0);
        }

        Block groundBlock = groundObj.GetComponent<Block>();
        groundBlock.FetchSegments();
        groundBlock.MobilityType = Block.Mobility.Pinned;
    }

    void RunTest()
    {
        
    }
}
