public class OledTouchEvent
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public OledTouchEventType EventType { get; private set; }
    public OledTouchSource Source { get; private set; }

    public OledTouchEvent(
        int x,
        int y,
        OledTouchEventType eventType,
        OledTouchSource source
    )
    {
        X = x;
        Y = y;
        EventType = eventType;
        Source = source;
    }

    public override string ToString()
    {
        return "source="
            + Source
            + " type="
            + EventType
            + " x="
            + X
            + " y="
            + Y;
    }
}
