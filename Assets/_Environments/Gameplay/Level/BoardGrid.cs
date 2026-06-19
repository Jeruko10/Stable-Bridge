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
    readonly Dictionary<Vector2Int, HashSet<BlockSegment>> tileBlocks = new();
    readonly Dictionary<BlockSegment, Vector2Int> blockTiles = new();
    readonly Dictionary<Vector2Int, GameObject> tileVisuals = new();
    readonly HashSet<Vector2Int> reservedTiles = new();

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
        reservedTiles.Clear();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int tileCoord = new(x, y);
                tileBlocks[tileCoord] = new HashSet<BlockSegment>();
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
                tileBlocks[tileCoord] = new HashSet<BlockSegment>();

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
                tileBlocks[tileCoord] = new HashSet<BlockSegment>();

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
            if (tileBlocks.TryGetValue(tileCoord, out HashSet<BlockSegment> occupants))
                foreach (BlockSegment segment in occupants)
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
            if (tileBlocks.TryGetValue(tileCoord, out HashSet<BlockSegment> occupants))
                foreach (BlockSegment segment in occupants)
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

    public IReadOnlyDictionary<Vector2Int, HashSet<BlockSegment>> GetAllTiles() => tileBlocks;

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

    public BlockSegment GetBlockAtTile(Vector2Int tile)
    {
        if (!IsValidTile(tile))
        {
            Debug.LogWarning($"Querying out-of-bounds tile {tile}");
            return null;
        }

        return tileBlocks.TryGetValue(tile, out HashSet<BlockSegment> occupants) ? occupants.FirstOrDefault() : null;
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

    public bool ContainsSegment(BlockSegment segment) => blockTiles.ContainsKey(segment);

    public void RemoveBlock(Block block)
    {
        if (!blocks.Remove(block)) return;

        foreach (BlockSegment segment in block.Segments)
            if (blockTiles.Remove(segment, out Vector2Int tile) && tileBlocks.TryGetValue(tile, out HashSet<BlockSegment> occupants))
                occupants.Remove(segment);

        UpdateOverlapStates();
    }

    // Free blocks are allowed to overlap others (they just get flagged red, see
    // UpdateOverlapStates) - every other mobility type has no way to show that state, so
    // overlapping one of them fails placement by default. Pass ignoreOverlap explicitly to
    // override that, which rotating and flipping in place do: those should only ever fail
    // by leaving the grid boundary.
    public bool TryPlaceBlock(Block block, Vector2Int pivotTile, BlockSegment pivotSegment, bool tryAllPivots = false, bool? ignoreOverlap = null)
    {
        bool ignoringOverlap = ignoreOverlap ?? block.MobilityType == Block.Mobility.Free;
        Vector3 requiredMovement = TileToWorld(pivotTile) - pivotSegment.transform.position;

        foreach (BlockSegment segment in block.Segments)
        {
            Vector2Int tile = WorldToTile(segment.transform.position + requiredMovement);
            if (!IsValidTile(tile) || (!ignoringOverlap && IsObstructed(tile, block)))
                return tryAllPivots && TryPlaceWithAnyPivot(block, pivotTile, ignoringOverlap);
        }

        foreach (BlockSegment segment in block.Segments)
        {
            Vector2Int tile = WorldToTile(segment.transform.position + requiredMovement);
            tileBlocks[tile].Add(segment);
            blockTiles[segment] = tile;
        }

        block.Position2D = block.transform.position + requiredMovement;
        blocks.Add(block);

        UpdateOverlapStates();

        return true;
    }

    bool IsObstructed(Vector2Int tile, Block block) =>
        tileBlocks.TryGetValue(tile, out HashSet<BlockSegment> occupants) && occupants.Any(segment => segment.GetParent() != block);

    bool TryPlaceWithAnyPivot(Block block, Vector2Int pivotTile, bool ignoreOverlap)
    {
        foreach (BlockSegment segment in block.Segments)
            if (TryPlaceBlock(block, pivotTile, segment, ignoreOverlap: ignoreOverlap)) return true;
        return false;
    }

    public bool TryPlaceBlockClosest(Block block, Vector2Int nearTile, BlockSegment pivot)
    {
        foreach (Vector2Int tile in tileBlocks.Keys.OrderBy(t => (t - nearTile).sqrMagnitude))
            if (TryPlaceBlock(block, tile, pivot, tryAllPivots: true)) return true;
        return false;
    }

    // Marks the given tiles (e.g. the start/goal positions) as illegal to occupy - any block
    // sitting on one is treated the same as overlapping another block.
    public void SetReservedTiles(IEnumerable<Vector2Int> tiles)
    {
        reservedTiles.Clear();
        foreach (Vector2Int tile in tiles) reservedTiles.Add(tile);
        UpdateOverlapStates();
    }

    // A tile occupied by segments from more than one block, or by any block on a reserved
    // tile, puts the involved blocks in the overlapping (red) state; everything else reverts
    // to its original color.
    void UpdateOverlapStates()
    {
        HashSet<Block> overlapping = new();

        foreach (var pair in tileBlocks)
        {
            HashSet<BlockSegment> occupants = pair.Value;
            if (occupants.Count == 0) continue;

            bool illegal = reservedTiles.Contains(pair.Key) || occupants.Select(s => s.GetParent()).Distinct().Count() > 1;
            if (illegal)
                foreach (BlockSegment segment in occupants)
                    overlapping.Add(segment.GetParent());
        }

        foreach (Block block in blocks)
            block.SetOverlapping(block.MobilityType == Block.Mobility.Free && overlapping.Contains(block));
    }

    // Rotates the block 90° around the clicked segment. See TurnInPlace.
    public bool TryRotateBlock(Block block, BlockSegment pivot, bool clockwise)
    {
        if (block.MobilityType == Block.Mobility.RotateOnly) pivot = block.Pivot;
        else if (block.MobilityType != Block.Mobility.Free) return false;

        TurnInPlace(block, () => block.Rotate(pivot, clockwise));
        return true;
    }

    // Flips the block across the clicked segment. See TurnInPlace.
    public bool TryFlipBlock(Block block, BlockSegment pivot)
    {
        if (block.MobilityType != Block.Mobility.Free) return false;

        TurnInPlace(block, () => block.Flip(pivot));
        return true;
    }

    // Turns (rotates/flips) the block around its clicked pivot, which Block keeps fixed in
    // place. The turn always happens, anywhere on or off the grid - overlapping another block
    // or a reserved tile just flags it red. A block already on the grid has its tile
    // registration refreshed in place; one being dragged simply turns visually.
    void TurnInPlace(Block block, System.Action turn)
    {
        bool onGrid = ContainsBlock(block);
        if (onGrid) RemoveBlock(block);

        turn();

        if (onGrid) RegisterBlock(block);
    }

    // Registers the block on the tiles its segments currently occupy without moving it,
    // skipping any segment that lands outside the grid. Overlap is allowed and flagged by
    // UpdateOverlapStates.
    void RegisterBlock(Block block)
    {
        foreach (BlockSegment segment in block.Segments)
        {
            Vector2Int tile = WorldToTile(block.GetSegmentPosition(segment));
            if (!tileBlocks.TryGetValue(tile, out HashSet<BlockSegment> occupants)) continue;

            occupants.Add(segment);
            blockTiles[segment] = tile;
        }

        blocks.Add(block);
        UpdateOverlapStates();
    }

    public bool TrySlideBlock(Block block)
    {
        if (block.SlidePositions == null || block.SlidePositions.Length <= 1) return false;

        // The current slide position is the logical source of truth - don't read it back from
        // the animating transform, which may still be lerping toward its target.
        Vector2Int currentTile = block.SlidePositions[block.SlidePositionIndex];
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

        TryPlaceBlock(block, currentTile, block.Pivot);
        return false;
    }
}