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
    static readonly Dictionary<Graph.Edge, EdgeData> edgeDataMap = new();

    class EdgeData
    {
        public string Tag { get; set; }
        public BlockSegment Segment { get; }

        public EdgeData(string tag, BlockSegment segment)
        {
            Tag = tag;
            Segment = segment;
        }
    }

    public static Graph GridToGraph(BoardGrid boardGrid)
    {
        grid = boardGrid;
        edgeDataMap.Clear();
        graph = CreateEmptyGraph();

        AddVoidEdges();
        AddBlockTransitions();
        MarkTrueEdges();
        DebugDrawGraph();

        return graph;
    }

    public static Dictionary<Vector2Int, BlockSegment> GetPath(Vector2Int startTile, Vector2Int endTile, Graph graph)
    {
        var current = graph.FindVertex(startTile);

        if (current == null) return new Dictionary<Vector2Int, BlockSegment>();

        Dictionary<Vector2Int, BlockSegment> path = new() { { startTile, null } };
        HashSet<Graph.Vertex> seen = new() { current };

        while (true)
        {
            List<Graph.Edge> options = current.Edges.Where(e => edgeDataMap[e].Tag == trueEdgeTag && !seen.Contains(e.Destination)).ToList();
            if (!options.Any()) break;

            Graph.Edge bestEdge = options.OrderBy(e => Manhattan(e.Destination.Coordinate, endTile)).First();
            current = bestEdge.Destination;

            seen.Add(current);
            path.Add(current.Coordinate, edgeDataMap[bestEdge].Segment);

            if (current.Coordinate == endTile) break;
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

                if (returnEdge != null && (edgeDataMap[edge].Tag == blockEdgeTag || edgeDataMap[returnEdge].Tag == blockEdgeTag))
                {
                    edgeDataMap[edge].Tag = trueEdgeTag;
                    edgeDataMap[returnEdge].Tag = trueEdgeTag;
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
                    edgeDataMap[edge] = new EdgeData(blockEdgeTag, bSegment);
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
                    edgeDataMap[edge] = new EdgeData(voidEdgeTag, null);
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
                string tag = edgeDataMap[edge].Tag;
                
                Color lineColor = tag == voidEdgeTag ? Color.gray.WithAlpha(0.1f) : 
                                  tag == blockEdgeTag ? Color.red.WithAlpha(0.3f) : 
                                  tag == trueEdgeTag ? Color.green : Color.white;

                Vector2 sourceCoord = grid.TileToWorld(vertex.Coordinate);
                Vector2 destinationCoord = grid.TileToWorld(edge.Destination.Coordinate);
                Vector3 sourcePos = new(sourceCoord.x, sourceCoord.y, -1);
                Vector3 destinationPos = new(destinationCoord.x, destinationCoord.y, -1);

                Debug.DrawLine(sourcePos, destinationPos, lineColor, 2f);
            }
        }
    }
}