using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Solution", menuName = "Game/Solution")]
public class LevelSolution : ScriptableObject
{
    [System.Serializable]
    public struct Placement
    {
        public int blockId;
        public Vector2Int tile;
        public BoardGrid.Rotation rotation;
        public bool flipped;
    }

    [SerializeField] List<Placement> placements = new();
    public IReadOnlyList<Placement> Placements => placements;

#if UNITY_EDITOR
    public void Initialize(List<Placement> data)
    {
        placements = data;
    }
#endif
}
