using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlopeSegment : BlockSegment
{
    readonly LocalTransition[] transitions0Deg = new LocalTransition[]
    {
        new(from: new(0, 0), to: new(0, 1), animation: new(new(-0.5f, 1))),
        new(from: new(0, 0), to: new(1, 0), animation: new(new(0.5f, 0)))
    };

    readonly LocalTransition[] transitions90Deg = new LocalTransition[]
    {
        new(from: new(0, 1), to: new(1, 1), animation: new(new(0.5f, 1))),
        new(from: new(0, 1), to: new(-1, 1), animation: new(new(-0.5f, 1)))
    };

    readonly LocalTransition[] transitions180Deg = new LocalTransition[]
    {
        new(from: new(0, 1), to: new(1, 1), animation: new(new(0.5f, 1))),
        new(from: new(0, 1), to: new(-1, 1), animation: new(new(-0.5f, 1)))
    };

    readonly LocalTransition[] transitions270Deg = new LocalTransition[]
    {
        new(from: new(0, 0), to: new(0, 1), animation: new(new(0.5f, 1))),
        new(from: new(0, 0), to: new(-1, 0), animation: new(new(-0.5f, 0)))
    };

    Block parent;
    bool mirrored = false;
    
    public override void Initialize(Block parent) => this.parent = parent;
    
    public override Block GetParent() => parent;

    public override void Mirror() => mirrored = !mirrored;

    public override IEnumerable<LocalTransition> GetAvailableTransitions(BoardGrid grid)
    {
        Vector2Int? myTile = grid.GetTileOfBlock(this);
        if (!myTile.HasValue) yield break;

        foreach (LocalTransition transition in GetProcessedTransitions())
        {
            Vector2Int destinationTile = myTile.Value + transition.To;

            if (grid.IsValidTile(destinationTile) && grid.GetBlockAtTile(destinationTile) == null)
                yield return transition;
        }
    }

    IEnumerable<LocalTransition> GetProcessedTransitions()
    {
        IEnumerable<LocalTransition> rotatedTransitions = parent.Rotation switch
        {
            BoardGrid.Rotation.Deg0 => transitions0Deg,
            BoardGrid.Rotation.Deg90 => transitions90Deg,
            BoardGrid.Rotation.Deg180 => transitions180Deg,
            BoardGrid.Rotation.Deg270 => transitions270Deg,
            _ => transitions0Deg
        };
    
        return mirrored ? rotatedTransitions.Select(t => t.Mirrored()) : rotatedTransitions;
    }
}
