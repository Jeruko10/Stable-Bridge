using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;

    void Start()
    {
        LevelManager.Victory += OnVictory;
        LevelManager.LoadLevel(LevelSelectorUI.PendingLevelIndex);
        pauseMenu.SetActive(false);
    }

    void OnDestroy() => LevelManager.Victory -= OnVictory;

    void OnVictory()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }

    public void OnReadyButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIStartPath);
        LevelManager.Current.ExitEditMode();
    }

    public void OnPauseButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIConfirmation);
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnHintButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        LevelManager.Current.HintRenderer.SpawnHardCodedHint();
    }

    public void OnResumeButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnMenuButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }
}
