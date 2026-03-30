using System.Collections.Generic;
using UnityEngine;

public class BasicSegment : BlockSegment
{
    readonly LocalTransition[] transitions = new LocalTransition[]
    {
        new(from: new(0, 1), to: new(1, 1)),
        new(from: new(0, 1), to: new(-1, 1))
    };
    
    Block parent;
    
    public override void Initialize(Block parent) => this.parent = parent;

    public override IEnumerable<LocalTransition> GetAvailableTransitions(BoardGrid grid)
    {
        Vector2Int? myTile = grid.GetTileOfBlock(this);
        if (!myTile.HasValue) yield break;

        Vector2Int topTile = myTile.Value + Vector2Int.up;

        if (grid.IsValidTile(topTile) && grid.GetBlockAtTile(topTile) != null)
            yield break;

        foreach (LocalTransition transition in transitions) yield return transition;
    }
}
