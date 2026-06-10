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

    public event Action<GuiEventMessage> TouchReceived;
    public event Action<GuiEventMessage> MechaStatusReceived;

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

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.IgOn))
        {
            IgOnReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.IgOff))
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

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.MediaVolumeUp))
        {
            MediaVolumeUpReceived?.Invoke(message);
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.MediaVolumeDown))
        {
            MediaVolumeDownReceived?.Invoke(message);
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

        UnknownEventReceived?.Invoke(message);
    }
}
