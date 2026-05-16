using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Block : MonoBehaviour
{
    [field: SerializeField] GameObject rotatePivotPrefab;
    [field: SerializeField] GameObject slidePivotPrefab;
    [field: SerializeField] GameObject fixedPivotPrefab;
    [field: SerializeField] float unsnappedZOffset = -2f;
    [field: SerializeField] float snapAnimSpeed = 10f;
    [field: SerializeField] float moveLerpSpeed = 10f;

    public Block Prefab { get; private set; }
    public BlockSegment[] Segments => segments.ToArray();
    public Vector2Int[] SlidePositions { get; set; }
    public Color Color { get => materialColor; set => SetBlockColor(value); }
    public int SlidePositionIndex { get; set; } = 0;
    public BlockSegment Pivot { get; private set; }
    public Mobility MobilityType { get; private set; } = Mobility.Free;
    public BoardGrid.Rotation Rotation { get; private set; } = BoardGrid.Rotation.Deg0;
    public Vector2 Position2D { get => targetPosition2D; set => targetPosition2D = value; }
    public Rigidbody Rigidbody { get; private set; }
    public bool IsFlipped { get; private set; }
    public float DepthOffset { get; set; } = 0f;

    public enum Mobility { Free, RotateOnly, SlideOnly, Fixed }

    readonly List<BlockSegment> segments = new();
    Vector2 targetPosition2D;
    bool physicsEnabled;
    Color materialColor;

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        SetPhysics(false);
    }

    public void Initialize(Block originalPrefab, int pivotIndex, Mobility mobilityType)
    {
        FetchSegmentChildren();
        SetPhysics(false);

        Prefab = originalPrefab;
        materialColor = GetComponentInChildren<Renderer>().material.color;
        Pivot = segments[pivotIndex];
        MobilityType = mobilityType;

        if (MobilityType == Mobility.RotateOnly) CreateRotateVisual();
        else if (MobilityType == Mobility.SlideOnly) CreateSlideVisual();
        else if (MobilityType == Mobility.Fixed) CreateFixedVisual();
    }

    void Update()
    {
        if (physicsEnabled) return;
        
        bool beingDragged = !LevelManager.Current.Grid.ContainsBlock(this) && MobilityType == Mobility.Free;
        float targetZ = beingDragged ? unsnappedZOffset : DepthOffset;
        float newZ = Mathf.Lerp(transform.position.z, targetZ, Time.deltaTime * snapAnimSpeed);
        Vector2 newPos2D = Vector2.Lerp(transform.position, targetPosition2D, Time.deltaTime * moveLerpSpeed);

        transform.position = new Vector3(newPos2D.x, newPos2D.y, newZ);
    }

    public void SetPhysics(bool enabled)
    {
        physicsEnabled = enabled;
        Rigidbody.isKinematic = !enabled;
    }

    public bool ContainsSegment(BlockSegment segment) => segments.Contains(segment);

    public void Rotate(BlockSegment pivotSegment, bool clockwise)
    {
        if (IsFlipped) clockwise = !clockwise;
        
        int rotationIndex = (int)Rotation / 90;
        int nextIndex = (rotationIndex + (clockwise ? 3 : 1)) % 4;
        BoardGrid.Rotation nextRotation = (BoardGrid.Rotation)(nextIndex * 90);
        Debug.Log($"New Rotation: {nextRotation}. Clockwise: {clockwise}. IsFlipped: {IsFlipped}.");
        SetRotation(pivotSegment, nextRotation);
    }

    public void SetRotation(BlockSegment pivotSegment, BoardGrid.Rotation newRotation)
    {
        Vector2 pivotBefore = pivotSegment.transform.position;

        Rotation = newRotation;
        transform.rotation = Quaternion.Euler(0f, IsFlipped ? 180f : 0f, (float)Rotation);

        Vector2 delta = pivotBefore - (Vector2)pivotSegment.transform.position;
        transform.position += (Vector3)delta;
        targetPosition2D += delta;
    }

    public void Flip(BlockSegment pivotSegment)
    {
        IsFlipped = !IsFlipped;
        
        SetRotation(pivotSegment, Rotation);

        foreach (BlockSegment segment in segments)
            segment.Flip();
    }

    void FetchSegmentChildren()
    {
        segments.Clear();
        foreach (BlockSegment segment in GetComponentsInChildren<BlockSegment>())
        {
            segments.Add(segment);
            segment.Initialize(parent: this);
        }
    }

    void SetBlockColor(Color value)
    {
        materialColor = value;
        foreach (BlockSegment segment in segments)
        {
            Renderer renderer = segment.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = materialColor;
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