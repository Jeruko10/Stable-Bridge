using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Level")]
public class LevelData : ScriptableObject
{
    [field: SerializeField] public Vector2Int LevelSize { get; set; } = new();
    [field: SerializeField] public List<BlockPlacement> Blocks { get; set; } = new();
}