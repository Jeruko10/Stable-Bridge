using System.Collections.Generic;
using UnityEngine;

public class BasicSegment : BlockSegment
{
    readonly IEnumerable<Vector2Int> deg0Navigable = NavigationMatrix.GetDirections(topLeft: true, topCenter: true, topRight: true);
    readonly IEnumerable<Vector2Int> deg90Navigable = NavigationMatrix.GetDirections(topLeft: true, topCenter: true, topRight: true);
    readonly IEnumerable<Vector2Int> deg180Navigable = NavigationMatrix.GetDirections(topLeft: true, topCenter: true, topRight: true);
    readonly IEnumerable<Vector2Int> deg270Navigable = NavigationMatrix.GetDirections(topLeft: true, topCenter: true, topRight: true);
    
    Block parent;

    public override void Initialize(Block parent) => this.parent = parent;

    public override IEnumerable<Vector2Int> GetOutgoingDirections() => parent.Rotation switch
    {
        BoardGrid.Rotation.Deg0 => deg0Navigable,
        BoardGrid.Rotation.Deg90 => deg90Navigable,
        BoardGrid.Rotation.Deg180 => deg180Navigable,
        BoardGrid.Rotation.Deg270 => deg270Navigable,
        _ => deg0Navigable
    };
}