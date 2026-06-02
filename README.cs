using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DemoAudioOutputKeyboardInput : MonoBehaviour
{
    [SerializeField] private DemoWindowsAudioOutputSwitcher outputSwitcher;

    private void Awake()
    {
        if (outputSwitcher == null)
        {
            outputSwitcher = GetComponent<DemoWindowsAudioOutputSwitcher>();
        }

        if (outputSwitcher == null)
        {
            outputSwitcher = FindFirstObjectByType<DemoWindowsAudioOutputSwitcher>();
        }
    }

    private void Update()
    {
        if (outputSwitcher == null)
        {
            return;
        }

        if (IsSwitchDeviceKeyDown())
        {
            outputSwitcher.SwitchToNextDevice();
        }
    }

    private bool IsSwitchDeviceKeyDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Z))
        {
            return true;
        }
#endif

        return false;
    }
}
