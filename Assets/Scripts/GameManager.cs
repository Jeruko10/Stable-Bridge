using UnityEngine;

public class GameManager : MonoBehaviour
{
    public BoardView BoardView;
    public PieceDefinition Bridge;

    public static GameManager Instance { get; private set; }

    BoardState board;
    PathAnalyzer pathAnalyzer;
    StabilityAnalyzer stabilityAnalyzer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Initialize();
        RunTest();
    }

    void Initialize()
    {
        board = new BoardState();
        pathAnalyzer = new PathAnalyzer();
        stabilityAnalyzer = new StabilityAnalyzer();
    }

    void RunTest()
    {
        board.AddPiece(new PieceInstance(Bridge, new(0, 0, 0)));
        board.AddPiece(new PieceInstance(Bridge, new(1, 0, 0)));
        board.AddPiece(new PieceInstance(Bridge, new(2, 0, 0)));

        BoardView.Render(board);

        bool hasPath = pathAnalyzer.HasPath(board, new Vector3Int(0, 0, 0), new Vector3Int(2, 0, 0));
        bool isStable = stabilityAnalyzer.IsStable(board);

        Debug.Log($"Path: {hasPath} | Stable: {isStable}");
    }
}