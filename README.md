using UnityEngine;

public class KinemaMockKeyboardInput : MonoBehaviour
{
    [SerializeField] private KinemaMockDisplayController controller;

    [Header("Keys")]
    [SerializeField] private KeyCode ignToggleKey = KeyCode.Alpha0;
    [SerializeField] private KeyCode ignToggleSubKey = KeyCode.I;
    [SerializeField] private KeyCode parkingKey = KeyCode.P;
    [SerializeField] private KeyCode driveKey = KeyCode.D;
    [SerializeField] private KeyCode rearKey = KeyCode.R;

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        if (Input.GetKeyDown(ignToggleKey) || Input.GetKeyDown(ignToggleSubKey))
        {
            controller.ToggleIgn();
            return;
        }

        if (Input.GetKeyDown(parkingKey))
        {
            controller.ShiftP();
            return;
        }

        if (Input.GetKeyDown(driveKey))
        {
            controller.ShiftD();
            return;
        }

        if (Input.GetKeyDown(rearKey))
        {
            controller.ShiftR();
        }
    }
}
