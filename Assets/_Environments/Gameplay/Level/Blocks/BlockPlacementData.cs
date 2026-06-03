using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockPlacementData
{
    [field: SerializeField] public Block BlockPrefab { get; private set; }
    [field: SerializeField] public Block.Mobility MobilityType { get; private set; }
    [field: SerializeField] public int PivotIndex { get; private set; }
    [field: SerializeField] public bool Flipped { get; private set; }
    [field: SerializeField] public BoardGrid.Rotation StartingRotation { get; private set; }
    [field: SerializeField] public Vector2Int StartingTile { get; private set; }
    [field: SerializeField] public List<Vector2Int> SlideTiles { get; private set; }

    public BlockPlacementData(
        Block blockPrefab, 
        Block.Mobility mobilityType, 
        int pivotIndex, 
        bool flipped, 
        BoardGrid.Rotation rotation, 
        Vector2Int startingTile, 
        List<Vector2Int> slideTiles)
    {
        BlockPrefab = blockPrefab;
        MobilityType = mobilityType;
        PivotIndex = pivotIndex;
        Flipped = flipped;
        StartingRotation = rotation;
        StartingTile = startingTile;
        SlideTiles = slideTiles ?? new List<Vector2Int>();
    }
}