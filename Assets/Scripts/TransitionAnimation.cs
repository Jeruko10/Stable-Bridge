using UnityEngine;

public class TransitionAnimation
{
    public AnimationClip Clip { get; }
    public Vector3 LocalDisplacement { get; }
    public float Duration { get; }

    public TransitionAnimation(AnimationClip clip, Vector3 displacement, float duration)
    {
        Clip = clip;
        LocalDisplacement = displacement;
        Duration = duration;
    }
}