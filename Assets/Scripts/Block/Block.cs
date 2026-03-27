using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Block : MonoBehaviour
{
    [field: SerializeField] public float UnsnappedZOffset { get; set; } = 5f;
    public BlockSegment[] Segments => segments.ToArray();
    public BlockSegment Pivot { get; set; }
    public Mobility MobilityType { get; set; } = Mobility.Free;
    
    public enum Mobility { Free, RotateOnly, SlideOnly, Fixed }

    readonly List<BlockSegment> segments = new();
    BoardGrid.Rotation rotation = BoardGrid.Rotation.Deg0;
    bool isMirrored = false;

    void Awake()
    {
        FetchSegments();
        GetComponent<Rigidbody>().isKinematic = true;
    }

    void Update()
    {
        // Vector3 pos = transform.position;
        // pos.z = Mathf.Lerp(pos.z, IsInGrid ? 0f : UnsnappedZOffset, Time.deltaTime * 10f);
        // transform.position = pos;
    }

    public bool ContainsSegment(BlockSegment segment) => segments.Contains(segment);

    public void Rotate(BlockSegment pivotSegment, bool clockwise)
    {
        rotation = (BoardGrid.Rotation)(((int)rotation + (clockwise ? 1 : 3)) % 4);
        transform.SetPositionAndRotation(pivotSegment.transform.position, BoardGrid.GetDiscreteRotation(rotation));
    }

    public void Mirror()
    {
        isMirrored = !isMirrored;

        transform.Rotate(0f, 180f, 0f);
    }

    public void FetchSegments()
    {
        segments.Clear();
        foreach (BlockSegment segment in GetComponentsInChildren<BlockSegment>())
            segments.Add(segment);
    }
}