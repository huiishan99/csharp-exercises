using UnityEngine;

public class KinemaEventBridge : MonoBehaviour
{
    [SerializeField] private GuiEventDispatcher eventDispatcher;
    [SerializeField] private KinemaMockDisplayController displayController;
    [SerializeField] private DemoSpeakerState speakerState;

    [Header("Handle Options")]
    [SerializeField] private bool handleIgEvents = true;
    [SerializeField] private bool handleShifterEvents = true;
    [SerializeField] private bool handleHvacEvents = true;
    [SerializeField] private bool handleAudioEvents = true;
    [SerializeField] private bool logTouchEvents = true;
    [SerializeField] private bool logMechaStatusEvents = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (eventDispatcher == null)
        {
            return;
        }

        eventDispatcher.IgOnReceived -= OnIgOnReceived;
        eventDispatcher.IgOffReceived -= OnIgOffReceived;
        eventDispatcher.ShifterChangedReceived -= OnShifterChangedReceived;
        eventDispatcher.HvacPopupReceived -= OnHvacPopupReceived;
        eventDispatcher.MediaVolumeUpReceived -= OnMediaVolumeUpReceived;
        eventDispatcher.MediaVolumeDownReceived -= OnMediaVolumeDownReceived;
        eventDispatcher.TouchReceived -= OnTouchReceived;
        eventDispatcher.MechaStatusReceived -= OnMechaStatusReceived;
        eventDispatcher.HvacResultReceived -= OnHvacResultReceived;
        eventDispatcher.UnknownEventReceived -= OnUnknownEventReceived;

        eventDispatcher.IgOnReceived += OnIgOnReceived;
        eventDispatcher.IgOffReceived += OnIgOffReceived;
        eventDispatcher.ShifterChangedReceived += OnShifterChangedReceived;
        eventDispatcher.HvacPopupReceived += OnHvacPopupReceived;
        eventDispatcher.MediaVolumeUpReceived += OnMediaVolumeUpReceived;
        eventDispatcher.MediaVolumeDownReceived += OnMediaVolumeDownReceived;
        eventDispatcher.TouchReceived += OnTouchReceived;
        eventDispatcher.MechaStatusReceived += OnMechaStatusReceived;
        eventDispatcher.HvacResultReceived += OnHvacResultReceived;
        eventDispatcher.UnknownEventReceived += OnUnknownEventReceived;
    }

    private void OnDisable()
    {
        if (eventDispatcher == null)
        {
            return;
        }

        eventDispatcher.IgOnReceived -= OnIgOnReceived;
        eventDispatcher.IgOffReceived -= OnIgOffReceived;
        eventDispatcher.ShifterChangedReceived -= OnShifterChangedReceived;
        eventDispatcher.HvacPopupReceived -= OnHvacPopupReceived;
        eventDispatcher.MediaVolumeUpReceived -= OnMediaVolumeUpReceived;
        eventDispatcher.MediaVolumeDownReceived -= OnMediaVolumeDownReceived;
        eventDispatcher.TouchReceived -= OnTouchReceived;
        eventDispatcher.MechaStatusReceived -= OnMechaStatusReceived;
        eventDispatcher.HvacResultReceived -= OnHvacResultReceived;
        eventDispatcher.UnknownEventReceived -= OnUnknownEventReceived;
    }

    private void OnIgOnReceived(GuiEventMessage message)
    {
        if (!handleIgEvents || displayController == null)
        {
            return;
        }

        displayController.IgnOn();
    }

    private void OnIgOffReceived(GuiEventMessage message)
    {
        if (!handleIgEvents || displayController == null)
        {
            return;
        }

        displayController.IgnOff();
    }

    private void OnShifterChangedReceived(GuiEventMessage message)
    {
        if (!handleShifterEvents || displayController == null)
        {
            return;
        }

        if (message == null || message.ShifterPayload == null)
        {
            Debug.LogWarning("[GUI EVT Bridge] Shifter payload is null.");
            return;
        }

        string gear = NormalizeText(message.ShifterPayload.GetGearText());

        if (gear == "p" || gear == "parking" || gear == "park")
        {
            displayController.ShiftP();
            return;
        }

        if (gear == "d" || gear == "drive" || gear == "normaldrive")
        {
            displayController.ShiftD();
            return;
        }

        if (gear == "r" || gear == "reverse" || gear == "rear")
        {
            displayController.ShiftR();
            return;
        }

        Debug.LogWarning("[GUI EVT Bridge] Unknown gear: " + message.ShifterPayload.GetGearText());
    }

    private void OnHvacPopupReceived(GuiEventMessage message)
    {
        if (!handleHvacEvents || displayController == null)
        {
            return;
        }

        displayController.ToggleAutoPopup();
    }

    private void OnMediaVolumeUpReceived(GuiEventMessage message)
    {
        if (!handleAudioEvents || speakerState == null)
        {
            return;
        }

        speakerState.IncreaseVolume();
    }

    private void OnMediaVolumeDownReceived(GuiEventMessage message)
    {
        if (!handleAudioEvents || speakerState == null)
        {
            return;
        }

        speakerState.DecreaseVolume();
    }

    private void OnTouchReceived(GuiEventMessage message)
    {
        if (!logTouchEvents)
        {
            return;
        }

        if (message == null || message.TouchPayload == null)
        {
            Debug.LogWarning("[GUI EVT Touch] payload is null.");
            return;
        }

        GuiEventTouchPayload payload = message.TouchPayload;

        Debug.Log(
            "[GUI EVT Touch] source="
            + payload.source
            + " event="
            + payload.GetTouchEventText()
            + " x="
            + payload.x
            + " y="
            + payload.y
        );
    }

    private void OnMechaStatusReceived(GuiEventMessage message)
    {
        if (!logMechaStatusEvents || message == null)
        {
            return;
        }

        Debug.Log("[GUI EVT MechaStatus] " + message.MessageType);
    }

    private void OnHvacResultReceived(GuiEventMessage message)
    {
        if (message == null)
        {
            return;
        }

        string payloadText = "";

        if (message.HvacPayload != null)
        {
            payloadText = " disp_mode="
                + message.HvacPayload.disp_mode
                + " result="
                + message.HvacPayload.result
                + " value="
                + message.HvacPayload.value;
        }

        Debug.Log("[GUI EVT HVAC Result] " + message.MessageType + payloadText);
    }

    private void OnUnknownEventReceived(GuiEventMessage message)
    {
        if (message == null)
        {
            return;
        }

        Debug.LogWarning("[GUI EVT Bridge] Unknown event: " + message.MessageType);
    }

    private void ResolveReferences()
    {
        if (eventDispatcher == null)
        {
            eventDispatcher = FindFirstObjectByType<GuiEventDispatcher>();
        }

        if (displayController == null)
        {
            displayController = FindFirstObjectByType<KinemaMockDisplayController>();
        }

        if (speakerState == null)
        {
            speakerState = FindFirstObjectByType<DemoSpeakerState>();
        }
    }

    private string NormalizeText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "");
    }
}
