using UnityEngine;

[System.Serializable]
public class AudioEntry
{
    public AudioClip Clip;
    [Range(0f, 1f)] public float Volume = 1f;
}