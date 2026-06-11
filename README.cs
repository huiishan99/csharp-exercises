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
    [SerializeField] private bool handleMechaStatusEvents = true;
    [SerializeField] private bool logTouchEvents = true;
    [SerializeField] private bool logHvacResultEvents = true;

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

        Unsubscribe();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (eventDispatcher == null)
        {
            return;
        }

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

    private void Unsubscribe()
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
        Debug.Log("[GUI EVT Bridge] IG_ON received.");

        if (!handleIgEvents || displayController == null)
        {
            Debug.LogWarning("[GUI EVT Bridge] IG_ON ignored.");
            return;
        }

        displayController.IgnOn();
    }

    private void OnIgOffReceived(GuiEventMessage message)
    {
        Debug.Log("[GUI EVT Bridge] IG_OFF received.");

        if (!handleIgEvents || displayController == null)
        {
            Debug.LogWarning("[GUI EVT Bridge] IG_OFF ignored.");
            return;
        }

        displayController.IgnOff();
    }

    private void OnShifterChangedReceived(GuiEventMessage message)
    {
        Debug.Log("[GUI EVT Bridge] EVT_SHIFTER_CHANGED received.");

        if (!handleShifterEvents || displayController == null)
        {
            Debug.LogWarning("[GUI EVT Bridge] Shifter event ignored.");
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
        Debug.Log("[GUI EVT Bridge] EVT_HVAC_POPUP received.");

        if (!handleHvacEvents || displayController == null)
        {
            Debug.LogWarning("[GUI EVT Bridge] HVAC popup event ignored.");
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
        if (!handleMechaStatusEvents || displayController == null || message == null)
        {
            return;
        }

        Debug.Log("[GUI EVT Bridge] Mecha status received: " + message.MessageType);

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.HalfModeStatus))
        {
            displayController.OnMechaHalfModeStatus();
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.FullModeStatus))
        {
            displayController.OnMechaFullModeStatus();
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.CloseModeStatus))
        {
            displayController.OnMechaCloseModeStatus();
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.OtherModeStatus))
        {
            displayController.OnMechaOtherModeStatus();
        }
    }

    private void OnHvacResultReceived(GuiEventMessage message)
    {
        if (!logHvacResultEvents || message == null)
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
