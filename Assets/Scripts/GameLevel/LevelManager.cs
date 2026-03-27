using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class LevelManager : MonoBehaviour
{
    [field: SerializeField] Level levelPrefab;

    public static Level Current { get; private set; }

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

    void LoadLevel(int levelIndex)
    {
        Current = Instantiate(levelPrefab);

        LevelLayout lvlData = levels[levelIndex];

        Current.Initialize(lvlData);
    }
}
