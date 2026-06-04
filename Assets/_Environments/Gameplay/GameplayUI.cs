using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    [SerializeField] PopUpWindow pauseMenu;

    void Start()
    {
        LevelManager.Victory += OnVictory;
        LevelManager.LoadLevel(LevelSelectorUI.PendingLevelIndex);
    }

    void OnDestroy() => LevelManager.Victory -= OnVictory;

    void OnVictory()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }

    public void OnReadyButtonPressed()
    {
        LevelManager.Current.ExitEditMode();
    }

    public void OnPauseButtonPressed()
    {
        Time.timeScale = 0f;
        pauseMenu.Show();
    }

    public void OnHintButtonPressed()
    {
        LevelManager.Current.HintRenderer.SpawnHardCodedHint();
    }

    public void OnResumeButtonPressed()
    {
        Time.timeScale = 1f;
        pauseMenu.Hide();
    }

    public void OnMenuButtonPressed()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }
}
