using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    [field: SerializeField] public bool Mirrored { get; set; } = false;
    [field: SerializeField] public Mobility MobilityType { get; set; } = Mobility.Free;
    [field: SerializeField] public Vector2Int MobilityPivot { get; set; } = Vector2Int.zero;
    [field: SerializeField] public bool Snapped { get; set; } = true;
    [field: SerializeField] public float UnsnappedZOffset { get; set; } = 5f;
    public IEnumerable<BlockSegment> Segments => segments;

    public enum Mobility { Free, RotateOnly, SlideOnly, Pinned }

    readonly HashSet<BlockSegment> segments = new();
    BoardGrid.Rotation rotation = BoardGrid.Rotation.Deg0;

    void Awake() => FetchSegments();

    void Update()
    {
        Vector3 pos = transform.position;
        pos.z = Mathf.Lerp(pos.z, Snapped ? 0f : UnsnappedZOffset, Time.deltaTime * 10f);
        transform.position = pos;
    }

    public void Rotate()
    {
        rotation = rotation == BoardGrid.Rotation.Deg270 ? BoardGrid.Rotation.Deg0 : rotation + 1;
        transform.rotation = BoardGrid.GetDiscreteRotation(rotation);
    }

    public void FetchSegments()
    {
        segments.Clear();
        foreach (BlockSegment segment in GetComponentsInChildren<BlockSegment>()) segments.Add(segment);
    }
}