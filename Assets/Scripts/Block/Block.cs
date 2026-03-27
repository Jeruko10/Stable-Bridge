using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Block : MonoBehaviour
{
    [field: SerializeField] GameObject pivotPrefab;
    [field: SerializeField] public float UnsnappedZOffset { get; set; } = -2f;
    [field: SerializeField] public float SnapAnimSpeed { get; set; } = 10f;
    [field: SerializeField] public float MoveLerpSpeed { get; set; } = 10f;
    public BlockSegment[] Segments => segments.ToArray();
    public BlockSegment Pivot { get; set; }
    public Mobility MobilityType { get; set; } = Mobility.Free;
    public bool IsInGrid { get; set; } = false;
    public Vector2 Position2D { get => targetPosition2D; set => targetPosition2D = value; }
    
    public enum Mobility { Free, RotateOnly, SlideOnly, Fixed }

    readonly List<BlockSegment> segments = new();
    Vector2 targetPosition2D;
    BoardGrid.Rotation rotation = BoardGrid.Rotation.Deg0;
    bool isMirrored = false;

    void Awake()
    {
        FetchSegments();
        GetComponent<Rigidbody>().isKinematic = true;
    }

    void Start()
    {
        if (MobilityType == Mobility.RotateOnly) CreatePivotVisual();
    }

    void Update()
    {
        float targetZ = IsInGrid ? 0f : UnsnappedZOffset;
        float newZ = Mathf.Lerp(transform.position.z, targetZ, Time.deltaTime * SnapAnimSpeed);
        Vector2 newPos2D = Vector2.Lerp(transform.position, targetPosition2D, Time.deltaTime * MoveLerpSpeed);

        transform.position = new Vector3(newPos2D.x, newPos2D.y, newZ);
    }

    public bool ContainsSegment(BlockSegment segment) => segments.Contains(segment);

    public void Rotate(BlockSegment pivotSegment, bool clockwise)
    {
        rotation = (BoardGrid.Rotation)(((int)rotation + (clockwise ? 1 : 3)) % 4);
        transform.SetPositionAndRotation(pivotSegment.transform.position, BoardGrid.GetDiscreteRotation(rotation));
        Position2D = transform.position;
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

    void CreatePivotVisual()
    {
        GameObject pivot = Instantiate(pivotPrefab, Pivot.transform);
        pivot.name = "Pivot";
        pivot.transform.Rotate(90f, 0f, 0f);
    }
}