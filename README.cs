using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DemoSpeakerKeyboardInput : MonoBehaviour
{
    [SerializeField] private DemoSpeakerState speakerState;

    private void Awake()
    {
        if (speakerState == null)
        {
            speakerState = GetComponent<DemoSpeakerState>();
        }

        if (speakerState == null)
        {
            speakerState = FindFirstObjectByType<DemoSpeakerState>();
        }
    }

    private void Update()
    {
        if (speakerState == null)
        {
            return;
        }

        if (IsLeftSpeakerKeyDown())
        {
            speakerState.ToggleLeftSpeaker();
            return;
        }

        if (IsRightSpeakerKeyDown())
        {
            speakerState.ToggleRightSpeaker();
            return;
        }

        if (IsBothSpeakerKeyDown())
        {
            speakerState.ToggleBothSpeakers();
            return;
        }

        if (IsVolumeDownKeyDown())
        {
            speakerState.DecreaseVolume();
            return;
        }

        if (IsVolumeUpKeyDown())
        {
            speakerState.IncreaseVolume();
        }
    }

    private bool IsLeftSpeakerKeyDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            return true;
        }
#endif

        return false;
    }

    private bool IsRightSpeakerKeyDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            return true;
        }
#endif

        return false;
    }

    private bool IsBothSpeakerKeyDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            return true;
        }
#endif

        return false;
    }

    private bool IsVolumeDownKeyDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.commaKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            return true;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            return true;
        }
#endif

        return false;
    }

    private bool IsVolumeUpKeyDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.periodKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Period))
        {
            return true;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            return true;
        }
#endif

        return false;
    }
}
