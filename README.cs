using UnityEngine;

[DisallowMultipleComponent]
public class KinemaSystemSoundPlayer : MonoBehaviour
{
    [Header("Opening Sound")]
    [SerializeField] private AudioClip openingClip;
    [SerializeField] private AudioSource openingAudioSource;
    [SerializeField, Range(0f, 1f)] private float openingVolume = 1f;
    [SerializeField] private bool loopOpeningSound = false;

    [Header("Closing Sound")]
    [SerializeField] private AudioClip closingClip;
    [SerializeField] private AudioSource closingAudioSource;
    [SerializeField, Range(0f, 1f)] private float closingVolume = 1f;
    [SerializeField] private bool loopClosingSound = false;

    [Header("Behavior")]
    [SerializeField] private bool stopOtherSystemSoundOnPlay = true;
    [SerializeField] private bool restartFromBeginning = true;

    [Header("Debug")]
    [SerializeField] private bool logState = true;

    private const string OpeningSourceName = "OpeningSoundSource";
    private const string ClosingSourceName = "ClosingSoundSource";

    private void Awake()
    {
        ResolveAudioSources();
        ConfigureAudioSources();
    }

    private void OnValidate()
    {
        openingVolume = Mathf.Clamp01(openingVolume);
        closingVolume = Mathf.Clamp01(closingVolume);
    }

    public void PlayOpeningSound()
    {
        ResolveAudioSources();
        ConfigureAudioSources();

        if (stopOtherSystemSoundOnPlay)
        {
            StopClosingSound();
        }

        PlayClip(
            openingAudioSource,
            openingClip,
            openingVolume,
            loopOpeningSound,
            "Opening"
        );
    }

    public void PlayClosingSound()
    {
        ResolveAudioSources();
        ConfigureAudioSources();

        if (stopOtherSystemSoundOnPlay)
        {
            StopOpeningSound();
        }

        PlayClip(
            closingAudioSource,
            closingClip,
            closingVolume,
            loopClosingSound,
            "Closing"
        );
    }

    public void StopOpeningSound()
    {
        StopSource(openingAudioSource, "Opening");
    }

    public void StopClosingSound()
    {
        StopSource(closingAudioSource, "Closing");
    }

    public void StopAllSystemSounds()
    {
        StopOpeningSound();
        StopClosingSound();
    }

    private void PlayClip(
        AudioSource source,
        AudioClip clip,
        float volume,
        bool loop,
        string label
    )
    {
        if (source == null)
        {
            Debug.LogWarning("[SystemSound] AudioSource is null. label=" + label);
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[SystemSound] AudioClip is not assigned. label=" + label);
            return;
        }

        if (restartFromBeginning)
        {
            source.Stop();
            source.time = 0f;
        }

        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        source.Play();

        if (logState)
        {
            Debug.Log(
                "[SystemSound] Play "
                + label
                + " clip="
                + clip.name
                + " volume="
                + source.volume.ToString("0.###")
            );
        }
    }

    private void StopSource(AudioSource source, string label)
    {
        if (source == null)
        {
            return;
        }

        if (!source.isPlaying)
        {
            return;
        }

        source.Stop();

        if (logState)
        {
            Debug.Log("[SystemSound] Stop " + label);
        }
    }

    private void ResolveAudioSources()
    {
        if (openingAudioSource == null)
        {
            openingAudioSource = FindOrCreateChildAudioSource(OpeningSourceName);
        }

        if (closingAudioSource == null)
        {
            closingAudioSource = FindOrCreateChildAudioSource(ClosingSourceName);
        }
    }

    private AudioSource FindOrCreateChildAudioSource(string childName)
    {
        Transform existing = transform.Find(childName);

        if (existing != null)
        {
            AudioSource existingSource = existing.GetComponent<AudioSource>();

            if (existingSource != null)
            {
                return existingSource;
            }

            return existing.gameObject.AddComponent<AudioSource>();
        }

        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);

        return child.AddComponent<AudioSource>();
    }

    private void ConfigureAudioSources()
    {
        ConfigureAudioSource(openingAudioSource, openingVolume, loopOpeningSound);
        ConfigureAudioSource(closingAudioSource, closingVolume, loopClosingSound);
    }

    private void ConfigureAudioSource(AudioSource source, float volume, bool loop)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.volume = Mathf.Clamp01(volume);

        // UI / system sound として扱うため 2D 再生に固定する。
        source.spatialBlend = 0f;

        source.ignoreListenerPause = false;
        source.ignoreListenerVolume = false;
    }
}
