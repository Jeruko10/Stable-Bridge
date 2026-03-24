using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StabilityAnalyzer
{
    public bool IsStable(BoardState board)
    {
        var supports = ComputeSupportPoints(board);
        var com = ComputeGlobalCenterOfMass(board);

        return IsPointInsideSupportPolygon(com, supports);
    }

    List<Vector3> ComputeSupportPoints(BoardState board)
    {
        var points = new List<Vector3>();

        foreach (var piece in board.Pieces.Values)
        {
            foreach (var contact in piece.Definition.ContactPoints)
            {
                Vector3 world = piece.Position + contact.LocalPosition;

                // Solo normales hacia arriba (soporte real)
                if (Vector3.Dot(contact.Normal, Vector3.up) > 0.5f)
                    points.Add(world);
            }
        }

        return points;
    }

    Vector3 ComputeGlobalCenterOfMass(BoardState board)
    {
        float totalMass = 0f;
        Vector3 weightedSum = Vector3.zero;

        foreach (var piece in board.Pieces.Values)
        {
            float mass = piece.Definition.Mass;
            Vector3 worldCom = piece.Position + piece.Definition.CenterOfMass;

            weightedSum += worldCom * mass;
            totalMass += mass;
        }

        return weightedSum / totalMass;
    }

    bool IsPointInsideSupportPolygon(Vector3 point, List<Vector3> supports)
    {
        if (supports.Count < 3)
            return false;

        var polygon = supports
            .Select(p => new Vector2(p.x, p.z))
            .ToList();

        Vector2 p = new(point.x, point.z);

        return PointInPolygon(p, polygon);
    }

    bool PointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        int intersections = 0;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];

            if (((a.y > point.y) != (b.y > point.y)) &&
                (point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x))
            {
                intersections++;
            }
        }

        return intersections % 2 == 1;
    }
}