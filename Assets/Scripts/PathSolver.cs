using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PathSolver : MonoBehaviour
{
    [field: SerializeField] GameObject pathVisualPrefab;

    IEnumerable<Vector2Int> path;
    readonly List<GameObject> activeBlocks = new();

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            path = GetAllNavigableTiles();
            DrawPath(path);
        }
    }

    IEnumerable<Vector2Int> GetAllNavigableTiles()
    {
        BoardGrid grid = LevelManager.Current.Grid;
        List<Vector2Int> path = new();

        foreach (BlockSegment segment in grid.GetAllSegments())
            path.AddRange(segment.GetNavigableTiles());

        return path;
    }

    void DrawPath(IEnumerable<Vector2Int> path)
    {
        foreach (GameObject block in activeBlocks) Destroy(block);
        activeBlocks.Clear();

        BoardGrid grid = LevelManager.Current.Grid;

        foreach (Vector2Int tile in path)
        {
            GameObject instance = Instantiate(pathVisualPrefab, grid.TileToWorld(tile), Quaternion.identity, transform);
            activeBlocks.Add(instance);
        }
    }
}