using System.Globalization;

public static class GuiCommandFactory
{
    // ============================================================
    // Mecha
    // ============================================================

    public const string HalfModeCommand =
        "CMD_MECHA_TRANSITION_TO_HALF";

    public const string FullModeCommand =
        "CMD_MECHA_TRANSITION_TO_FULL";

    public const string CloseModeCommand =
        "CMD_MECHA_TRANSITION_TO_CLOSE";

    // ============================================================
    // LED Power
    // ============================================================

    public const string LedMainPowerOnCommand =
        "CMD_LED_MAIN_POWER_ON";

    public const string LedSubPowerOnCommand =
        "CMD_LED_SUB_POWER_ON";

    public const string LedMainPowerOffCommand =
        "CMD_LED_MAIN_POWER_OFF";

    public const string LedSubPowerOffCommand =
        "CMD_LED_SUB_POWER_OFF";

    // ============================================================
    // Shifter
    // ============================================================

    public const string ShifterStartCommand =
        "CMD_SHIFTER_START";

    public const string ShifterStopCommand =
        "CMD_SHIFTER_STOP";

    // ============================================================
    // Lighting
    // ============================================================

    public const string StartLedPresetCommand =
        "CMD_LED_MAIN_START_PRESET";

    public const string StopLedMainPresetCommand =
        "CMD_LED_MAIN_STOP_PRESET";

    public const string SetLedBrightnessCommand =
        "CMD_LED_MAIN_SET_BRIGHTNESS";

    public const string SetLedSaturationCommand =
        "CMD_LED_MAIN_SET_SATURATION";

    // ============================================================
    // HVAC
    // ============================================================

    public const string SetHvacDisplayModeCommand =
        "CMD_HVAC_SET_DISPLAY_MODE";

    public const string SetHvacVibrationCommand =
        "CMD_HVAC_SET_VIBRATION";

    public const string HvacDisplayModeOn = "ON";
    public const string HvacDisplayModeAuto = "AUTO";
    public const string HvacDisplayModeOff = "OFF";

    // ============================================================
    // Haptic Sound
    // ============================================================

    public const string SetHvacPushSoundCommand =
        "CMD_SOUND_SET_PUSH_HVAC";

    public const string SetHvacReleaseSoundCommand =
        "CMD_SOUND_SET_RELEASE_HVAC";

    // ============================================================
    // Command envelope
    // ============================================================

    /// <summary>
    /// payloadが空のCommand JSONを生成する。
    /// </summary>
    public static string CreateCommand(string messageType)
    {
        return CreateCommand(
            messageType,
            "{}",
            GuiMessageTypeFieldName.Type
        );
    }

    /// <summary>
    /// payloadを指定してCommand JSONを生成する。
    /// </summary>
    public static string CreateCommand(
        string messageType,
        string payloadJson
    )
    {
        return CreateCommand(
            messageType,
            payloadJson,
            GuiMessageTypeFieldName.Type
        );
    }

    /// <summary>
    /// message type fieldを指定してCommand JSONを生成する。
    /// 正式Backend通信ではTypeを使用する。
    /// </summary>
    public static string CreateCommand(
        string messageType,
        GuiMessageTypeFieldName fieldName
    )
    {
        return CreateCommand(
            messageType,
            "{}",
            fieldName
        );
    }

    /// <summary>
    /// payloadとmessage type fieldを指定してCommand JSONを生成する。
    /// </summary>
    public static string CreateCommand(
        string messageType,
        string payloadJson,
        GuiMessageTypeFieldName fieldName
    )
    {
        string jsonFieldName = fieldName.ToJsonFieldName();

        return CreateCommand(
            messageType,
            payloadJson,
            jsonFieldName
        );
    }

    /// <summary>
    /// Command JSON本体を生成する。
    /// </summary>
    public static string CreateCommand(
        string messageType,
        string payloadJson,
        string messageTypeFieldName
    )
    {
        if (string.IsNullOrWhiteSpace(messageType))
        {
            messageType = "";
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            payloadJson = "{}";
        }

        if (string.IsNullOrWhiteSpace(messageTypeFieldName))
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

    // ============================================================
    // Common payload
    // ============================================================

    /// <summary>
    /// int値を1件含むpayloadを生成する。
    /// 例: {"index":2}
    /// </summary>
    public static string CreateIndexPayload(
        string key,
        int value
    )
    {
        return "{\""
            + EscapeJson(key)
            + "\":"
            + value
            + "}";
    }

    /// <summary>
    /// float値を1件含むpayloadを生成する。
    /// 例: {"brightness":0.5}
    /// </summary>
    public static string CreateFloatPayload(
        string key,
        float value
    )
    {
        return "{\""
            + EscapeJson(key)
            + "\":"
            + FloatToJson(value)
            + "}";
    }

    /// <summary>
    /// string値を1件含むpayloadを生成する。
    /// 例: {"mode":"AUTO"}
    /// </summary>
    public static string CreateStringPayload(
        string key,
        string value
    )
    {
        return "{\""
            + EscapeJson(key)
            + "\":\""
            + EscapeJson(value)
            + "\"}";
    }

    // ============================================================
    // HVAC payload
    // ============================================================

    /// <summary>
    /// HVAC表示Mode payloadを生成する。
    /// mode: ON / AUTO / OFF
    /// </summary>
    public static string CreateHvacDisplayModePayload(
        string mode
    )
    {
        string safeMode = NormalizeHvacDisplayMode(mode);

        return CreateStringPayload(
            "mode",
            safeMode
        );
    }

    /// <summary>
    /// HVAC振動Pattern payloadを生成する。
    /// 例: {"pattern":"Set_A"}
    /// </summary>
    public static string CreateHvacVibrationPatternPayload(
        string pattern
    )
    {
        string safePattern = string.IsNullOrWhiteSpace(pattern)
            ? "Set_A"
            : pattern.Trim();

        return CreateStringPayload(
            "pattern",
            safePattern
        );
    }

    // ============================================================
    // Sound payload
    // ============================================================

    /// <summary>
    /// Push / Release Sound共通payloadを生成する。
    /// soundとdefault_volumeは0～255に制限する。
    /// </summary>
    public static string CreateSoundPayload(
        int sound,
        int defaultVolume
    )
    {
        int safeSound = ClampByte(sound);
        int safeVolume = ClampByte(defaultVolume);

        return "{\"sound\":"
            + safeSound
            + ",\"default_volume\":"
            + safeVolume
            + "}";
    }

    // ============================================================
    // Internal helper
    // ============================================================

    private static string NormalizeHvacDisplayMode(
        string mode
    )
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return HvacDisplayModeOn;
        }

        string normalized = mode.Trim().ToUpperInvariant();

        if (normalized == HvacDisplayModeOn
            || normalized == HvacDisplayModeAuto
            || normalized == HvacDisplayModeOff)
        {
            return normalized;
        }

        return HvacDisplayModeOn;
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

    private static string FloatToJson(float value)
    {
        return value.ToString(
            "0.###",
            CultureInfo.InvariantCulture
        );
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
