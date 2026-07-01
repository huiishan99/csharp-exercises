using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class DemoVideoPageView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage targetRawImage;

    [Header("Playback")]
    [SerializeField] private bool prepareOnAwake = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool restartFromBeginningOnEnable = true;
    [SerializeField] private bool loop = true;

    [Header("Reset Policy")]
    [SerializeField] private bool stopOnDisable = true;
    [SerializeField] private bool clearRenderTextureOnDisable = true;
    [SerializeField] private Color clearColor = Color.black;

    [Header("Visibility")]
    [SerializeField] private bool hideRawImageBeforePlay = true;
    [SerializeField] private bool showRawImageAfterPlay = true;
    [SerializeField] private float showRawImageDelaySec = 0.05f;

    [Header("Prepare Timeout")]
    [SerializeField] private float prepareTimeoutSec = 1.0f;
    [SerializeField] private bool playEvenIfPrepareTimeout = true;

    [Header("Recovery")]
    [SerializeField] private bool enableRuntimeRecovery = true;
    [SerializeField] private float recoveryCheckIntervalSec = 0.5f;
    [SerializeField] private float notPlayingRecoverDelaySec = 1.0f;

    [Header("Transition")]
    [SerializeField] private bool readyForTransitionAfterPlay = true;

    [Header("Debug")]
    [SerializeField] private bool logState = false;

    public bool IsReadyForTransition { get; private set; }

    private Coroutine playRoutine;
    private RenderTexture targetRenderTexture;

    private bool prepareRequested;
    private float nextRecoveryCheckTime;
    private float notPlayingStartTime = -1f;

    private void Awake()
    {
        ResolveReferences();
        SetupVideoPlayer();
        CacheRenderTexture();
        EnsureRenderTextureCreated();

        IsReadyForTransition = false;

        if (prepareOnAwake)
        {
            PrepareVideo();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        SetupVideoPlayer();
        CacheRenderTexture();
        EnsureRenderTextureCreated();
        AssignTextureReferences();

        IsReadyForTransition = false;
        notPlayingStartTime = -1f;

        if (hideRawImageBeforePlay && targetRawImage != null)
        {
            targetRawImage.enabled = false;
        }

        if (playOnEnable)
        {
            StartPlayRoutine();
        }
    }

    private void Update()
    {
        if (!enableRuntimeRecovery)
        {
            return;
        }

        if (!isActiveAndEnabled)
        {
            return;
        }

        if (videoPlayer == null)
        {
            return;
        }

        if (Time.unscaledTime < nextRecoveryCheckTime)
        {
            return;
        }

        nextRecoveryCheckTime = Time.unscaledTime + recoveryCheckIntervalSec;

        if (!playOnEnable)
        {
            return;
        }

        if (playRoutine != null)
        {
            return;
        }

        if (videoPlayer.isPlaying)
        {
            notPlayingStartTime = -1f;
            return;
        }

        if (notPlayingStartTime < 0f)
        {
            notPlayingStartTime = Time.unscaledTime;
            return;
        }

        if (Time.unscaledTime - notPlayingStartTime < notPlayingRecoverDelaySec)
        {
            return;
        }

        Log("Recovery restart because VideoPlayer is not playing.");
        notPlayingStartTime = -1f;
        StartPlayRoutine();
    }

    private void OnDisable()
    {
        StopPlayRoutine();

        IsReadyForTransition = false;
        notPlayingStartTime = -1f;
        prepareRequested = false;

        if (videoPlayer != null)
        {
            if (stopOnDisable)
            {
                videoPlayer.Stop();
            }
            else
            {
                videoPlayer.Pause();
            }
        }

        if (targetRawImage != null)
        {
            targetRawImage.enabled = false;
        }

        if (clearRenderTextureOnDisable)
        {
            ClearRenderTexture();
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
            videoPlayer.errorReceived -= OnVideoErrorReceived;
        }
    }

    public void PrepareVideo()
    {
        if (videoPlayer == null)
        {
            return;
        }

        if (videoPlayer.isPrepared)
        {
            return;
        }

        if (prepareRequested)
        {
            return;
        }

        prepareRequested = true;
        videoPlayer.Prepare();

        Log("Prepare requested.");
    }

    public void ReplayFromBeginning()
    {
        StartPlayRoutine();
    }

    private void StartPlayRoutine()
    {
        StopPlayRoutine();
        playRoutine = StartCoroutine(PlayRoutine());
    }

    private void StopPlayRoutine()
    {
        if (playRoutine == null)
        {
            return;
        }

        StopCoroutine(playRoutine);
        playRoutine = null;
    }

    private IEnumerator PlayRoutine()
    {
        if (videoPlayer == null)
        {
            yield break;
        }

        ResolveReferences();
        SetupVideoPlayer();
        CacheRenderTexture();
        EnsureRenderTextureCreated();
        AssignTextureReferences();

        if (!videoPlayer.isPrepared)
        {
            prepareRequested = false;
            PrepareVideo();

            float waitStart = Time.unscaledTime;

            while (videoPlayer != null && !videoPlayer.isPrepared)
            {
                if (Time.unscaledTime - waitStart >= prepareTimeoutSec)
                {
                    Log("Prepare timeout.");

                    if (!playEvenIfPrepareTimeout)
                    {
                        playRoutine = null;
                        yield break;
                    }

                    break;
                }

                yield return null;
            }
        }

        if (videoPlayer == null)
        {
            yield break;
        }

        if (restartFromBeginningOnEnable)
        {
            TrySeekFirstFrame();
        }

        videoPlayer.Play();

        if (showRawImageDelaySec > 0f)
        {
            float elapsed = 0f;

            while (elapsed < showRawImageDelaySec)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            yield return null;
        }

        if (targetRawImage != null && showRawImageAfterPlay)
        {
            targetRawImage.enabled = true;
        }

        IsReadyForTransition = readyForTransitionAfterPlay;

        Log("Play.");
        playRoutine = null;
    }

    private void TrySeekFirstFrame()
    {
        if (videoPlayer == null)
        {
            return;
        }

        try
        {
            videoPlayer.time = 0;
            videoPlayer.frame = 0;
        }
        catch
        {
            // Some video sources do not allow frame seek before play.
        }
    }

    private void SetupVideoPlayer()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = loop;

        videoPlayer.prepareCompleted -= OnPrepareCompleted;
        videoPlayer.prepareCompleted += OnPrepareCompleted;

        videoPlayer.errorReceived -= OnVideoErrorReceived;
        videoPlayer.errorReceived += OnVideoErrorReceived;
    }

    private void AssignTextureReferences()
    {
        if (videoPlayer != null && targetRenderTexture != null)
        {
            videoPlayer.targetTexture = targetRenderTexture;
        }

        if (targetRawImage != null && targetRenderTexture != null)
        {
            targetRawImage.texture = targetRenderTexture;
        }
    }

    private void OnPrepareCompleted(VideoPlayer player)
    {
        prepareRequested = false;
        Log("Prepare completed.");
    }

    private void OnVideoErrorReceived(VideoPlayer player, string message)
    {
        prepareRequested = false;
        Debug.LogWarning("[VideoPage] Error: " + message + " object=" + gameObject.name);
    }

    private void ClearRenderTexture()
    {
        CacheRenderTexture();
        EnsureRenderTextureCreated();

        if (targetRenderTexture == null)
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = targetRenderTexture;

        GL.Clear(true, true, clearColor);

        RenderTexture.active = previous;
    }

    private void CacheRenderTexture()
    {
        targetRenderTexture = null;

        if (videoPlayer != null && videoPlayer.targetTexture != null)
        {
            targetRenderTexture = videoPlayer.targetTexture;
            return;
        }

        if (targetRawImage != null && targetRawImage.texture is RenderTexture renderTexture)
        {
            targetRenderTexture = renderTexture;
        }
    }

    private void EnsureRenderTextureCreated()
    {
        if (targetRenderTexture == null)
        {
            return;
        }

        if (!targetRenderTexture.IsCreated())
        {
            targetRenderTexture.Create();
        }
    }

    private void ResolveReferences()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        }

        if (targetRawImage == null)
        {
            targetRawImage = GetComponentInChildren<RawImage>(true);
        }
    }

    private void Log(string message)
    {
        if (!logState)
        {
            return;
        }

        Debug.Log("[VideoPage] " + message + " object=" + gameObject.name);
    }
}
