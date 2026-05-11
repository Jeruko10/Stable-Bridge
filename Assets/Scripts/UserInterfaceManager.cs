using System;
using Unity.VisualScripting;
using UnityEngine;

public class UserInterfaceManager : MonoBehaviour
{
    [field: SerializeField] GameObject gameplayInterface;
    [field: SerializeField] GameObject pauseMenu;
    [field: SerializeField] GameObject levelSelector;
    [field: SerializeField] GameObject mainMenu;

    void Start()
    {
        LevelManager.Victory += OnVictory;
    }

    void Update()
    {
        
    }

    void OnVictory()
    {
        gameplayInterface.SetActive(false);
        levelSelector.SetActive(true);
    }

    public void OnTestLevelButtonPressed()
    {
        levelSelector.SetActive(false);
        gameplayInterface.SetActive(true);
        LevelManager.LoadLevel(0);
    }

    public void OnPlayButtonPressed() // Connected through the editor
    {
        mainMenu.SetActive(false);
        levelSelector.SetActive(true);
    }

    public void OnExitButtonPressed() // Connected through the editor
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call<bool>("moveTaskToBack", true);
        #else
            Application.Quit();
        #endif
    }

    public void OnReadyButtonPressed() // Connected through the editor
    {
        LevelManager.Current.ExitEditMode();
    }

    public void OnPauseButtonPressed() // Connected through the editor
    {
        pauseMenu.SetActive(true);
    }

    public void OnHintButtonPressed() // Connected through the editor
    {
        // TODO
    }

    public void OnResumeButtonPressed() // Connected through the editor
    {
        pauseMenu.SetActive(false);
    }

    public void OnMenuButtonPressed() // Connected through the editor
    {
        LevelManager.ExitLevel();
        mainMenu.SetActive(true);
        pauseMenu.SetActive(false);
        gameplayInterface.SetActive(false);
    }
}
