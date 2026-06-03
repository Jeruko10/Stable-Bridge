using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    void Awake()
    {
        SceneTransitionManager.LoadScene("Menu");
    }
}
