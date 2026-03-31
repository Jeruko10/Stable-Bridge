using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PathSolver
{
    readonly BoardGrid grid;
    readonly Level currentLevel;
    const string voidEdgeTag = "void", blockEdgeTag = "solid", trueEdgeTag = "true";

    public PathSolver(Level level)
    {
        currentLevel = level;
        grid = level.Grid;
    }

    public IEnumerable<Vector2Int> GetPath() =>
        FollowTrueConnections(currentLevel.StartPosition, currentLevel.EndPosition, PrepareGraph());

    Graph PrepareGraph()
    {
        Graph graph = CreateGraph();
        AddVoidVertices(graph);
        AddBlockTransitions(graph);
        MarkTrueEdges(graph);
        DebugDrawTrueConnections(graph);
        return graph;
    }

    IEnumerable<Vector2Int> FollowTrueConnections(Vector2Int startTile, Vector2Int endTile, Graph graph)
    {
        var current = graph.FindVertex(startTile);
        if (current == null) return new List<Vector2Int>();
        var path = new List<Vector2Int> { startTile };
        var seen = new HashSet<Graph.Vertex> { current };

        while (true)
        {
            var options = current.Edges
                .Where(e => e.Tag == trueEdgeTag && !seen.Contains(e.Destination))
                .ToList();
            if (!options.Any()) break;

            current = options
                .OrderBy(e => Manhattan(e.Destination.Coordinate, endTile))
                .First()
                .Destination;

            seen.Add(current);
            path.Add(current.Coordinate);

            if (current.Coordinate == endTile) break;
        }

        return path;
    }

    static int Manhattan(Vector2Int a, Vector2Int b) =>
        Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

    void MarkTrueEdges(Graph graph)
    {
        foreach (Graph.Vertex vertex in graph.Vertices)
            foreach (Graph.Edge edge in vertex.Edges)
            {
                // A edge is true if it connects to a vertex which leads back to the start vertex
                // and one of the connected edges is a block edge.
                Graph.Edge returnEdge = edge.Destination.Edges.Find(e => e.Destination == vertex);

                if (returnEdge != null && (edge.Tag == blockEdgeTag || returnEdge.Tag == blockEdgeTag))
                {
                    edge.Tag = trueEdgeTag;
                    returnEdge.Tag = trueEdgeTag;
                }
            }
    }

    void AddBlockTransitions(Graph graph)
    {
        foreach (var pair in grid.GetAllTiles())
        {
            Vector2Int? tile = pair.Key;
            BlockSegment block = pair.Value;

            if (block == null || tile.Value == null) continue;

            Vector2Int tilePos = tile.Value;

            foreach (LocalTransition transition in block.GetAvailableTransitions(grid))
            {
                Vector2Int fromTile = tilePos + transition.From;
                Vector2Int toTile = tilePos + transition.To;

                Graph.Vertex fromVertex = graph.FindVertex(fromTile);
                Graph.Vertex toVertex = graph.FindVertex(toTile);

                if (fromVertex != null && toVertex != null)
                    graph.AddEdge(fromVertex, toVertex, blockEdgeTag);
            }
        }
    }

    Graph CreateGraph()
    {
        Graph graph = new();

        foreach (var pair in grid.GetAllTiles())
            graph.AddVertex(pair.Key);

        return graph;
    }

    void AddVoidVertices(Graph graph)
    {
        foreach (var pair in grid.GetAllTiles())
        {
            if (pair.Value != null) continue; // only void tiles

            Graph.Vertex vertex = graph.FindVertex(pair.Key);
            foreach (Vector2Int adjacent in grid.GetAdjacents(pair.Key))
            {
                Graph.Vertex destinationVertex = graph.FindVertex(adjacent);
                if (destinationVertex != null)
                    graph.AddEdge(vertex, destinationVertex, voidEdgeTag);
            }
        }
    }

    void DebugDrawTrueConnections(Graph graph)
    {
        foreach (Graph.Vertex vertex in graph.Vertices)
        {
            foreach (Graph.Edge edge in vertex.Edges)
            {
                Color lineColor = edge.Tag switch
                {
                    voidEdgeTag => Color.gray.WithAlpha(0.1f),
                    blockEdgeTag => Color.red.WithAlpha(0.3f),
                    trueEdgeTag => Color.green,
                    _ => Color.white
                };

                Vector2 sourceCoord = grid.TileToWorld(vertex.Coordinate);
                Vector2 destinationCoord = grid.TileToWorld(edge.Destination.Coordinate);
                Vector3 sourcePos = new(sourceCoord.x, sourceCoord.y, -1);
                Vector3 destinationPos = new(destinationCoord.x, destinationCoord.y, -1);

                Debug.DrawLine(sourcePos, destinationPos, lineColor, 2f);
            }
        }
    }
}