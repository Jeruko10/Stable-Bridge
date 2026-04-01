using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockPlacementData
{
    [field: SerializeField] public Block BlockPrefab { get; private set; }
    [field: SerializeField] public Block.Mobility MobilityType { get; private set; } = Block.Mobility.Free;
    [field: SerializeField] public int PivotIndex { get; private set; } = -1;
    [field: SerializeField] public bool Mirrored { get; private set; } = false;
    [field: SerializeField] public BoardGrid.Rotation StartingRotation { get; private set; }
    [field: SerializeField] public Vector2Int StartingTile { get; private set; } = Vector2Int.zero;
    [field: SerializeField] public List<Vector2Int> SlideTiles { get; private set; } = new();
}