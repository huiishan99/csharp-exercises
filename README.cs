using UnityEngine;

namespace PushButtonSliderLite
{
    public class LightingThemeCommandEmitter : MonoBehaviour
    {
        [SerializeField] private ThemeButtonGroup themeButtonGroup;
        [SerializeField] private global::KinemaCommandBridge commandBridge;

        [Header("Theme Names")]
        [SerializeField] private string[] themeNames =
        {
            "SmartDrive",
            "Exiting",
            "Working",
            "Gaming",
            "Movie",
            "Manual"
        };

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (themeButtonGroup != null)
            {
                themeButtonGroup.onUserSelectedIndexChanged.RemoveListener(OnUserSelectedTheme);
                themeButtonGroup.onUserSelectedIndexChanged.AddListener(OnUserSelectedTheme);
            }
        }

        private void OnDisable()
        {
            if (themeButtonGroup != null)
            {
                themeButtonGroup.onUserSelectedIndexChanged.RemoveListener(OnUserSelectedTheme);
            }
        }

        public void OnUserSelectedTheme(int selectedIndex)
        {
            if (selectedIndex < 0)
            {
                Debug.LogWarning("[Lighting CMD] Invalid theme index: " + selectedIndex);
                return;
            }

            string themeName = GetThemeName(selectedIndex);

            Debug.Log(
                "[Lighting CMD] Theme selected. index="
                + selectedIndex
                + " name="
                + themeName
            );

            if (commandBridge == null)
            {
                Debug.LogWarning("[Lighting CMD] CommandBridge is not assigned.");
                return;
            }

            commandBridge.SendLightingPresetCommand(selectedIndex);
        }

        private string GetThemeName(int index)
        {
            if (themeNames == null || index < 0 || index >= themeNames.Length)
            {
                return "Unknown";
            }

            return themeNames[index];
        }

        private void ResolveReferences()
        {
            if (themeButtonGroup == null)
            {
                themeButtonGroup = GetComponent<ThemeButtonGroup>();
            }

            if (commandBridge == null)
            {
                commandBridge = FindFirstObjectByType<global::KinemaCommandBridge>();
            }
        }
    }
}
