using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float cinematicDistance = 10f;
    [SerializeField] float cinematicHorizontalAngle = 30f;
    [SerializeField] float cinematicVerticalAngle = 15f;
    [SerializeField] float transitionDuration = 1.5f;

    public Vector3 LookTarget { get; set; }

    void Update()
    {
        transform.LookAt(LookTarget);
    }

    public Task DoCinematic() => TravelTo(CinematicPosition());

    public Task TravelTo(Vector3 target)
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(TransitionRoutine(target, tcs));
        return tcs.Task;
    }

    Vector3 CinematicPosition()
    {
        float h = cinematicHorizontalAngle * Mathf.Deg2Rad;
        float v = cinematicVerticalAngle * Mathf.Deg2Rad;
        Vector3 offset = new(
            Mathf.Sin(h) * Mathf.Cos(v),
            Mathf.Sin(v),
            -Mathf.Cos(h) * Mathf.Cos(v)
        );
        return LookTarget + offset * cinematicDistance;
    }

    IEnumerator TransitionRoutine(Vector3 target, TaskCompletionSource<bool> tcs)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, target, elapsed / transitionDuration);
            yield return null;
        }

        tcs.SetResult(true);
    }
}
