using DG.Tweening;
using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    [SerializeField] PopUpWindow pauseMenu;
    [SerializeField] RectTransform hintButton;
    [SerializeField] RectTransform readyButton;
    [SerializeField] RectTransform inventorySidebar;
    [SerializeField] CanvasGroup pauseMenuButtons;

    const float SlideButtonsOffset = 300f;
    const float SlideInventoryOffset = 400f;
    const float SlideDuration = 0.4f;

    Vector2 hintButtonShownPos;
    Vector2 readyButtonShownPos;
    Vector2 inventoryShownPos;

    Level currentLevel;

    void Start()
    {
        hintButtonShownPos = hintButton.anchoredPosition;
        readyButtonShownPos = readyButton.anchoredPosition;
        inventoryShownPos = inventorySidebar.anchoredPosition;

        pauseMenuButtons.interactable = false;
        pauseMenu.onShown.AddListener(() => pauseMenuButtons.interactable = true);

        LevelManager.Victory += OnVictory;
        LevelManager.LevelLoaded += OnLevelLoaded;
        LevelManager.LoadLevel(LevelSelectorUI.PendingLevelIndex);
    }

    void OnDestroy()
    {
        LevelManager.Victory -= OnVictory;
        LevelManager.LevelLoaded -= OnLevelLoaded;
        if (currentLevel != null) currentLevel.LevelComplete -= OnLevelComplete;
    }

    void OnLevelLoaded(Level level)
    {
        if (currentLevel != null) currentLevel.LevelComplete -= OnLevelComplete;
        currentLevel = level;
        currentLevel.LevelComplete += OnLevelComplete;
        AnimateIn();
    }

    void OnLevelComplete(bool success)
    {
        if (!success) AnimateIn();
    }

    void AnimateIn()
    {
        hintButton.DOKill();
        readyButton.DOKill();
        inventorySidebar.DOKill();
        hintButton.DOAnchorPos(hintButtonShownPos, SlideDuration).SetEase(Ease.OutCubic);
        readyButton.DOAnchorPos(readyButtonShownPos, SlideDuration).SetEase(Ease.OutCubic);
        inventorySidebar.DOAnchorPos(inventoryShownPos, SlideDuration).SetEase(Ease.OutCubic);
    }

    void AnimateOut()
    {
        hintButton.DOKill();
        readyButton.DOKill();
        inventorySidebar.DOKill();
        hintButton.DOAnchorPos(hintButtonShownPos + Vector2.up * SlideButtonsOffset, SlideDuration).SetEase(Ease.InCubic);
        readyButton.DOAnchorPos(readyButtonShownPos + Vector2.down * SlideButtonsOffset, SlideDuration).SetEase(Ease.InCubic);
        inventorySidebar.DOAnchorPos(inventoryShownPos + Vector2.left * SlideInventoryOffset, SlideDuration).SetEase(Ease.InCubic);
    }

    void OnVictory()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }

    public void OnReadyButtonPressed()
    {
        LevelManager.Current.ExitEditMode();
        DOVirtual.DelayedCall(0f, AnimateOut);
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
