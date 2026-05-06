using UnityEngine;
using UnityEngine.UI;

public class UserInterfaceManager : MonoBehaviour
{
    [field: SerializeField] public Button RotateButton { get; private set; }
    [field: SerializeField] public Button FlipButton { get; private set; }
}
