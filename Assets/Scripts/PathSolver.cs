using System;
using System.Collections.Generic;
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

    public IEnumerable<Vector2Int> FindShortestPath()
    {
        grid.AddRow(false);
        Graph graph = CreateGraph();
        
        AddVoidVertices(graph);
        AddBlockTransitions(graph);
        MarkTrueEdges(graph);
        DebugDrawTrueConnections(graph);
        return BreadthFirstSearch(currentLevel.StartPosition, currentLevel.EndPosition, graph);
    }

    IEnumerable<Vector2Int> BreadthFirstSearch(Vector2Int startTile, Vector2Int endTile, Graph graph)
    {
        Graph.Vertex startVertex = graph.FindVertex(startTile);
        Graph.Vertex endVertex = graph.FindVertex(endTile);

        if (startVertex == null || endVertex == null) return new List<Vector2Int>();
        if (startTile == endTile) return new List<Vector2Int> { startTile };

        Queue<Graph.Vertex> frontier = new();
        Dictionary<Graph.Vertex, Graph.Vertex> parent = new();
        HashSet<Graph.Vertex> visited = new() { startVertex };

        frontier.Enqueue(startVertex);

        while (frontier.Count > 0)
        {
            Graph.Vertex current = frontier.Dequeue();

            foreach (Graph.Edge edge in current.Edges)
            {
                if (edge.Tag != trueEdgeTag) continue;

                Graph.Vertex next = edge.Destination;
                if (visited.Contains(next)) continue;

                visited.Add(next);
                parent[next] = current;

                if (next == endVertex)
                {
                    // reconstruct path
                    List<Vector2Int> path = new();
                    Graph.Vertex node = endVertex;
                    while (node != null)
                    {
                        path.Add(node.Coordinate);
                        parent.TryGetValue(node, out node);
                    }
                    path.Reverse();
                    return path;
                }

                frontier.Enqueue(next);
            }
        }

        return new List<Vector2Int>();
    }

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

                Debug.Log("Line drawn from " + vertex.Coordinate + " to " + edge.Destination.Coordinate);
                Debug.DrawLine(sourcePos, destinationPos, lineColor, 100000);
            }
        }
    }
}