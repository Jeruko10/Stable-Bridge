using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardGrid : MonoBehaviour
{
    [field: SerializeField] public float TileSize { get; set; } = 1f;
    [field: SerializeField] public GameObject TileVisualPrefab { get; set; }
    public Vector2Int Size { get; private set; }
    public enum Rotation { Deg0, Deg90, Deg180, Deg270 }

    GameObject visualsFolder;
    readonly HashSet<Block> blocks = new();
    readonly Dictionary<Vector2Int, BlockSegment> tileBlocks = new();
    readonly Dictionary<BlockSegment, Vector2Int> blockTiles = new();
    readonly Dictionary<Vector2Int, GameObject> tileVisuals = new();

    void Awake()
    {
        visualsFolder = new("Visuals");
    }

    public void Initialize(Vector2Int size)
    {
        Size = size;
        tileBlocks.Clear();
        blockTiles.Clear();
        tileVisuals.Clear();
        blocks.Clear();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int tileCoord = new(x, y);
                tileBlocks[tileCoord] = null;
                GameObject instance = Instantiate(TileVisualPrefab, TileToWorld(tileCoord), Quaternion.identity, visualsFolder.transform);
                tileVisuals.Add(tileCoord, instance);
            }
        }
    }

    public void AddRow(bool isVisual)
    {
        int newY = Size.y;
        Size += Vector2Int.up;

        for (int x = 0; x < Size.x; x++)
        {
            Vector2Int tileCoord = new(x, newY);
            tileBlocks[tileCoord] = null;

            if (!isVisual) continue;

            GameObject instance = Instantiate(TileVisualPrefab, TileToWorld(tileCoord), Quaternion.identity, visualsFolder.transform);
            tileVisuals.Add(tileCoord, instance);
        }
    }

    public Dictionary<Vector2Int, BlockSegment> GetAllTiles() => tileBlocks;

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

    public Vector3 GetGridCenter() => new(Size.x * TileSize / 2, Size.y * TileSize / 2);

    public bool IsValidTile(Vector2Int tile) => tile.x >= 0 && tile.x < Size.x && tile.y >= 0 && tile.y < Size.y;

    public BlockSegment GetBlockAtTile(Vector2Int tile)
    {
        if (!IsValidTile(tile))
        {
            Debug.LogWarning($"Querying out-of-bounds tile {tile}");
            return null;
        }

        tileBlocks.TryGetValue(tile, out var block);
        return block;
    }

    public Vector2Int? GetTileOfBlock(BlockSegment block)
    {
        if (!blockTiles.TryGetValue(block, out var tile))
        {
            Debug.LogWarning($"Block {block.name} is not placed on the grid");
            return null;
        }

        return tile;
    }

    public IEnumerable<Vector2Int> GetAdjacents(Vector2Int tile)
    {
        if (!IsValidTile(tile)) yield break;

        Vector2Int[] directions = new Vector2Int[]
        {
            new(0, 1), // Up
            new(1, 0), // Right
            new(0, -1), // Down
            new(-1, 0) // Left
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborTile = tile + dir;

            if (IsValidTile(neighborTile)) yield return neighborTile;
        }
    }

    public bool ContainsBlock(Block block) => blocks.Contains(block);

    public bool ContainsSegment(BlockSegment segment) => tileBlocks.Values.Contains(segment);

    public void RemoveBlock(Block block)
    {
        blocks.Remove(block);
        List<Vector2Int> tilesToClear = new();

        foreach (var kvp in tileBlocks)
            if (kvp.Value != null && block.Segments.Contains(kvp.Value))
                tilesToClear.Add(kvp.Key);
        
        foreach (var tile in tilesToClear)
            tileBlocks[tile] = null;
    }

    public bool TryPlaceBlock(Block block, Vector2Int pivotTile, BlockSegment pivotSegment)
    {
        Vector3 requiredMovement = TileToWorld(pivotTile) - pivotSegment.transform.position;

        foreach (BlockSegment segment in block.Segments)
        {
            Vector2Int tile = WorldToTile(segment.transform.position + requiredMovement);
            
            if (!IsValidTile(tile) || (tileBlocks.TryGetValue(tile, out BlockSegment existing) && existing != null && !block.Segments.Contains(existing)))
                return false;
        }

        foreach (BlockSegment segment in block.Segments)
        {
            Vector2Int tile = WorldToTile(segment.transform.position + requiredMovement);
            tileBlocks[tile] = segment;
            blockTiles[segment] = tile;
        }
        
        block.Position2D = block.transform.position + requiredMovement;
        blocks.Add(block);

        return true;
    }

    public bool TryRotateBlock(Block block, bool clockwise)
    {
        Vector2Int pivotTile = WorldToTile(block.Pivot.transform.position);
        
        RemoveBlock(block);
        block.Rotate(block.Pivot, clockwise);

        if (TryPlaceBlock(block, pivotTile, block.Pivot)) return true;

        block.Rotate(block.Pivot, !clockwise);
        TryPlaceBlock(block, pivotTile, block.Pivot);

        return false;
    }

    public bool TrySlideBlock(Block block)
    {
        if (block.SlidePositions == null || block.SlidePositions.Length <= 1) return false;

        Vector2Int pivotTile = WorldToTile(block.Pivot.transform.position);
        int length = block.SlidePositions.Length;

        RemoveBlock(block);

        for (int i = 1; i < length; i++)
        {
            int targetIndex = (block.SlideIndex + i) % length;
            if (TryPlaceBlock(block, block.SlidePositions[targetIndex], block.Pivot)) { block.SlideIndex = targetIndex; return true; }
        }

        TryPlaceBlock(block, pivotTile, block.Pivot);
        return false;
    }
}