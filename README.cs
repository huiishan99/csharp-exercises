using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PushButtonSliderLite
{
    [DisallowMultipleComponent]
    public sealed class HapticPresetCommandEmitter : MonoBehaviour
    {
        private sealed class HapticPreset
        {
            public string key;
            public int sound;
            public int soundVolumeDefault;
            public int vibration;
            public int vibrationVolumeDefault;
        }

        [SerializeField] private ThemeButtonGroup hapticButtonGroup;
        [SerializeField] private global::KinemaCommandBridge commandBridge;

        [Header("Config")]
        [SerializeField] private bool useJsonConfig = true;
        [SerializeField] private string configRelativePath = "Haptic/haptic_presets.json";
        [SerializeField] private bool loadConfigOnEnable = true;

        [Header("Command")]
        [SerializeField] private bool sendVibrationCommand = true;
        [SerializeField] private bool sendSoundCommand = true;

        [Header("Initial Send")]
        [SerializeField] private bool sendInitialSelectionOnFirstEnable = true;
        [SerializeField] private bool sendCurrentSelectionOnEveryEnable = false;
        [SerializeField] private int fallbackDefaultIndex = 0;

        [Header("Debug")]
        [SerializeField] private bool logSend = true;
        [SerializeField] private bool logConfig = true;

        private readonly Dictionary<int, HapticPreset> presets =
            new Dictionary<int, HapticPreset>();

        private Coroutine initialSendRoutine;
        private bool hasSentInitialSelection;

        private void Awake()
        {
            ResolveReferences();
            LoadConfig(true);
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (loadConfigOnEnable)
            {
                LoadConfig(true);
            }

            if (hapticButtonGroup != null)
            {
                hapticButtonGroup.onUserSelectedIndexChanged.RemoveListener(OnUserSelectedHaptic);
                hapticButtonGroup.onUserSelectedIndexChanged.AddListener(OnUserSelectedHaptic);
            }

            if (ShouldSendInitialOrCurrent())
            {
                StartDelayedCurrentSelectionSend();
            }
        }

        private void OnDisable()
        {
            if (hapticButtonGroup != null)
            {
                hapticButtonGroup.onUserSelectedIndexChanged.RemoveListener(OnUserSelectedHaptic);
            }

            if (initialSendRoutine != null)
            {
                StopCoroutine(initialSendRoutine);
                initialSendRoutine = null;
            }
        }

        [ContextMenu("Reload Haptic Config")]
        public void ReloadConfig()
        {
            LoadConfig(true);
        }

        public void OnUserSelectedHaptic(int index)
        {
            SendHapticPreset(index, "UserSelectionChanged");
        }

        public void SendCurrentSelection()
        {
            int index = ResolveCurrentIndex();
            SendHapticPreset(index, "CurrentSelection");
        }

        private bool ShouldSendInitialOrCurrent()
        {
            if (sendCurrentSelectionOnEveryEnable)
            {
                return true;
            }

            if (sendInitialSelectionOnFirstEnable && !hasSentInitialSelection)
            {
                return true;
            }

            return false;
        }

        private void StartDelayedCurrentSelectionSend()
        {
            if (initialSendRoutine != null)
            {
                StopCoroutine(initialSendRoutine);
            }

            initialSendRoutine = StartCoroutine(SendCurrentSelectionNextFrame());
        }

        private IEnumerator SendCurrentSelectionNextFrame()
        {
            // ThemeButtonGroup.Start() のDefault選択処理が終わるのを待つ。
            yield return null;

            initialSendRoutine = null;

            int index = ResolveCurrentIndex();

            SendHapticPreset(
                index,
                hasSentInitialSelection ? "EnableCurrentSelection" : "InitialDefaultSelection"
            );

            hasSentInitialSelection = true;
        }

        private int ResolveCurrentIndex()
        {
            ResolveReferences();

            if (hapticButtonGroup != null && hapticButtonGroup.SelectedIndex >= 0)
            {
                return hapticButtonGroup.SelectedIndex;
            }

            return Mathf.Clamp(fallbackDefaultIndex, 0, 5);
        }

        private void SendHapticPreset(int index, string reason)
        {
            ResolveReferences();
            EnsureConfigLoaded();

            if (index < 0)
            {
                Debug.LogWarning("[Haptic CMD] Invalid index: " + index);
                return;
            }

            if (commandBridge == null)
            {
                Debug.LogWarning("[Haptic CMD] KinemaCommandBridge is not assigned.");
                return;
            }

            HapticPreset preset = GetPresetOrDefault(index);

            if (logSend)
            {
                Debug.Log(
                    "[Haptic CMD] key="
                    + preset.key
                    + " sound="
                    + preset.sound
                    + " soundVolume="
                    + preset.soundVolumeDefault
                    + " vibration="
                    + preset.vibration
                    + " vibrationVolume="
                    + preset.vibrationVolumeDefault
                    + " reason="
                    + reason
                );
            }

            if (sendVibrationCommand)
            {
                commandBridge.SendHvacVibrationCommand(
                    preset.vibration,
                    preset.vibrationVolumeDefault
                );
            }

            if (sendSoundCommand)
            {
                commandBridge.SendHvacSoundCommand(
                    preset.sound,
                    preset.soundVolumeDefault
                );
            }
        }

        private void EnsureConfigLoaded()
        {
            if (presets.Count > 0)
            {
                return;
            }

            LoadConfig(false);
        }

        private void LoadConfig(bool forceReload)
        {
            if (!forceReload && presets.Count > 0)
            {
                return;
            }

            presets.Clear();

            if (!useJsonConfig)
            {
                LoadDefaultConfig();
                return;
            }

            string fullPath = Path.Combine(
                Application.streamingAssetsPath,
                configRelativePath
            );

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("[Haptic Config] File not found: " + fullPath + ". Use default config.");
                LoadDefaultConfig();
                return;
            }

            string json = File.ReadAllText(fullPath);
            ParseJsonConfig(json);

            if (presets.Count == 0)
            {
                Debug.LogWarning("[Haptic Config] No valid presets found. Use default config.");
                LoadDefaultConfig();
                return;
            }

            if (logConfig)
            {
                Debug.Log("[Haptic Config] Loaded from " + fullPath);
            }
        }

        private void ParseJsonConfig(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            Regex presetRegex = new Regex(
                "\"(?<key>Hap(?<number>\\d+))\"\\s*:\\s*\\{(?<body>.*?)\\}",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            MatchCollection matches = presetRegex.Matches(json);

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];

                string key = match.Groups["key"].Value;
                string body = match.Groups["body"].Value;

                if (!int.TryParse(match.Groups["number"].Value, out int oneBasedNumber))
                {
                    continue;
                }

                int index = Mathf.Clamp(oneBasedNumber - 1, 0, 5);

                HapticPreset fallback = CreateDefaultPreset(index);

                HapticPreset preset = new HapticPreset
                {
                    key = key,
                    sound = ReadIntOrDefault(body, "Sound", fallback.sound),
                    soundVolumeDefault = ReadIntOrDefault(
                        body,
                        "Sound_Volume_Default",
                        fallback.soundVolumeDefault
                    ),
                    vibration = ReadIntOrDefault(body, "Vibration", fallback.vibration),
                    vibrationVolumeDefault = ReadIntOrDefault(
                        body,
                        "Vibration_Volume_Default",
                        fallback.vibrationVolumeDefault
                    )
                };

                ClampPreset(preset);
                presets[index] = preset;
            }
        }

        private int ReadIntOrDefault(string body, string fieldName, int defaultValue)
        {
            Regex fieldRegex = new Regex(
                "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*(?<value>-?\\d+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            Match match = fieldRegex.Match(body);

            if (!match.Success)
            {
                return defaultValue;
            }

            if (!int.TryParse(match.Groups["value"].Value, out int value))
            {
                return defaultValue;
            }

            return value;
        }

        private void LoadDefaultConfig()
        {
            presets.Clear();

            for (int i = 0; i < 6; i++)
            {
                presets[i] = CreateDefaultPreset(i);
            }

            if (logConfig)
            {
                Debug.Log("[Haptic Config] Loaded default config.");
            }
        }

        private HapticPreset GetPresetOrDefault(int index)
        {
            int safeIndex = Mathf.Clamp(index, 0, 5);

            if (presets.TryGetValue(safeIndex, out HapticPreset preset))
            {
                return preset;
            }

            return CreateDefaultPreset(safeIndex);
        }

        private HapticPreset CreateDefaultPreset(int index)
        {
            int safeIndex = Mathf.Clamp(index, 0, 5);

            return new HapticPreset
            {
                key = "Hap" + (safeIndex + 1),
                sound = safeIndex * 2,
                soundVolumeDefault = 128,
                vibration = safeIndex,
                vibrationVolumeDefault = 128
            };
        }

        private void ClampPreset(HapticPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            preset.sound = Mathf.Clamp(preset.sound, 0, 255);
            preset.soundVolumeDefault = Mathf.Clamp(preset.soundVolumeDefault, 0, 255);
            preset.vibration = Mathf.Clamp(preset.vibration, 0, 255);
            preset.vibrationVolumeDefault = Mathf.Clamp(preset.vibrationVolumeDefault, 0, 255);
        }

        private void ResolveReferences()
        {
            if (hapticButtonGroup == null)
            {
                hapticButtonGroup = GetComponent<ThemeButtonGroup>();
            }

            if (commandBridge == null)
            {
                commandBridge = FindFirstObjectByType<global::KinemaCommandBridge>();
            }
        }
    }
}
