public class GuiEventMessage
{
    public string MessageType { get; private set; }
    public string RawJson { get; private set; }

    public GuiEventShifterPayload ShifterPayload { get; set; }
    public GuiEventTouchPayload TouchPayload { get; set; }
    public GuiEventHvacPayload HvacPayload { get; set; }

    public GuiEventMessage(string messageType, string rawJson)
    {
        MessageType = messageType;
        RawJson = rawJson;
    }

    public override string ToString()
    {
        return "message_type=" + MessageType;
    }
}
