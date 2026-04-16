using UnityEngine;

public readonly struct LocalTransition
{
    public Vector2Int From { get; }
    public Vector2Int To { get; }
    public TransitionAnimation Animation { get; }

    public LocalTransition(Vector2Int from, Vector2Int to, TransitionAnimation animation)
    {
        From = from;
        To = to;
        Animation = animation;
    }

    public LocalTransition Mirrored() => new(new(-From.x, From.y), new(-To.x, To.y), Animation.Mirrored());
}