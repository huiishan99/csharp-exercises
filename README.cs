using UnityEngine;

public class KinemaCommandBridge : MonoBehaviour
{
    [Header("Backend TCP")]
    [SerializeField]
    private GuiBackendTcpClientService commandSender;

    [Header("Default HVAC Sound Volume")]
    [SerializeField, Range(0, 255)]
    private int defaultHvacSoundVolume = 128;

    [Header("Legacy Command Protection")]
    [SerializeField]
    private bool logBlockedLegacyCommand = true;

    private void Awake()
    {
        ResolveReferences();
    }

    // ============================================================
    // Mecha
    // ============================================================

    public void SendHalfModeCommand()
    {
        SendCommand(
            GuiCommandFactory.HalfModeCommand
        );
    }

    public void SendFullModeCommand()
    {
        SendCommand(
            GuiCommandFactory.FullModeCommand
        );
    }

    public void SendCloseModeCommand()
    {
        SendCommand(
            GuiCommandFactory.CloseModeCommand
        );
    }

    // 新しい名称をUnityコード側でも使用する場合の入口。
    public void SendMechaTransitionToHalfCommand()
    {
        SendHalfModeCommand();
    }

    public void SendMechaTransitionToFullCommand()
    {
        SendFullModeCommand();
    }

    public void SendMechaTransitionToCloseCommand()
    {
        SendCloseModeCommand();
    }

    // ============================================================
    // LED Power
    // ============================================================

    public void SendLedMainPowerOnCommand()
    {
        SendCommand(
            GuiCommandFactory.LedMainPowerOnCommand
        );
    }

    public void SendLedSubPowerOnCommand()
    {
        SendCommand(
            GuiCommandFactory.LedSubPowerOnCommand
        );
    }

    public void SendLedMainPowerOffCommand()
    {
        SendCommand(
            GuiCommandFactory.LedMainPowerOffCommand
        );
    }

    public void SendLedSubPowerOffCommand()
    {
        SendCommand(
            GuiCommandFactory.LedSubPowerOffCommand
        );
    }

    // ============================================================
    // Shifter
    // ============================================================

    public void SendShifterStartCommand()
    {
        SendCommand(
            GuiCommandFactory.ShifterStartCommand
        );
    }

    public void SendShifterStopCommand()
    {
        SendCommand(
            GuiCommandFactory.ShifterStopCommand
        );
    }

    /// <summary>
    /// IGN ON時に送信するDevice起動Command。
    /// Mecha遷移Commandは別途送信する。
    /// </summary>
    public void SendSystemStartRelatedCommands()
    {
        SendLedMainPowerOnCommand();
        SendLedSubPowerOnCommand();
        SendShifterStartCommand();
    }

    /// <summary>
    /// IGN OFF時に送信するDevice停止Command。
    /// Mecha遷移Commandは別途送信する。
    /// </summary>
    public void SendSystemStopRelatedCommands()
    {
        SendLedMainPowerOffCommand();
        SendLedSubPowerOffCommand();
        SendShifterStopCommand();
    }

    // ============================================================
    // Lighting
    // ============================================================

    /// <summary>
    /// Lighting presetを開始する。
    /// payloadはindexのみを送信する。
    /// </summary>
    public void SendLightingPresetCommand(int index)
    {
        if (index < 0 || index > 7)
        {
            Debug.LogWarning(
                "[GUI CMD] Lighting preset index is out of range: "
                + index
            );

            return;
        }

        string payload =
            GuiCommandFactory.CreateIndexPayload(
                "index",
                index
            );

        SendCommand(
            GuiCommandFactory.StartLedPresetCommand,
            payload
        );
    }

    /// <summary>
    /// 現在のLighting presetを停止する。
    /// </summary>
    public void SendLightingPresetStopCommand()
    {
        SendCommand(
            GuiCommandFactory.StopLedMainPresetCommand
        );
    }

    public void SendLightingBrightnessCommand(
        float brightness
    )
    {
        float safeBrightness =
            Mathf.Clamp01(brightness);

        string payload =
            GuiCommandFactory.CreateFloatPayload(
                "brightness",
                safeBrightness
            );

        SendCommand(
            GuiCommandFactory.SetLedBrightnessCommand,
            payload
        );
    }

    public void SendLightingSaturationCommand(
        float saturation
    )
    {
        float safeSaturation =
            Mathf.Clamp01(saturation);

        string payload =
            GuiCommandFactory.CreateFloatPayload(
                "saturation",
                safeSaturation
            );

        SendCommand(
            GuiCommandFactory.SetLedSaturationCommand,
            payload
        );
    }

    // ============================================================
    // HVAC Display Mode
    // ============================================================

    public void SendHvacDisplayModeCommand(
        string mode
    )
    {
        string payload =
            GuiCommandFactory.CreateHvacDisplayModePayload(
                mode
            );

        SendCommand(
            GuiCommandFactory.SetHvacDisplayModeCommand,
            payload
        );
    }

    public void SendHvacDisplayModeOnCommand()
    {
        SendHvacDisplayModeCommand(
            GuiCommandFactory.HvacDisplayModeOn
        );
    }

    public void SendHvacDisplayModeAutoCommand()
    {
        SendHvacDisplayModeCommand(
            GuiCommandFactory.HvacDisplayModeAuto
        );
    }

    public void SendHvacDisplayModeOffCommand()
    {
        SendHvacDisplayModeCommand(
            GuiCommandFactory.HvacDisplayModeOff
        );
    }

