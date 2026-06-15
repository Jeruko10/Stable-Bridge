using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Block : MonoBehaviour
{
    [field: SerializeField] public Sprite InterfaceImage { get; private set; }
    [field: SerializeField] GameObject rotatePivotPrefab;
    [field: SerializeField] GameObject slidePivotPrefab;
    [field: SerializeField] GameObject fixedPivotPrefab;
    [field: SerializeField] ParticleSystem destroyFXPrefab;
    [field: SerializeField] float colliderShrinkAmount = 0.1f;
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
    public Vector2 Position2D { get; set; }
    public Rigidbody Rigidbody { get; private set; }
    public bool IsFlipped { get; private set; }
    public float DepthOffset { get; set; } = 0f;

    public enum Mobility { Free, RotateOnly, SlideOnly, Fixed, Ground }

    readonly List<BlockSegment> segments = new();
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
        else if (MobilityType == Mobility.Ground) CreateGroundVisual();

        ShrinkCollider(colliderShrinkAmount);
    }

    public void ShrinkCollider(float amount)
    {
        float scale = 1f - amount;

        Vector3 centroid = segments.Aggregate(Vector3.zero, (sum, s) => sum + s.transform.position) / segments.Count;

        foreach (BlockSegment segment in segments)
        {
            Collider col = null;
            foreach (Transform child in segment.transform)
                if (child.TryGetComponent(out col)) break;
            if (col == null) continue;

            Transform colTransform = col.transform;
            Vector3 newWorldPos = centroid + (segment.transform.position - centroid) * scale;
            colTransform.localPosition = segment.transform.InverseTransformPoint(newWorldPos);
            colTransform.localScale *= scale;
        }
    }

    void Update()
    {
        if (physicsEnabled) return;
        
        bool beingDragged = !LevelManager.Current.Grid.ContainsBlock(this) && MobilityType == Mobility.Free;
        float targetZ = beingDragged ? unsnappedZOffset : DepthOffset;
        float newZ = Mathf.Lerp(transform.position.z, targetZ, Time.deltaTime * snapAnimSpeed);
        Vector2 newPos2D = Vector2.Lerp(transform.position, Position2D, Time.deltaTime * moveLerpSpeed);

        transform.position = new Vector3(newPos2D.x, newPos2D.y, newZ);
    }

    public void Destroy()
    {
        if (destroyFXPrefab != null)
            foreach (BlockSegment segment in segments)
                Instantiate(destroyFXPrefab, segment.transform.position, Quaternion.identity);

        AudioManager.Play(AudioManager.Instance.BlockBreaking);
        Destroy(gameObject);
    }

    public void SetPhysics(bool enabled)
    {
        physicsEnabled = enabled;
        Rigidbody.isKinematic = !enabled;
    }

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
        Position2D += delta;
    }

    public void Flip(BlockSegment pivotSegment)
    {
        IsFlipped = !IsFlipped;
        
        SetRotation(pivotSegment, Rotation);

        foreach (BlockSegment segment in segments)
            segment.Flip();
    }

    public bool ShapeMatchesPlacement(Vector2 rootWorld, BoardGrid.Rotation rotation, bool flipped)
    {
        // if I were to place this block at rootWorld with rotation/flipped, would it cover the exact same floor tiles it covers right now?
        var actual   = new HashSet<(int, int)>(GetShape().Select(ShapeKey));
        var expected = new HashSet<(int, int)>(GetShapeAtPlacement(rootWorld, rotation, flipped).Select(ShapeKey));
        return actual.SetEquals(expected);
    }

    IEnumerable<Vector2> GetShape()
    {
        var keys = new HashSet<(int, int)>();
        var result = new List<Vector2>();
        foreach (BlockSegment segment in segments)
            foreach (Vector2 corner in segment.GetShape())
            {
                Vector2 world = segment.transform.TransformPoint(corner);
                if (keys.Add(ShapeKey(world))) result.Add(world);
            }
        return result;
    }

    IEnumerable<Vector2> GetShapeAtPlacement(Vector2 rootWorld, BoardGrid.Rotation rotation, bool flipped)
    {
        Quaternion rot = Quaternion.Euler(0f, flipped ? 180f : 0f, (float)rotation);
        var keys = new HashSet<(int, int)>();
        var result = new List<Vector2>();
        foreach (BlockSegment segment in segments)
        {
            Vector2 segWorld = rootWorld + (Vector2)(rot * segment.transform.localPosition);
            foreach (Vector2 corner in segment.GetShape())
            {
                Vector2 world = segWorld + (Vector2)(rot * (Vector3)corner);
                if (keys.Add(ShapeKey(world))) result.Add(world);
            }
        }
        return result;
    }

    static (int, int) ShapeKey(Vector2 p) => (Mathf.RoundToInt(p.x * 2), Mathf.RoundToInt(p.y * 2));

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

    void CreateGroundVisual()
    {
        foreach (BlockSegment segment in segments)
        {
            GameObject pivot = Instantiate(fixedPivotPrefab, segment.transform);
            pivot.transform.Rotate(90f, 0f, 0f);
        }
    }
}