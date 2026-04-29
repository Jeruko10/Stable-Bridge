using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BlockSegment : MonoBehaviour
{
    public abstract void Initialize(Block parent);
    public abstract Block GetParent();
    public abstract void Flip();
    public abstract IEnumerable<LocalTransition> GetTransitions();

    protected Vector2 TopLeft => new(-0.5f, 0.5f);
    protected Vector2 TopRight => new(0.5f, 0.5f);
    protected Vector2 BottomLeft => new(-0.5f, -0.5f);
    protected Vector2 BottomRight => new(0.5f, -0.5f);
}
