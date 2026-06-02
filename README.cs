using System;
using UnityEngine;

public class DemoSpeakerState : MonoBehaviour
{
    [SerializeField] private bool leftSpeakerOn = true;
    [SerializeField] private bool rightSpeakerOn = true;

    [SerializeField] private float volume = 0.8f;
    [SerializeField] private float volumeStep = 0.1f;

    public event Action SpeakerStateChanged;

    public bool LeftSpeakerOn
    {
        get { return leftSpeakerOn; }
    }

    public bool RightSpeakerOn
    {
        get { return rightSpeakerOn; }
    }

    public float Volume
    {
        get { return volume; }
    }

    private void Start()
    {
        NotifyChanged("Initial Speaker State");
    }

    public void ToggleLeftSpeaker()
    {
        leftSpeakerOn = !leftSpeakerOn;
        NotifyChanged("Toggle Left Speaker");
    }

    public void ToggleRightSpeaker()
    {
        rightSpeakerOn = !rightSpeakerOn;
        NotifyChanged("Toggle Right Speaker");
    }

    public void ToggleBothSpeakers()
    {
        bool shouldTurnOn = !(leftSpeakerOn && rightSpeakerOn);

        leftSpeakerOn = shouldTurnOn;
        rightSpeakerOn = shouldTurnOn;

        NotifyChanged("Toggle Both Speakers");
    }

    public void IncreaseVolume()
    {
        volume = Mathf.Clamp01(volume + volumeStep);
        NotifyChanged("Volume Up");
    }

    public void DecreaseVolume()
    {
        volume = Mathf.Clamp01(volume - volumeStep);
        NotifyChanged("Volume Down");
    }

    private void NotifyChanged(string actionName)
    {
        Debug.Log(
            "[Speaker] "
            + actionName
            + " | Left: "
            + ToOnOff(leftSpeakerOn)
            + " | Right: "
            + ToOnOff(rightSpeakerOn)
            + " | Volume: "
            + Mathf.RoundToInt(volume * 100f)
            + "%"
        );

        SpeakerStateChanged?.Invoke();
    }

    private string ToOnOff(bool value)
    {
        return value ? "ON" : "OFF";
    }
}
