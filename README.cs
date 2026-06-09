using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class KinemaMockKeyboardInput : MonoBehaviour
{
    [SerializeField] private KinemaMockDisplayController controller;
    [SerializeField] private KinemaCommandBridge commandBridge;

    private void Awake()
    {
        if (commandBridge == null)
        {
            commandBridge = FindFirstObjectByType<KinemaCommandBridge>();
        }
    }

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        if (IsIgnTogglePressed())
        {
            HandleIgnToggle();
            return;
        }

        if (IsAutoPressed())
        {
            controller.ToggleAutoPopup();
            return;
        }

        if (IsParkingPressed())
        {
            HandleParking();
            return;
        }

        if (IsDrivePressed())
        {
            HandleDrive();
            return;
        }

        if (IsRearPressed())
        {
            HandleRear();
        }
    }

    private void HandleIgnToggle()
    {
        bool wasIgnOn = controller.IsIgnOn;

        controller.ToggleIgn();

        if (commandBridge == null)
        {
            return;
        }

        if (!wasIgnOn && controller.IsIgnOn)
        {
            // 実機Displayを閉状態から上げるため、IG ON時はFull要求を送る。
            commandBridge.SendFullModeCommand();
            return;
        }

        if (wasIgnOn && !controller.IsIgnOn)
        {
            // 実機Displayを下げて閉じる。
            commandBridge.SendCloseModeCommand();
        }
    }

    private void HandleParking()
    {
        if (!CanSendShiftCommand())
        {
            controller.ShiftP();
            return;
        }

        controller.ShiftP();

        if (commandBridge != null)
        {
            commandBridge.SendFullModeCommand();
        }
    }

    private void HandleDrive()
    {
        if (!CanSendShiftCommand())
        {
            controller.ShiftD();
            return;
        }

        controller.ShiftD();

        if (commandBridge != null)
        {
            commandBridge.SendHalfModeCommand();
        }
    }

    private void HandleRear()
    {
        if (!CanSendShiftCommand())
        {
            controller.ShiftR();
            return;
        }

        controller.ShiftR();

        if (commandBridge != null)
        {
            commandBridge.SendHalfModeCommand();
        }
    }

    private bool CanSendShiftCommand()
    {
        if (!controller.IsIgnOn)
        {
            return false;
        }

        if (controller.CurrentDisplayMode == KinemaMockDisplayMode.Opening)
        {
            return false;
        }

        return true;
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

    private bool IsAutoPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard.aKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.A);
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
