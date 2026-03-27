using System.Collections.Generic;
using UnityEngine;

public abstract class BlockSegment : MonoBehaviour
{
    public abstract void Initialize(Block parent);
    public abstract IEnumerable<Vector2Int> GetNavigableTiles();
}