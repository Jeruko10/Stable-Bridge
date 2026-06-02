using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    void Start()
    {
        AudioManager.StopAll();
        AudioManager.Play(AudioManager.Instance.MenuTheme);
    }

    public void OnPlayButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        SceneManager.LoadScene("LevelSelector");
    }

    public void OnMasterButtonPressed()
    {
        AudioManager.Play(AudioManager.Instance.UIButtonClick);
        //TODO
    }
}
