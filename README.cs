using UnityEngine;

namespace PushButtonSliderLite
{
    [DisallowMultipleComponent]
    public sealed class HapticPresetCommandEmitter : MonoBehaviour
    {
        [SerializeField] private ThemeButtonGroup hapticButtonGroup;
        [SerializeField] private global::KinemaCommandBridge commandBridge;

        [Header("Command")]
        [SerializeField] private bool sendVibrationCommand = true;
        [SerializeField] private bool sendSoundCommand = false;

        [Header("Initial")]
        [SerializeField] private bool sendCurrentOnEnable = false;

        [Header("Debug")]
        [SerializeField] private bool logSend = true;

        private readonly string[] presetNames =
        {
            "HAP-1 / Cadillac Like 1",
            "HAP-2 / Cadillac Like 2",
            "HAP-3 / Cadillac Like 3",
            "HAP-4 / Audi Like 1",
            "HAP-5 / Audi Like 2",
            "HAP-6 / Audi Like 3"
        };

        private bool hasEnabledOnce;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (hapticButtonGroup != null)
            {
                hapticButtonGroup.onUserSelectedIndexChanged.RemoveListener(OnUserSelectedHaptic);
                hapticButtonGroup.onUserSelectedIndexChanged.AddListener(OnUserSelectedHaptic);
            }

            if (sendCurrentOnEnable && hasEnabledOnce)
            {
                SendCurrentSelection();
            }

            hasEnabledOnce = true;
        }

        private void OnDisable()
        {
            if (hapticButtonGroup != null)
            {
                hapticButtonGroup.onUserSelectedIndexChanged.RemoveListener(OnUserSelectedHaptic);
            }
        }

        public void OnUserSelectedHaptic(int index)
        {
            SendHapticPreset(index, "UserSelected");
        }

        public void SendCurrentSelection()
        {
            ResolveReferences();

            if (hapticButtonGroup == null)
            {
                Debug.LogWarning("[Haptic CMD] HapticButtonGroup is not assigned.");
                return;
            }

            if (hapticButtonGroup.SelectedIndex < 0)
            {
                Debug.LogWarning("[Haptic CMD] No selected haptic preset.");
                return;
            }

            SendHapticPreset(hapticButtonGroup.SelectedIndex, "CurrentSelection");
        }

        private void SendHapticPreset(int index, string reason)
        {
            ResolveReferences();

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

            if (logSend)
            {
                Debug.Log(
                    "[Haptic CMD] index="
                    + index
                    + " name="
                    + GetPresetName(index)
                    + " reason="
                    + reason
                );
            }

            if (sendVibrationCommand)
            {
                commandBridge.SendHvacVibrationCommand(index);
            }

            if (sendSoundCommand)
            {
                commandBridge.SendHvacSoundCommand(index);
            }
        }

        private string GetPresetName(int index)
        {
            if (index < 0 || index >= presetNames.Length)
            {
                return "Unknown";
            }

            return presetNames[index];
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
