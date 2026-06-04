using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] float duration = 0.5f;
    [SerializeField] float holdDuration = 0.2f;
    [SerializeField] float maxScale = 1.5f;
    [SerializeField] Shader transitionShader;
    [SerializeField] Sprite silouetteSprite;
    [SerializeField] AudioEntry transitionInSound;
    [SerializeField] AudioEntry transitionOutSound;

    Canvas canvas;
    Material material;
    bool transitioning;

    static SceneTransitionManager instance;
    static readonly int ScaleId = Shader.PropertyToID("_Scale");

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    public static void LoadScene(string sceneName, System.Action beforeLoad = null)
    {
        if (!instance.transitioning)
            instance.StartCoroutine(instance.Transition(sceneName, beforeLoad));
    }

    void BuildUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        gameObject.AddComponent<CanvasScaler>();

        GameObject overlayGO = new("Overlay");
        overlayGO.transform.SetParent(canvas.transform, false);

        Image img = overlayGO.AddComponent<Image>();
        img.raycastTarget = false;

        material = new Material(transitionShader);
        if (silouetteSprite != null)
            material.mainTexture = silouetteSprite.texture;

        img.material = material;

        var rect = img.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        material.SetFloat(ScaleId, maxScale);
        canvas.enabled = false;
    }

    IEnumerator Transition(string sceneName, System.Action beforeLoad)
    {
        transitioning = true;
        canvas.enabled = true;

        // Close: shrink the hole to zero (screen goes black)
        AudioManager.Play(transitionInSound);
        yield return Animate(maxScale, 0f);

        beforeLoad?.Invoke();
        SceneManager.LoadScene(sceneName);

        float elapsed = 0f;
        while (elapsed < holdDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Open: grow the hole to max (new scene revealed)
        AudioManager.Play(transitionOutSound);
        yield return Animate(0f, maxScale);

        canvas.enabled = false;
        transitioning = false;
    }

    IEnumerator Animate(float from, float to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(t + Time.unscaledDeltaTime / duration, 1f);
            float s = t * t * (3f - 2f * t); // smoothstep
            material.SetFloat(ScaleId, Mathf.Lerp(from, to, s));
            yield return null;
        }
    }
}
