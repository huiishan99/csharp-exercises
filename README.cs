using UnityEngine;

public class KinemaCommandBridge : MonoBehaviour
{
    [SerializeField] private GuiBackendTcpClientService commandSender;

    [Header("Default HVAC")]
    [SerializeField] private int defaultHvacVolume = 128;

    [Header("Legacy Audio Output Command")]
    [SerializeField] private bool logBlockedAudioOutputCommand = true;

    [Header("Legacy Haptic Vibration Command")]
    [SerializeField] private bool logBlockedLegacyVibrationCommand = true;

    private void Awake()
    {
        ResolveReferences();
    }

    public void SendFullModeCommand()
    {
        SendCommand(GuiCommandFactory.FullModeCommand);
    }

    public void SendHalfModeCommand()
    {
        SendCommand(GuiCommandFactory.HalfModeCommand);
    }

    public void SendCloseModeCommand()
    {
        SendCommand(GuiCommandFactory.CloseModeCommand);
    }

    public void SendLedMainPowerOnCommand()
    {
        SendCommand(GuiCommandFactory.LedMainPowerOnCommand);
    }

    public void SendLedSubPowerOnCommand()
    {
        SendCommand(GuiCommandFactory.LedSubPowerOnCommand);
    }

    public void SendLedMainPowerOffCommand()
    {
        SendCommand(GuiCommandFactory.LedMainPowerOffCommand);
    }

    public void SendLedSubPowerOffCommand()
    {
        SendCommand(GuiCommandFactory.LedSubPowerOffCommand);
    }

    public void SendShifterStartCommand()
    {
        SendCommand(GuiCommandFactory.ShifterStartCommand);
    }

    public void SendShifterStopCommand()
    {
        SendCommand(GuiCommandFactory.ShifterStopCommand);
    }

    public void SendSystemStartRelatedCommands()
    {
        SendLedMainPowerOnCommand();
        SendLedSubPowerOnCommand();
        SendShifterStartCommand();
    }

    public void SendSystemStopRelatedCommands()
    {
        SendLedMainPowerOffCommand();
        SendLedSubPowerOffCommand();
        SendShifterStopCommand();
    }

    /// <summary>
    /// Lighting preset command.
    /// Backend最新仕様ではpreset_idを送信せず、indexのみ送信する。
    /// </summary>
    public void SendLightingPresetCommand(int index)
    {
        string payload = GuiCommandFactory.CreateIndexPayload("index", index);
        SendCommand(GuiCommandFactory.StartLedPresetCommand, payload);
    }

    public void SendLightingBrightnessCommand(float brightness)
    {
        string payload = GuiCommandFactory.CreateFloatPayload(
            "brightness",
            Mathf.Clamp01(brightness)
        );

        SendCommand(GuiCommandFactory.SetLedBrightnessCommand, payload);
    }

    public void SendLightingSaturationCommand(float saturation)
    {
        string payload = GuiCommandFactory.CreateFloatPayload(
            "saturation",
            Mathf.Clamp01(saturation)
        );

        SendCommand(GuiCommandFactory.SetLedSaturationCommand, payload);
    }

    /// <summary>
    /// HVAC display mode command.
    /// mode: ON / AUTO / OFF
    /// </summary>
    public void SendHvacDisplayModeCommand(string mode)
    {
        string payload = GuiCommandFactory.CreateHvacDisplayModePayload(mode);
        SendCommand(GuiCommandFactory.SetHvacDisplayModeCommand, payload);
    }

    public void SendHvacDisplayModeOnCommand()
    {
        SendHvacDisplayModeCommand(GuiCommandFactory.HvacDisplayModeOn);
    }

    public void SendHvacDisplayModeAutoCommand()
    {
        SendHvacDisplayModeCommand(GuiCommandFactory.HvacDisplayModeAuto);
    }

    public void SendHvacDisplayModeOffCommand()
    {
        SendHvacDisplayModeCommand(GuiCommandFactory.HvacDisplayModeOff);
    }

    /// <summary>
    /// 最新Haptic仕様。
    /// CMD_HVAC_SET_VIBRATION payload: {"pattern":"Set_A"}
    /// </summary>
    public void SendHvacVibrationPatternCommand(string pattern)
    {
        string payload = GuiCommandFactory.CreateHvacVibrationPatternPayload(pattern);
        SendCommand(GuiCommandFactory.SetHvacVibrationCommand, payload);
    }

    public void SendHvacSoundCommand(int sound)
    {
        SendHvacSoundCommand(sound, defaultHvacVolume);
    }

    public void SendHvacSoundCommand(int sound, int defaultVolume)
    {
        string payload = GuiCommandFactory.CreateHvacSoundPayload(
            sound,
            defaultVolume
        );

        SendCommand(GuiCommandFactory.SetHvacSoundCommand, payload);
    }

    /// <summary>
    /// Legacy method.
    /// 旧仕様ではvibration/default_volumeを送信していたが、
    /// 最新仕様ではpattern指定に変更されたため正式送信しない。
    /// </summary>
    [System.Obsolete("Use SendHvacVibrationPatternCommand instead.")]
    public void SendHvacVibrationCommand(int vibration)
    {
        SendHvacVibrationCommand(vibration, defaultHvacVolume);
    }

    /// <summary>
    /// Legacy method.
    /// 既存コンポーネントが呼んでもbackendへ旧payloadを送らない。
    /// </summary>
    [System.Obsolete("Use SendHvacVibrationPatternCommand instead.")]
    public void SendHvacVibrationCommand(int vibration, int defaultVolume)
    {
        if (!logBlockedLegacyVibrationCommand)
        {
            return;
        }

        Debug.Log(
            "[GUI CMD] Blocked deprecated CMD_HVAC_SET_VIBRATION numeric payload. "
            + "vibration="
            + vibration
            + " defaultVolume="
            + defaultVolume
            + ". Use pattern payload instead."
        );
    }

    /// <summary>
    /// Legacy method.
    /// 最新sequenceではCMD_SET_AUDIO_OUTPUT_STATEをGUIからbackendへ送信しない。
    /// 既存コンポーネントが呼んでも通信しないようno-opにする。
    /// </summary>
    [System.Obsolete("CMD_SET_AUDIO_OUTPUT_STATE is deprecated. This method intentionally does not send any command.")]
    public void SendAudioOutputStateCommand(bool leftOn, bool rightOn, float volume)
    {
        if (!logBlockedAudioOutputCommand)
        {
            return;
        }

        Debug.Log(
            "[GUI CMD] Blocked deprecated CMD_SET_AUDIO_OUTPUT_STATE. "
            + "left="
            + leftOn
            + " right="
            + rightOn
            + " volume="
            + Mathf.Clamp01(volume).ToString("0.###")
        );
    }

    private void SendCommand(string messageType)
    {
        ResolveReferences();

        if (commandSender == null)
        {
            Debug.LogWarning("[GUI CMD] Command sender is not assigned. type=" + messageType);
            return;
        }

        commandSender.SendCommand(messageType);
    }

    private void SendCommand(string messageType, string payloadJson)
    {
        ResolveReferences();

        if (commandSender == null)
        {
            Debug.LogWarning("[GUI CMD] Command sender is not assigned. type=" + messageType);
            return;
        }

        commandSender.SendCommand(messageType, payloadJson);
    }

    private void ResolveReferences()
    {
        if (commandSender == null)
        {
            commandSender = GetComponent<GuiBackendTcpClientService>();
        }

        if (commandSender == null)
        {
            commandSender = FindFirstObjectByType<GuiBackendTcpClientService>();
        }
    }
}
