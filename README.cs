using System;
using UnityEngine;
using UnityEngine.Events;

namespace PushButtonSliderLite
{
    /// <summary>
    /// +/- Button操作によって、HorizontalSliderValueを固定Stepで変更する。
    /// 現行SettingPageではdrag操作を使用せず、0.1単位の離散値として扱う。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HorizontalSliderStepController : MonoBehaviour
    {
        [Serializable]
        public sealed class FloatEvent : UnityEvent<float>
        {
        }

        [Header("Target Slider")]
        [SerializeField] private HorizontalSliderValue sliderValue;

        [Header("Optional Visual Effect")]
        [Tooltip("旧Slider Handle / Glowを使用しない場合はNoneでよい。")]
        [SerializeField] private SliderDragVisualEffect visualEffect;

        [Header("Step Settings")]
        [SerializeField, Range(0.001f, 1f)] private float step = 0.1f;

        [Tooltip("値をStep単位へ丸める。0.1の場合は0.0, 0.1 ... 1.0になる。")]
        [SerializeField] private bool quantizeValueToStep = true;

        [Tooltip("起動時にInspector上の初期値もStep単位へ丸める。")]
        [SerializeField] private bool normalizeInitialValue = true;

        [Header("Value Changed Event")]
        public FloatEvent onValueChangedByStep = new FloatEvent();

        public float Step
        {
            get { return step; }
        }

        private void Awake()
        {
            ResolveReferences();

            if (normalizeInitialValue && sliderValue != null)
            {
                sliderValue.SetValue(QuantizeValue(sliderValue.Value));
            }
        }

        private void OnValidate()
        {
            step = Mathf.Clamp(step, 0.001f, 1f);
        }

        /// <summary>
        /// + ButtonのOnClickに設定する。
        /// </summary>
        public void Increase()
        {
            AddStep(1f);
        }

        /// <summary>
        /// - ButtonのOnClickに設定する。
        /// </summary>
        public void Decrease()
        {
            AddStep(-1f);
        }

        /// <summary>
        /// directionが正の場合は加算、負の場合は減算する。
        /// </summary>
        public void AddStep(float direction)
        {
            ResolveReferences();

            if (sliderValue == null)
            {
                return;
            }

            if (Mathf.Approximately(direction, 0f))
            {
                return;
            }

            float previousValue = QuantizeValue(sliderValue.Value);

            float nextValue = previousValue
                + step * Mathf.Sign(direction);

            nextValue = QuantizeValue(nextValue);

            sliderValue.SetValue(nextValue);

            float appliedValue = QuantizeValue(sliderValue.Value);

            if (visualEffect != null)
            {
                visualEffect.SyncGlowPosition();
            }

            // 0.0で-を押した場合、1.0で+を押した場合は通知しない。
            if (Mathf.Approximately(previousValue, appliedValue))
            {
                return;
            }

            onValueChangedByStep.Invoke(appliedValue);
        }

        public void SetStep(float newStep)
        {
            step = Mathf.Clamp(newStep, 0.001f, 1f);

            ResolveReferences();

            if (sliderValue != null)
            {
                sliderValue.SetValue(QuantizeValue(sliderValue.Value));
            }
        }

        [ContextMenu("Normalize Current Value")]
        public void NormalizeCurrentValue()
        {
            ResolveReferences();

            if (sliderValue == null)
            {
                return;
            }

            sliderValue.SetValue(QuantizeValue(sliderValue.Value));
        }

        private float QuantizeValue(float inputValue)
        {
            float clampedValue = Mathf.Clamp01(inputValue);

            if (!quantizeValueToStep)
            {
                return clampedValue;
            }

            float safeStep = Mathf.Clamp(step, 0.001f, 1f);

            float quantizedValue =
                Mathf.Round(clampedValue / safeStep) * safeStep;

            // 0.30000004等がCommandへ送信されないよう、小数誤差を整理する。
            quantizedValue =
                Mathf.Round(quantizedValue * 1000f) / 1000f;

            return Mathf.Clamp01(quantizedValue);
        }

        private void ResolveReferences()
        {
            if (sliderValue == null)
            {
                sliderValue = GetComponent<HorizontalSliderValue>();
            }

            if (visualEffect == null)
            {
                visualEffect = GetComponent<SliderDragVisualEffect>();
            }
        }
    }
}
