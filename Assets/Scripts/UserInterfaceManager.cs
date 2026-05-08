using UnityEngine;

public class UserInterfaceManager : MonoBehaviour
{
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void OnReadyButtonClicked() // Connected through the editor
    {
        LevelManager.Current.ExitEditMode();
    }

    public void OnPauseButtonClicked() // Connected through the editor
    {
        
    }

    public void OnHintButtonClicked() // Connected through the editor
    {
        
    }
}
