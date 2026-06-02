using UnityEngine;

public class DemoSpeakerState : MonoBehaviour
{
    [SerializeField] private bool leftSpeakerOn = true;
    [SerializeField] private bool rightSpeakerOn = true;

    [SerializeField] private float volume = 0.8f;
    [SerializeField] private float volumeStep = 0.1f;

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
        LogState("Initial Speaker State");
    }

    public void ToggleLeftSpeaker()
    {
        leftSpeakerOn = !leftSpeakerOn;
        LogState("Toggle Left Speaker");
    }

    public void ToggleRightSpeaker()
    {
        rightSpeakerOn = !rightSpeakerOn;
        LogState("Toggle Right Speaker");
    }

    public void ToggleBothSpeakers()
    {
        bool shouldTurnOn = !(leftSpeakerOn && rightSpeakerOn);

        leftSpeakerOn = shouldTurnOn;
        rightSpeakerOn = shouldTurnOn;

        LogState("Toggle Both Speakers");
    }

    public void IncreaseVolume()
    {
        volume = Mathf.Clamp01(volume + volumeStep);
        LogState("Volume Up");
    }

    public void DecreaseVolume()
    {
        volume = Mathf.Clamp01(volume - volumeStep);
        LogState("Volume Down");
    }

    private void LogState(string actionName)
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
    }

    private string ToOnOff(bool value)
    {
        return value ? "ON" : "OFF";
    }
}
