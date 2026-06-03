using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PathSolver
{
    public static Graph GridToGraph(BoardGrid grid)
    {
        Graph graph = new();

        AddTransitions(graph, grid);

        // if (!LevelManager.TrainModeEnabled) DebugDrawGraph(graph, grid);

        return graph;
    }

    public static IEnumerable<Vector2> GetPath(Vector2Int startTile, Vector2Int endTile, Graph graph)
    {
        Graph.Vertex start = graph.GetVertex((Vector2)startTile + BlockSegment.BottomRight);
        List<Vector2> path = new();

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

            if (!options.Any()) break;

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

    static void AddTransitions(Graph graph, BoardGrid grid)
    {
        foreach (var pair in grid.GetAllTiles())
        {
            Vector2Int tile = pair.Key;
            BlockSegment segment = pair.Value;

            if (segment == null || grid.GetBlockAtTile(tile + Vector2Int.up) != null) continue;

            foreach (LocalTransition transition in segment.GetTransitions())
            {
                Vector2 fromCoord = tile + transition.From;
                Vector2 toCoord = tile + transition.To;

                Graph.Vertex from = graph.GetVertex(fromCoord) ?? graph.AddVertex(fromCoord);
                Graph.Vertex to = graph.GetVertex(toCoord) ?? graph.AddVertex(toCoord);

                if (from == null || to == null) continue;

                graph.AddEdge(from, to);
            }
        }
    }

    static float Manhattan(Vector2 a, Vector2 b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

    // static void DebugDrawGraph(Graph graph, BoardGrid grid)
    // {
    //     const float gizmoDuration = 3f;

    //     foreach (Graph.Vertex v in graph.Vertices)
    //         foreach (Graph.Edge e in v.Edges)
    //         {
    //             Graph.Vertex other = e.GetOther(v);
    //             if (other == null) continue;

    //             Vector2 from = grid.TileToWorld(v.Coordinate);
    //             Vector2 to = grid.TileToWorld(other.Coordinate);

    //             Debug.DrawLine(
    //                 new(from.x, from.y + 0.2f, -1),
    //                 new(to.x, to.y + 0.2f, -1),
    //                 Color.white.WithAlpha(0.2f),
    //                 gizmoDuration
    //             );
    //         }
    // }
}