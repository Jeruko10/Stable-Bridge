using UnityEngine;

[System.Serializable]
public class BlockPlacement
{
    [field: SerializeField] public Block BlockPrefab { get; set; }
    [field: SerializeField] public Block.Mobility MobilityType { get; set; } = Block.Mobility.Free;
    [field: SerializeField] public int PivotIndex { get; set; } = -1;
    [field: SerializeField] public bool Mirrored { get; set; } = false;
    [field: SerializeField] public Vector2Int StartingTile { get; set; } = Vector2Int.zero;
}