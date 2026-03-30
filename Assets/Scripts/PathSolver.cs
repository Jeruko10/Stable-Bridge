using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PathSolver : MonoBehaviour
{
    [field: SerializeField] public GameObject PathVisualPrefab { get; set; }

    Dictionary<Vector2Int, List<Vector2Int>> navGraph;
    readonly List<GameObject> activeBlocks = new();

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            navGraph = GenerateGraph();
            DrawGraphEdges(navGraph);
        }
    }

    Dictionary<Vector2Int, List<Vector2Int>> GenerateGraph()
    {
        BoardGrid grid = LevelManager.Current.Grid;
        Dictionary<Vector2Int, List<Vector2Int>> graph = new();

        foreach (BlockSegment segment in grid.GetAllSegments())
        {
            Vector2Int currentTile = grid.WorldToTile(segment.transform.position);
            graph[currentTile] = new();

            foreach (Vector2Int dir in segment.GetOutgoingDirections())
            {
                Vector2Int neighborTile = currentTile + dir;
                BlockSegment neighbor = grid.GetBlockAtTile(neighborTile);
                Vector2Int oppositeDir = new(-dir.x, -dir.y);

                if (neighbor != null && neighbor.GetOutgoingDirections().Contains(oppositeDir)) graph[currentTile].Add(neighborTile);
            }
        }

        return graph;
    }

    void DrawGraphEdges(Dictionary<Vector2Int, List<Vector2Int>> graph)
    {
        foreach (GameObject block in activeBlocks) Destroy(block);
        activeBlocks.Clear();

        BoardGrid grid = LevelManager.Current.Grid;
        HashSet<Vector2Int> drawnTiles = new();

        foreach (var kvp in graph.Where(k => k.Value.Count > 0))
        {
            if (drawnTiles.Add(kvp.Key)) activeBlocks.Add(Instantiate(PathVisualPrefab, grid.TileToWorld(kvp.Key), Quaternion.identity, transform));
            
            foreach (Vector2Int connection in kvp.Value)
                if (drawnTiles.Add(connection)) activeBlocks.Add(Instantiate(PathVisualPrefab, grid.TileToWorld(connection), Quaternion.identity, transform));
        }

        Debug.Log($"Graph has {graph.Count} nodes and {graph.Sum(k => k.Value.Count)} edges.");
    }
}