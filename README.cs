public void SendHvacVibrationCommand(int vibration)
{
    SendHvacVibrationCommand(vibration, 128);
}

public void SendHvacVibrationCommand(int vibration, int defaultVolume)
{
    string payload = GuiCommandFactory.CreateHvacVibrationPayload(
        vibration,
        defaultVolume
    );

    SendCommand(GuiCommandFactory.SetHvacVibrationCommand, payload);
}

public void SendHvacSoundCommand(int sound)
{
    SendHvacSoundCommand(sound, 128);
}

public void SendHvacSoundCommand(int sound, int defaultVolume)
{
    string payload = GuiCommandFactory.CreateHvacSoundPayload(
        sound,
        defaultVolume
    );

    SendCommand(GuiCommandFactory.SetHvacSoundCommand, payload);
}
