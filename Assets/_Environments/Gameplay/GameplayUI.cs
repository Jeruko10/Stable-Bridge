using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    [SerializeField] PopUpWindow pauseMenu;
    [SerializeField] CanvasGroup pauseMenuButtons;

    void Start()
    {
        LevelManager.Victory += OnVictory;
        LevelManager.LoadLevel(LevelSelectorUI.PendingLevelIndex);

        pauseMenuButtons.interactable = false;
        pauseMenu.onShown.AddListener(() => pauseMenuButtons.interactable = true);
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
        pauseMenuButtons.interactable = false;
        pauseMenu.Show();
    }

    public void OnHintButtonPressed()
    {
        LevelManager.Current.HintRenderer.SpawnHardCodedHint();
    }

    public void OnResumeButtonPressed()
    {
        pauseMenuButtons.interactable = false;
        Time.timeScale = 1f;
        pauseMenu.Hide();
    }

    public void OnMenuButtonPressed()
    {
        pauseMenuButtons.interactable = false;
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }
}
