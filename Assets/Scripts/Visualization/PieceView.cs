using UnityEngine;

public class PieceView : MonoBehaviour
{
    [field: SerializeField] public PieceDefinition Definition { get; set; }

    public void Initialize(PieceInstance instance)
    {
        Definition = instance.Definition;
        transform.position = instance.Position;
    }
}