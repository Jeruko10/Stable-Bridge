using System;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    [field: SerializeField] public List<BlockSegment> Segments { get; set; } = new();
    [field: SerializeField] public bool Mirrored { get; set; } = false;
    [field: SerializeField] public Mobility MobilityType { get; set; } = Mobility.Free;
    [field: SerializeField] public Vector2Int MobilityPivot { get; set; } = Vector2Int.zero;

    public enum Mobility
    {
        Free,
        RotateOnly,
        SlideOnly,
        Pinned
    }

    BoardGrid.Rotation rotation = BoardGrid.Rotation.Deg0;

    void Awake()
    {
        Segments.Clear();
        foreach (var segment in GetComponentsInChildren<BlockSegment>()) Segments.Add(segment);
    }

    public bool ContainsSegment(BlockSegment segment) => Segments.Contains(segment);

    public void Rotate()
    {
        rotation = rotation == BoardGrid.Rotation.Deg270 ? BoardGrid.Rotation.Deg0 : rotation + 1;
        transform.rotation = BoardGrid.GetDiscreteRotation(rotation);
    }

    public void FetchSegments()
    {
        Segments.Clear();
        foreach (BlockSegment segment in GetComponentsInChildren<BlockSegment>())
            Segments.Add(segment);
    }
}