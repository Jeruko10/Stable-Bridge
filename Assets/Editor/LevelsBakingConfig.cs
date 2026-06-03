using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelsBakingConfig", menuName = "Baking Config")]
public class LevelsBakingConfig : ScriptableObject
{
    [System.Serializable]
    public struct BlockMapping
    {
        public int width;
        public int height;
        public bool isStair;
        public Block prefab;
        public int pivotIndex;
    }

    public Block towerPrefab;
    public List<BlockMapping> blockMappings = new();

    public Block FindPrefab(int w, int h, bool isStair)
    {
        foreach (var m in blockMappings)
            if (m.width == w && m.height == h && m.isStair == isStair)
                return m.prefab;
        return null;
    }

    public int FindPivotIndex(int w, int h, bool isStair)
    {
        foreach (var m in blockMappings)
            if (m.width == w && m.height == h && m.isStair == isStair)
                return m.pivotIndex;
        return 0;
    }
}
