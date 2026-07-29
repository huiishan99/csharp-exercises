using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;

namespace PushButtonSliderLite
{
    [DisallowMultipleComponent]
    public sealed class HapticPresetCommandEmitter
        : MonoBehaviour
    {
        private sealed class HapticPreset
        {
            public string key;

            public int pushSound;
            public int releaseSound;
            public int soundVolumeDefault;

            public string vibrationPattern;
        }

        private static readonly int[] DefaultPushSounds =
        {
            1,
            3,
            5,
            7,
            9,
            11
        };

        private static readonly int[] DefaultReleaseSounds =
        {
            3,
            5,
            7,
            9,
            11,
            1
        };

        private static readonly string[] DefaultPatterns =
        {
            "Set_A",
            "Set_B",
            "Set_C",
            "Set_D",
            "Set_E",
            "Set_F"
        };

        [Header("References")]
        [SerializeField]
        private ThemeButtonGroup hapticButtonGroup;

        [SerializeField]
        private global::KinemaCommandBridge commandBridge;

        [Header("External Config")]
        [SerializeField]
        private bool useJsonConfig = true;

        [FormerlySerializedAs("configRelativePath")]
        [SerializeField]
        private string configPath =
            "%USERPROFILE%\\Desktop\\backend_ver19"
            + "\\config\\haptic_presets.json";

        [SerializeField]
        private bool loadConfigOnEnable = true;

        [Header("Commands")]
        [SerializeField]
        private bool sendPushSoundCommand = true;

        [SerializeField]
        private bool sendReleaseSoundCommand = true;

        [SerializeField]
        private bool sendVibrationPatternCommand = true;

        [Header("Initial Selection")]
        [SerializeField]
        private bool sendInitialSelectionOnFirstEnable = true;

        [SerializeField]
        private bool sendCurrentSelectionOnEveryEnable = false;

        [SerializeField, Range(0, 5)]
        private int fallbackDefaultIndex = 0;

        [Header("Debug")]
        [SerializeField]
        private bool logSend = true;

        [SerializeField]
        private bool logConfig = true;

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

            SubscribeEvents();

            if (ShouldSendCurrentSelection())
            {
                StartDelayedCurrentSelectionSend();
            }
        }

