using UnityEngine;
using PushButtonSliderLite;

public enum LightingSliderCommandType
{
    Brightness,
    Saturation
}

public class LightingSliderCommandEmitter : MonoBehaviour
{
    [SerializeField] private LightingSliderCommandType commandType = LightingSliderCommandType.Brightness;
    [SerializeField] private HorizontalSliderValue sliderValue;
    [SerializeField] private KinemaCommandBridge commandBridge;

    [Header("Drag Send Option")]
    [SerializeField] private bool sendWhileChanging = false;
    [SerializeField] private float minSendIntervalSec = 0.1f;

    private float latestValue;
    private float lastSendTime = -999f;

    private void Awake()
    {
        ResolveReferences();

        if (sliderValue != null)
        {
            latestValue = sliderValue.Value;
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

        SendValue(latestValue);
    }

    public void OnSliderDragEnded()
    {
        SendCurrentValue();
    }

    public void SendValueImmediately(float value)
    {
        latestValue = Mathf.Clamp01(value);
        SendValue(latestValue);
    }

    public void SendCurrentValue()
    {
        ResolveReferences();

        if (sliderValue != null)
        {
            latestValue = sliderValue.Value;
        }

        SendValue(latestValue);
    }

    private void SendValue(float value)
    {
        ResolveReferences();

        if (commandBridge == null)
        {
            Debug.LogWarning("[Lighting CMD] Command bridge is not assigned.");
            return;
        }

        lastSendTime = Time.unscaledTime;

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
