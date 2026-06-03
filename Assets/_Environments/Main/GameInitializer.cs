using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    void Awake()
    {
        SceneManager.LoadScene("Menu");
    }
}
