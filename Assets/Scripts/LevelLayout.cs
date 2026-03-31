using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Level")]
public class LevelLayout : ScriptableObject
{
    [field: SerializeField] public Vector2Int LevelSize { get; private set; } = new();
    [field: SerializeField] public Vector2Int StartPosition { get; private set; } = new();
    [field: SerializeField] public Vector2Int EndPosition { get; private set; } = new();
    [field: SerializeField] public List<BlockPlacementData> Blocks { get; private set; } = new();
}