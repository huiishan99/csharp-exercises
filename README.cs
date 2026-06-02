using UnityEngine;

public class DemoSpeakerAudioApplier : MonoBehaviour
{
    [SerializeField] private DemoSpeakerState speakerState;
    [SerializeField] private AudioSource[] targetAudioSources;

    [SerializeField] private bool force2DAudio = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (speakerState != null)
        {
            speakerState.SpeakerStateChanged -= ApplySpeakerState;
            speakerState.SpeakerStateChanged += ApplySpeakerState;
        }

        ApplySpeakerState();
    }

    private void OnDisable()
    {
        if (speakerState != null)
        {
            speakerState.SpeakerStateChanged -= ApplySpeakerState;
        }
    }

    public void ApplySpeakerState()
    {
        if (speakerState == null || targetAudioSources == null)
        {
            return;
        }

        for (int i = 0; i < targetAudioSources.Length; i++)
        {
            AudioSource audioSource = targetAudioSources[i];

            if (audioSource == null)
            {
                continue;
            }

            ApplyToAudioSource(audioSource);
        }
    }

    private void ApplyToAudioSource(AudioSource audioSource)
    {
        if (force2DAudio)
        {
            audioSource.spatialBlend = 0f;
        }

        bool leftOn = speakerState.LeftSpeakerOn;
        bool rightOn = speakerState.RightSpeakerOn;

        if (!leftOn && !rightOn)
        {
            audioSource.volume = 0f;
            audioSource.panStereo = 0f;
            return;
        }

        audioSource.volume = speakerState.Volume;

        if (leftOn && rightOn)
        {
            audioSource.panStereo = 0f;
            return;
        }

        if (leftOn)
        {
            audioSource.panStereo = -1f;
            return;
        }

        audioSource.panStereo = 1f;
    }

    private void ResolveReferences()
    {
        if (speakerState == null)
        {
            speakerState = FindFirstObjectByType<DemoSpeakerState>();
        }
    }
}
