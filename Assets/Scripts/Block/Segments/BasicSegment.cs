using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BasicSegment : BlockSegment
{
    readonly LocalTransition[] transitions = new LocalTransition[]
    {
        new(from: new(0, 1), to: new(1, 1)),
        new(from: new(0, 1), to: new(-1, 1))
    };

    Block parent;
    bool flipped = false;
    
    public override void Initialize(Block parent) => this.parent = parent;
    
    public override Block GetParent() => parent;

    public override void Flip() => flipped = !flipped;

    public override IEnumerable<LocalTransition> GetTransitions() => flipped ? transitions.Select(t => t.Flipped()) : transitions;
}
