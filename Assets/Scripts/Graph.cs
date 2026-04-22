using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    public IReadOnlyCollection<Vertex> Vertices => vertices;

    readonly List<Vertex> vertices = new();
    readonly Dictionary<Vector2Int, Vertex> coordinateCache = new();

    public Vertex AddVertex(Vector2Int coordinate)
    {
        if (coordinateCache.TryGetValue(coordinate, out var existing))
            return existing;

        Vertex v = new(coordinate);
        vertices.Add(v);
        coordinateCache.Add(coordinate, v);
        return v;
    }

    public Vertex GetVertex(Vector2Int coordinate) => coordinateCache.TryGetValue(coordinate, out var v) ? v : null;

    public Edge AddEdge(Vertex a, Vertex b)
    {
        if (a == null || b == null || a == b) return null;

        Edge existing = a.GetEdge(b);
        if (existing != null) return existing;

        Edge edge = new(a, b);

        a.AddEdgeInternal(edge);
        b.AddEdgeInternal(edge);

        return edge;
    }

    public class Vertex
    {
        public Vector2Int Coordinate { get; }
        public IReadOnlyCollection<Edge> Edges => edges;

        readonly List<Edge> edges = new();

        public Vertex(Vector2Int coordinate)
        {
            Coordinate = coordinate;
        }

        internal void AddEdgeInternal(Edge edge) => edges.Add(edge);

        public Edge GetEdge(Vertex other)
        {
            foreach (var e in edges)
                if (e.Connects(this, other))
                    return e;

            return null;
        }
    }

    public class Edge
    {
        public Vertex A { get; private set; }
        public Vertex B { get; private set; }

        public Edge(Vertex a, Vertex b)
        {
            A = a;
            B = b;
        }

        public Vertex GetOther(Vertex v)
        {
            if (v == A) return B;
            if (v == B) return A;
            return null;
        }

        public bool Connects(Vertex a, Vertex b) => (A == a && B == b) || (A == b && B == a);
    }
}