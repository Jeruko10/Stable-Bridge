using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KnightBehaviour : MonoBehaviour
{
    [field: SerializeField] public float MoveSpeed { get; set; } = 3f;
    [field: SerializeField] public float ArrivalThreshold { get; set; } = 0.05f;
    [field: SerializeField] public float EndKeepDistance { get; set; } = 0.5f;
    public float HeightOffset { get; set; }
    public event Action<bool> GoalReached;

    bool isActivated = false;
    bool pathReachesGoal = false;
    List<Vector2> waypoints = new();
    int targetIndex = 0;
    Rigidbody body;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
    }

    void Update()
    {
        if (!isActivated || waypoints == null || targetIndex >= waypoints.Count) return;

        Vector3 targetWorld = GetTargetPosition();
        float currentThreshold = GetCurrentThreshold();

        if (HasReachedTarget(targetWorld, currentThreshold))
        {
            if (HandleTargetReached())
            {
                CompletePath();
                return;
            }
            targetWorld = GetTargetPosition();
        }

        MoveTowardTarget(targetWorld);
    }

    Vector3 GetTargetPosition()
    {
        Vector2 target2D = waypoints[targetIndex];
        return new Vector3(target2D.x, target2D.y + HeightOffset, transform.position.z);
    }

    float GetCurrentThreshold() => targetIndex == waypoints.Count - 1 ? EndKeepDistance : ArrivalThreshold;

    bool HasReachedTarget(Vector3 targetWorld, float threshold) => Vector3.Distance(transform.position, targetWorld) <= threshold;

    bool HandleTargetReached()
    {
        if (targetIndex < waypoints.Count - 1)
            transform.position = GetTargetPosition();

        if (targetIndex == waypoints.Count - 1) // Final target reached
        {
            isActivated = false;
            return true;
        }

        targetIndex++;

        return false;
    }

    void MoveTowardTarget(Vector3 targetWorld)
    {
        Vector3 next = Vector3.MoveTowards(transform.position, targetWorld, MoveSpeed * Time.deltaTime);
        RotateSpriteForDirection(targetWorld - transform.position);
        transform.position = next;
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

        GoalReached?.Invoke(pathReachesGoal);
    }

    public void FollowPath(IEnumerable<Vector2> path, bool reachesGoal)
    {
        pathReachesGoal = reachesGoal;
        waypoints = new List<Vector2>(path);

        if (waypoints.Count == 0)
        {
            targetIndex = 0;
            CompletePath();
            return;
        }

        targetIndex = 0;
        isActivated = true;
    }
}

