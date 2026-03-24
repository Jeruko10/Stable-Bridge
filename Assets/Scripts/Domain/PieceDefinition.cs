using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Piece Definition")]
public class PieceDefinition : ScriptableObject
{
    public string Id;

    public List<Connection> Connections = new();
    public List<Vector3Int> WalkablePoints = new();

    public float Mass;
    public Vector3 CenterOfMass;

    public List<ContactPoint> ContactPoints = new();
}