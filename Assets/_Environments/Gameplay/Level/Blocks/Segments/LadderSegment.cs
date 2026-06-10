using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LadderSegment : BlockSegment
{
    LocalTransition[] transitions0Deg, transitions90Deg, transitions180Deg, transitions270Deg;
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

        transitions0Deg = new LocalTransition[]
        {
            new(from: BottomRight, to: TopRight)
        };

        transitions90Deg = new LocalTransition[]
        {
            new(from: TopLeft, to: TopRight)
        };

        transitions180Deg = new LocalTransition[]
        {
            new(from: TopLeft, to: TopRight),
        };

        transitions270Deg = new LocalTransition[]
        {
            new(from: BottomLeft, to: TopLeft),
        };
    }
    
    public override Block GetParent() => parent;

    public override void Flip() => flipped = !flipped;

    public override IEnumerable<Vector2> GetShape() => shape;
    
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
