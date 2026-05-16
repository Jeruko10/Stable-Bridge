using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [field: SerializeField] public AudioEntry UIConfirmation { get; private set; }
    [field: SerializeField] public AudioEntry UIStartPath { get; private set; }
    [field: SerializeField] public AudioEntry UIButtonClick { get; private set; }
    [field: SerializeField] public AudioEntry Failure { get; private set; }
    [field: SerializeField] public AudioEntry Success { get; private set; }
    [field: SerializeField] public AudioEntry MenuTheme { get; private set; }
    [field: SerializeField] public AudioEntry LevelTheme { get; private set; }
    [field: SerializeField] public AudioEntry[] Blocks { get; private set; }

    public static AudioManager Instance { get; private set; }
    static AudioSource source;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        source = GetComponent<AudioSource>();
    }

    public static void Play(AudioEntry entry)
    {
        if (entry?.Clip == null) return;
        source.PlayOneShot(entry.Clip, entry.Volume);
    }

    public static void Play(AudioEntry[] entries)
    {
        if (entries == null || entries.Length == 0) return;
        Play(entries[Random.Range(0, entries.Length)]);
    }

    public static void StopAll() => source.Stop();
}
