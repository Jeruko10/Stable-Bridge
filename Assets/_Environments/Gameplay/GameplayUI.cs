using System.Collections;
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
    Coroutine slideCoroutine;
    Coroutine fadeCoroutine;

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
        StartSlide(hiddenPos, shownPos, activate: true);
        StartFade(0f, 0.7f);
    }

    public void OnHintButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        LevelManager.Current.HintRenderer.SpawnHardCodedHint();
    }

    public void OnResumeButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        StartSlide(shownPos, hiddenPos, activate: false);
        StartFade(0.7f, 0f);
    }

    public void OnMenuButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }

    void StartFade(float from, float to)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadePanel(from, to));
    }

    IEnumerator FadePanel(float from, float to)
    {
        blackPanel.blocksRaycasts = to > 0f;
        blackPanel.alpha = from;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackPanel.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration)));
            yield return null;
        }

        blackPanel.alpha = to;
        blackPanel.blocksRaycasts = to > 0f;
    }

    void StartSlide(Vector2 from, Vector2 to, bool activate)
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideMenu(from, to, activate));
    }

    IEnumerator SlideMenu(Vector2 from, Vector2 to, bool activate)
    {
        if (activate) pauseMenu.SetActive(true);

        pauseMenuRect.anchoredPosition = from;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
            pauseMenuRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        pauseMenuRect.anchoredPosition = to;
        if (!activate)
        {
            Time.timeScale = 1f;
            pauseMenu.SetActive(false);
        }
    }
}
