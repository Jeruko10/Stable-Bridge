using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class LevelManager : MonoBehaviour
{
    [field: SerializeField] Level levelPrefab;

    public static Level Current { get; private set; }
    public static event Action<Level> LevelLoaded;

    static LevelManager instance;
    List<LevelLayout> levels;
    int currentLevelIndex = 0;

    void Awake()
    {
        if (instance != null && instance != this)
        { 
            Destroy(gameObject); 
            return; 
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        
        levels = Resources.LoadAll<LevelLayout>("Levels").ToList();
    }

    void Start()
    {
        LoadLevel(currentLevelIndex);
    }

    public static Level PassLevel()
    {
        instance.currentLevelIndex++;

        if (instance.currentLevelIndex >= instance.levels.Count)
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

        Current.Initialize(lvlData);
        LevelLoaded?.Invoke(Current);

        return Current;
    }

    static void TriggerVictory()
    {
        Debug.Log("All levels completed! Victory!");
    }
}
