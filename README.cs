using System.Collections;
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
    private Coroutine delayedSendCoroutine;

    private void Awake()
    {
        ResolveReferences();

        if (sliderValue != null)
        {
            latestValue = Mathf.Clamp01(sliderValue.Value);
        }
    }

    private void OnDisable()
    {
        if (delayedSendCoroutine != null)
        {
            StopCoroutine(delayedSendCoroutine);
            delayedSendCoroutine = null;
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

    /// <summary>
    /// +/- Button 或 StepController から呼ぶ。
    /// 渡された value は信用せず、次フレームで HorizontalSliderValue から再取得する。
    /// </summary>
    public void SendValueImmediately(float ignoredValue)
    {
        RequestSendCurrentValueNextFrame("StepButton");
    }

    /// <summary>
    /// Button OnClick など、float を渡せない場合はこちらを使う。
    /// </summary>
    public void SendCurrentValueAfterStepButton()
    {
        RequestSendCurrentValueNextFrame("StepButton");
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

    private void RequestSendCurrentValueNextFrame(string reason)
    {
        if (delayedSendCoroutine != null)
        {
            StopCoroutine(delayedSendCoroutine);
        }

        delayedSendCoroutine = StartCoroutine(SendCurrentValueNextFrame(reason));
    }

    private IEnumerator SendCurrentValueNextFrame(string reason)
    {
        // StepController / SliderValue の更新完了を待つ。
        yield return null;

        delayedSendCoroutine = null;
        SendCurrentValue(reason);
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
