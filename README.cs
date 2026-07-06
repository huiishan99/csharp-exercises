using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            public string vibrationPattern;
        }

        [SerializeField] private ThemeButtonGroup hapticButtonGroup;
        [SerializeField] private global::KinemaCommandBridge commandBridge;

        [Header("Config")]
        [SerializeField] private bool useJsonConfig = true;

        // 外部絶対Pathも指定可能。
        // 例: %USERPROFILE%\Desktop\backend_ver19\config\haptic_presets.json
        [SerializeField] private string configPath =
            "%USERPROFILE%\\Desktop\\backend_ver19\\config\\haptic_presets.json";

        [SerializeField] private bool loadConfigOnEnable = true;

        [Header("Command")]
        [SerializeField] private bool sendSoundCommand = true;
        [SerializeField] private bool sendVibrationPatternCommand = true;

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
                    + " vibrationPattern="
                    + preset.vibrationPattern
                    + " reason="
                    + reason
                );
            }

            if (sendSoundCommand)
            {
                commandBridge.SendHvacSoundCommand(
                    preset.sound,
                    preset.soundVolumeDefault
                );
            }

            if (sendVibrationPatternCommand)
            {
                commandBridge.SendHvacVibrationPatternCommand(
                    preset.vibrationPattern
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

            string fullPath = ResolveConfigFullPath(configPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning(
                    "[Haptic Config] File not found: "
                    + fullPath
                    + ". Use default config."
                );

                LoadDefaultConfig();
                return;
            }

            try
            {
                string json = File.ReadAllText(fullPath, Encoding.UTF8);
                ParseJsonConfig(json);

                if (presets.Count == 0)
                {
                    Debug.LogWarning(
                        "[Haptic Config] No valid presets found. Use default config. path="
                        + fullPath
                    );

                    LoadDefaultConfig();
                    return;
                }

                if (logConfig)
                {
                    Debug.Log("[Haptic Config] Loaded from " + fullPath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Haptic Config] Load error: "
                    + exception.Message
                    + ". Use default config."
                );

                LoadDefaultConfig();
            }
        }

        private string ResolveConfigFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Path.Combine(
                    Application.streamingAssetsPath,
                    "Haptic/haptic_presets.json"
                );
            }

            string trimmedPath = path.Trim();

            if (trimmedPath.StartsWith("\"") && trimmedPath.EndsWith("\""))
            {
                trimmedPath = trimmedPath.Substring(1, trimmedPath.Length - 2);
            }

            string expandedPath = Environment.ExpandEnvironmentVariables(trimmedPath);

            if (Path.IsPathRooted(expandedPath))
            {
                return expandedPath;
            }

            return Path.Combine(
                Application.streamingAssetsPath,
                expandedPath
            );
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
                    sound = ReadIntOrDefault(
                        body,
                        fallback.sound,
                        "Sound",
                        "sound"
                    ),
                    soundVolumeDefault = ReadIntOrDefault(
                        body,
                        fallback.soundVolumeDefault,
                        "Sound_Volume_Default",
                        "SoundVolumeDefault",
                        "default_volume",
                        "DefaultVolume"
                    ),
                    vibrationPattern = ReadStringOrDefault(
                        body,
                        fallback.vibrationPattern,
                        "Vibration_Pattern",
                        "VibrationPattern",
                        "Pattern",
                        "pattern"
                    )
                };

                ClampPreset(preset);
                presets[index] = preset;
            }
        }

        private int ReadIntOrDefault(
            string body,
            int defaultValue,
            params string[] fieldNames
        )
        {
            if (fieldNames == null)
            {
                return defaultValue;
            }

            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldName = fieldNames[i];

                Regex fieldRegex = new Regex(
                    "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*(?<value>-?\\d+)",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline
                );

                Match match = fieldRegex.Match(body);

                if (!match.Success)
                {
                    continue;
                }

                if (int.TryParse(match.Groups["value"].Value, out int value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        private string ReadStringOrDefault(
            string body,
            string defaultValue,
            params string[] fieldNames
        )
        {
            if (fieldNames == null)
            {
                return defaultValue;
            }

            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldName = fieldNames[i];

                Regex fieldRegex = new Regex(
                    "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*\"(?<value>[^\"]*)\"",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline
                );

                Match match = fieldRegex.Match(body);

                if (!match.Success)
                {
                    continue;
                }

                string value = match.Groups["value"].Value;

                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return defaultValue;
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
                sound = safeIndex * 2 + 1,
                soundVolumeDefault = 128,
                vibrationPattern = "Set_" + GetPatternLetter(safeIndex)
            };
        }

        private string GetPatternLetter(int index)
        {
            int safeIndex = Mathf.Clamp(index, 0, 5);

            switch (safeIndex)
            {
                case 0:
                    return "A";

                case 1:
                    return "B";

                case 2:
                    return "C";

                case 3:
                    return "D";

                case 4:
                    return "E";

                case 5:
                    return "F";

                default:
                    return "A";
            }
        }

        private void ClampPreset(HapticPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            preset.sound = Mathf.Clamp(preset.sound, 0, 255);
            preset.soundVolumeDefault = Mathf.Clamp(preset.soundVolumeDefault, 0, 255);

            if (string.IsNullOrEmpty(preset.vibrationPattern))
            {
                preset.vibrationPattern = "Set_A";
            }
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
