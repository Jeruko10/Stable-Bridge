using DG.Tweening;
using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;
    [SerializeField] float slideDuration = 0.35f;
    [SerializeField] float pauseMenuAnimHeight = 200f;
    [SerializeField] CanvasGroup blackPanel;

    RectTransform pauseMenuRect;
    Vector2 shownPos;
    Vector2 hiddenPos;

    void Start()
    {
        LevelManager.Victory += OnVictory;
        LevelManager.LoadLevel(LevelSelectorUI.PendingLevelIndex);

        pauseMenuRect = pauseMenu.GetComponent<RectTransform>();
        shownPos = pauseMenuRect.anchoredPosition;
        hiddenPos = shownPos + Vector2.up * pauseMenuAnimHeight;

        pauseMenuRect.anchoredPosition = hiddenPos;
        pauseMenu.SetActive(false);

        blackPanel.gameObject.SetActive(true);
        blackPanel.alpha = 0f;
        blackPanel.blocksRaycasts = false;
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
        Time.timeScale = 0f;

        pauseMenu.SetActive(true);
        pauseMenuRect.DOKill();
        pauseMenuRect.DOAnchorPos(shownPos, slideDuration).SetEase(Ease.OutCubic).SetUpdate(true);

        blackPanel.DOKill();
        blackPanel.blocksRaycasts = true;
        blackPanel.DOFade(0.7f, slideDuration).SetUpdate(true);
    }

    public void OnHintButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        LevelManager.Current.HintRenderer.SpawnHardCodedHint();
    }

    public void OnResumeButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);

        pauseMenuRect.DOKill();
        pauseMenuRect.DOAnchorPos(hiddenPos, slideDuration).SetEase(Ease.InCubic).SetUpdate(true)
            .OnComplete(() => pauseMenu.SetActive(false));

        blackPanel.DOKill();
        blackPanel.DOFade(0f, slideDuration).SetUpdate(true)
            .OnComplete(() =>
            {
                blackPanel.blocksRaycasts = false;
                Time.timeScale = 1f;
            });
    }

    public void OnMenuButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }
}
