using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardGrid : MonoBehaviour
{
    [field: SerializeField] public int Width { get; set; } = 6;
    [field: SerializeField] public int Height { get; set; } = 5;
    [field: SerializeField] public float TileSize { get; set; } = 1.1f;
    [field: SerializeField] public GameObject TileVisualPrefab { get; set; }

    readonly Dictionary<Vector2Int, BlockSegment> tiles = new();
    readonly Dictionary<Vector2Int, GameObject> tileVisuals = new();
    public event Action Initialized;

    public enum Rotation { Deg0, Deg90, Deg180, Deg270 }

    void Start() => InitializeTiles();

    void Update()
    {
        foreach (var pair in tileVisuals) pair.Value.transform.position = TileToWorld(pair.Key);
    }

    void InitializeTiles()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Vector2Int tileCoord = new(x, y);
                tiles[tileCoord] = null;
                GameObject instance = Instantiate(TileVisualPrefab, TileToWorld(tileCoord), Quaternion.identity, transform);
                tileVisuals.Add(tileCoord, instance);
            }
        }
        Initialized?.Invoke();
    }

    public static Quaternion GetDiscreteRotation(Rotation rotation) => rotation switch
    {
        Rotation.Deg0 => Quaternion.Euler(0, 0, 0),
        Rotation.Deg90 => Quaternion.Euler(0, 0, 90),
        Rotation.Deg180 => Quaternion.Euler(0, 0, 180),
        Rotation.Deg270 => Quaternion.Euler(0, 0, 270),
        _ => Quaternion.identity
    };

    public Vector3 TileToWorld(Vector2Int tile) => new(tile.x * TileSize, tile.y * TileSize);

    public Vector2Int WorldToTile(Vector3 worldPos)
    {
        Vector2Int tile = new(Mathf.RoundToInt(worldPos.x / TileSize), Mathf.RoundToInt(worldPos.y / TileSize));
        if (!IsValidTile(tile)) Debug.LogWarning($"Tile {tile} is out of bounds!");
        return tile;
    }

    public Vector3 GetGridCenter() => new(Width * TileSize / 2, Height * TileSize / 2);

    public bool IsValidTile(Vector2Int tile) => tile.x >= 0 && tile.x < Width && tile.y >= 0 && tile.y < Height;

    public void SetBlockAtTile(Vector2Int tile, BlockSegment block)
    {
        if (!IsValidTile(tile)) Debug.LogWarning($"Cannot place block on out-of-bounds tile {tile}");
        else tiles[tile] = block;
    }

    public BlockSegment GetBlockAtTile(Vector2Int tile)
    {
        if (!IsValidTile(tile))
        {
            Debug.LogWarning($"Querying out-of-bounds tile {tile}");
            return null;
        }

        tiles.TryGetValue(tile, out var block);
        return block;
    }

    public void RemoveBlock(Block block)
    {
        List<Vector2Int> tilesToClear = new();

        foreach (var kvp in tiles)
            if (kvp.Value != null && block.Segments.Contains(kvp.Value))
                tilesToClear.Add(kvp.Key);
        
        foreach (var tile in tilesToClear)
            tiles[tile] = null;
    }

    public bool CanPlaceBlock(Block block, Vector2Int pivotTile, BlockSegment pivotSegment)
    {
        Vector3 requiredMovement = TileToWorld(pivotTile) - pivotSegment.transform.position;

        foreach (BlockSegment segment in block.Segments)
        {
            Vector2Int tile = WorldToTile(segment.transform.position + requiredMovement);
            
            if (!IsValidTile(tile) || (tiles.TryGetValue(tile, out BlockSegment existing) && existing != null && !block.Segments.Contains(existing)))
                return false;
        }

        return true;
    }

    public void PlaceBlock(Block block, Vector2Int pivotTile, BlockSegment pivotSegment)
    {
        block.transform.position += TileToWorld(pivotTile) - pivotSegment.transform.position;
        foreach (BlockSegment segment in block.Segments) tiles[WorldToTile(segment.transform.position)] = segment;
    }

    public bool IsBlockOnGrid(BlockSegment block, out Vector2Int tileCoord)
    {
        foreach (var kvp in tiles)
        {
            if (kvp.Value == block)
            {
                tileCoord = kvp.Key;
                return true;
            }
        }

        tileCoord = new Vector2Int(-1, -1);
        return false;
    }
}