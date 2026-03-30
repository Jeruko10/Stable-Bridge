using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PathSolver : MonoBehaviour
{
    [field: SerializeField] public GameObject PathVisualPrefab { get; set; }

    BoardGrid grid;
    const string voidEdgeTag = "void", blockEdgeTag = "solid", trueEdgeTag = "true";

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            CalculateSolution();
        }
    }

    void CalculateSolution()
    {
        grid = LevelManager.Current.Grid;
        grid.AddRow(false);
        Graph graph = CreateGraph();
        AddVoidVertices(graph);
        AddBlockTransitions(graph);
        MarkTrueEdges(graph);
        RemoveNonTrueEdges(graph);

        DebugDrawTrueConnections(graph);
    }

    void RemoveNonTrueEdges(Graph graph)
    {
        foreach (Graph.Vertex vertex in graph.Vertices)
            vertex.Edges.RemoveAll(e => e.Tag != trueEdgeTag);
    }

    void MarkTrueEdges(Graph graph)
    {
        foreach (Graph.Vertex vertex in graph.Vertices)
            foreach (Graph.Edge edge in vertex.Edges)
            {
                // A edge is true if it connects to a vertex which leads back to the start vertex
                // and one of the connected edges is a block edge.

                Graph.Edge returnEdge = edge.Destination.Edges.Find(e => e.Destination == vertex);
                bool hasBlockEdge = vertex.Edges.Exists(e => e.Tag == blockEdgeTag);
                bool destinationHasBlockEdge = edge.Destination.Edges.Exists(e => e.Tag == blockEdgeTag);

                if (returnEdge != null && (hasBlockEdge || destinationHasBlockEdge))
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
            Vector2Int? tile = pair.Key;

            if (tile.Value != null) continue; // Only connect void tiles

            Graph.Vertex vertex = graph.FindVertex(tile.Value);

            foreach (Vector2Int adjacent in grid.GetAdjacents(tile.Value))
            {
                Graph.Vertex destinationVertex = graph.FindVertex(adjacent);

                if (destinationVertex != null) graph.AddEdge(vertex, destinationVertex, voidEdgeTag);
            }
        }
    }

    void DebugDrawTrueConnections(Graph graph)
    {
        foreach (Graph.Vertex vertex in graph.Vertices)
        {
            foreach (Graph.Edge edge in vertex.Edges)
            {
                if (edge.Tag == trueEdgeTag)
                {
                    Vector2 sourceCoord = grid.TileToWorld(vertex.Coordinate);
                    Vector2 destinationCoord = grid.TileToWorld(edge.Destination.Coordinate);

                    Debug.DrawLine(sourceCoord, destinationCoord, Color.green);
                }
            }
        }
    }
}