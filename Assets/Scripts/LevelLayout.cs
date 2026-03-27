using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Level")]
public class LevelLayout : ScriptableObject
{
    [field: SerializeField] public Vector2Int LevelSize { get; set; } = new();
    [field: SerializeField] public List<BlockPlacementData> Blocks { get; set; } = new();
}