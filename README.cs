using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DemoMusicPlayer : MonoBehaviour
{
    [SerializeField] private DemoMusicState musicState;
    [SerializeField] private DemoPageSwitcher pageSwitcher;

    [SerializeField] private DemoPageId[] musicPages =
    {
        DemoPageId.NormalDrive,
        DemoPageId.RearView
    };

    [SerializeField] private bool playOnMusicPageEntered = true;
    [SerializeField] private bool resetOnMusicPageEntered = true;
    [SerializeField] private bool stopOnNonMusicPage = true;
    [SerializeField] private bool loopCurrentClip = true;

    private AudioSource audioSource;
    private DemoPageId currentPage;

    public float NormalizedProgress
    {
        get
        {
            if (audioSource == null || audioSource.clip == null || audioSource.clip.length <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(audioSource.time / audioSource.clip.length);
        }
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loopCurrentClip;

        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (musicState != null)
        {
            musicState.TrackChanged -= OnTrackChanged;
            musicState.TrackChanged += OnTrackChanged;
        }

        if (pageSwitcher != null)
        {
            pageSwitcher.PageChanged -= OnPageChanged;
            pageSwitcher.PageChanged += OnPageChanged;
        }
    }

    private void OnDisable()
    {
        if (musicState != null)
        {
            musicState.TrackChanged -= OnTrackChanged;
        }

        if (pageSwitcher != null)
        {
            pageSwitcher.PageChanged -= OnPageChanged;
        }
    }

    public void PlaySelectedFromStart()
    {
        if (musicState == null)
        {
            return;
        }

        DemoMusicTrack track = musicState.GetSelectedTrack();

        if (track == null || track.audioClip == null)
        {
            Stop();
            return;
        }

        audioSource.clip = track.audioClip;
        audioSource.loop = loopCurrentClip;
        audioSource.time = 0f;
        audioSource.Play();
    }

    public void Stop()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        audioSource.time = 0f;
    }

    private void OnTrackChanged(int index, DemoMusicTrack track)
    {
        if (IsMusicPage(currentPage))
        {
            PlaySelectedFromStart();
        }
    }

    private void OnPageChanged(DemoPageId pageId)
    {
        currentPage = pageId;

        if (IsMusicPage(pageId))
        {
            if (playOnMusicPageEntered)
            {
                if (resetOnMusicPageEntered)
                {
                    PlaySelectedFromStart();
                }
                else if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }

            return;
        }

        if (stopOnNonMusicPage)
        {
            Stop();
        }
    }

    private bool IsMusicPage(DemoPageId pageId)
    {
        for (int i = 0; i < musicPages.Length; i++)
        {
            if (musicPages[i] == pageId)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (musicState == null)
        {
            musicState = FindFirstObjectByType<DemoMusicState>();
        }

        if (pageSwitcher == null)
        {
            pageSwitcher = FindFirstObjectByType<DemoPageSwitcher>();
        }
    }
}
