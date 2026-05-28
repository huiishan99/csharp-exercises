using System;
using UnityEngine;
using UnityEngine.Events;

namespace PushButtonSliderLite
{
    /// <summary>
    /// 只负责用固定步长调整 HorizontalSliderValue。
    /// 不负责 Pointer 拖拽，也不控制 Slider glow。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HorizontalSliderStepController : MonoBehaviour
    {
        [Serializable]
        public sealed class FloatEvent : UnityEvent<float> { }

        [Header("目标 Slider")]
        [SerializeField] private HorizontalSliderValue sliderValue;

        [Header("可选：只用于同步隐藏状态下的 Glow 位置，不会显示 Glow")]
        [SerializeField] private SliderDragVisualEffect visualEffect;

        [Header("步进参数")]
        [SerializeField, Range(0.001f, 1f)] private float step = 0.05f;

        [Header("数值变化事件")]
        public FloatEvent onValueChangedByStep = new FloatEvent();

        private void Awake()
        {
            if (sliderValue == null)
                sliderValue = GetComponent<HorizontalSliderValue>();

            if (visualEffect == null)
                visualEffect = GetComponent<SliderDragVisualEffect>();
        }

        /// <summary>
        /// 给 + 按钮的 OnClick 绑定这个方法。
        /// </summary>
        public void Increase()
        {
            AddStep(1f);
        }

        /// <summary>
        /// 给 - 按钮的 OnClick 绑定这个方法。
        /// </summary>
        public void Decrease()
        {
            AddStep(-1f);
        }

        /// <summary>
        /// 外部也可以直接传入方向：1 表示增加，-1 表示减少。
        /// </summary>
        public void AddStep(float direction)
        {
            if (sliderValue == null)
                return;

            float previousValue = sliderValue.Value;
            float nextValue = previousValue + step * Mathf.Sign(direction);
            sliderValue.SetValue(nextValue);

            if (visualEffect != null)
                visualEffect.SyncGlowPosition();

            if (!Mathf.Approximately(previousValue, sliderValue.Value))
                onValueChangedByStep.Invoke(sliderValue.Value);
        }

        public void SetStep(float newStep)
        {
            step = Mathf.Clamp(newStep, 0.001f, 1f);
        }
    }
}
