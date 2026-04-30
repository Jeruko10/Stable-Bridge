using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BlockSegment : MonoBehaviour
{
    public abstract void Initialize(Block parent);
    public abstract Block GetParent();
    public abstract void Flip();
    public abstract IEnumerable<LocalTransition> GetTransitions();

    public const float SideLength = 1f;
    public const float Apothem = SideLength / 2;
    public static readonly Vector2 TopLeft = new(-Apothem, Apothem);
    public static readonly Vector2 TopRight = new(Apothem, Apothem);
    public static readonly Vector2 BottomLeft = new(-Apothem, -Apothem);
    public static readonly Vector2 BottomRight = new(Apothem, -Apothem);
}
