using UnityEngine;
using PushButtonSliderLite;

public enum LightingSliderCommandType
{
    Brightness,
    Saturation
}

[DisallowMultipleComponent]
public class LightingSliderCommandEmitter : MonoBehaviour
{
    [Header("Command Type")]
    [SerializeField] private LightingSliderCommandType commandType = LightingSliderCommandType.Brightness;

    [Header("References")]
    [SerializeField] private HorizontalSliderValue sliderValue;
    [SerializeField] private KinemaCommandBridge commandBridge;

    [Header("Drag Send Option")]
    [SerializeField] private bool sendWhileChanging = false;
    [SerializeField] private float minSendIntervalSec = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool logSend = true;

    private float latestValue;
    private float lastSendTime = -999f;

    private void Awake()
    {
        ResolveReferences();

        if (sliderValue != null)
        {
            latestValue = Mathf.Clamp01(sliderValue.Value);
        }
    }

    public void OnSliderValueChanged(float value)
    {
        latestValue = Mathf.Clamp01(value);

        if (!sendWhileChanging)
        {
            return;
        }

        if (Time.unscaledTime - lastSendTime < minSendIntervalSec)
        {
            return;
        }

        SendValue(latestValue, "ValueChanged");
    }

    public void OnSliderDragEnded()
    {
        SendCurrentValue("DragEnded");
    }

    public void SendValueImmediately(float value)
    {
        latestValue = Mathf.Clamp01(value);
        SendValue(latestValue, "StepButton");
    }

    public void SendCurrentValue()
    {
        SendCurrentValue("Manual");
    }

    private void SendCurrentValue(string reason)
    {
        ResolveReferences();

        if (sliderValue != null)
        {
            latestValue = Mathf.Clamp01(sliderValue.Value);
        }

        SendValue(latestValue, reason);
    }

    private void SendValue(float value, string reason)
    {
        ResolveReferences();

        if (commandBridge == null)
        {
            Debug.LogWarning("[Lighting Slider CMD] CommandBridge is not assigned. object=" + gameObject.name);
            return;
        }

        lastSendTime = Time.unscaledTime;

        if (logSend)
        {
            Debug.Log(
                "[Lighting Slider CMD] object="
                + gameObject.name
                + " type="
                + commandType
                + " value="
                + value.ToString("0.###")
                + " reason="
                + reason
            );
        }

        if (commandType == LightingSliderCommandType.Brightness)
        {
            commandBridge.SendLightingBrightnessCommand(value);
            return;
        }

        commandBridge.SendLightingSaturationCommand(value);
    }

    private void ResolveReferences()
    {
        if (sliderValue == null)
        {
            sliderValue = GetComponent<HorizontalSliderValue>();
        }

        if (commandBridge == null)
        {
            commandBridge = FindFirstObjectByType<KinemaCommandBridge>();
        }
    }
}
