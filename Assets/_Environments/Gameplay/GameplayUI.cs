using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameplayUI : MonoBehaviour
{
    [Header("Animations")]
    [SerializeField] float slideButtonsOffset = 300f;
    [SerializeField] float slideInventoryOffset = 400f;
    [SerializeField] float slideDuration = 0.4f;
    [SerializeField] float notReadyAlpha = 0.5f;

    [Header("References")]
    [SerializeField] PopUpWindow pauseMenu;
    [SerializeField] RectTransform hintButton;
    [SerializeField] RectTransform readyButton;
    [SerializeField] RectTransform inventorySidebar;
    [SerializeField] CanvasGroup pauseMenuButtons;
    [SerializeField] TMP_Text hintsRemainingText;

    Vector2 hintButtonShownPos;
    Vector2 readyButtonShownPos;
    Vector2 inventoryShownPos;
    CanvasGroup readyButtonGroup;

    Level currentLevel;
    HintRenderer currentHintRenderer;

    void Start()
    {
        hintButtonShownPos = hintButton.anchoredPosition;
        readyButtonShownPos = readyButton.anchoredPosition;
        inventoryShownPos = inventorySidebar.anchoredPosition;
        readyButtonGroup = readyButton.GetComponent<CanvasGroup>();

        pauseMenuButtons.interactable = false;
        pauseMenu.onShown.AddListener(() => pauseMenuButtons.interactable = true);

        LevelManager.Victory += OnVictory;
        LevelManager.LevelLoaded += OnLevelLoaded;
        LevelManager.LoadLevel(LevelSelectorUI.PendingLevelIndex);
    }

    void Update()
    {
        if (currentLevel == null || !currentLevel.IsEditing) return;

        bool ready = IsLevelReady();
        readyButtonGroup.interactable = ready;
        readyButtonGroup.alpha = ready ? 1f : notReadyAlpha;
    }

    bool IsLevelReady() =>
        currentLevel.Inventory.All(block => currentLevel.Grid.ContainsBlock(block)) &&
        currentLevel.Grid.GetAllBlocks().All(block => !block.IsOverlapping);

    void OnDestroy()
    {
        LevelManager.Victory -= OnVictory;
        LevelManager.LevelLoaded -= OnLevelLoaded;
        if (currentLevel != null) currentLevel.LevelComplete -= OnLevelComplete;
        if (currentHintRenderer != null) currentHintRenderer.HintsRemainingChanged -= OnHintsRemainingChanged;
    }

    void OnLevelLoaded(Level level)
    {
        if (currentLevel != null) currentLevel.LevelComplete -= OnLevelComplete;
        if (currentHintRenderer != null) currentHintRenderer.HintsRemainingChanged -= OnHintsRemainingChanged;

        currentLevel = level;
        currentLevel.LevelComplete += OnLevelComplete;

        currentHintRenderer = level.GetComponent<HintRenderer>();
        if (currentHintRenderer != null)
        {
            currentHintRenderer.HintsRemainingChanged += OnHintsRemainingChanged;
            OnHintsRemainingChanged(currentHintRenderer.HintsRemaining);
        }

        AnimateIn();
    }

    void OnHintsRemainingChanged(int remaining)
    {
        if (hintsRemainingText != null) hintsRemainingText.text = $"{remaining}";
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
        hintButton.DOAnchorPos(hintButtonShownPos, slideDuration).SetEase(Ease.OutCubic);
        readyButton.DOAnchorPos(readyButtonShownPos, slideDuration).SetEase(Ease.OutCubic);
        inventorySidebar.DOAnchorPos(inventoryShownPos, slideDuration).SetEase(Ease.OutCubic);
    }

    void AnimateOut()
    {
        hintButton.DOKill();
        readyButton.DOKill();
        inventorySidebar.DOKill();
        hintButton.DOAnchorPos(hintButtonShownPos + Vector2.up * slideButtonsOffset, slideDuration).SetEase(Ease.InCubic);
        readyButton.DOAnchorPos(readyButtonShownPos + Vector2.down * slideButtonsOffset, slideDuration).SetEase(Ease.InCubic);
        inventorySidebar.DOAnchorPos(inventoryShownPos + Vector2.left * slideInventoryOffset, slideDuration).SetEase(Ease.InCubic);
    }

    void OnVictory()
    {
        Time.timeScale = 1f;
        LevelSelectorUI.CameFromGameplay = true;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }

    public void OnReadyButtonPressed()
    {
        if (currentLevel != null && currentLevel.IsEditing && !IsLevelReady()) return;

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
        LevelSelectorUI.CameFromGameplay = true;
        SceneTransitionManager.LoadScene("LevelSelector", LevelManager.ExitLevel);
    }
}
