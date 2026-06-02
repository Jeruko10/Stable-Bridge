using UnityEngine;
using UnityEngine.SceneManagement;

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
        LevelManager.ExitLevel();
        SceneManager.LoadScene("LevelSelector");
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
    }

    public void OnMenuButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        LevelManager.ExitLevel();
        SceneManager.LoadScene("LevelSelector");
    }
}
