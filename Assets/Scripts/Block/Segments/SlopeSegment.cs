using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlopeSegment : BlockSegment
{
    readonly LocalTransition[] transitions0Deg = new LocalTransition[]
    {
        new(from: new(0, 1), to: new(1, 0)),
    };

    readonly LocalTransition[] transitions90Deg = new LocalTransition[]
    {
        new(from: new(0, 1), to: new(1, 1)),
        new(from: new(0, 1), to: new(-1, 1))
    };

    readonly LocalTransition[] transitions180Deg = new LocalTransition[]
    {
        new(from: new(0, 1), to: new(1, 1)),
        new(from: new(0, 1), to: new(-1, 1))
    };

    readonly LocalTransition[] transitions270Deg = new LocalTransition[]
    {
        new(from: new(0, 0), to: new(1, 1)),
    };

    Block parent;
    bool flipped = false;
    
    public override void Initialize(Block parent) => this.parent = parent;
    
    public override Block GetParent() => parent;

    public override void Flip() => flipped = !flipped;

    public override IEnumerable<LocalTransition> GetTransitions()
    {
        IEnumerable<LocalTransition> rotatedTransitions = parent.Rotation switch
        {
            BoardGrid.Rotation.Deg0 => transitions0Deg,
            BoardGrid.Rotation.Deg90 => transitions90Deg,
            BoardGrid.Rotation.Deg180 => transitions180Deg,
            BoardGrid.Rotation.Deg270 => transitions270Deg,
            _ => transitions0Deg
        };
    
        return flipped ? rotatedTransitions.Select(t => t.Flipped()) : rotatedTransitions;
    }
}
