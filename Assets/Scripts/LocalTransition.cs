using UnityEngine;

public readonly struct LocalTransition
{
    public Vector2 From { get; }
    public Vector2 To { get; }

    public LocalTransition(Vector2 from, Vector2 to)
    {
        From = from;
        To = to;
    }

    public LocalTransition Flipped() => new(new(-From.x, From.y), new(-To.x, To.y));
}