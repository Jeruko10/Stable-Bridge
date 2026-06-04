using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PopButton : MonoBehaviour
{
    [SerializeField] float punchStrength = 0.22f;
    [SerializeField] float duration = 0.45f;
    [SerializeField] int vibrato = 8;
    [SerializeField] float elasticity = 0.6f;
    [SerializeField] AudioEntry clickSound;

    Button button;
    Vector3 originalScale;

    void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
    }

    void OnEnable() => button.onClick.AddListener(OnClick);
    
    void OnDisable() => button.onClick.RemoveListener(OnClick);

    void OnClick()
    {
        transform.DOKill();
        transform.localScale = originalScale;
        transform.DOPunchScale(Vector3.one * punchStrength, duration, vibrato, elasticity).SetUpdate(true);
        AudioManager.Play(clickSound);
    }
}
