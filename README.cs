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

    [Header("Realtime Send Option")]
    [SerializeField] private bool sendWhileChanging = true;

    // 0 = 每次 Slider value 改变都发送。
    // 如果硬件侧太卡，可以改成 0.03 或 0.05。
    [SerializeField] private float minSendIntervalSec = 0f;

    // 避免同一个值重复发送太多次。
    [SerializeField] private float minValueDelta = 0.001f;

    // 松手时再补发最终值。
    [SerializeField] private bool sendOnDragEnded = true;

    [Header("Debug")]
    [SerializeField] private bool logSend = true;

    private float latestValue;
    private float lastSentValue = -999f;
    private float lastSendTime = -999f;
    private bool hasSentValue;
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

    /// <summary>
    /// Slider drag 中に呼ばれる。
    /// UnityEvent の float 引数が Static 0 になる場合があるため、
    /// 渡された value は使わず、HorizontalSliderValue.Value を直接読む。
    /// </summary>
    public void OnSliderValueChanged(float ignoredValue)
    {
        if (!sendWhileChanging)
        {
            return;
        }

        SendCurrentValue("ValueChanged", false);
    }

    /// <summary>
    /// Drag 終了時に最終値を必ず送る。
    /// </summary>
    public void OnSliderDragEnded()
    {
        if (!sendOnDragEnded)
        {
            return;
        }

        SendCurrentValue("DragEnded", true);
    }

    /// <summary>
    /// +/- Button または StepController から呼ぶ。
    /// StepController の更新完了を待つため、次フレームで現在値を読む。
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
        SendCurrentValue("Manual", true);
    }

    private void SendCurrentValue(string reason, bool forceSend)
    {
        ResolveReferences();

        if (sliderValue == null)
        {
            Debug.LogWarning("[Lighting Slider CMD] SliderValue is not assigned. object=" + gameObject.name);
            return;
        }

        latestValue = Mathf.Clamp01(sliderValue.Value);
        SendValue(latestValue, reason, forceSend);
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
        // StepController / HorizontalSliderValue の更新完了を待つ。
        yield return null;

        delayedSendCoroutine = null;
        SendCurrentValue(reason, true);
    }

    private bool CanSendRealtime(float value)
    {
        if (minSendIntervalSec > 0f)
        {
            if (Time.unscaledTime - lastSendTime < minSendIntervalSec)
            {
                return false;
            }
        }

        if (hasSentValue)
        {
            if (Mathf.Abs(value - lastSentValue) < minValueDelta)
            {
                return false;
            }
        }

        return true;
    }

    private void SendValue(float value, string reason, bool forceSend)
    {
        ResolveReferences();

        value = Mathf.Clamp01(value);

        if (!forceSend && !CanSendRealtime(value))
        {
            return;
        }

        if (commandBridge == null)
        {
            Debug.LogWarning("[Lighting Slider CMD] CommandBridge is not assigned. object=" + gameObject.name);
            return;
        }

        lastSendTime = Time.unscaledTime;
        lastSentValue = value;
        hasSentValue = true;

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
