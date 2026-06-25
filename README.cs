using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class DemoDecorativeLoopVideoView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage targetRawImage;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool restartFromBeginningOnEnable = true;
    [SerializeField] private bool loop = true;

    [Header("Prepare")]
    [SerializeField] private bool prepareOnAwake = true;
    [SerializeField] private bool waitPrepareOnEnable = true;
    [SerializeField] private float maxPrepareWaitSec = 0.5f;

    [Header("First Frame")]
    [SerializeField] private bool hideRawImageBeforePlay = true;
    [SerializeField] private float showRawImageDelaySec = 0.05f;

    [Header("Disable")]
    [SerializeField] private bool pauseOnDisable = true;
    [SerializeField] private bool hideRawImageOnDisable = true;
    [SerializeField] private bool clearRenderTextureOnDisable = false;
    [SerializeField] private Color clearColor = Color.black;

    [Header("Debug")]
    [SerializeField] private bool logState = false;

    private Coroutine playRoutine;
    private bool prepareRequested;
    private RenderTexture cachedRenderTexture;

    private void Awake()
    {
        ResolveReferences();
        SetupVideoPlayer();
        CacheRenderTexture();

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

        if (hideRawImageBeforePlay && targetRawImage != null)
        {
            targetRawImage.enabled = false;
        }

        if (playOnEnable)
        {
            StartPlayRoutine();
        }
    }

    private void OnDisable()
    {
        StopPlayRoutine();

        if (videoPlayer != null && pauseOnDisable)
        {
            videoPlayer.Pause();
        }

        if (targetRawImage != null && hideRawImageOnDisable)
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

    public void RestartVideo()
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

        if (waitPrepareOnEnable && !videoPlayer.isPrepared)
        {
            PrepareVideo();

            float waitStart = Time.unscaledTime;

            while (videoPlayer != null && !videoPlayer.isPrepared)
            {
                if (Time.unscaledTime - waitStart >= maxPrepareWaitSec)
                {
                    Log("Prepare timeout. Start play anyway.");
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
            TrySetVideoToFirstFrame();
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

        if (targetRawImage != null)
        {
            targetRawImage.enabled = true;
        }

        Log("Play.");
        playRoutine = null;
    }

    private void TrySetVideoToFirstFrame()
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
            // 一部Video sourceではframe設定が失敗する場合があるため無視する。
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

    private void OnPrepareCompleted(VideoPlayer player)
    {
        prepareRequested = false;
        Log("Prepare completed.");
    }

    private void OnVideoErrorReceived(VideoPlayer player, string message)
    {
        prepareRequested = false;
        Debug.LogWarning("[DecorativeLoopVideo] Error: " + message + " object=" + gameObject.name);
    }

    private void ClearRenderTexture()
    {
        CacheRenderTexture();

        if (cachedRenderTexture == null)
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = cachedRenderTexture;

        GL.Clear(true, true, clearColor);

        RenderTexture.active = previous;
    }

    private void CacheRenderTexture()
    {
        cachedRenderTexture = null;

        if (videoPlayer != null && videoPlayer.targetTexture != null)
        {
            cachedRenderTexture = videoPlayer.targetTexture;
            return;
        }

        if (targetRawImage != null && targetRawImage.texture is RenderTexture renderTexture)
        {
            cachedRenderTexture = renderTexture;
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

        Debug.Log("[DecorativeLoopVideo] " + message + " object=" + gameObject.name);
    }
}
