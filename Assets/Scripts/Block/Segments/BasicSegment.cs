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

    public override IEnumerable<LocalTransition> GetAvailableTransitions(BoardGrid grid)
    {
        Vector2Int? myTile = grid.GetTileOfBlock(this);
        if (!myTile.HasValue) yield break;

        Vector2Int topTile = myTile.Value + Vector2Int.up;

        if (grid.IsValidTile(topTile) && grid.GetBlockAtTile(topTile) != null)
            yield break;

        foreach (LocalTransition transition in GetProcessedTransitions())
            yield return transition;
    }

    IEnumerable<LocalTransition> GetProcessedTransitions() => flipped ? transitions.Select(t => t.Flipped()) : transitions;
}
