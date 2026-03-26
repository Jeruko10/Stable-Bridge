using UnityEngine;

public class GameManager : MonoBehaviour
{
    [field: SerializeField] public float CameraDistance { get; set; } = 1f;
    [field: SerializeField] public float BackgroundDistance { get; set; } = 1f;
    [field: SerializeField] public BoardGrid Board { get; set; }
    [field: SerializeField] public Camera MainCamera { get; set; }
    [field: SerializeField] public GameObject BackgroundPrefab { get; set; }
    [field: SerializeField] public GameObject BaseBlockPrefab { get; set; }
    [field: SerializeField] public GameObject BasicSegmentPrefab { get; set; }
    
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        Initialize();
        RunTest();
    }

    void Initialize() => Board.Initialized += OnBoardInitialized;

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
        Vector3 startPos = Board.TileToWorld(new Vector2Int(0, -1));
        GameObject groundObj = Instantiate(BaseBlockPrefab, startPos, Quaternion.identity);
        groundObj.name = "Ground";
        groundObj.GetComponent<Rigidbody>().isKinematic = true;
        Block groundBlock = groundObj.GetComponent<Block>();

        for (int x = 0; x < Board.Width; x++)
        {
            GameObject segmentObj = Instantiate(BasicSegmentPrefab, groundObj.transform);
            segmentObj.transform.localPosition = new Vector3(x * Board.TileSize, 0, 0);
        }

        groundBlock.FetchSegments();
        groundBlock.MobilityType = Block.Mobility.Pinned;
    }

    void RunTest()
    {
        
    }
}
