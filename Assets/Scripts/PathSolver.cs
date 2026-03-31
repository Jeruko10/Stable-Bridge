using System;
using Unity.VisualScripting;
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
        // RemoveNonTrueEdges(graph);

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