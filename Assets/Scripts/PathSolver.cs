using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public static class PathSolver
{
    public static Graph GridToGraph(BoardGrid grid)
    {
        Graph graph = new();

        foreach (var pair in grid.GetAllTiles())
            graph.AddVertex(pair.Key);

        AddConnections(graph, grid);

        if (!LevelManager.TrainModeEnabled) DebugDrawGraph(graph, grid);

        return graph;
    }

    public static IEnumerable<Vector2Int> GetPath(Vector2Int startTile, Vector2Int endTile, Graph graph)
    {
        Graph.Vertex start = graph.GetVertex(startTile);
        List<Vector2Int> path = new();

        if (start == null) return path;

        HashSet<Graph.Vertex> visited = new();

        Graph.Vertex current = start;
        visited.Add(current);

        path.Add(current.Coordinate);

        while (true)
        {
            IEnumerable<Graph.Vertex> options =
                current.Edges
                .Select(e => e.GetOther(current))
                .Where(v => v != null && !visited.Contains(v));

            if (!options.Any())
                break;

            Graph.Vertex best =
                options
                .OrderBy(v => Manhattan(v.Coordinate, endTile))
                .First();

            current = best;
            visited.Add(current);

            path.Add(current.Coordinate);

            if (current.Coordinate == endTile) break;
        }

        return path;
    }

    static void AddConnections(Graph graph, BoardGrid grid)
    {
        foreach (var pair in grid.GetAllTiles())
        {
            Vector2Int tile = pair.Key;
            BlockSegment segment = pair.Value;

            if (segment == null) continue;

            foreach (LocalTransition transition in segment.GetAvailableTransitions(grid))
            {
                Vector2Int fromCoord = tile + transition.From;
                Vector2Int toCoord = tile + transition.To;

                Graph.Vertex from = graph.GetVertex(fromCoord);
                Graph.Vertex to = graph.GetVertex(toCoord);

                if (from == null || to == null)
                    continue;

                graph.AddEdge(from, to);
            }
        }
    }

    static int Manhattan(Vector2Int a, Vector2Int b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

    static void DebugDrawGraph(Graph graph, BoardGrid grid)
    {
        foreach (Graph.Vertex v in graph.Vertices)
        {
            foreach (Graph.Edge e in v.Edges)
            {
                Graph.Vertex other = e.GetOther(v);
                if (other == null)
                    continue;

                Vector2 from = grid.TileToWorld(v.Coordinate);
                Vector2 to = grid.TileToWorld(other.Coordinate);

                Debug.DrawLine(
                    new Vector3(from.x, from.y, -1),
                    new Vector3(to.x, to.y, -1),
                    Color.white.WithAlpha(0.2f),
                    4f
                );
            }
        }
    }
}