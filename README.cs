public enum GuiMessageTypeFieldName
{
    MessageType,
    Type
}

public static class GuiMessageTypeFieldNameExtensions
{
    public static string ToJsonFieldName(this GuiMessageTypeFieldName fieldName)
    {
        if (fieldName == GuiMessageTypeFieldName.Type)
        {
            return "type";
        }

        return "message_type";
    }
}
