using UnityEngine;

public class CanvasEventCameraSwitcher : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private KinemaMockDisplayController displayController;

    [Header("Event Cameras")]
    [SerializeField] private Camera driverEventCamera;
    [SerializeField] private Camera passengerEventCamera;
    [SerializeField] private Camera fallbackEventCamera;

    [Header("Update")]
    [SerializeField] private bool updateEveryFrame = true;
    [SerializeField] private bool logSwitch = true;

    private Camera currentCamera;

    private void Awake()
    {
        ResolveReferences();
        ApplyByCurrentMode();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyByCurrentMode();
    }

    private void LateUpdate()
    {
        if (!updateEveryFrame)
        {
            return;
        }

        ApplyByCurrentMode();
    }

    public void ApplyByCurrentMode()
    {
        ResolveReferences();

        if (targetCanvas == null)
        {
            return;
        }

        Camera nextCamera = GetCameraForCurrentMode();

        if (nextCamera == null)
        {
            return;
        }

        if (currentCamera == nextCamera && targetCanvas.worldCamera == nextCamera)
        {
            return;
        }

        currentCamera = nextCamera;
        targetCanvas.worldCamera = nextCamera;

        if (logSwitch)
        {
            Debug.Log("[CanvasEventCamera] Event Camera = " + nextCamera.name);
        }
    }

    private Camera GetCameraForCurrentMode()
    {
        if (displayController == null)
        {
            return fallbackEventCamera;
        }

        switch (displayController.CurrentDisplayMode)
        {
            case KinemaMockDisplayMode.Full:
                return driverEventCamera != null ? driverEventCamera : fallbackEventCamera;

            case KinemaMockDisplayMode.Half:
            case KinemaMockDisplayMode.RearView:
                return passengerEventCamera != null ? passengerEventCamera : fallbackEventCamera;

            case KinemaMockDisplayMode.Opening:
            case KinemaMockDisplayMode.Close:
            default:
                return fallbackEventCamera != null ? fallbackEventCamera : driverEventCamera;
        }
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (displayController == null)
        {
            displayController = FindFirstObjectByType<KinemaMockDisplayController>();
        }
    }
}
