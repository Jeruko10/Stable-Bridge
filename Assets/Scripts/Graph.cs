using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    public List<Vertex> Vertices { get; } = new();

    public Vertex AddVertex(Vector2Int coordinate)
    {
        Vertex newVertex = new(coordinate);
        Vertices.Add(newVertex);
        return newVertex;
    }

    public Edge AddEdge(Vertex source, Vertex destination, string tag = "") => source.AddEdge(destination, tag);

    public Vertex FindVertex(Vector2Int coordinate) => Vertices.Find(v => v.Coordinate == coordinate);

    public class Vertex
    {
        public Vector2Int Coordinate { get; }
        public List<Edge> Edges { get; } = new();

        public Vertex(Vector2Int coordinate) => Coordinate = coordinate;

        public Edge AddEdge(Vertex destination, string tag = "")
        {
            Edge newEdge = new(this, destination, tag);
            Edges.Add(newEdge);
            return newEdge;
        }
    }

    public class Edge
    {
        public Vertex Source { get; }
        public Vertex Destination { get; }
        public string Tag { get; set; }

        public Edge(Vertex source, Vertex destination, string tag = "")
        {
            Source = source;
            Destination = destination;
            Tag = tag;
        }
    }
}