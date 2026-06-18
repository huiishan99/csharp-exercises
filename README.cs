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

    // Backend処理負荷を抑えるため、0.05以上変化した場合のみドラッグ中Commandを送信する。
    [SerializeField] private float minCommandValueDelta = 0.05f;

    // 0なら時間間隔では制限しない。
    // 必要であれば 0.03〜0.05 を設定する。
    [SerializeField] private float minSendIntervalSec = 0f;

    // Drag終了時に最終値を必ず送る。
    [SerializeField] private bool sendOnDragEnded = true;

    [Header("Debug")]
    [SerializeField] private bool logSend = true;
    [SerializeField] private bool logSkippedValue = false;

    private float latestValue;
    private float lastSentValue;
    private float lastSendTime = -999f;
    private bool hasSentValue;
    private Coroutine delayedSendCoroutine;

    private void Awake()
    {
        ResolveReferences();

        latestValue = ReadCurrentSliderValue();

        // 既存Hardware状態との差分基準として、初期値を送信済み基準にする。
        // これにより、ドラッグ開始直後の0.001単位の微小変化では送信しない。
        lastSentValue = latestValue;
        hasSentValue = true;
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
    /// Slider drag中に呼ばれる。
    /// UnityEventのfloat引数がStatic 0になる場合があるため、
    /// 渡されたvalueは使わずHorizontalSliderValue.Valueを直接読む。
    /// </summary>
    public void OnSliderValueChanged(float ignoredValue)
    {
        latestValue = ReadCurrentSliderValue();

        if (!sendWhileChanging)
        {
            return;
        }

        if (!CanSendRealtime(latestValue))
        {
            if (logSkippedValue)
            {
                Debug.Log(
                    "[Lighting Slider CMD] Skip "
                    + gameObject.name
                    + " type="
                    + commandType
                    + " value="
                    + latestValue.ToString("0.###")
                    + " lastSent="
                    + lastSentValue.ToString("0.###")
                    + " delta="
                    + Mathf.Abs(latestValue - lastSentValue).ToString("0.###")
                );
            }

            return;
        }

        SendValue(latestValue, "ValueChanged", false);
    }

    /// <summary>
    /// Drag終了時に最終値を必ず送る。
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
    /// 渡されたvalueは信用せず、次フレームでHorizontalSliderValueから再取得する。
    /// </summary>
    public void SendValueImmediately(float ignoredValue)
    {
        RequestSendCurrentValueNextFrame("StepButton");
    }

    /// <summary>
    /// Button OnClickなど、floatを渡せない場合はこちらを使う。
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
        latestValue = ReadCurrentSliderValue();
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

        if (!hasSentValue)
        {
            return true;
        }

        float effectiveDelta = Mathf.Max(0f, minCommandValueDelta);
        float currentDelta = Mathf.Abs(value - lastSentValue);

        return currentDelta >= effectiveDelta;
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
                + " threshold="
                + minCommandValueDelta.ToString("0.###")
            );
        }

        if (commandType == LightingSliderCommandType.Brightness)
        {
            commandBridge.SendLightingBrightnessCommand(value);
            return;
        }

        commandBridge.SendLightingSaturationCommand(value);
    }

    private float ReadCurrentSliderValue()
    {
        ResolveReferences();

        if (sliderValue == null)
        {
            Debug.LogWarning("[Lighting Slider CMD] SliderValue is not assigned. object=" + gameObject.name);
            return latestValue;
        }

        return Mathf.Clamp01(sliderValue.Value);
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
