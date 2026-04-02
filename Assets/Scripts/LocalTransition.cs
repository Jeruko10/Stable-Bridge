using UnityEngine;

public readonly struct LocalTransition
{
    public Vector2Int From { get; }
    public Vector2Int To { get; }

    public LocalTransition(Vector2Int from, Vector2Int to)
    {
        From = from;
        To = to;
    }

    public LocalTransition Mirrored() => new(new(-From.x, From.y), new(-To.x, To.y));
}