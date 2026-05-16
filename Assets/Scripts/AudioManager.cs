using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [field: SerializeField] public AudioClip UIConfirmation { get; private set; }
    [field: SerializeField] public AudioClip UIStartPath { get; private set; }
    [field: SerializeField] public AudioClip UIButtonClick { get; private set; }
    [field: SerializeField] public AudioClip Failure { get; private set; }
    [field: SerializeField] public AudioClip Success { get; private set; }
    [field: SerializeField] public AudioClip MenuTheme { get; private set; }
    [field: SerializeField] public AudioClip GameplayTheme { get; private set; }
    [field: SerializeField] public AudioClip[] Blocks { get; private set; }

    static AudioManager instance;
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

    public static void Play(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        Play(clips[Random.Range(0, clips.Length)]);
    }
}
