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

    [Header("Media Volume Config")]
    [SerializeField] private bool useMediaVolumeConfig = true;

    // GuiMediaVolumeController と同じ外部Jsonを参照する。
    // 例: %USERPROFILE%\Desktop\backend_ver19\config\media_volume_config.json
    [SerializeField] private string mediaVolumeConfigPath =
        "%USERPROFILE%\\Desktop\\backend_ver19\\config\\media_volume_config.json";

    [SerializeField] private bool loadMediaVolumeConfigOnAwake = true;
    [SerializeField] private bool reloadMediaVolumeConfigOnEnable = true;
    [SerializeField] private bool reloadMediaVolumeConfigBeforePlay = true;

    // trueの場合、Opening/Closing Soundの実効音量をmedia volume configのmax以下に制限する。
    [SerializeField] private bool capSystemSoundVolumeByMediaMax = true;

    // Jsonが使われない場合の上限値。
    [SerializeField, Range(0f, 1f)] private float fallbackMaxVolume = 0.8f;

    [Header("Behavior")]
    [SerializeField] private bool stopOtherSystemSoundOnPlay = true;
    [SerializeField] private bool restartFromBeginning = true;

    [Header("Debug")]
    [SerializeField] private bool logState = true;

    private const string OpeningSourceName = "OpeningSoundSource";
    private const string ClosingSourceName = "ClosingSoundSource";

    private float systemSoundMaxVolume = 1f;

    private void Awake()
    {
        ResolveAudioSources();
        ConfigureAudioSources();

        if (loadMediaVolumeConfigOnAwake)
        {
            LoadSystemSoundMaxVolume();
        }
        else
        {
            ApplyFallbackMaxVolume();
        }
    }

    private void OnEnable()
    {
        ResolveAudioSources();
        ConfigureAudioSources();

        if (reloadMediaVolumeConfigOnEnable)
        {
            LoadSystemSoundMaxVolume();
        }
    }

    private void OnValidate()
    {
        openingVolume = Mathf.Clamp01(openingVolume);
        closingVolume = Mathf.Clamp01(closingVolume);
        fallbackMaxVolume = Mathf.Clamp01(fallbackMaxVolume);
    }

    [ContextMenu("Reload Media Volume Max")]
    public void ReloadMediaVolumeMax()
    {
        LoadSystemSoundMaxVolume();
    }

    public void PlayOpeningSound()
    {
        ResolveAudioSources();
        ConfigureAudioSources();

        if (reloadMediaVolumeConfigBeforePlay)
        {
            LoadSystemSoundMaxVolume();
        }

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

        if (reloadMediaVolumeConfigBeforePlay)
        {
            LoadSystemSoundMaxVolume();
        }

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

    private void LoadSystemSoundMaxVolume()
    {
        if (!useMediaVolumeConfig)
        {
            ApplyFallbackMaxVolume();
            return;
        }

        MediaVolumeConfig config = MediaVolumeConfigLoader.Load(mediaVolumeConfigPath);

        if (config == null)
        {
            ApplyFallbackMaxVolume();
            return;
        }

        config.Normalize();
        systemSoundMaxVolume = Mathf.Clamp01(config.max);

        if (logState)
        {
            Debug.Log(
                "[SystemSound] Media volume max loaded. path="
                + mediaVolumeConfigPath
                + " max="
                + systemSoundMaxVolume.ToString("0.###")
            );
        }
    }

    private void ApplyFallbackMaxVolume()
    {
        systemSoundMaxVolume = Mathf.Clamp01(fallbackMaxVolume);

        if (logState)
        {
            Debug.Log(
                "[SystemSound] Use fallback max volume="
                + systemSoundMaxVolume.ToString("0.###")
            );
        }
    }

    private void PlayClip(
        AudioSource source,
        AudioClip clip,
        float baseVolume,
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

        float effectiveVolume = GetEffectiveVolume(baseVolume);

        source.clip = clip;
        source.volume = effectiveVolume;
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
                + " baseVolume="
                + Mathf.Clamp01(baseVolume).ToString("0.###")
                + " max="
                + systemSoundMaxVolume.ToString("0.###")
                + " effectiveVolume="
                + effectiveVolume.ToString("0.###")
            );
        }
    }

    private float GetEffectiveVolume(float baseVolume)
    {
        float safeBaseVolume = Mathf.Clamp01(baseVolume);

        if (!capSystemSoundVolumeByMediaMax)
        {
            return safeBaseVolume;
        }

        return Mathf.Min(safeBaseVolume, systemSoundMaxVolume);
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

    private void ConfigureAudioSource(AudioSource source, float baseVolume, bool loop)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.volume = GetEffectiveVolume(baseVolume);

        // UI / system sound として扱うため 2D 再生に固定する。
        source.spatialBlend = 0f;

        source.ignoreListenerPause = false;
        source.ignoreListenerVolume = false;
    }
}
