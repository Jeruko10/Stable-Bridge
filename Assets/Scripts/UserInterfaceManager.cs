using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserInterfaceManager : MonoBehaviour
{
    enum UIState { MainMenu, LevelSelector, Gameplay }

    [Header("Environments")]
    [SerializeField] GameObject gameplayInterface;
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject levelSelector;
    [SerializeField] GameObject mainMenu;
    
    [Header("References")]
    [SerializeField] ScrollRect levelSelectorScroll;
    [SerializeField] GameObject levelButtonPrefab;
    [SerializeField] Transform topLayout;
    [SerializeField] Transform bottomLayout;

    HintManager currentHints;

    void Start()
    {
        LevelManager.Victory += OnVictory;
        LevelManager.LevelLoaded += OnLevelLoaded;
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
        {
            levelSelectorScroll.horizontalNormalizedPosition = 0f;
        }
        else if (state == UIState.MainMenu)
        {
            AudioManager.StopAll();
            AudioManager.Play(AudioManager.Instance.MenuTheme);
        }
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
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        ShowState(UIState.Gameplay);
        LevelManager.LoadLevel(index);
    }

    void OnVictory()
    {
        ShowState(UIState.LevelSelector);
    }

    void OnLevelLoaded(Level level)
    {
        currentHints = level.GetComponent<HintManager>();
    }

    public void OnPlayButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        ShowState(UIState.LevelSelector);
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
        // currentHints.DisplayTestHint();
        currentHints.HighlightBlock(LevelManager.Current.Inventory.FirstOrDefault());
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
        ShowState(UIState.LevelSelector);
    }
}
