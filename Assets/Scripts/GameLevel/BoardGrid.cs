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
    readonly Dictionary<Vector2Int, BlockSegment> tiles = new();
    readonly Dictionary<Vector2Int, GameObject> tileVisuals = new();

    void Awake()
    {
        visualsFolder = new("Visuals");
    }

    public void Initialize(Vector2Int size)
    {
        Size = size;
        tiles.Clear();
        tileVisuals.Clear();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int tileCoord = new(x, y);
                tiles[tileCoord] = null;
                GameObject instance = Instantiate(TileVisualPrefab, TileToWorld(tileCoord), Quaternion.identity, visualsFolder.transform);
                tileVisuals.Add(tileCoord, instance);
            }
        }
    }

    public IEnumerable<BlockSegment> GetAllSegments() => tiles.Values.Where(segment => segment != null);

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

    public Dictionary<Vector2Int, BlockSegment> GetNeighbors(BlockSegment segment)
    {
        Dictionary<Vector2Int, BlockSegment> neighbors = new();
        Vector2Int segmentTile = WorldToTile(segment.transform.position);

        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip center tile

                Vector2Int relativeDir = new(x, y);
                Vector2Int neighborCoord = segmentTile + relativeDir;

                if (IsValidTile(neighborCoord)) 
                    neighbors.Add(relativeDir, GetBlockAtTile(neighborCoord));
            }

        return neighbors;
    }

    public bool ContainsBlock(Block block) => blocks.Contains(block);

    public bool ContainsSegment(BlockSegment block) => tiles.Values.Contains(block);

    public void RemoveBlock(Block block)
    {
        blocks.Remove(block);
        List<Vector2Int> tilesToClear = new();

        foreach (var kvp in tiles)
            if (kvp.Value != null && block.Segments.Contains(kvp.Value))
                tilesToClear.Add(kvp.Key);
        
        foreach (var tile in tilesToClear)
            tiles[tile] = null;
    }

    public bool TryPlaceBlock(Block block, Vector2Int pivotTile, BlockSegment pivotSegment)
    {
        Vector3 requiredMovement = TileToWorld(pivotTile) - pivotSegment.transform.position;

        foreach (BlockSegment segment in block.Segments)
        {
            Vector2Int tile = WorldToTile(segment.transform.position + requiredMovement);
            
            if (!IsValidTile(tile) || (tiles.TryGetValue(tile, out BlockSegment existing) && existing != null && !block.Segments.Contains(existing)))
                return false;
        }

        foreach (BlockSegment segment in block.Segments)
            tiles[WorldToTile(segment.transform.position + requiredMovement)] = segment;
        
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