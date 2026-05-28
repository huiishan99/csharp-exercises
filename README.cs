using System;
using UnityEngine;
using UnityEngine.UI;

namespace PushButtonSliderLite
{
    /// <summary>
    /// 只负责根据主题按钮 index 替换目标 Image 的 Sprite。
    /// 不负责按钮输入、不负责按钮组状态、不负责 Slider 数值。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThemeSpriteApplier : MonoBehaviour
    {
        [Serializable]
        public sealed class ThemeSpriteSet
        {
            [Tooltip("对应 Slider Track 的 Sprite")]
            public Sprite sliderTrackSprite;

            [Tooltip("对应外部 Image 的 Sprite")]
            public Sprite externalImageSprite;
        }

        [Header("主题按钮组")]
        [SerializeField] private ThemeButtonGroup buttonGroup;

        [Header("要被替换的 Image")]
        [SerializeField] private Image sliderTrackImage;
        [SerializeField] private Image externalImage;

        [Header("6组主题 Sprite：顺序对应按钮 1,2,3,4,5,6")]
        [SerializeField] private ThemeSpriteSet[] themeSprites = new ThemeSpriteSet[6];

        private void Awake()
        {
            if (buttonGroup == null)
                buttonGroup = GetComponent<ThemeButtonGroup>();
        }

        private void OnEnable()
        {
            if (buttonGroup != null)
                buttonGroup.onSelectedIndexChanged.AddListener(ApplyByIndex);
        }

        private void Start()
        {
            if (buttonGroup != null)
                buttonGroup.NotifyCurrentSelection();
        }

        private void OnDisable()
        {
            if (buttonGroup != null)
                buttonGroup.onSelectedIndexChanged.RemoveListener(ApplyByIndex);
        }

        /// <summary>
        /// index 从 0 开始。按钮1对应 index=0，按钮6对应 index=5。
        /// </summary>
        public void ApplyByIndex(int index)
        {
            if (themeSprites == null || index < 0 || index >= themeSprites.Length)
                return;

            ThemeSpriteSet spriteSet = themeSprites[index];
            if (spriteSet == null)
                return;

            if (sliderTrackImage != null && spriteSet.sliderTrackSprite != null)
                sliderTrackImage.sprite = spriteSet.sliderTrackSprite;

            if (externalImage != null && spriteSet.externalImageSprite != null)
                externalImage.sprite = spriteSet.externalImageSprite;
        }
    }
}
