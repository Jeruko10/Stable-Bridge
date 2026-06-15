using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardGrid : MonoBehaviour
{
    [field: SerializeField] public float TileSize { get; private set; } = BlockSegment.SideLength;

    [field: Header("References")]
    [field: SerializeField] public GameObject TileVisualPrefab { get; private set; }

    public Vector2Int Size { get; private set; }
    public Vector2Int MinTile { get; private set; }
    public enum Rotation { Deg0 = 0, Deg90 = 90, Deg180 = 180, Deg270 = 270 }

    GameObject visualsFolder;
    readonly HashSet<Block> blocks = new();
    readonly Dictionary<Vector2Int, BlockSegment> tileBlocks = new();
    readonly Dictionary<BlockSegment, Vector2Int> blockTiles = new();
    readonly Dictionary<Vector2Int, GameObject> tileVisuals = new();

    void Awake()
    {
        visualsFolder = new("Visuals");
        visualsFolder.transform.SetParent(transform);
    }

    public void Initialize(Vector2Int size)
    {
        Size = size;
        MinTile = Vector2Int.zero;
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
                Vector3 visualPos = TileToWorld(tileCoord);
                visualPos.z = 1f;
                GameObject instance = Instantiate(TileVisualPrefab, visualPos, Quaternion.identity, visualsFolder.transform);
                tileVisuals.Add(tileCoord, instance);
            }
        }
    }

    public void AddRow(bool atTop = true, int count = 1)
    {
        int startY;
        if (atTop)
        {
            startY = MinTile.y + Size.y;
            Size += Vector2Int.up * count;
        }
        else
        {
            MinTile += Vector2Int.down * count;
            startY = MinTile.y;
            Size += Vector2Int.up * count;
        }

        for (int i = 0; i < count; i++)
        {
            for (int x = MinTile.x; x < MinTile.x + Size.x; x++)
            {
                Vector2Int tileCoord = new(x, startY + i);
                tileBlocks[tileCoord] = null;

                GameObject instance = Instantiate(TileVisualPrefab, TileToWorld(tileCoord), Quaternion.identity, visualsFolder.transform);
                tileVisuals.Add(tileCoord, instance);
            }
        }
    }

    public void AddColumn(bool atRight = true, int count = 1)
    {
        int startX;
        if (atRight)
        {
            startX = MinTile.x + Size.x;
            Size += Vector2Int.right * count;
        }
        else
        {
            MinTile += Vector2Int.left * count;
            startX = MinTile.x;
            Size += Vector2Int.right * count;
        }

        for (int i = 0; i < count; i++)
        {
            for (int y = MinTile.y; y < MinTile.y + Size.y; y++)
            {
                Vector2Int tileCoord = new(startX + i, y);
                tileBlocks[tileCoord] = null;

                GameObject instance = Instantiate(TileVisualPrefab, TileToWorld(tileCoord), Quaternion.identity, visualsFolder.transform);
                tileVisuals.Add(tileCoord, instance);
            }
        }
    }

    public void RemoveRow()
    {
        if (Size.y <= 1) return;

        int removeY = MinTile.y + Size.y - 1;

        for (int x = MinTile.x; x < MinTile.x + Size.x; x++)
        {
            Vector2Int tileCoord = new(x, removeY);
            if (tileBlocks.TryGetValue(tileCoord, out BlockSegment segment) && segment != null)
                blockTiles.Remove(segment);
            tileBlocks.Remove(tileCoord);

            if (tileVisuals.TryGetValue(tileCoord, out GameObject visual))
            {
                Destroy(visual);
                tileVisuals.Remove(tileCoord);
            }
        }

        Size -= Vector2Int.up;
    }

    public void RemoveColumn()
    {
        if (Size.x <= 1) return;

        int removeX = MinTile.x + Size.x - 1;

        for (int y = MinTile.y; y < MinTile.y + Size.y; y++)
        {
            Vector2Int tileCoord = new(removeX, y);
            if (tileBlocks.TryGetValue(tileCoord, out BlockSegment segment) && segment != null)
                blockTiles.Remove(segment);
            tileBlocks.Remove(tileCoord);

            if (tileVisuals.TryGetValue(tileCoord, out GameObject visual))
            {
                Destroy(visual);
                tileVisuals.Remove(tileCoord);
            }
        }

        Size -= Vector2Int.right;
    }

    public Dictionary<Vector2Int, BlockSegment> GetAllTiles() => tileBlocks;

    public IEnumerable<Block> GetAllBlocks() => blocks;

    public Vector3 TileToWorld(Vector2 tile) => new(tile.x * TileSize, tile.y * TileSize);

    public Vector2Int WorldToTile(Vector3 worldPos)
    {
        Vector2Int tile = new(Mathf.RoundToInt(worldPos.x / TileSize), Mathf.RoundToInt(worldPos.y / TileSize));
        return tile;
    }

    public Vector3 GetGridCenter() => TileToWorld(new Vector2(MinTile.x + Size.x / 2f, MinTile.y + Size.y / 2f));

    public bool IsValidTile(Vector2Int tile) =>
        tile.x >= MinTile.x && tile.x < MinTile.x + Size.x &&
        tile.y >= MinTile.y && tile.y < MinTile.y + Size.y;

    public bool TryPlaceBlockClosest(Block block, Vector2Int nearTile, BlockSegment pivot)
    {
        foreach (Vector2Int tile in tileBlocks.Keys.OrderBy(t => (t - nearTile).sqrMagnitude))
            if (TryPlaceBlock(block, tile, pivot, tryAllPivots: true)) return true;
        return false;
    }

    public BlockSegment GetBlockAtTile(Vector2Int tile)
    {
        if (!IsValidTile(tile))
        {
            Debug.LogWarning($"Querying out-of-bounds tile {tile}");
            return null;
        }

        tileBlocks.TryGetValue(tile, out BlockSegment block);
        return block;
    }

    public Vector2Int? GetTileOfBlock(BlockSegment block)
    {
        if (!blockTiles.TryGetValue(block, out Vector2Int tile))
            return null;

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

    public bool TryPlaceBlock(Block block, Vector2Int pivotTile, BlockSegment pivotSegment, bool tryAllPivots = false)
    {
        Vector3 requiredMovement = TileToWorld(pivotTile) - pivotSegment.transform.position;

        // Check availability for all segments
        foreach (BlockSegment segment in block.Segments)
        {
            Vector2Int tile = WorldToTile(segment.transform.position + requiredMovement);
            bool obstructedTile = tileBlocks.TryGetValue(tile, out BlockSegment existing) && existing != null && !block.Segments.Contains(existing);
            bool success = IsValidTile(tile) && !obstructedTile;

            if (!success) return tryAllPivots && TryPlaceWithAnyPivot(block, pivotTile);
        }

        // Register all segment movements
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

    bool TryPlaceWithAnyPivot(Block block, Vector2Int pivotTile)
    {
        foreach (BlockSegment segment in block.Segments)
            if (TryPlaceBlock(block, pivotTile, segment)) return true;
        return false;
    }

    public bool TryRotateBlock(Block block, BlockSegment pivot, bool clockwise)
    {
        if (block.MobilityType == Block.Mobility.RotateOnly) pivot = block.Pivot;
        else if (block.MobilityType != Block.Mobility.Free) return false;

        if (!ContainsBlock(block))
        {
            block.Rotate(pivot, clockwise);
            return true;
        }

        bool multiPivot = block.MobilityType == Block.Mobility.Free;
        Vector2Int pivotTile = WorldToTile(pivot.transform.position);
        RemoveBlock(block);

        for (int i = 0; i < 3; i++)
        {
            block.Rotate(pivot, clockwise);
            bool fits = multiPivot ? TryPlaceWithAnyPivot(block, pivotTile) : TryPlaceBlock(block, pivotTile, pivot);
            if (fits) return true;
        }

        block.Rotate(pivot, clockwise);
        TryPlaceBlock(block, pivotTile, pivot);
        return false;
    }

    public bool TryFlipBlock(Block block, BlockSegment pivot)
    {
        if (block.MobilityType != Block.Mobility.Free) return false;

        if (!ContainsBlock(block))
        {
            block.Flip(pivot);
            return true;
        }

        Vector2Int pivotTile = WorldToTile(pivot.transform.position);
        RemoveBlock(block);

        block.Flip(pivot);
        if (TryPlaceWithAnyPivot(block, pivotTile)) return true;

        block.Flip(pivot);
        TryPlaceBlock(block, pivotTile, pivot);
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
            int targetIndex = (block.SlidePositionIndex + i) % length;
            if (TryPlaceBlock(block, block.SlidePositions[targetIndex], block.Pivot))
            {
                block.SlidePositionIndex = targetIndex;
                return true;
            }
        }

        TryPlaceBlock(block, pivotTile, block.Pivot);
        return false;
    }
}