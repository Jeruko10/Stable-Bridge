using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


public class LevelManager : MonoBehaviour
{
    [SerializeField] bool trainModeEnabled;
    [field: SerializeField] Level levelPrefab;

    public static Level Current { get; private set; }
    public static int LastLevelIndex { get; private set; }
    public static bool TrainModeEnabled => instance.trainModeEnabled;
    public static event Action<Level> LevelLoaded;
    public static event Action Victory;

    static LevelManager instance;
    LevelLayout[] levels;

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

        if (LastLevelIndex >= instance.levels.Count())
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

        LevelLayout lvlData = instance.levels[levelIndex];

        Current.Initialize(lvlData, instance.trainModeEnabled);
        LevelLoaded?.Invoke(Current);

        return Current;
    }

    public static void ExitLevel()
    {
        if (Current == null) return;

        Destroy(Current.gameObject);
        Current = null;
    }
}
