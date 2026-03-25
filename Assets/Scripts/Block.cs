using System;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public List<BlockSegment> Segments = new();
    public bool Mirrored = false;
    
    BoardGrid.Rotation rotation = BoardGrid.Rotation.Deg0;

    void Awake()
    {
        Segments.Clear();

        foreach (var segment in GetComponentsInChildren<BlockSegment>())
            Segments.Add(segment);
    }

    public bool ContainsSegment(BlockSegment segment) => Segments.Contains(segment);

    public void Rotate()
    {
        if (++rotation > BoardGrid.Rotation.Deg270) rotation = BoardGrid.Rotation.Deg0;

        transform.rotation = BoardGrid.GetDiscreteRotation(rotation);
    }
}