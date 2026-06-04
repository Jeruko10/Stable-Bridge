using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PopUpWindow : MonoBehaviour
{
    [SerializeField] float animHeight = 1000f;
    [SerializeField] float duration = 0.35f;
    [SerializeField] float backgroundAlpha = 0.7f;

    public UnityEvent onHidden;

    RectTransform rectTransform;
    CanvasGroup background;
    Vector2 shownPos;
    Vector2 hiddenPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        shownPos = rectTransform.anchoredPosition;
        hiddenPos = shownPos + Vector2.up * animHeight;
        rectTransform.anchoredPosition = hiddenPos;

        background = CreateBackground();

        gameObject.SetActive(false);
    }

    CanvasGroup CreateBackground()
    {
        var bgGO = new GameObject("PopUpBackground");
        bgGO.transform.SetParent(transform.parent, false);
        bgGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

        var image = bgGO.AddComponent<Image>();
        image.color = Color.black;

        var rect = bgGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = bgGO.AddComponent<CanvasGroup>();
        bg.alpha = 0f;
        bg.blocksRaycasts = false;

        return bg;
    }

    public void Show()
    {
        gameObject.SetActive(true);

        rectTransform.DOKill();
        rectTransform.DOAnchorPos(shownPos, duration).SetEase(Ease.OutCubic).SetUpdate(true);

        background.DOKill();
        background.blocksRaycasts = true;
        background.DOFade(backgroundAlpha, duration).SetUpdate(true);
    }

    public void Hide()
    {
        rectTransform.DOKill();
        rectTransform.DOAnchorPos(hiddenPos, duration).SetEase(Ease.InCubic).SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));

        background.DOKill();
        background.DOFade(0f, duration).SetUpdate(true)
            .OnComplete(() =>
            {
                background.blocksRaycasts = false;
                onHidden.Invoke();
            });
    }
}
