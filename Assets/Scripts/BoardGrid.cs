using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardGrid : MonoBehaviour
{
    public int Width = 6;
    public int Height = 5;
    public float TileSize = 1f;
    public GameObject TileVisualPrefab;

    public Dictionary<Vector2Int, BlockSegment> Tiles = new();
    public Dictionary<Vector2Int, GameObject> TileVisuals = new();
    public event Action Initialized;

    void Start()
    {
        InitializeTiles();
    }

    void Update()
    {
        foreach (var pair in TileVisuals)
            pair.Value.transform.position = TileToWorld(pair.Key);
    }

    void InitializeTiles()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                Vector2Int tileCoord = new(x, y);
                Tiles[tileCoord] = null;
                Vector3 worldPos = TileToWorld(tileCoord);
                GameObject instance = Instantiate(TileVisualPrefab, worldPos, Quaternion.identity, transform);
                TileVisuals.Add(tileCoord, instance);
            }

        Initialized?.Invoke();
    }

    public Vector3 TileToWorld(Vector2Int tile) => new(tile.x * TileSize, tile.y * TileSize);

    public Vector2Int WorldToTile(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / TileSize);
        int y = Mathf.RoundToInt(worldPos.z / TileSize);
        Vector2Int tile = new(x, y);

        if (!IsValidTile(tile))
            Debug.LogWarning($"Tile {tile} is out of bounds!");

        return tile;
    }

    public Vector3 GetGridCenter() => new(Width / 2, Height / 2);

    public bool IsValidTile(Vector2Int tile) => tile.x >= 0 && tile.x < Width && tile.y >= 0 && tile.y < Height;

    public void SetBlockAtTile(Vector2Int tile, BlockSegment block)
    {
        if (!IsValidTile(tile))
        {
            Debug.LogWarning($"Cannot place block on out-of-bounds tile {tile}");
            return;
        }

        Tiles[tile] = block;
    }

    public BlockSegment GetBlockAtTile(Vector2Int tile)
    {
        if (!IsValidTile(tile))
        {
            Debug.LogWarning($"Querying out-of-bounds tile {tile}");
            return null;
        }

        Tiles.TryGetValue(tile, out var block);
        return block;
    }

    public bool IsBlockOnGrid(BlockSegment block, out Vector2Int tileCoord)
    {
        foreach (var kvp in Tiles)
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