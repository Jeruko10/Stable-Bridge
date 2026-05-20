using System;
using System.Linq;
using UnityEngine;


public class LevelManager : MonoBehaviour
{
    [Header("Dev Tools")]
    [SerializeField] bool fastGameplay;
    [SerializeField] bool levelCreationEnabled;

    [Header("References")]
    [field: SerializeField] Level levelPrefab;
    [SerializeField] BlockInventory blockInventory;

    public static Level Current { get; private set; }
    public static int LastLevelIndex { get; private set; }
    public static int LevelAmount => levels.Count();
    public static bool LevelCreationEnabled => instance.levelCreationEnabled;
    public static event Action<Level> LevelLoaded;
    public static event Action Victory;

    static LevelManager instance;
    static LevelLayout[] levels;

    void Awake()
    {
        if (instance != null && instance != this)
        { 
            Destroy(gameObject);
            return; 
        }

        instance = this;
        levels = Resources.LoadAll<LevelLayout>("Levels");
    }

    public static Level PassLevel()
    {
        LastLevelIndex++;

        if (LastLevelIndex >= LevelAmount)
        {
            LastLevelIndex = 0;
            Victory?.Invoke();
            ExitLevel();
            return null;
        }

        return LoadLevel(LastLevelIndex);
    }

    public static void RestartLevel() => LoadLevel(LastLevelIndex);

    public static Level LoadLevel(int levelIndex)
    {
        ExitLevel();
        Current = Instantiate(instance.levelPrefab, instance.transform);
        Current.name = $"Level {levelIndex}";

        LevelLayout lvlData = levels[levelIndex];

        Current.Initialize(lvlData, instance.fastGameplay, instance.blockInventory);
        AudioManager.StopAll();
        AudioManager.Play(AudioManager.Instance.LevelTheme);
        LevelLoaded?.Invoke(Current);

        return Current;
    }

    public static void ExitLevel()
    {
        if (Current == null) return;

        instance.blockInventory.Clear();
        Destroy(Current.gameObject);
        Current = null;
    }
}
