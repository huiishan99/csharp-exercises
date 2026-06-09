using UnityEngine;

public class KinemaCommandBridge : MonoBehaviour
{
    [SerializeField] private GuiCommandTcpClientSender commandSender;

    [Header("Audio Command")]
    [SerializeField] private bool sendAudioOutputCommand = true;
    [SerializeField] private string audioOutputCommandName = GuiCommandFactory.SetAudioOutputStateCommand;

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

    public void SendLightingPresetCommand(int index)
    {
        string payload = GuiCommandFactory.CreateIndexPayload("index", index);
        SendCommand(GuiCommandFactory.StartLedPresetCommand, payload);
    }

    public void SendLightingBrightnessCommand(float brightness)
    {
        string payload = GuiCommandFactory.CreateFloatPayload("brightness", Mathf.Clamp01(brightness));
        SendCommand(GuiCommandFactory.SetLedBrightnessCommand, payload);
    }

    public void SendLightingSaturationCommand(float saturation)
    {
        string payload = GuiCommandFactory.CreateFloatPayload("saturation", Mathf.Clamp01(saturation));
        SendCommand(GuiCommandFactory.SetLedSaturationCommand, payload);
    }

    public void SendAudioOutputStateCommand(bool leftOn, bool rightOn, float volume)
    {
        if (!sendAudioOutputCommand)
        {
            return;
        }

        string payload = GuiCommandFactory.CreateAudioOutputStatePayload(
            leftOn,
            rightOn,
            Mathf.Clamp01(volume)
        );

        SendCommand(audioOutputCommandName, payload);
    }

    private void SendCommand(string messageType)
    {
        ResolveReferences();

        if (commandSender == null)
        {
            Debug.LogWarning("[GUI CMD] Command sender is not assigned. message_type=" + messageType);
            return;
        }

        commandSender.SendCommand(messageType);
    }

    private void SendCommand(string messageType, string payloadJson)
    {
        ResolveReferences();

        if (commandSender == null)
        {
            Debug.LogWarning("[GUI CMD] Command sender is not assigned. message_type=" + messageType);
            return;
        }

        commandSender.SendCommand(messageType, payloadJson);
    }

    private void ResolveReferences()
    {
        if (commandSender == null)
        {
            commandSender = GetComponent<GuiCommandTcpClientSender>();
        }

        if (commandSender == null)
        {
            commandSender = FindFirstObjectByType<GuiCommandTcpClientSender>();
        }
    }
}
