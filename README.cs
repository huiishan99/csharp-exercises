using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class GuiMediaVolumeController : MonoBehaviour
{
    [Header("Event")]
    [SerializeField] private GuiEventDispatcher eventDispatcher;

    [Header("Config")]
    [SerializeField] private bool useJsonConfig = true;

    // 外部絶対Pathも指定可能。
    // 例: %USERPROFILE%\Desktop\backend_ver19\config\media_volume_config.json
    [SerializeField] private string configPath =
        "%USERPROFILE%\\Desktop\\backend_ver19\\config\\media_volume_config.json";

    [SerializeField] private bool loadConfigOnEnable = true;

    [Header("Targets")]
    [SerializeField] private VideoPlayer[] targetVideoPlayers;
    [SerializeField] private AudioSource[] targetAudioSources;

    [Header("Auto Find VideoPlayers")]
    [SerializeField] private bool autoFindVideoPlayersOnAwake = true;
    [SerializeField] private bool includeInactiveVideoPlayers = true;

    [Header("Auto Find AudioSources")]
    [Tooltip("Opening/Closing/HVACなどのAudioSourceを拾いやすいため、通常はfalse。")]
    [SerializeField] private bool autoFindAudioSourcesOnAwake = false;
    [SerializeField] private bool includeInactiveAudioSources = true;

    [Header("VideoPlayer Audio Track")]
    [SerializeField] private ushort directAudioTrackIndex = 0;

    [Header("Behavior")]
    [SerializeField] private bool applyDefaultVolumeOnStart = true;
    [SerializeField] private bool applyVolumeOnEnable = true;
    [SerializeField] private bool reloadConfigOnManualReload = true;

    [Header("Debug")]
    [SerializeField] private bool logVolume = true;

    private MediaVolumeConfig config;
    private float currentVolume = 0.4f;

    public float CurrentVolume
    {
        get { return currentVolume; }
    }

    private void Awake()
    {
        ResolveReferences();

        if (autoFindVideoPlayersOnAwake)
        {
            RefreshVideoPlayerTargets();
        }

        if (autoFindAudioSourcesOnAwake)
        {
            RefreshAudioSourceTargets();
        }

        LoadConfig();

        if (applyDefaultVolumeOnStart)
        {
            SetVolume(config.@default, "StartDefault");
        }
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (loadConfigOnEnable)
        {
            LoadConfig();
        }

        if (applyVolumeOnEnable)
        {
            SetVolume(currentVolume, "OnEnable");
        }

        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    [ContextMenu("Reload Media Volume Config")]
    public void ReloadConfig()
    {
        if (!reloadConfigOnManualReload)
        {
            return;
        }

        LoadConfig();
        SetVolume(currentVolume, "ReloadConfigClamp");
    }

    [ContextMenu("Refresh VideoPlayer Targets")]
    public void RefreshVideoPlayerTargets()
    {
        FindObjectsInactive inactiveMode = includeInactiveVideoPlayers
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;

        targetVideoPlayers = FindObjectsByType<VideoPlayer>(
            inactiveMode,
            FindObjectsSortMode.None
        );
    }

    [ContextMenu("Refresh AudioSource Targets")]
    public void RefreshAudioSourceTargets()
    {
        FindObjectsInactive inactiveMode = includeInactiveAudioSources
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;

        targetAudioSources = FindObjectsByType<AudioSource>(
            inactiveMode,
            FindObjectsSortMode.None
        );
    }

    public void IncreaseVolume()
    {
        EnsureConfig();
        SetVolume(currentVolume + config.step, "EVT_MEDIA_VOLUME_UP");
    }

    public void DecreaseVolume()
    {
        EnsureConfig();
        SetVolume(currentVolume - config.step, "EVT_MEDIA_VOLUME_DOWN");
    }

    public void SetVolume(float value, string reason)
    {
        EnsureConfig();

        float nextVolume = Mathf.Clamp(value, config.min, config.max);
        currentVolume = nextVolume;

        ApplyVolumeToTargets();

        if (logVolume)
        {
            Debug.Log(
                "[MediaVolume] volume="
                + currentVolume.ToString("0.###")
                + " reason="
                + reason
                + " range="
                + config.min.ToString("0.###")
                + "-"
                + config.max.ToString("0.###")
                + " step="
                + config.step.ToString("0.###")
            );
        }
    }

    private void ApplyVolumeToTargets()
    {
        ApplyVolumeToVideoPlayers();
        ApplyVolumeToAudioSources();
    }

    private void ApplyVolumeToVideoPlayers()
    {
        if (targetVideoPlayers == null)
        {
            return;
        }

        for (int i = 0; i < targetVideoPlayers.Length; i++)
        {
            VideoPlayer videoPlayer = targetVideoPlayers[i];

            if (videoPlayer == null)
            {
                continue;
            }

            ApplyVolumeToVideoPlayer(videoPlayer);
        }
    }

    private void ApplyVolumeToVideoPlayer(VideoPlayer videoPlayer)
    {
        if (videoPlayer == null)
        {
            return;
        }

        try
        {
            if (videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
            {
                videoPlayer.SetDirectAudioVolume(directAudioTrackIndex, currentVolume);
                return;
            }
        }
        catch
        {
            // Track未設定などの場合はAudioSource側にfallbackする。
        }

        try
        {
            AudioSource source = videoPlayer.GetTargetAudioSource(directAudioTrackIndex);

            if (source != null)
            {
                source.volume = currentVolume;
            }
        }
        catch
        {
            // AudioSource未設定の場合は何もしない。
        }
    }

    private void ApplyVolumeToAudioSources()
    {
        if (targetAudioSources == null)
        {
            return;
        }

        for (int i = 0; i < targetAudioSources.Length; i++)
        {
            AudioSource source = targetAudioSources[i];

            if (source == null)
            {
                continue;
            }

            source.volume = currentVolume;
        }
    }

    private void SubscribeEvents()
    {
        if (eventDispatcher == null)
        {
            return;
        }

        eventDispatcher.MediaVolumeUpReceived -= OnMediaVolumeUpReceived;
        eventDispatcher.MediaVolumeDownReceived -= OnMediaVolumeDownReceived;

        eventDispatcher.MediaVolumeUpReceived += OnMediaVolumeUpReceived;
        eventDispatcher.MediaVolumeDownReceived += OnMediaVolumeDownReceived;
    }

    private void UnsubscribeEvents()
    {
        if (eventDispatcher == null)
        {
            return;
        }

        eventDispatcher.MediaVolumeUpReceived -= OnMediaVolumeUpReceived;
        eventDispatcher.MediaVolumeDownReceived -= OnMediaVolumeDownReceived;
    }

    private void OnMediaVolumeUpReceived(GuiEventMessage message)
    {
        IncreaseVolume();
    }

    private void OnMediaVolumeDownReceived(GuiEventMessage message)
    {
        DecreaseVolume();
    }

    private void LoadConfig()
    {
        if (useJsonConfig)
        {
            config = MediaVolumeConfigLoader.Load(configPath);
        }
        else
        {
            config = MediaVolumeConfig.CreateDefault();
        }

        currentVolume = Mathf.Clamp(currentVolume, config.min, config.max);
    }

    private void EnsureConfig()
    {
        if (config != null)
        {
            return;
        }

        LoadConfig();
    }

    private void ResolveReferences()
    {
        if (eventDispatcher == null)
        {
            eventDispatcher = FindFirstObjectByType<GuiEventDispatcher>();
        }
    }
}
