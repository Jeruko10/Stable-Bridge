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
            DebugDrawGraph();

        return graph;
    }

    public static Dictionary<Vector2Int, TransitionAnimation?> GetPath(Vector2Int startTile, Vector2Int endTile, Graph graph)
    {
        Graph.Vertex current = graph.FindVertex(startTile);

        if (current == null) return new();

        Dictionary<Vector2Int, TransitionAnimation?> path = new() { { startTile, null } };
        HashSet<Graph.Vertex> seen = new() { current };

        while (true)
        {
            IEnumerable<Graph.Edge> options = current.Edges.Where(e => edgeTags[e] == trueEdgeTag && !seen.Contains(e.Destination));
            
            if (!options.Any()) break;

            IEnumerable<Graph.Edge> preferredOptions = options
                .GroupBy(e => e.Destination.Coordinate)
                .Select(group => group.FirstOrDefault(e => edgeTransitions[e] != null) ?? group.First());

            Graph.Edge bestEdge = preferredOptions.OrderBy(e => Manhattan(e.Destination.Coordinate, endTile)).First();

            current = bestEdge.Destination;
            seen.Add(current);
            path.Add(current.Coordinate, edgeTransitions[bestEdge]);

            if (current.Coordinate == endTile) break;
        }

        // if (!LevelManager.TrainModeEnabled)
        //     DebugDrawPath(path);

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
        {
            foreach (Graph.Edge edge in vertex.Edges)
            {
                string tag = edgeTags[edge];
                
                Color lineColor = tag == voidEdgeTag ? Color.gray.WithAlpha(0.1f) : 
                                  tag == blockEdgeTag ? Color.red.WithAlpha(0.3f) : 
                                  tag == trueEdgeTag ? Color.green.WithAlpha(0.3f) :
                                  Color.white;

                Vector2 sourceCoord = grid.TileToWorld(vertex.Coordinate);
                Vector2 destinationCoord = grid.TileToWorld(edge.Destination.Coordinate);
                Vector3 sourcePos = new(sourceCoord.x, sourceCoord.y, -1);
                Vector3 destinationPos = new(destinationCoord.x, destinationCoord.y, -1);

                Debug.DrawLine(sourcePos, destinationPos, lineColor, 2f);
            }
        }
    }

    static void DebugDrawPath(Dictionary<Vector2Int, TransitionAnimation?> path)
    {
        const float depth = -2f;
        Vector3 currentPos = new(path.First().Key.x, path.First().Key.y, depth);

        foreach (var pair in path)
        {
            Vector2Int tile = pair.Key;
            TransitionAnimation? transition = pair.Value;

            Vector3 nextPos = new(tile.x, tile.y, depth);

            Color lineColor = transition.HasValue ? Color.green : Color.red;
            Debug.DrawLine(currentPos, nextPos, lineColor, 5f);

            currentPos = nextPos;
        }
    }
}