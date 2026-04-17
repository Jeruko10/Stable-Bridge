using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public static class PathSolver
{
    static readonly string voidEdgeTag = "void";
    static readonly string blockEdgeTag = "solid";
    static readonly string trueEdgeTag = "true";

    static BoardGrid grid;
    static Graph graph;
    static readonly Dictionary<Graph.Edge, string> edgeTags = new();
    static readonly Dictionary<Graph.Edge, TransitionAnimation?> edgeTransitions = new();

    public static Graph GridToGraph(BoardGrid boardGrid)
    {
        grid = boardGrid;
        edgeTransitions.Clear();
        edgeTags.Clear();
        
        graph = CreateEmptyGraph();

        AddVoidEdges();
        AddBlockTransitions();
        MarkTrueEdges();

        if (!LevelManager.TrainModeEnabled)
        {
            // DebugDrawGraph();
        }

        return graph;
    }

    public static Dictionary<Vector2Int, TransitionAnimation?> GetPath(Vector2Int startTile, Vector2Int endTile, Graph graph)
    {
        Graph.Vertex start = graph.FindVertex(startTile);
        Graph.Vertex end = graph.FindVertex(endTile);

        if (start == null || end == null) return new();

        if (start == end)
        {
            Dictionary<Vector2Int, TransitionAnimation?> startPath = new() { { startTile, null } };

            if (!LevelManager.TrainModeEnabled)
            {
                DebugDrawTransitions(startPath);
            }

            return startPath;
        }

        Dictionary<Graph.Vertex, Graph.Edge> parentEdge = new();
        HashSet<Graph.Vertex> visited = new() { start };
        Queue<Graph.Vertex> queue = new();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Graph.Vertex current = queue.Dequeue();

            if (current == end) break;

            foreach (Graph.Edge edge in current.Edges.Where(e => edgeTags[e] == trueEdgeTag && !visited.Contains(e.Destination)))
            {
                visited.Add(edge.Destination);
                parentEdge[edge.Destination] = edge;
                queue.Enqueue(edge.Destination);
            }
        }

        List<KeyValuePair<Vector2Int, TransitionAnimation?>> pathEntries = new() { new(startTile, null) };

        if (!parentEdge.ContainsKey(end))
        {
            if (!LevelManager.TrainModeEnabled)
            {
                DebugDrawTransitions(pathEntries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }

            return pathEntries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        Stack<Graph.Edge> edges = new();
        Graph.Vertex cursor = end;

        while (cursor != start)
        {
            Graph.Edge edge = parentEdge[cursor];
            edges.Push(edge);
            cursor = edge.Source;
        }

        while (edges.Count > 0)
        {
            Graph.Edge edge = edges.Pop();
            pathEntries.Add(new(edge.Destination.Coordinate, edgeTransitions[edge]));
        }

        while (pathEntries.Count > 1 && pathEntries[^1].Value == null)
        {
            pathEntries.RemoveAt(pathEntries.Count - 1);
        }

        Dictionary<Vector2Int, TransitionAnimation?> path = pathEntries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (!LevelManager.TrainModeEnabled)
        {
            DebugDrawTransitions(path);
        }

        return path;
    }

    static Graph CreateEmptyGraph()
    {
        Graph graph = new();
        foreach (var pair in grid.GetAllTiles()) graph.AddVertex(pair.Key);
        return graph;
    }

    static int Manhattan(Vector2Int a, Vector2Int b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

    static void MarkTrueEdges()
    {
        foreach (Graph.Vertex vertex in graph.Vertices)
            foreach (Graph.Edge edge in vertex.Edges)
            {
                Graph.Edge returnEdge = edge.Destination.Edges.Find(e => e.Destination == vertex);

                if (returnEdge != null && (edgeTags[edge] == blockEdgeTag || edgeTags[returnEdge] == blockEdgeTag))
                {
                    edgeTags[edge] = trueEdgeTag;
                    edgeTags[returnEdge] = trueEdgeTag;
                }
                else edgeTransitions[edge] = null;
            }
    }

    static void AddBlockTransitions()
    {
        foreach (var pair in grid.GetAllTiles())
        {
            Vector2Int? tile = pair.Key;
            BlockSegment bSegment = pair.Value;

            if (bSegment == null || tile.Value == null) continue;

            Vector2Int tilePos = tile.Value;

            foreach (LocalTransition transition in bSegment.GetAvailableTransitions(grid))
            {
                Vector2Int fromTile = tilePos + transition.From;
                Vector2Int toTile = tilePos + transition.To;

                Graph.Vertex fromVertex = graph.FindVertex(fromTile);
                Graph.Vertex toVertex = graph.FindVertex(toTile);

                if (fromVertex != null && toVertex != null)
                {
                    Graph.Edge edge = graph.AddEdge(fromVertex, toVertex);
                    edgeTags[edge] = blockEdgeTag;
                    edgeTransitions[edge] = transition.Animation.ToGlobal(bSegment.transform.position);
                }
            }
        }
    }

    static void AddVoidEdges()
    {
        foreach (var pair in grid.GetAllTiles())
        {
            if (pair.Value != null) continue;

            Graph.Vertex vertex = graph.FindVertex(pair.Key);

            foreach (Vector2Int adjacent in grid.GetAdjacents(pair.Key))
            {
                Graph.Vertex destinationVertex = graph.FindVertex(adjacent);

                if (destinationVertex != null)
                {
                    Graph.Edge edge = graph.AddEdge(vertex, destinationVertex);
                    edgeTags.Add(edge, voidEdgeTag);
                    edgeTransitions.Add(edge, null);
                }
            }
        }
    }

    static void DebugDrawGraph()
    {
        foreach (Graph.Vertex vertex in graph.Vertices)
            foreach (Graph.Edge edge in vertex.Edges)
            {
                string tag = edgeTags[edge];
                const float depth = -1f;
                
                Color lineColor = tag == voidEdgeTag ? Color.gray.WithAlpha(0.1f) : 
                                  tag == blockEdgeTag ? Color.red.WithAlpha(0.3f) : 
                                  tag == trueEdgeTag ? Color.green.WithAlpha(0.3f) :
                                  Color.white;

                Vector2 sourceCoord = grid.TileToWorld(vertex.Coordinate);
                Vector2 destinationCoord = grid.TileToWorld(edge.Destination.Coordinate);
                Vector3 sourcePos = new(sourceCoord.x, sourceCoord.y, depth);
                Vector3 destinationPos = new(destinationCoord.x, destinationCoord.y, depth);

                Debug.DrawLine(sourcePos, destinationPos, lineColor, 2f);
            }
    }

    static void DebugDrawTransitions(Dictionary<Vector2Int, TransitionAnimation?> path)
    {
        if (path.Count < 2) return;

        const float depth = -2f;
        using var enumerator = path.GetEnumerator();
        
        if (!enumerator.MoveNext()) return;
        
        Vector2 firstWorld = grid.TileToWorld(enumerator.Current.Key);
        Vector3 lastPos = new(firstWorld.x, firstWorld.y, depth);

        while (enumerator.MoveNext())
        {
            Vector2 tileWorld = grid.TileToWorld(enumerator.Current.Key);
            Vector3 currentPos = new(tileWorld.x, tileWorld.y, depth);
            Color color = enumerator.Current.Value == null ? Color.red : Color.green;

            Debug.DrawLine(lastPos, currentPos, color, 2f);
            Debug.Log($"Transition {enumerator.Current.Value?.ToString() ?? "void"} from {(Vector2)lastPos} to {(Vector2)currentPos}");
            lastPos = currentPos;
        }
    }
}
