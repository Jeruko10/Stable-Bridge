using System.Collections;
using DG.Tweening;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    [SerializeField] PopUpWindow dataCollectionWindow;
    [SerializeField] RectTransform gameTitle;
    [SerializeField] float swingAngle = 6f;
    [SerializeField] float swingDuration = 1.4f;

    static bool hasShownPopUp;

    void OnDestroy() => gameTitle.DOKill();

    void Start()
    {
        AudioManager.Play(AudioManager.Instance.MenuTheme);

        gameTitle.DOLocalRotate(new Vector3(0f, 0f, -swingAngle), swingDuration)
            .From(new Vector3(0f, 0f, swingAngle))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        if (hasShownPopUp) return;
        hasShownPopUp = true;
        StartCoroutine(WaitAndShowPopUp());
    }

    public void OnPlayButtonPressed()
    {
        SceneTransitionManager.LoadScene("LevelSelector");
    }

    public void OnMasterButtonPressed()
    {
        //TODO
    }

    IEnumerator WaitAndShowPopUp()
    {
        yield return new WaitForSeconds(4f);
        dataCollectionWindow.Show();
    }
}
