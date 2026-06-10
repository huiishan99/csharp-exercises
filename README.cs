using System;
using UnityEngine;

public static class GuiEventJsonParser
{
    public static bool TryParse(
        string rawJson,
        out GuiEventMessage message,
        out string errorMessage
    )
    {
        message = null;
        errorMessage = "";

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            errorMessage = "Raw json is empty.";
            return false;
        }

        GuiEventMessageTypeEnvelope typeEnvelope;

        try
        {
            typeEnvelope = JsonUtility.FromJson<GuiEventMessageTypeEnvelope>(rawJson);
        }
        catch (Exception exception)
        {
            errorMessage = "Failed to parse message_type. " + exception.Message;
            return false;
        }

        if (typeEnvelope == null || string.IsNullOrWhiteSpace(typeEnvelope.message_type))
        {
            errorMessage = "message_type is missing.";
            return false;
        }

        string messageType = typeEnvelope.message_type.Trim();
        message = new GuiEventMessage(messageType, rawJson);

        try
        {
            FillPayload(rawJson, message);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = "Failed to parse payload. " + exception.Message;
            return false;
        }
    }

    private static void FillPayload(string rawJson, GuiEventMessage message)
    {
        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.ShifterChanged))
        {
            GuiEventShifterEnvelope envelope =
                JsonUtility.FromJson<GuiEventShifterEnvelope>(rawJson);

            message.ShifterPayload = envelope == null ? null : envelope.payload;
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.Touch))
        {
            GuiEventTouchEnvelope envelope =
                JsonUtility.FromJson<GuiEventTouchEnvelope>(rawJson);

            message.TouchPayload = envelope == null ? null : envelope.payload;
            return;
        }

        if (GuiEventType.EqualsType(message.MessageType, GuiEventType.HvacDisplayModeResult))
        {
            GuiEventHvacEnvelope envelope =
                JsonUtility.FromJson<GuiEventHvacEnvelope>(rawJson);

            message.HvacPayload = envelope == null ? null : envelope.payload;
            return;
        }

        // Empty payload 系は message_type だけ分かればよい。
        JsonUtility.FromJson<GuiEventEmptyEnvelope>(rawJson);
    }
}
