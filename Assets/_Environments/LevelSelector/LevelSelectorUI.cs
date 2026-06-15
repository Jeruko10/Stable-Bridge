using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectorUI : MonoBehaviour
{
    public static int PendingLevelIndex { get; private set; }
    public static bool CameFromGameplay { get; set; }

    [SerializeField] ScrollRect scroll;
    [SerializeField] GameObject levelButtonPrefab;
    [SerializeField] Transform topLayout;
    [SerializeField] Transform bottomLayout;

    void Start()
    {
        PopulateLevelButtons();

        int targetIndex = CameFromGameplay
            ? PendingLevelIndex
            : SaveManager.Instance.HighestUnlockedLevel;

        CameFromGameplay = false;
        StartCoroutine(ScrollToLevelNextFrame(targetIndex));
    }

    IEnumerator ScrollToLevelNextFrame(int levelIndex)
    {
        yield return null;
        ScrollToLevel(levelIndex);
    }

    public void ScrollToLevel(int levelIndex)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        Canvas.ForceUpdateCanvases();

        string buttonName = $"LevelButton.{levelIndex + 1}";
        RectTransform btn = topLayout.Find(buttonName) as RectTransform;
        if (btn == null) btn = bottomLayout.Find(buttonName) as RectTransform;

        if (btn == null) return;

        float contentWidth = scroll.content.rect.width;
        float viewportWidth = ((RectTransform)scroll.transform).rect.width;

        if (contentWidth <= viewportWidth)
        {
            scroll.horizontalNormalizedPosition = 0f;
            return;
        }

        Vector2 btnLocalPos = (Vector2)scroll.content.InverseTransformPoint(btn.position);
        float btnFromLeft = btnLocalPos.x + scroll.content.pivot.x * contentWidth;
        float targetX = btnFromLeft - viewportWidth / 2f;
        scroll.horizontalNormalizedPosition = Mathf.Clamp01(targetX / (contentWidth - viewportWidth));
    }

    public void OnExitButtonPressed()
    {
        SceneTransitionManager.LoadScene("Menu");
    }

    void PopulateLevelButtons()
    {
        foreach (Transform child in topLayout) Destroy(child.gameObject);
        foreach (Transform child in bottomLayout) Destroy(child.gameObject);

        int highest = SaveManager.Instance.HighestUnlockedLevel;
        int count = Resources.LoadAll<LevelLayout>("Levels").Length;
        for (int i = 0; i < count; i++)
        {
            int index = i + 1;
            Transform parent = (index % 2 == 0) ? topLayout : bottomLayout;
            GameObject btn = Instantiate(levelButtonPrefab, parent);
            btn.name = $"LevelButton.{index}";
            btn.GetComponentInChildren<TMP_Text>().text = index.ToString();

            Button button = btn.GetComponent<Button>();
            bool unlocked = i <= highest;
            button.interactable = unlocked;
            if (unlocked) button.onClick.AddListener(() => OnLevelButtonPressed(index - 1));
        }
    }

    void OnLevelButtonPressed(int index)
    {
        PendingLevelIndex = index;
        SceneTransitionManager.LoadScene("Gameplay");
    }
}
