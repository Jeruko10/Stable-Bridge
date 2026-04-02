using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlopeSegment : BlockSegment
{
    IEnumerable<LocalTransition> Transitions => mirrored ? transitions.Select(t => t.Mirrored()) : transitions;
    
    readonly LocalTransition[] transitions = new LocalTransition[]
    {
        new(from: new(0, 0), to: new(1, 0)),
        new(from: new(0, 0), to: new(0, 1))
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

        foreach (LocalTransition transition in Transitions)
        {
            Vector2Int destinationTile = myTile.Value + transition.To;

            if (grid.IsValidTile(destinationTile) && grid.GetBlockAtTile(destinationTile) == null)
                yield return transition;
        }
    }
}
