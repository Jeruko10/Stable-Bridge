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
    public static bool TrainModeEnabled => instance.trainModeEnabled;
    public static event Action<Level> LevelLoaded;

    static LevelManager instance;
    LevelLayout[] levels;
    int currentLevelIndex = 0;

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

    void Start()
    {
        LoadLevel(currentLevelIndex);
    }

    public static Level PassLevel()
    {
        instance.currentLevelIndex++;

        if (instance.currentLevelIndex >= instance.levels.Count())
        {
            TriggerVictory();
            return null;
        }

        return LoadLevel(instance.currentLevelIndex);
    }

    public static void RestartLevel() => LoadLevel(instance.currentLevelIndex);

    public static Level LoadLevel(int levelIndex)
    {
        if (Current != null) Destroy(Current.gameObject);
        Current = Instantiate(instance.levelPrefab);

        LevelLayout lvlData = instance.levels[levelIndex];

        Current.Initialize(lvlData, instance.trainModeEnabled);
        LevelLoaded?.Invoke(Current);

        return Current;
    }

    static void TriggerVictory()
    {
        Debug.Log("All levels completed! Victory!");
    }
}
