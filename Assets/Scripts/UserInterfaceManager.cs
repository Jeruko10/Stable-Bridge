using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UserInterfaceManager : MonoBehaviour
{
    VisualElement root;
    public Button RotateButton { get; private set; }
    public Button FlipButton { get; private set; }

    void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        RotateButton = root.Q<Button>("RotateButton");
        FlipButton = root.Q<Button>("FlipButton");
    }

    void Update()
    {
        
    }
}
