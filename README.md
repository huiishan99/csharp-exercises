using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class KinemaMockKeyboardInput : MonoBehaviour
{
    [SerializeField] private KinemaMockDisplayController controller;

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        if (IsIgnTogglePressed())
        {
            controller.ToggleIgn();
            return;
        }

        if (IsParkingPressed())
        {
            controller.ShiftP();
            return;
        }

        if (IsDrivePressed())
        {
            controller.ShiftD();
            return;
        }

        if (IsRearPressed())
        {
            controller.ShiftR();
        }
    }

    private bool IsIgnTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard.digit0Key.wasPressedThisFrame
            || keyboard.numpad0Key.wasPressedThisFrame
            || keyboard.iKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Alpha0)
            || Input.GetKeyDown(KeyCode.Keypad0)
            || Input.GetKeyDown(KeyCode.I);
#else
        return false;
#endif
    }

    private bool IsParkingPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard.pKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.P);
#else
        return false;
#endif
    }

    private bool IsDrivePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard.dKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.D);
#else
        return false;
#endif
    }

    private bool IsRearPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }
}
