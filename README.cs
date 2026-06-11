public static class GuiEventType
{
    public const string IgOn = "EVT_IG_ON";
    public const string IgOff = "EVT_IG_OFF";

    // Backend側の短縮表記も受ける。
    public const string IgOnShort = "IG_ON";
    public const string IgOffShort = "IG_OFF";

    public const string ShifterChanged = "EVT_SHIFTER_CHANGED";

    public const string HvacPopup = "EVT_HVAC_POPUP";
    public const string HvacDisplayModeResult = "EVT_HVAC_DISPLAY_MODE_RESULT";

    public const string MediaVolumeUp = "EVT_MEDIA_VOLUME_UP";
    public const string MediaVolumeDown = "EVT_MEDIA_VOLUME_DOWN";

    public const string Touch = "EVT_TOUCH";

    public const string CloseModeStatus = "close_mode_sts";
    public const string HalfModeStatus = "half_mode_sts";
    public const string FullModeStatus = "full_mode_sts";
    public const string OtherModeStatus = "other_mode_sts";

    public static bool EqualsType(string actual, string expected)
    {
        if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        return actual.Trim().ToLowerInvariant() == expected.Trim().ToLowerInvariant();
    }

    public static bool IsIgOn(string messageType)
    {
        return EqualsType(messageType, IgOn) || EqualsType(messageType, IgOnShort);
    }

    public static bool IsIgOff(string messageType)
    {
        return EqualsType(messageType, IgOff) || EqualsType(messageType, IgOffShort);
    }

    public static bool IsMechaStatus(string messageType)
    {
        return EqualsType(messageType, CloseModeStatus)
            || EqualsType(messageType, HalfModeStatus)
            || EqualsType(messageType, FullModeStatus)
            || EqualsType(messageType, OtherModeStatus);
    }
}
