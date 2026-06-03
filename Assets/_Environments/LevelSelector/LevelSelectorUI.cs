using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectorUI : MonoBehaviour
{
    public static int PendingLevelIndex { get; private set; }

    [SerializeField] ScrollRect scroll;
    [SerializeField] GameObject levelButtonPrefab;
    [SerializeField] Transform topLayout;
    [SerializeField] Transform bottomLayout;

    void Start()
    {
        PopulateLevelButtons();
        scroll.horizontalNormalizedPosition = 0f;
    }

    void PopulateLevelButtons()
    {
        foreach (Transform child in topLayout) Destroy(child.gameObject);
        foreach (Transform child in bottomLayout) Destroy(child.gameObject);

        int count = Resources.LoadAll<LevelLayout>("Levels").Length;
        for (int i = 0; i < count; i++)
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
        PendingLevelIndex = index;
        SceneTransitionManager.LoadScene("Gameplay");
    }
}
