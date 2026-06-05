using System;
using UnityEngine;

public static class OledTouchJsonParser
{
    [Serializable]
    private class OledTouchJsonPayload
    {
        public int x;
        public int y;
        public string event_type;
        public string source;
    }

    public static bool TryParse(
        string json,
        out OledTouchEvent touchEvent,
        out string errorMessage
    )
    {
        touchEvent = null;
        errorMessage = "";

        if (string.IsNullOrWhiteSpace(json))
        {
            errorMessage = "Json is empty.";
            return false;
        }

        OledTouchJsonPayload payload;

        try
        {
            payload = JsonUtility.FromJson<OledTouchJsonPayload>(json);
        }
        catch (Exception exception)
        {
            errorMessage = "Json parse exception: " + exception.Message;
            return false;
        }

        if (payload == null)
        {
            errorMessage = "Json payload is null.";
            return false;
        }

        OledTouchEventType eventType = ParseEventType(payload.event_type);
        OledTouchSource source = ParseSource(payload.source);

        if (eventType == OledTouchEventType.Unknown)
        {
            errorMessage = "Unknown event_type: " + payload.event_type;
            return false;
        }

        if (source == OledTouchSource.Unknown)
        {
            errorMessage = "Unknown source: " + payload.source;
            return false;
        }

        touchEvent = new OledTouchEvent(
            payload.x,
            payload.y,
            eventType,
            source
        );

        return true;
    }

    private static OledTouchEventType ParseEventType(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return OledTouchEventType.Unknown;
        }

        string normalized = value.Trim().ToLowerInvariant();

        if (normalized == "down")
        {
            return OledTouchEventType.Down;
        }

        if (normalized == "move")
        {
            return OledTouchEventType.Move;
        }

        if (normalized == "up")
        {
            return OledTouchEventType.Up;
        }

        return OledTouchEventType.Unknown;
    }

    private static OledTouchSource ParseSource(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return OledTouchSource.Unknown;
        }

        string normalized = value.Trim().ToLowerInvariant();

        if (normalized == "driver")
        {
            return OledTouchSource.Driver;
        }

        if (normalized == "passenger")
        {
            return OledTouchSource.Passenger;
        }

        return OledTouchSource.Unknown;
    }
}
