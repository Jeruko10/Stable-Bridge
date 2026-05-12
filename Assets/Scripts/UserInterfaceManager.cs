using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserInterfaceManager : MonoBehaviour
{
    enum UIState { MainMenu, LevelSelector, Gameplay }

    [field: SerializeField] GameObject gameplayInterface;
    [field: SerializeField] GameObject pauseMenu;
    [field: SerializeField] GameObject levelSelector;
    [field: SerializeField] GameObject mainMenu;
    [SerializeField] ScrollRect levelSelectorScroll;
    [SerializeField] GameObject levelButtonPrefab;
    [SerializeField] Transform topLayout;
    [SerializeField] Transform bottomLayout;

    void Start()
    {
        LevelManager.Victory += OnVictory;
        PopulateLevelButtons();
        ShowState(UIState.MainMenu);
    }

    void ShowState(UIState state)
    {
        mainMenu.SetActive(state == UIState.MainMenu);
        levelSelector.SetActive(state == UIState.LevelSelector);
        gameplayInterface.SetActive(state == UIState.Gameplay);
        pauseMenu.SetActive(false);

        if (state == UIState.LevelSelector)
            levelSelectorScroll.horizontalNormalizedPosition = 0f;
    }

    void PopulateLevelButtons()
    {
        foreach (Transform child in topLayout) Destroy(child.gameObject);
        foreach (Transform child in bottomLayout) Destroy(child.gameObject);

        for (int i = 0; i < LevelManager.LevelAmount; i++)
        {
            int index = i + 1;
            Transform parent = (index % 2 == 0) ? topLayout : bottomLayout;
            GameObject btn = Instantiate(levelButtonPrefab, parent);
            btn.name = $"LevelButton.{index}";
            btn.GetComponentInChildren<TMP_Text>().text = index.ToString();
            btn.GetComponent<Button>().onClick.AddListener(() => OnLevelButtonPressed(index - 1));
        }
    }

    void OnLevelButtonPressed(int index)
    {
        ShowState(UIState.Gameplay);
        LevelManager.LoadLevel(index);
    }

    void OnVictory()
    {
        ShowState(UIState.LevelSelector);
    }

    public void OnTestLevelButtonPressed()
    {
        ShowState(UIState.Gameplay);
        LevelManager.LoadLevel(0);
    }

    public void OnPlayButtonPressed()
    {
        ShowState(UIState.LevelSelector);
    }

    public void OnReadyButtonPressed()
    {
        LevelManager.Current.ExitEditMode();
    }

    public void OnPauseButtonPressed()
    {
        pauseMenu.SetActive(true);
    }

    public void OnHintButtonPressed()
    {
        // TODO
    }

    public void OnResumeButtonPressed()
    {
        pauseMenu.SetActive(false);
    }

    public void OnMenuButtonPressed()
    {
        LevelManager.ExitLevel();
        ShowState(UIState.LevelSelector);
    }
}
