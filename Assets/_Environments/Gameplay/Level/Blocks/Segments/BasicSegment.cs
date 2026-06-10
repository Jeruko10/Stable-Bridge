using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BasicSegment : BlockSegment
{
    LocalTransition[] transitions;
    Vector2[] shape;
    Block parent;
    bool flipped = false;

    public override void Initialize(Block parent)
    {
        this.parent = parent;

        shape = new Vector2[]
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        };

        transitions = new LocalTransition[]
        {
            new(from: TopLeft, to: TopRight),
        };
    }
    
    public override Block GetParent() => parent;

    public override void Flip() => flipped = !flipped;

    public override IEnumerable<Vector2> GetShape() => shape;

    public override IEnumerable<LocalTransition> GetTransitions() => flipped ? transitions.Select(t => t.Flipped()) : transitions;
}
