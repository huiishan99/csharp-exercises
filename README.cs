using System.Globalization;

public static class GuiCommandFactory
{
    public const string FullModeCommand = "full_mode_cmd";
    public const string HalfModeCommand = "half_mode_cmd";
    public const string CloseModeCommand = "close_mode_cmd";

    public const string StartLedPresetCommand = "CMD_START_LED_PRESET";
    public const string SetLedBrightnessCommand = "CMD_SET_LED_BRIGHTNESS";
    public const string SetLedSaturationCommand = "CMD_SET_LED_SATURATION";

    public const string SetHvacVibrationCommand = "CMD_SET_HVAC_VIBRATION";
    public const string SetHvacSoundCommand = "CMD_SET_HVAC_SOUND";

    // 暫定Command。正式名が決まったらここだけ変更する。
    public const string SetAudioOutputStateCommand = "CMD_SET_AUDIO_OUTPUT_STATE";

    public static string CreateCommand(string messageType)
    {
        return CreateCommand(messageType, "{}");
    }

    public static string CreateCommand(string messageType, string payloadJson)
    {
        if (string.IsNullOrEmpty(payloadJson))
        {
            payloadJson = "{}";
        }

        return "{\"message_type\":\""
            + EscapeJson(messageType)
            + "\",\"payload\":"
            + payloadJson
            + "}";
    }

    public static string CreateIndexPayload(string key, int value)
    {
        return "{\"" + EscapeJson(key) + "\":" + value + "}";
    }

    public static string CreateFloatPayload(string key, float value)
    {
        return "{\""
            + EscapeJson(key)
            + "\":"
            + FloatToJson(value)
            + "}";
    }

    public static string CreateAudioOutputStatePayload(bool left, bool right, float volume)
    {
        return "{\"left\":"
            + BoolToJson(left)
            + ",\"right\":"
            + BoolToJson(right)
            + ",\"volume\":"
            + FloatToJson(Clamp01(volume))
            + "}";
    }

    private static string BoolToJson(bool value)
    {
        return value ? "true" : "false";
    }

    private static string FloatToJson(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 1f)
        {
            return 1f;
        }

        return value;
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
