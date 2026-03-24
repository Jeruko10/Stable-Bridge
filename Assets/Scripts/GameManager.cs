using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public float CameraDistance;
    public float BackgroundDistance;
    public BoardGrid Board;
    public Camera Camera;
    public GameObject BackgroundPrefab;
    public static GameManager Instance;

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
        Board.Initialized += OnBoardInitialized;
    }

    void OnBoardInitialized()
    {
        GameObject background = Instantiate(BackgroundPrefab);
        Vector3 center = Board.GetGridCenter();
        float boardSize = Board.Width * Board.Height;

        Camera.transform.position = center + new Vector3(0f, 0f, boardSize * -CameraDistance);
        background.transform.position = center + new Vector3(0f, 0f, boardSize * BackgroundDistance);
    }

    void RunTest()
    {

    }
}