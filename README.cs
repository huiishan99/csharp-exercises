using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PushButtonSliderLite
{
    [DisallowMultipleComponent]
    public sealed class ThemeSpriteApplier : MonoBehaviour
    {
        [Serializable]
        public sealed class ThemeSpriteSet
        {
            public Sprite sliderTrackSprite;
            public Sprite externalImageSprite;
        }

        [Header("主题按钮组")]
        [SerializeField] private ThemeButtonGroup buttonGroup;

        [Header("要被替换的 Image")]
        [SerializeField] private Image sliderTrackImage;
        [SerializeField] private Image externalImage;

        [Header("6组主题 Sprite：顺序对应 0-5")]
        [SerializeField] private ThemeSpriteSet[] themeSprites = new ThemeSpriteSet[6];

        private Coroutine applyRoutine;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (buttonGroup != null)
            {
                buttonGroup.onSelectedIndexChanged.RemoveListener(ApplyByIndex);
                buttonGroup.onSelectedIndexChanged.AddListener(ApplyByIndex);
            }

            RequestApplyCurrentSelection();
        }

        private void OnDisable()
        {
            if (buttonGroup != null)
            {
                buttonGroup.onSelectedIndexChanged.RemoveListener(ApplyByIndex);
            }

            if (applyRoutine != null)
            {
                StopCoroutine(applyRoutine);
                applyRoutine = null;
            }
        }

        public void ApplyByIndex(int index)
        {
            if (themeSprites == null || index < 0 || index >= themeSprites.Length)
            {
                return;
            }

            ThemeSpriteSet spriteSet = themeSprites[index];

            if (spriteSet == null)
            {
                return;
            }

            if (sliderTrackImage != null && spriteSet.sliderTrackSprite != null)
            {
                sliderTrackImage.sprite = spriteSet.sliderTrackSprite;
            }

            if (externalImage != null && spriteSet.externalImageSprite != null)
            {
                externalImage.sprite = spriteSet.externalImageSprite;
            }
        }

        private void RequestApplyCurrentSelection()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (applyRoutine != null)
            {
                StopCoroutine(applyRoutine);
            }

            applyRoutine = StartCoroutine(ApplyCurrentSelectionNextFrame());
        }

        private IEnumerator ApplyCurrentSelectionNextFrame()
        {
            yield return null;

            applyRoutine = null;

            if (buttonGroup == null)
            {
                yield break;
            }

            if (buttonGroup.SelectedIndex >= 0)
            {
                ApplyByIndex(buttonGroup.SelectedIndex);
            }
        }

        private void ResolveReferences()
        {
            if (buttonGroup == null)
            {
                buttonGroup = GetComponent<ThemeButtonGroup>();
            }
        }
    }
}