    // ============================================================
    // Haptic Sound
    // ============================================================

    /// <summary>
    /// HVAC Push音を設定する。
    /// </summary>
    public void SendHvacPushSoundCommand(
        int sound
    )
    {
        SendHvacPushSoundCommand(
            sound,
            defaultHvacSoundVolume
        );
    }

    /// <summary>
    /// HVAC Push音を設定する。
    /// Command:
    /// CMD_SOUND_SET_PUSH_HVAC
    /// </summary>
    public void SendHvacPushSoundCommand(
        int sound,
        int defaultVolume
    )
    {
        string payload =
            GuiCommandFactory.CreateSoundPayload(
                sound,
                defaultVolume
            );

        SendCommand(
            GuiCommandFactory.SetHvacPushSoundCommand,
            payload
        );
    }

    /// <summary>
    /// HVAC Release音を設定する。
    /// </summary>
    public void SendHvacReleaseSoundCommand(
        int sound
    )
    {
        SendHvacReleaseSoundCommand(
            sound,
            defaultHvacSoundVolume
        );
    }

    /// <summary>
    /// HVAC Release音を設定する。
    /// Command:
    /// CMD_SOUND_SET_RELEASE_HVAC
    /// </summary>
    public void SendHvacReleaseSoundCommand(
        int sound,
        int defaultVolume
    )
    {
        string payload =
            GuiCommandFactory.CreateSoundPayload(
                sound,
                defaultVolume
            );

        SendCommand(
            GuiCommandFactory.SetHvacReleaseSoundCommand,
            payload
        );
    }

    // ============================================================
    // Haptic Vibration
    // ============================================================

    /// <summary>
    /// HVAC振動パターンを設定する。
    /// Command:
    /// CMD_HVAC_SET_VIBRATION
    /// </summary>
    public void SendHvacVibrationPatternCommand(
        string pattern
    )
    {
        string payload =
            GuiCommandFactory
                .CreateHvacVibrationPatternPayload(
                    pattern
                );

        SendCommand(
            GuiCommandFactory.SetHvacVibrationCommand,
            payload
        );
    }

    // ============================================================
    // Legacy compatibility
    // ============================================================

    /// <summary>
    /// 旧CMD_AUDIO_SET_HVAC_SOUNDは廃止済み。
    /// 既存Scriptから呼ばれてもBackendへは送信しない。
    /// </summary>
    public void SendHvacSoundCommand(
        int sound
    )
    {
        LogBlockedLegacyCommand(
            "SendHvacSoundCommand",
            "Use SendHvacPushSoundCommand and "
            + "SendHvacReleaseSoundCommand."
        );
    }

    /// <summary>
    /// 旧CMD_AUDIO_SET_HVAC_SOUNDは廃止済み。
    /// </summary>
    public void SendHvacSoundCommand(
        int sound,
        int defaultVolume
    )
    {
        LogBlockedLegacyCommand(
            "SendHvacSoundCommand",
            "Use SendHvacPushSoundCommand and "
            + "SendHvacReleaseSoundCommand."
        );
    }

    /// <summary>
    /// 旧vibration/default_volume形式は廃止済み。
    /// </summary>
    public void SendHvacVibrationCommand(
        int vibration
    )
    {
        LogBlockedLegacyCommand(
            "SendHvacVibrationCommand",
            "Use SendHvacVibrationPatternCommand."
        );
    }

    /// <summary>
    /// 旧vibration/default_volume形式は廃止済み。
    /// </summary>
    public void SendHvacVibrationCommand(
        int vibration,
        int defaultVolume
    )
    {
        LogBlockedLegacyCommand(
            "SendHvacVibrationCommand",
            "Use SendHvacVibrationPatternCommand."
        );
    }

    /// <summary>
    /// CMD_SET_AUDIO_OUTPUT_STATEは廃止済み。
    /// </summary>
    public void SendAudioOutputStateCommand(
        bool leftOn,
        bool rightOn,
        float volume
    )
    {
        LogBlockedLegacyCommand(
            "SendAudioOutputStateCommand",
            "CMD_SET_AUDIO_OUTPUT_STATE is not sent."
        );
    }

    private void LogBlockedLegacyCommand(
        string methodName,
        string replacement
    )
    {
        if (!logBlockedLegacyCommand)
        {
            return;
        }

        Debug.LogWarning(
            "[GUI CMD] Blocked legacy command call: "
            + methodName
            + ". "
            + replacement
        );
    }

    // ============================================================
    // Internal send
    // ============================================================

    private void SendCommand(string messageType)
    {
        ResolveReferences();

        if (commandSender == null)
        {
            Debug.LogWarning(
                "[GUI CMD] Backend TCP client is not assigned. type="
                + messageType
            );

            return;
        }

        commandSender.SendCommand(messageType);
    }

    private void SendCommand(
        string messageType,
        string payloadJson
    )
    {
        ResolveReferences();

        if (commandSender == null)
        {
            Debug.LogWarning(
                "[GUI CMD] Backend TCP client is not assigned. type="
                + messageType
            );

            return;
        }

        commandSender.SendCommand(
            messageType,
            payloadJson
        );
    }

    private void ResolveReferences()
    {
        if (commandSender == null)
        {
            commandSender =
                GetComponent<GuiBackendTcpClientService>();
        }

        if (commandSender == null)
        {
            commandSender =
                FindFirstObjectByType<
                    GuiBackendTcpClientService
                >();
        }
    }
}
