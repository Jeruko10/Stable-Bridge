using UnityEngine;

public class PieceInstance
{
    public PieceDefinition Definition { get; }
    public Vector3Int Position { get; }

    public PieceInstance(PieceDefinition definition, Vector3Int position)
    {
        Definition = definition;
        Position = position;
    }
}