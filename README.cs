using UnityEngine;

public class DemoSpeakerCommandEmitter : MonoBehaviour
{
    [SerializeField] private DemoSpeakerState speakerState;
    [SerializeField] private KinemaCommandBridge commandBridge;
    [SerializeField] private bool ignoreInitialNotification = true;

    private bool hasReceivedNotification;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (speakerState != null)
        {
            speakerState.SpeakerStateChanged -= OnSpeakerStateChanged;
            speakerState.SpeakerStateChanged += OnSpeakerStateChanged;
        }
    }

    private void OnDisable()
    {
        if (speakerState != null)
        {
            speakerState.SpeakerStateChanged -= OnSpeakerStateChanged;
        }
    }

    private void OnSpeakerStateChanged()
    {
        if (ignoreInitialNotification && !hasReceivedNotification)
        {
            hasReceivedNotification = true;
            return;
        }

        hasReceivedNotification = true;
        SendCurrentState();
    }

    public void SendCurrentState()
    {
        ResolveReferences();

        if (speakerState == null || commandBridge == null)
        {
            return;
        }

        commandBridge.SendAudioOutputStateCommand(
            speakerState.LeftSpeakerOn,
            speakerState.RightSpeakerOn,
            speakerState.Volume
        );
    }

    private void ResolveReferences()
    {
        if (speakerState == null)
        {
            speakerState = GetComponent<DemoSpeakerState>();
        }

        if (speakerState == null)
        {
            speakerState = FindFirstObjectByType<DemoSpeakerState>();
        }

        if (commandBridge == null)
        {
            commandBridge = GetComponent<KinemaCommandBridge>();
        }

        if (commandBridge == null)
        {
            commandBridge = FindFirstObjectByType<KinemaCommandBridge>();
        }
    }
}
