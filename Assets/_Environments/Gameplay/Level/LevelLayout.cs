#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Level")]
public class LevelLayout : ScriptableObject
{
    [SerializeField] Vector2Int levelSize = new(6, 5);
    [SerializeField] Vector2Int startPosition = Vector2Int.zero;
    [SerializeField] Vector2Int endPosition = Vector2Int.one;
    [SerializeField] List<BlockPlacementData> blocks = new();

    public Vector2Int LevelSize => levelSize;
    public Vector2Int StartPosition => startPosition;
    public Vector2Int EndPosition => endPosition;
    public IReadOnlyList<BlockPlacementData> Blocks => blocks;

    public static LevelLayout FromLevel(Level level, Vector2Int startPosition, Vector2Int endPosition)
    {
        LevelLayout instance = CreateInstance<LevelLayout>();
        List<BlockPlacementData> blocksData = new();
        BoardGrid grid = level.Grid;

        foreach (Block block in level.GetComponentsInChildren<Block>())
        {
            if (block.name == "Ground") continue;

            Vector2Int? tile = grid.GetTileOfBlock(block.Pivot);

            blocksData.Add(new(
                blockPrefab: block.Prefab,
                mobilityType: block.MobilityType,
                pivotIndex: block.Segments.ToList().IndexOf(block.Pivot),
                flipped: block.IsFlipped,
                rotation: block.Rotation,
                startingTile: tile ?? Vector2Int.zero,
                slideTiles: block.SlidePositions == null ? new() : block.SlidePositions.ToList()
            ));
        }

        instance.levelSize = level.Grid.Size;
        instance.startPosition = startPosition;
        instance.endPosition = endPosition;
        instance.blocks = blocksData;

        return instance;
    }

    public void SaveAsAsset()
    {
#if UNITY_EDITOR
        const string folder = "Assets/Resources/Levels";
        
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        int index = 1;
        string path = $"{folder}/Level{index}.asset";

        while (File.Exists(path))
        {
            index++;
            path = $"{folder}/Level{index}.asset";
        }
        
        AssetDatabase.CreateAsset(this, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Level Saved: Level{index}");
#endif
    }
}