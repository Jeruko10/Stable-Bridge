using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlopeSegment : BlockSegment
{
    LocalTransition[] transitions0Deg, transitions90Deg, transitions180Deg, transitions270Deg;
    Block parent;
    bool flipped = false;
    
    public override void Initialize(Block parent)
    {
        this.parent = parent;

        transitions0Deg = new LocalTransition[]
        {
            new(from: TopLeft, to: BottomRight)
        };

        transitions90Deg = new LocalTransition[]
        {
            new(from: BottomLeft, to: TopRight)
        };

        transitions180Deg = new LocalTransition[]
        {
            new(from: TopLeft, to: TopRight),
        };

        transitions270Deg = new LocalTransition[]
        {
            new(from: TopLeft, to: TopRight),
        };
    }
    
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
