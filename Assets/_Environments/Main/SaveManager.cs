using UnityEngine;

public class SaveManager
{
    public static SaveManager Instance { get; } = new SaveManager();

    const string HighestLevelKey = "HighestUnlockedLevel";

    public int HighestUnlockedLevel { get; private set; }

    SaveManager()
    {
        HighestUnlockedLevel = PlayerPrefs.GetInt(HighestLevelKey, 0);
    }

    public void UnlockLevel(int levelIndex)
    {
        if (levelIndex <= HighestUnlockedLevel) return;
        HighestUnlockedLevel = levelIndex;
        PlayerPrefs.SetInt(HighestLevelKey, HighestUnlockedLevel);
        PlayerPrefs.Save();
    }

    public void ResetProgress()
    {
        HighestUnlockedLevel = 0;
        PlayerPrefs.DeleteKey(HighestLevelKey);
        PlayerPrefs.Save();
    }
}
