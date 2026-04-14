using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KnightBehaviour : MonoBehaviour
{
    [field: SerializeField] public float MoveSpeed { get; set; } = 3f;
    [field: SerializeField] public float MaxTime { get; set; } = 20f;
    [field: SerializeField] public float ArrivalThreshold { get; set; } = 0.05f;
    [field: SerializeField] public float EndKeepDistance { get; set; } = 0.5f;
    public float HeightOffset { get; set; }
    public event Action PathEnded;

    bool isActivated = false, pathReachesGoal = false;
    List<Vector2> waypoints = new();
    List<BlockSegment> destinations = new();
    int targetIndex = 0;
    float timer;
    Rigidbody body;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
    }

    void Update()
    {
        if (!isActivated || waypoints == null || targetIndex >= waypoints.Count) return;

        if (timer <= 0f) CompletePath();

        Vector3 targetWorld = GetTargetPosition();
        BlockSegment targetBlock = destinations[targetIndex];
        float currentThreshold = GetCurrentThreshold();

        if (HasReachedTarget(targetWorld, currentThreshold))
        {
            if (targetIndex == waypoints.Count - 1)
            {
                CompletePath();
                return;
            }

            transform.position = targetWorld; // Fix exact position
            targetIndex++;
            targetWorld = GetTargetPosition(); // Re-calculate new index
        }

        MoveTowardTarget(targetWorld, targetBlock);
        timer -= Time.deltaTime;
    }

    public void FollowPath(Dictionary<Vector2, BlockSegment> path, bool reachesGoal)
    {
        pathReachesGoal = reachesGoal;
        waypoints = new(path.Keys);
        destinations = new(path.Values);

        if (waypoints.Count <= 0)
        {
            Debug.LogWarning($"Path is too short: {waypoints.Count} waypoints. Check if it was intentional.");

            targetIndex = 0;
            CompletePath();
            return;
        }

        targetIndex = 0;
        timer = MaxTime;
        isActivated = true;
    }

    Vector3 GetTargetPosition()
    {
        Vector2 target2D = waypoints[targetIndex];
        return new Vector3(target2D.x, target2D.y + HeightOffset, transform.position.z);
    }

    float GetCurrentThreshold() => targetIndex == waypoints.Count - 1 ? EndKeepDistance : ArrivalThreshold;

    bool HasReachedTarget(Vector3 targetWorld, float threshold) => Vector3.Distance(transform.position, targetWorld) <= threshold;

    void MoveTowardTarget(Vector3 position, BlockSegment segment)
    {
        if (segment is SlopeSegment) position.y += 1;
    
        Vector3 nextPos = Vector3.MoveTowards(transform.position, position, MoveSpeed * Time.deltaTime);
        RotateSpriteForDirection(position - transform.position);
        transform.position = nextPos;
    }

    void RotateSpriteForDirection(Vector3 delta)
    {
        if (Mathf.Abs(delta.x) <= Mathf.Abs(delta.y) || delta.sqrMagnitude <= 0.000001f) return;

        Vector3 scale = transform.localScale;
        scale.x = delta.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    void CompletePath()
    {
        isActivated = false;

        if (!pathReachesGoal)
        {
            body.isKinematic = false;

            // Apply a small upward force to make the failure more visually noticeable
            body.AddForce(Vector3.right * 2f, ForceMode.Impulse);
        }

        PathEnded?.Invoke();
    }
}

