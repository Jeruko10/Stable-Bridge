using UnityEngine;

public class LocalTransition
{
    public Vector2Int From { get; }
    public Vector2Int To { get; }

    public LocalTransition(Vector2Int from, Vector2Int to)
    {
        From = from;
        To = to;
    }
}
