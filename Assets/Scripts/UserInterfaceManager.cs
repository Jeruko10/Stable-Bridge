using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UserInterfaceManager : MonoBehaviour
{
    VisualElement root;
    Button rotateButton, flipButton;

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        rotateButton = root.Q<Button>("RotateButton");
        flipButton = root.Q<Button>("FlipButton");

        rotateButton.clicked += OnRotateButtonClicked;
        flipButton.clicked += OnFlipButtonClicked;
    }

    void Update()
    {
        
    }

    void OnRotateButtonClicked()
    {
        
    }

    void OnFlipButtonClicked()
    {
        
    }    
}
