using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [field: SerializeField] public AudioClip BlockPlace { get; private set; }
    [field: SerializeField] public AudioClip BlockPickup { get; private set; }
    [field: SerializeField] public AudioClip LevelSuccess { get; private set; }
    [field: SerializeField] public AudioClip LevelFail { get; private set; }
    [field: SerializeField] public AudioClip MinerWalk { get; private set; }
    [field: SerializeField] public AudioClip ButtonClick { get; private set; }

    public static AudioManager instance;
    AudioSource source;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        source = GetComponent<AudioSource>();
    }

    public static void Play(AudioClip clip)
    {
        if (clip == null) return;
        instance.source.PlayOneShot(clip);
    }
}
