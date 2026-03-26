using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    [field: SerializeField] public List<BlockSegment> Segments { get; set; } = new();
    [field: SerializeField] public bool Mirrored { get; set; } = false;
    [field: SerializeField] public Mobility MobilityType { get; set; } = Mobility.Free;
    [field: SerializeField] public Vector2Int MobilityPivot { get; set; } = Vector2Int.zero;
    [field: SerializeField] public float UnsnappedZOffset { get; set; } = 5f;

    public enum Mobility { Free, RotateOnly, SlideOnly, Pinned }

    BoardGrid.Rotation rotation = BoardGrid.Rotation.Deg0;

    void Awake() => FetchSegments();

    void Update()
    {
        // Vector3 pos = transform.position;
        // pos.z = Mathf.Lerp(pos.z, IsInGrid ? 0f : UnsnappedZOffset, Time.deltaTime * 10f);
        // transform.position = pos;
    }

    public bool ContainsSegment(BlockSegment segment) => Segments.Contains(segment);

    public void Rotate(BlockSegment pivotSegment = null)
    {
        Vector3 oldPos = pivotSegment != null ? pivotSegment.transform.position : transform.position;
        rotation = rotation == BoardGrid.Rotation.Deg270 ? BoardGrid.Rotation.Deg0 : rotation + 1;
        transform.SetPositionAndRotation(oldPos - (pivotSegment != null ? pivotSegment.transform.position : transform.position), BoardGrid.GetDiscreteRotation(rotation));
    }

    public void FetchSegments()
    {
        Segments.Clear();
        foreach (BlockSegment segment in GetComponentsInChildren<BlockSegment>()) Segments.Add(segment);
    }
}