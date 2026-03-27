using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Block : MonoBehaviour
{
    [field: SerializeField] GameObject rotatePivotPrefab;
    [field: SerializeField] GameObject slidePivotPrefab;
    [field: SerializeField] GameObject fixedPivotPrefab;
    [field: SerializeField] public float UnsnappedZOffset { get; set; } = -2f;
    [field: SerializeField] public float SnapAnimSpeed { get; set; } = 10f;
    [field: SerializeField] public float MoveLerpSpeed { get; set; } = 10f;
    public BlockSegment[] Segments => segments.ToArray();
    public Vector2Int[] SlidePositions { get; set; }
    public int SlideIndex { get; set; } = 0;
    public BlockSegment Pivot { get; private set; }
    public Mobility MobilityType { get; private set; } = Mobility.Free;
    public BoardGrid.Rotation Rotation { get; private set; } = BoardGrid.Rotation.Deg0;
    public Vector2 Position2D { get => targetPosition2D; set => targetPosition2D = value; }
    
    public enum Mobility { Free, RotateOnly, SlideOnly, Fixed }

    readonly List<BlockSegment> segments = new();
    Vector2 targetPosition2D;
    bool isMirrored = false;

    void Awake()
    {
        GetComponent<Rigidbody>().isKinematic = true;
    }

    public void Initialize(int pivotIndex, Mobility mobilityType)
    {
        InitializeSegments();

        Pivot = segments[pivotIndex];
        MobilityType = mobilityType;


        if (MobilityType == Mobility.RotateOnly) CreateRotateVisual();
        else if (MobilityType == Mobility.SlideOnly) CreateSlideVisual();
        else if (MobilityType == Mobility.Fixed) CreateFixedVisual();
    }

    void Update()
    {
        bool beingDragged = !LevelManager.Current.Grid.ContainsBlock(this) && MobilityType == Mobility.Free;
        float targetZ = beingDragged ? UnsnappedZOffset : 0f;
        float newZ = Mathf.Lerp(transform.position.z, targetZ, Time.deltaTime * SnapAnimSpeed);
        Vector2 newPos2D = Vector2.Lerp(transform.position, targetPosition2D, Time.deltaTime * MoveLerpSpeed);

        transform.position = new Vector3(newPos2D.x, newPos2D.y, newZ);
    }

    public bool ContainsSegment(BlockSegment segment) => segments.Contains(segment);

    public void Rotate(BlockSegment pivotSegment, bool clockwise)
    {
        Rotation = (BoardGrid.Rotation)(((int)Rotation + (clockwise ? 1 : 3)) % 4);
        transform.SetPositionAndRotation(pivotSegment.transform.position, BoardGrid.GetDiscreteRotation(Rotation));
        Position2D = transform.position;
    }

    public void Mirror()
    {
        isMirrored = !isMirrored;

        transform.Rotate(0f, 180f, 0f);
    }

    void InitializeSegments()
    {
        segments.Clear();
        foreach (BlockSegment segment in GetComponentsInChildren<BlockSegment>())
        {
            segments.Add(segment);
            segment.Initialize(this);
        }
    }

    void CreateRotateVisual()
    {
        GameObject pivot = Instantiate(rotatePivotPrefab, Pivot.transform);
        pivot.transform.Rotate(90f, 0f, 0f);
    }

    void CreateSlideVisual()
    {
        GameObject pivot = Instantiate(slidePivotPrefab, Pivot.transform);
        pivot.transform.Rotate(90f, 0f, 0f);
    }

    void CreateFixedVisual()
    {
        foreach (BlockSegment segment in segments)
        {
            GameObject pivot = Instantiate(fixedPivotPrefab, segment.transform);
            pivot.transform.Rotate(90f, 0f, 0f);
        }
    }
}