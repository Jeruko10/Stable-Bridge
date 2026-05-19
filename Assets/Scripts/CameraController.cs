using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float cinematicHorizontalAngle = 30f;
    [SerializeField] float cinematicVerticalAngle = 15f;
    [SerializeField] float transitionDuration = 1.5f;

    public Vector3 LookTarget { get; set; }

    Vector3 travelStart;
    Vector3 travelTarget;
    float elapsed;
    bool travelling;

    void Update()
    {
        if (travelling)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            transform.position = Vector3.Lerp(travelStart, travelTarget, t);

            if (elapsed >= transitionDuration)
                travelling = false;
        }

        transform.LookAt(LookTarget);
    }

    public void DoCinematic() => TravelTo(CinematicPosition());

    public void TravelTo(Vector3 target)
    {
        travelStart = transform.position;
        travelTarget = target;
        elapsed = 0f;
        travelling = true;
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
        return LookTarget + offset * Vector3.Distance(transform.position, LookTarget);
    }
}
