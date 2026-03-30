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

    public void AddEdge(Vertex source, Vertex destination) => source.AddEdge(destination);

    public Vertex FindVertex(Vector2Int coordinate) => Vertices.Find(v => v.Coordinate == coordinate);

    public class Vertex
    {
        public Vector2Int Coordinate { get; }
        public List<Edge> Edges { get; } = new();

        public Vertex(Vector2Int coordinate) => Coordinate = coordinate;

        public void AddEdge(Vertex destination) => Edges.Add(new Edge(this, destination));
    }

    public class Edge
    {
        public Vertex Source { get; }
        public Vertex Destination { get; }

        public Edge(Vertex source, Vertex destination)
        {
            Source = source;
            Destination = destination;
        }
    }
}