using System.Globalization;

public static class GuiCommandFactory
{
    public const string FullModeCommand = "full_mode_cmd";
    public const string HalfModeCommand = "half_mode_cmd";
    public const string CloseModeCommand = "close_mode_cmd";

    public const string LedMainPowerOnCommand = "SIG_LED_MAIN_POWER_ON";
    public const string LedSubPowerOnCommand = "SIG_LED_SUB_POWER_ON";
    public const string LedMainPowerOffCommand = "SIG_LED_MAIN_POWER_OFF";
    public const string LedSubPowerOffCommand = "SIG_LED_SUB_POWER_OFF";

    public const string ShifterStartCommand = "CMD_SHIFTER_START";
    public const string ShifterStopCommand = "CMD_SHIFTER_STOP";

    public const string StartLedPresetCommand = "CMD_LED_MAIN_START_PRESET";
    public const string SetLedBrightnessCommand = "CMD_LED_MAIN_SET_BRIGHTNESS";
    public const string SetLedSaturationCommand = "CMD_LED_MAIN_SET_SATURATION";

    public const string SetHvacVibrationCommand = "CMD_HVAC_SET_VIBRATION";
    public const string SetHvacSoundCommand = "CMD_AUDIO_SET_HVAC_SOUND";

    // 暫定Command。Backend正式仕様が決まったらここだけ変更する。
    public const string SetAudioOutputStateCommand = "CMD_SET_AUDIO_OUTPUT_STATE";

    public static string CreateCommand(string messageType)
    {
        return CreateCommand(messageType, "{}", GuiMessageTypeFieldName.Type);
    }

    public static string CreateCommand(string messageType, string payloadJson)
    {
        return CreateCommand(messageType, payloadJson, GuiMessageTypeFieldName.Type);
    }

    public static string CreateCommand(
        string messageType,
        GuiMessageTypeFieldName fieldName
    )
    {
        return CreateCommand(messageType, "{}", fieldName);
    }

    public static string CreateCommand(
        string messageType,
        string payloadJson,
        GuiMessageTypeFieldName fieldName
    )
    {
        string jsonFieldName = fieldName.ToJsonFieldName();
        return CreateCommand(messageType, payloadJson, jsonFieldName);
    }

    public static string CreateCommand(
        string messageType,
        string payloadJson,
        string messageTypeFieldName
    )
    {
        if (string.IsNullOrEmpty(payloadJson))
        {
            payloadJson = "{}";
        }

        if (string.IsNullOrEmpty(messageTypeFieldName))
        {
            messageTypeFieldName = "type";
        }

        return "{\""
            + EscapeJson(messageTypeFieldName)
            + "\":\""
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

    public static string CreateHvacVibrationPayload(int vibration, int defaultVolume)
    {
        int safeVibration = ClampByte(vibration);
        int safeVolume = ClampByte(defaultVolume);

        return "{\"vibration\":"
            + safeVibration
            + ",\"default_volume\":"
            + safeVolume
            + "}";
    }

    public static string CreateHvacSoundPayload(int sound, int defaultVolume)
    {
        int safeSound = ClampByte(sound);
        int safeVolume = ClampByte(defaultVolume);

        return "{\"sound\":"
            + safeSound
            + ",\"default_volume\":"
            + safeVolume
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

    private static int ClampByte(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return value;
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
