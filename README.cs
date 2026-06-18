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

    [Header("Visibility")]
    [SerializeField] private bool hideRawImageUntilPrepared = true;
    [SerializeField] private bool clearRenderTextureOnDisable = true;
    [SerializeField] private Color clearColor = Color.black;

    [Header("Debug")]
    [SerializeField] private bool logState = false;

    private Coroutine playRoutine;
    private RenderTexture targetRenderTexture;
    private bool prepareRequested;

    private void Awake()
    {
        ResolveReferences();
        CacheRenderTexture();
        SetupVideoPlayer();

        if (prepareOnAwake)
        {
            PrepareVideo();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheRenderTexture();
        SetupVideoPlayer();

        if (hideRawImageUntilPrepared && targetRawImage != null)
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

        if (videoPlayer != null)
        {
            // Stop() 会让 VideoPlayer 变成未准备状态。
            // 为了下次切回来更快，使用 Pause()。
            videoPlayer.Pause();
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
        playRoutine = StartCoroutine(PlayWhenReady());
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

    private IEnumerator PlayWhenReady()
    {
        if (videoPlayer == null)
        {
            yield break;
        }

        if (!videoPlayer.isPrepared)
        {
            PrepareVideo();

            while (videoPlayer != null && !videoPlayer.isPrepared)
            {
                yield return null;
            }
        }

        if (videoPlayer == null)
        {
            yield break;
        }

        if (restartFromBeginningOnEnable)
        {
            videoPlayer.time = 0;
            videoPlayer.frame = 0;
        }

        videoPlayer.Play();

        // 等一帧，避免 RawImage 在视频纹理还没刷新时显示旧画面。
        yield return null;

        if (targetRawImage != null)
        {
            targetRawImage.enabled = true;
        }

        Log("Play.");
        playRoutine = null;
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
        Debug.LogWarning("[VideoPage] Error: " + message + " object=" + gameObject.name);
    }

    private void ClearRenderTexture()
    {
        CacheRenderTexture();

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
