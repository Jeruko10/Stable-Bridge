using UnityEngine;

public class MenuUI : MonoBehaviour
{
    void Start()
    {
        AudioManager.StopAll();
        AudioManager.Play(AudioManager.Instance.MenuTheme);
    }

    public void OnPlayButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        SceneTransitionManager.LoadScene("LevelSelector");
    }

    public void OnMasterButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        //TODO
    }
}
