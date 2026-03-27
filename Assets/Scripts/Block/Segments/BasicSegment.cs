using System.Collections.Generic;
using UnityEngine;

public class BasicSegment : BlockSegment
{
    readonly IEnumerable<Vector2Int> deg0Navigable = NavigationMatrix.GetDirections(topCenter: true);
    readonly IEnumerable<Vector2Int> deg90Navigable = NavigationMatrix.GetDirections(topCenter: true);
    readonly IEnumerable<Vector2Int> deg180Navigable = NavigationMatrix.GetDirections(topCenter: true);
    readonly IEnumerable<Vector2Int> deg270Navigable = NavigationMatrix.GetDirections(topCenter: true);
    Block parent;

    public override void Initialize(Block parent)
    {
        this.parent = parent;
    }

    public override IEnumerable<Vector2Int> GetNavigableTiles()
    {
        IEnumerable<Vector2Int> navigableTiles = GetNavigableForRotation(parent.Rotation);
        Dictionary<Vector2Int, BlockSegment> blocked = LevelManager.Current.Grid.GetNeighbors(this);
    
        foreach (Vector2Int tile in navigableTiles)
        {
            if (blocked[tile] != null) // There is a segment
            {
                Debug.Log("Adding");
                yield return tile;
            }
        }

    }

    IEnumerable<Vector2Int> GetNavigableForRotation(BoardGrid.Rotation rotation)
    {
        return rotation switch
        {
            BoardGrid.Rotation.Deg0 => deg0Navigable,
            BoardGrid.Rotation.Deg90 => deg90Navigable,
            BoardGrid.Rotation.Deg180 => deg180Navigable,
            BoardGrid.Rotation.Deg270 => deg270Navigable,
            _ => deg0Navigable
        };
    }
}