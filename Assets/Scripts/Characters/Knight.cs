using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Knight : MonoBehaviour
{
    [field: SerializeField] public float Speed { get; set; } = 1f;
    [field: SerializeField] public float MaxTime { get; set; } = 20f;
    [field: SerializeField] public float ArrivalThreshold { get; set; } = 0.05f;
    [field: SerializeField] public float EndKeepDistance { get; set; } = 0.5f;
    public float HeightOffset { get; set; }
    public event Action PathEnded;

    bool isActivated = false, pathReachesGoal = false;
    Vector3[] path;
    Vector3 startPosition;
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
        if (!isActivated || path == null || targetIndex >= path.Length) return;

        if (timer <= 0f) CompletePath();

        Vector3 targetPosition = GetTargetPosition();
        float currentThreshold = GetCurrentThreshold();

        if (HasReachedTarget(targetPosition, currentThreshold))
        {
            if (targetIndex == path.Length - 1)
            {
                CompletePath();
                return;
            }

            transform.position = targetPosition; // Fix exact position
            targetIndex++;
            targetPosition = GetTargetPosition(); // Re-calculate new index
        }

        MoveTowardTarget(targetPosition, Speed);
        timer -= Time.deltaTime;
    }

    void OnDrawGizmos()
    {
        if (path == null || path.Length <= 1) return;

        const float depth = -1f;
        Gizmos.color = Color.pink;
        Vector3 currentPos = new(startPosition.x, startPosition.y, depth);

        for (int i = 0; i < path.Length; i++)
        {
            Vector2 tile = path[i];
            Vector3 nextPos = new(tile.x, tile.y + HeightOffset, depth);
            Gizmos.DrawLine(currentPos, nextPos);
            Gizmos.DrawSphere(currentPos, 0.1f);
            currentPos = nextPos;
        }
        Gizmos.DrawSphere(currentPos, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(new(GetTargetPosition().x, GetTargetPosition().y, depth), 0.13f);
    }

    public void StartPathAnimation(Vector3[] path, bool reachesGoal)
    {
        startPosition = transform.position;
        pathReachesGoal = reachesGoal;
        this.path = path;

        if (this.path.Length <= 0)
        {
            Debug.LogWarning($"Path is too short: {this.path.Length} animations. Check if it was intentional.");

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
        Vector2 position = path[targetIndex];

        position.y += HeightOffset;
        return new Vector3(position.x, position.y, transform.position.z);
    }

    float GetCurrentThreshold() => targetIndex == path.Length - 1 ? EndKeepDistance : ArrivalThreshold;

    bool HasReachedTarget(Vector3 targetPosition, float threshold) => Vector3.Distance(transform.position, targetPosition) <= threshold;

    void MoveTowardTarget(Vector3 position, float moveSpeed)
    {    
        Vector3 nextPos = Vector3.MoveTowards(transform.position, position, moveSpeed * Time.deltaTime);
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
