using System;
using UnityEngine;

public class DemoMusicState : MonoBehaviour
{
    [SerializeField] private DemoMusicTrack[] tracks = new DemoMusicTrack[9];
    [SerializeField] private int firstTrackIndex = 0;

    private int selectedTrackIndex;

    public event Action<int, DemoMusicTrack> TrackChanged;

    public int SelectedTrackIndex
    {
        get { return selectedTrackIndex; }
    }

    public int TrackCount
    {
        get { return tracks == null ? 0 : tracks.Length; }
    }

    private void Awake()
    {
        selectedTrackIndex = NormalizeIndex(firstTrackIndex);
    }

    private void Start()
    {
        NotifyTrackChanged();
    }

    public DemoMusicTrack GetSelectedTrack()
    {
        return GetTrack(selectedTrackIndex);
    }

    public DemoMusicTrack GetTrackByOffset(int offset)
    {
        return GetTrack(selectedTrackIndex + offset);
    }

    public void SelectRelative(int offset)
    {
        SelectTrack(selectedTrackIndex + offset);
    }

    public void SelectNext()
    {
        SelectRelative(1);
    }

    public void SelectPrevious()
    {
        SelectRelative(-1);
    }

    public void ResetTrack()
    {
        SelectTrack(firstTrackIndex);
    }

    public void SelectTrack(int trackIndex)
    {
        if (TrackCount == 0)
        {
            return;
        }

        int normalizedIndex = NormalizeIndex(trackIndex);

        if (selectedTrackIndex == normalizedIndex)
        {
            return;
        }

        selectedTrackIndex = normalizedIndex;
        NotifyTrackChanged();
    }

    private DemoMusicTrack GetTrack(int index)
    {
        if (TrackCount == 0)
        {
            return null;
        }

        return tracks[NormalizeIndex(index)];
    }

    private int NormalizeIndex(int index)
    {
        if (TrackCount == 0)
        {
            return 0;
        }

        int result = index % TrackCount;

        if (result < 0)
        {
            result += TrackCount;
        }

        return result;
    }

    private void NotifyTrackChanged()
    {
        TrackChanged?.Invoke(selectedTrackIndex, GetSelectedTrack());
    }
}