        private void OnDisable()
        {
            UnsubscribeEvents();

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

        [ContextMenu("Send Current Haptic Selection")]
        public void SendCurrentSelection()
        {
            int index = ResolveCurrentIndex();

            SendHapticPreset(
                index,
                "ManualCurrentSelection"
            );
        }

        public void OnUserSelectedHaptic(int index)
        {
            SendHapticPreset(
                index,
                "UserSelectionChanged"
            );
        }

        private void SubscribeEvents()
        {
            if (hapticButtonGroup == null)
            {
                return;
            }

            hapticButtonGroup
                .onUserSelectedIndexChanged
                .RemoveListener(OnUserSelectedHaptic);

            hapticButtonGroup
                .onUserSelectedIndexChanged
                .AddListener(OnUserSelectedHaptic);
        }

        private void UnsubscribeEvents()
        {
            if (hapticButtonGroup == null)
            {
                return;
            }

            hapticButtonGroup
                .onUserSelectedIndexChanged
                .RemoveListener(OnUserSelectedHaptic);
        }

        private bool ShouldSendCurrentSelection()
        {
            if (sendCurrentSelectionOnEveryEnable)
            {
                return true;
            }

            return sendInitialSelectionOnFirstEnable
                && !hasSentInitialSelection;
        }

        private void StartDelayedCurrentSelectionSend()
        {
            if (initialSendRoutine != null)
            {
                StopCoroutine(initialSendRoutine);
            }

            initialSendRoutine =
                StartCoroutine(
                    SendCurrentSelectionNextFrame()
                );
        }

        private IEnumerator SendCurrentSelectionNextFrame()
        {
            // ThemeButtonGroup.Start()によるDefault選択を待つ。
            yield return null;

            initialSendRoutine = null;

            int index = ResolveCurrentIndex();

            SendHapticPreset(
                index,
                hasSentInitialSelection
                    ? "EnableCurrentSelection"
                    : "InitialDefaultSelection"
            );

            hasSentInitialSelection = true;
        }

        private int ResolveCurrentIndex()
        {
            ResolveReferences();

            if (hapticButtonGroup != null
                && hapticButtonGroup.SelectedIndex >= 0)
            {
                return Mathf.Clamp(
                    hapticButtonGroup.SelectedIndex,
                    0,
                    5
                );
            }

            return Mathf.Clamp(
                fallbackDefaultIndex,
                0,
                5
            );
        }

        private void SendHapticPreset(
            int index,
            string reason
        )
        {
            ResolveReferences();
            EnsureConfigLoaded();

            if (index < 0 || index > 5)
            {
                Debug.LogWarning(
                    "[Haptic CMD] Invalid Haptic index: "
                    + index
                );

                return;
            }

            if (commandBridge == null)
            {
                Debug.LogWarning(
                    "[Haptic CMD] KinemaCommandBridge "
                    + "is not assigned."
                );

                return;
            }

            HapticPreset preset =
                GetPresetOrDefault(index);

            if (logSend)
            {
                Debug.Log(
                    "[Haptic CMD] key="
                    + preset.key
                    + " pushSound="
                    + preset.pushSound
                    + " releaseSound="
                    + preset.releaseSound
                    + " defaultVolume="
                    + preset.soundVolumeDefault
                    + " pattern="
                    + preset.vibrationPattern
                    + " reason="
                    + reason
                );
            }

            // 最新仕様では1つのHaptic選択につき
            // Push Sound / Release Sound / Vibrationの
            // 3 Commandを送信する。

            if (sendPushSoundCommand)
            {
                commandBridge.SendHvacPushSoundCommand(
                    preset.pushSound,
                    preset.soundVolumeDefault
                );
            }

            if (sendReleaseSoundCommand)
            {
                commandBridge.SendHvacReleaseSoundCommand(
                    preset.releaseSound,
                    preset.soundVolumeDefault
                );
            }

            if (sendVibrationPatternCommand)
            {
                commandBridge
                    .SendHvacVibrationPatternCommand(
                        preset.vibrationPattern
                    );
            }
        }

        // ========================================================
        // Config loading
        // ========================================================

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

            string fullPath =
                ResolveConfigFullPath(configPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning(
                    "[Haptic Config] File not found: "
                    + fullPath
                    + ". Default config is used."
                );

                LoadDefaultConfig();
                return;
            }

            try
            {
                string json =
                    File.ReadAllText(
                        fullPath,
                        Encoding.UTF8
                    );

                ParseJsonConfig(json);

                if (presets.Count == 0)
                {
                    Debug.LogWarning(
                        "[Haptic Config] No valid presets "
                        + "were found. Default config is used."
                    );

                    LoadDefaultConfig();
                    return;
                }

                FillMissingPresetsWithDefaults();

                if (logConfig)
                {
                    Debug.Log(
                        "[Haptic Config] Loaded from "
                        + fullPath
                        + ". presetCount="
                        + presets.Count
                    );
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[Haptic Config] Load failed: "
                    + exception.Message
                    + ". Default config is used."
                );

                LoadDefaultConfig();
            }
        }

