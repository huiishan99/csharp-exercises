using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PushButtonSliderLite
{
    public enum LightingLevelSpriteMode
    {
        Brightness,
        Saturation
    }

    /// <summary>
    /// 1つのThemeに対応するSaturation画像セット。
    /// levelSprites[0] = 0%
    /// levelSprites[10] = 100%
    /// </summary>
    [Serializable]
    public sealed class SaturationThemeSpriteSet
    {
        [Tooltip("Inspector確認用のTheme名。Commandには使用しない。")]
        public string themeName;

        [Tooltip("0%, 10% ... 100% の順で11枚設定する。")]
        public Sprite[] levelSprites = new Sprite[11];
    }

    /// <summary>
    /// Brightness / Saturationの現在値に応じて、
    /// 0%～100%の離散Spriteを表示する。
    ///
    /// Brightness:
    /// 11枚の共通Spriteを使用する。
    ///
    /// Saturation:
    /// Theme indexとlevel indexから6×11枚のSpriteを選択する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LightingLevelSpriteView : MonoBehaviour
    {
        private const int LevelCount = 11;
        private const int MaxLevelIndex = 10;

        [Header("Mode")]
        [SerializeField]
        private LightingLevelSpriteMode mode =
            LightingLevelSpriteMode.Brightness;

        [Header("References")]
        [SerializeField] private HorizontalSliderValue sliderValue;
        [SerializeField] private HorizontalSliderStepController stepController;
        [SerializeField] private Image targetImage;

        [Header("Theme Reference - Saturation Only")]
        [Tooltip("Saturationの場合、HapticGroupではなくLighting用ThemeGroupを設定する。")]
        [SerializeField] private ThemeButtonGroup themeButtonGroup;

        [SerializeField, Range(0, 5)]
        private int fallbackThemeIndex = 0;

        [Header("Brightness Sprites")]
        [Tooltip("0%, 10% ... 100% の順で11枚設定する。")]
        [SerializeField]
        private Sprite[] brightnessLevelSprites =
            new Sprite[LevelCount];

        [Header("Saturation Sprites")]
        [Tooltip("ThemeButtonGroupと同じ順番で6セット設定する。")]
        [SerializeField]
        private SaturationThemeSpriteSet[] saturationThemeSprites =
            new SaturationThemeSpriteSet[6];

        [Header("Image Settings")]
        [SerializeField] private bool disableTargetRaycast = true;

        [Header("Debug")]
        [SerializeField] private bool logMissingSprite = true;
        [SerializeField] private bool logAppliedSprite = false;

        private Coroutine refreshRoutine;

        private void Awake()
        {
            ResolveReferences();
            ApplyImageSettings();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplyImageSettings();
            SubscribeEvents();

            // 現在値を即時反映する。
            RefreshCurrentSprite();

            // ThemeButtonGroup.Start()によるDefault選択完了後にも再反映する。
            RequestRefreshNextFrame();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();

            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
                refreshRoutine = null;
            }
        }

        private void OnValidate()
        {
            fallbackThemeIndex =
                Mathf.Clamp(fallbackThemeIndex, 0, 5);

            ApplyImageSettings();
        }

        [ContextMenu("Refresh Current Sprite")]
        public void RefreshCurrentSprite()
        {
            ResolveReferences();

            if (sliderValue == null || targetImage == null)
            {
                return;
            }

            ApplySprite(
                sliderValue.Value,
                ResolveCurrentThemeIndex()
            );
        }

        private void OnStepValueChanged(float value)
        {
            ApplySprite(
                value,
                ResolveCurrentThemeIndex()
            );
        }

        private void OnThemeChanged(int themeIndex)
        {
            if (mode != LightingLevelSpriteMode.Saturation)
            {
                return;
            }

            float currentValue = sliderValue == null
                ? 0f
                : sliderValue.Value;

            ApplySprite(currentValue, themeIndex);
        }

        private void ApplySprite(float value, int themeIndex)
        {
            if (targetImage == null)
            {
                return;
            }

            int levelIndex = ConvertValueToLevelIndex(value);

            Sprite nextSprite = mode
                == LightingLevelSpriteMode.Brightness
                    ? GetBrightnessSprite(levelIndex)
                    : GetSaturationSprite(themeIndex, levelIndex);

            if (nextSprite == null)
            {
                if (logMissingSprite)
                {
                    Debug.LogWarning(
                        "[LightingLevelSpriteView] Sprite is not assigned."
                        + " object="
                        + gameObject.name
                        + " mode="
                        + mode
                        + " themeIndex="
                        + themeIndex
                        + " levelIndex="
                        + levelIndex
                    );
                }

                // 未設定時に現在の画像を消さない。
                return;
            }

            targetImage.sprite = nextSprite;
            targetImage.enabled = true;

            if (logAppliedSprite)
            {
                Debug.Log(
                    "[LightingLevelSpriteView] Applied."
                    + " object="
                    + gameObject.name
                    + " mode="
                    + mode
                    + " themeIndex="
                    + themeIndex
                    + " levelIndex="
                    + levelIndex
                    + " sprite="
                    + nextSprite.name
                );
            }
        }

        private int ConvertValueToLevelIndex(float value)
        {
            float normalizedValue = Mathf.Clamp01(value);

            return Mathf.Clamp(
                Mathf.RoundToInt(normalizedValue * MaxLevelIndex),
                0,
                MaxLevelIndex
            );
        }

        private Sprite GetBrightnessSprite(int levelIndex)
        {
            if (brightnessLevelSprites == null)
            {
                return null;
            }

            if (levelIndex < 0
                || levelIndex >= brightnessLevelSprites.Length)
            {
                return null;
            }

            return brightnessLevelSprites[levelIndex];
        }

        private Sprite GetSaturationSprite(
            int themeIndex,
            int levelIndex
        )
        {
            if (saturationThemeSprites == null
                || saturationThemeSprites.Length == 0)
            {
                return null;
            }

            int safeThemeIndex = Mathf.Clamp(
                themeIndex,
                0,
                saturationThemeSprites.Length - 1
            );

            SaturationThemeSpriteSet themeSet =
                saturationThemeSprites[safeThemeIndex];

            if (themeSet == null
                || themeSet.levelSprites == null)
            {
                return null;
            }

            if (levelIndex < 0
                || levelIndex >= themeSet.levelSprites.Length)
            {
                return null;
            }

            return themeSet.levelSprites[levelIndex];
        }

        private int ResolveCurrentThemeIndex()
        {
            if (mode != LightingLevelSpriteMode.Saturation)
            {
                return 0;
            }

            if (themeButtonGroup != null
                && themeButtonGroup.SelectedIndex >= 0)
            {
                return themeButtonGroup.SelectedIndex;
            }

            return fallbackThemeIndex;
        }

        private void SubscribeEvents()
        {
            if (stepController != null)
            {
                stepController.onValueChangedByStep.RemoveListener(
                    OnStepValueChanged
                );

                stepController.onValueChangedByStep.AddListener(
                    OnStepValueChanged
                );
            }

            if (mode == LightingLevelSpriteMode.Saturation
                && themeButtonGroup != null)
            {
                themeButtonGroup.onSelectedIndexChanged.RemoveListener(
                    OnThemeChanged
                );

                themeButtonGroup.onSelectedIndexChanged.AddListener(
                    OnThemeChanged
                );
            }
        }

        private void UnsubscribeEvents()
        {
            if (stepController != null)
            {
                stepController.onValueChangedByStep.RemoveListener(
                    OnStepValueChanged
                );
            }

            if (themeButtonGroup != null)
            {
                themeButtonGroup.onSelectedIndexChanged.RemoveListener(
                    OnThemeChanged
                );
            }
        }

        private void RequestRefreshNextFrame()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
            }

            refreshRoutine =
                StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;

            refreshRoutine = null;
            RefreshCurrentSprite();
        }

        private void ApplyImageSettings()
        {
            if (targetImage == null)
            {
                return;
            }

            if (disableTargetRaycast)
            {
                targetImage.raycastTarget = false;
            }
        }

        private void ResolveReferences()
        {
            if (sliderValue == null)
            {
                sliderValue =
                    GetComponent<HorizontalSliderValue>();
            }

            if (stepController == null)
            {
                stepController =
                    GetComponent<HorizontalSliderStepController>();
            }
        }
    }
}
