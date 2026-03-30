using UnityEngine;
using System.Collections.Generic;

public static class NavigationMatrix
{
    readonly static Vector2Int[] directionsByIndex = 
    {
        new(-1, 1),  new(0, 1),  new(1, 1),
        new(-1, 0),  new(0, 0),  new(1, 0),
        new(-1, -1), new(0, -1), new(1, -1)
    };

    public static IEnumerable<Vector2Int> GetDirections(
        bool topLeft = false, bool topCenter = false, bool topRight = false,
        bool left = false, bool center = false, bool right = false,
        bool bottomLeft = false, bool bottomCenter = false, bool bottomRight = false)
    {
        bool[] values = { topLeft, topCenter, topRight, left, center, right, bottomLeft, bottomCenter, bottomRight };

        for (int i = 0; i < values.Length; i++)
        {
            if (!values[i]) continue;
            yield return directionsByIndex[i];
        }
    }
}
