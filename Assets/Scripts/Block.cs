using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public List<BlockSegment> Segments = new();

    void Awake()
    {
        Segments.Clear();

        foreach (var segment in GetComponentsInChildren<BlockSegment>())
            Segments.Add(segment);
    }

    public bool ContainsSegment(BlockSegment segment) => Segments.Contains(segment);
}