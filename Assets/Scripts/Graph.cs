using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    public IReadOnlyCollection<Vertex> Vertices => vertices;

    readonly List<Vertex> vertices = new();
    readonly Dictionary<Vector2, Vertex> coordinateCache = new();
    readonly Dictionary<Vertex, List<Edge>> vertexEdges = new();

    public Vertex AddVertex(Vector2 coordinate)
    {
        if (coordinateCache.TryGetValue(coordinate, out Vertex existing))
            return existing;

        Vertex vertex = new(this, coordinate);
        vertices.Add(vertex);
        coordinateCache.Add(coordinate, vertex);
        vertexEdges[vertex] = new List<Edge>();
        return vertex;
    }

    public Vertex GetVertex(Vector2 coordinate) => coordinateCache.TryGetValue(coordinate, out Vertex vertex) ? vertex : null;

    public Edge AddEdge(Vertex a, Vertex b)
    {
        if (a == null || b == null || a == b) return null;

        Edge existing = a.GetEdge(b);
        if (existing != null) return existing;

        Edge edge = new(a, b);

        vertexEdges[a].Add(edge);
        vertexEdges[b].Add(edge);

        return edge;
    }

    public void RemoveEdge(Edge edge)
    {
        if (edge == null) return;

        if (edge.A != null && vertexEdges.TryGetValue(edge.A, out List<Edge> aEdges))
            aEdges.Remove(edge);

        if (edge.B != null && vertexEdges.TryGetValue(edge.B, out List<Edge> bEdges))
            bEdges.Remove(edge);
    }

    public class Vertex
    {
        public Vector2 Coordinate { get; }
        public IReadOnlyCollection<Edge> Edges => graph.vertexEdges[this];

        readonly Graph graph;

        internal Vertex(Graph graph, Vector2 coordinate)
        {
            this.graph = graph;
            Coordinate = coordinate;
        }

        public Edge GetEdge(Vertex other)
        {
            foreach (var e in Edges)
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