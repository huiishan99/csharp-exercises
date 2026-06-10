using System;

[Serializable]
public class GuiEventMessageTypeEnvelope
{
    public string message_type;
}

[Serializable]
public class GuiEventEmptyPayload
{
}

[Serializable]
public class GuiEventEmptyEnvelope
{
    public string message_type;
    public GuiEventEmptyPayload payload;
}

[Serializable]
public class GuiEventShifterPayload
{
    public string gear;
    public string shift;
    public string value;

    public string GetGearText()
    {
        if (!string.IsNullOrEmpty(gear))
        {
            return gear;
        }

        if (!string.IsNullOrEmpty(shift))
        {
            return shift;
        }

        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        return "";
    }
}

[Serializable]
public class GuiEventShifterEnvelope
{
    public string message_type;
    public GuiEventShifterPayload payload;
}

[Serializable]
public class GuiEventTouchPayload
{
    public string source;
    public int x;
    public int y;

    // backend文書側の可能性: event
    public string @event;

    // 口頭I/F側の可能性: event_type
    public string event_type;

    public string GetTouchEventText()
    {
        if (!string.IsNullOrEmpty(event_type))
        {
            return event_type;
        }

        if (!string.IsNullOrEmpty(@event))
        {
            return @event;
        }

        return "";
    }
}

[Serializable]
public class GuiEventTouchEnvelope
{
    public string message_type;
    public GuiEventTouchPayload payload;
}

[Serializable]
public class GuiEventHvacPayload
{
    public string disp_mode;
    public string result;
    public string value;
}

[Serializable]
public class GuiEventHvacEnvelope
{
    public string message_type;
    public GuiEventHvacPayload payload;
}
