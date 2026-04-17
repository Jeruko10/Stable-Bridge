using System;
using UnityEngine;

public readonly struct TransitionAnimation
{
    public Vector2 Destination { get; }
    public float Speed { get; }

    public TransitionAnimation(Vector2 destination, float speed = 1f)
    {
        Destination = destination;
        Speed = speed;
    }

    public TransitionAnimation Flipped() => new(new(-Destination.x, Destination.y), Speed);

    public TransitionAnimation ToGlobal(Vector2 globalPosition) => new(globalPosition + Destination, Speed);
}
