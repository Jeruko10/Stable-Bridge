using DG.Tweening;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    [SerializeField] RectTransform gameTitle;
    [SerializeField] float swingAngle = 6f;
    [SerializeField] float swingDuration = 1.4f;

    void Start()
    {
        AudioManager.StopAll();
        AudioManager.Play(AudioManager.Instance.MenuTheme);

        gameTitle.DOLocalRotate(new Vector3(0f, 0f, -swingAngle), swingDuration)
            .From(new Vector3(0f, 0f, swingAngle))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void OnPlayButtonPressed()
    {
        SceneTransitionManager.LoadScene("LevelSelector");
    }

    public void OnMasterButtonPressed()
    {
        //TODO
    }
}
