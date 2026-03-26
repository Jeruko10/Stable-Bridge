using UnityEngine;

[System.Serializable]
public struct BlockPlacement
{
    [field: SerializeField] public Block BlockPrefab { get; set; }
    [field: SerializeField] public Block.Mobility MobilityType { get; set; }
    [field: SerializeField] public Vector2Int StartingTile { get; set; }
}