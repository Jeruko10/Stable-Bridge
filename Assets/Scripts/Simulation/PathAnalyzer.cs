using System.Collections.Generic;
using UnityEngine;

public class PathAnalyzer
{
    public bool HasPath(BoardState board, Vector3Int start, Vector3Int goal)
    {
        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == goal)
                return true;

            var piece = board.GetPiece(current);
            if (piece == null) continue;

            foreach (var conn in piece.Definition.Connections)
            {
                var next = piece.Position + conn.To;

                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }
}