        private string ResolveConfigFullPath(
            string path
        )
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Path.Combine(
                    Application.streamingAssetsPath,
                    "Haptic/haptic_presets.json"
                );
            }

            string trimmedPath = path.Trim();

            if (trimmedPath.StartsWith("\"")
                && trimmedPath.EndsWith("\"")
                && trimmedPath.Length >= 2)
            {
                trimmedPath =
                    trimmedPath.Substring(
                        1,
                        trimmedPath.Length - 2
                    );
            }

            string expandedPath =
                Environment.ExpandEnvironmentVariables(
                    trimmedPath
                );

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

            // JSON全体に他の設定が含まれていても、
            // Hap1～Hap6のObjectだけを抽出する。
            Regex presetRegex = new Regex(
                "\"(?<key>Hap(?<number>\\d+))\""
                + "\\s*:\\s*\\{(?<body>.*?)\\}",
                RegexOptions.IgnoreCase
                | RegexOptions.Singleline
            );

            MatchCollection matches =
                presetRegex.Matches(json);

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];

                string key =
                    match.Groups["key"].Value;

                string body =
                    match.Groups["body"].Value;

                if (!int.TryParse(
                        match.Groups["number"].Value,
                        out int oneBasedNumber
                    ))
                {
                    continue;
                }

                int index = oneBasedNumber - 1;

                if (index < 0 || index > 5)
                {
                    continue;
                }

                HapticPreset fallback =
                    CreateDefaultPreset(index);

                HapticPreset preset =
                    new HapticPreset
                    {
                        key = key,

                        pushSound = ReadIntOrDefault(
                            body,
                            fallback.pushSound,
                            "Push_Sound",
                            "PushSound",
                            "push_sound",
                            "pushSound",
                            // 旧JSONとの一時互換。
                            "Sound",
                            "sound"
                        ),

                        releaseSound = ReadIntOrDefault(
                            body,
                            fallback.releaseSound,
                            "Release_Sound",
                            "ReleaseSound",
                            "release_sound",
                            "releaseSound"
                        ),

                        soundVolumeDefault =
                            ReadIntOrDefault(
                                body,
                                fallback.soundVolumeDefault,
                                "Sound_Volume_Default",
                                "SoundVolumeDefault",
                                "sound_volume_default",
                                "default_volume",
                                "DefaultVolume"
                            ),

                        vibrationPattern =
                            ReadStringOrDefault(
                                body,
                                fallback.vibrationPattern,
                                "Vibration_Pattern",
                                "VibrationPattern",
                                "vibration_pattern",
                                "Pattern",
                                "pattern"
                            )
                    };

                NormalizePreset(
                    preset,
                    fallback
                );

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
                    "\""
                    + Regex.Escape(fieldName)
                    + "\"\\s*:\\s*(?<value>-?\\d+)",
                    RegexOptions.IgnoreCase
                    | RegexOptions.Singleline
                );

                Match match =
                    fieldRegex.Match(body);

                if (!match.Success)
                {
                    continue;
                }

                if (int.TryParse(
                        match.Groups["value"].Value,
                        out int value
                    ))
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
                    "\""
                    + Regex.Escape(fieldName)
                    + "\"\\s*:\\s*\"(?<value>[^\"]*)\"",
                    RegexOptions.IgnoreCase
                    | RegexOptions.Singleline
                );

                Match match =
                    fieldRegex.Match(body);

                if (!match.Success)
                {
                    continue;
                }

                string value =
                    match.Groups["value"].Value;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return defaultValue;
        }

        private void FillMissingPresetsWithDefaults()
        {
            for (int i = 0; i < 6; i++)
            {
                if (presets.ContainsKey(i))
                {
                    continue;
                }

                presets[i] =
                    CreateDefaultPreset(i);
            }
        }

        private void LoadDefaultConfig()
        {
            presets.Clear();

            for (int i = 0; i < 6; i++)
            {
                presets[i] =
                    CreateDefaultPreset(i);
            }

            if (logConfig)
            {
                Debug.Log(
                    "[Haptic Config] Default config loaded."
                );
            }
        }

        private HapticPreset GetPresetOrDefault(
            int index
        )
        {
            int safeIndex =
                Mathf.Clamp(index, 0, 5);

            if (presets.TryGetValue(
                    safeIndex,
                    out HapticPreset preset
                ))
            {
                return preset;
            }

            return CreateDefaultPreset(
                safeIndex
            );
        }

        private HapticPreset CreateDefaultPreset(
            int index
        )
        {
            int safeIndex =
                Mathf.Clamp(index, 0, 5);

            return new HapticPreset
            {
                key = "Hap" + (safeIndex + 1),

                pushSound =
                    DefaultPushSounds[safeIndex],

                releaseSound =
                    DefaultReleaseSounds[safeIndex],

                soundVolumeDefault = 128,

                vibrationPattern =
                    DefaultPatterns[safeIndex]
            };
        }

        private void NormalizePreset(
            HapticPreset preset,
            HapticPreset fallback
        )
        {
            if (preset == null || fallback == null)
            {
                return;
            }

            preset.pushSound =
                Mathf.Clamp(
                    preset.pushSound,
                    0,
                    255
                );

            preset.releaseSound =
                Mathf.Clamp(
                    preset.releaseSound,
                    0,
                    255
                );

            preset.soundVolumeDefault =
                Mathf.Clamp(
                    preset.soundVolumeDefault,
                    0,
                    255
                );

            if (string.IsNullOrWhiteSpace(
                    preset.vibrationPattern
                ))
            {
                preset.vibrationPattern =
                    fallback.vibrationPattern;
            }
        }

        private void ResolveReferences()
        {
            if (hapticButtonGroup == null)
            {
                hapticButtonGroup =
                    GetComponent<ThemeButtonGroup>();
            }

            if (commandBridge == null)
            {
                commandBridge =
                    FindFirstObjectByType<
                        global::KinemaCommandBridge
                    >();
            }
        }
    }
}
