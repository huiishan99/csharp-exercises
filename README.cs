using System;
using UnityEngine;

public class GuiEventDispatcher : MonoBehaviour
{
    [SerializeField] private bool logRawJson = true;
    [SerializeField] private bool logParsedEvent = true;

    public event Action<GuiEventMessage> AnyEventReceived;

    public event Action<GuiEventMessage> IgOnReceived;
    public event Action<GuiEventMessage> IgOffReceived;

    public event Action<GuiEventMessage> ShifterChangedReceived;

    public event Action<GuiEventMessage> HvacPopupReceived;
    public event Action<GuiEventMessage> HvacResultReceived;

    public event Action<GuiEventMessage> MediaVolumeUpReceived;
    public event Action<GuiEventMessage> MediaVolumeDownReceived;

    public event Action<GuiEventMessage> SoundVolumeUpSignalReceived;
    public event Action<GuiEventMessage> SoundVolumeDownSignalReceived;

    public event Action<GuiEventMessage> TouchReceived;
    public event Action<GuiEventMessage> MechaStatusReceived;

    public event Action<GuiEventMessage> LedSubSignalReceived;
    public event Action<GuiEventMessage> UnknownEventReceived;

    public void ReceiveRawJson(string rawJson)
    {
        if (logRawJson)
        {
            Debug.Log("[GUI EVT Raw] " + rawJson);
        }

        if (!GuiEventJsonParser.TryParse(
                rawJson,
                out GuiEventMessage message,
                out string errorMessage
            ))
        {
            Debug.LogWarning("[GUI EVT] Parse failed: " + errorMessage + " | Raw: " + rawJson);
            return;
        }

        Dispatch(message);
    }

    private void Dispatch(GuiEventMessage message)
    {
        if (message == null)
        {
            return;
        }

        if (logParsedEvent)
        {
            Debug.Log("[GUI EVT] " + message.MessageType);
        }

        AnyEventReceived?.Invoke(message);

        if (GuiEventType.IsIgOn(message.MessageType))
        {
            IgOnReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.IsIgOff(message.MessageType))
        {
            IgOffReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.ShifterChanged))
        {
            ShifterChangedReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.HvacPopup))
        {
            HvacPopupReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.HvacDisplayModeResult))
        {
            HvacResultReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.IsMediaVolumeUp(message.MessageType))
        {
            MediaVolumeUpReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.IsMediaVolumeDown(message.MessageType))
        {
            MediaVolumeDownReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.IsSoundVolumeUpSignal(message.MessageType))
        {
            Debug.Log("[GUI EVT] Sound volume signal received. GUI does not handle media volume with this signal: " + message.MessageType);
            SoundVolumeUpSignalReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.IsSoundVolumeDownSignal(message.MessageType))
        {
            Debug.Log("[GUI EVT] Sound volume signal received. GUI does not handle media volume with this signal: " + message.MessageType);
            SoundVolumeDownSignalReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.Touch))
        {
            TouchReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.IsMechaStatus(message.MessageType))
        {
            MechaStatusReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.IsLedSubSignal(message.MessageType))
        {
            Debug.Log("[GUI EVT] LED Sub signal received: " + message.MessageType);
            LedSubSignalReceived?.Invoke(message);
            return;
        }

        UnknownEventReceived?.Invoke(message);
    }
}
