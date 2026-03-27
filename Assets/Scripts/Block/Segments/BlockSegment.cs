using UnityEngine;

public abstract class BlockSegment : MonoBehaviour
{
    public abstract void Initialize();
    public abstract Vector2Int[] GetNavigableTiles(Vector2Int blockedLocals);
}