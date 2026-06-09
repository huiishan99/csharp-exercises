using System.Collections;
using UnityEngine;

public enum KinemaMockDisplayMode
{
    Close,
    Opening,
    Full,
    Half,
    RearView
}

public class KinemaMockDisplayController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject screenViewportRoot;
    [SerializeField] private GameObject sourcePanelObject;

    [Header("Managers")]
    [SerializeField] private DemoScreenViewport screenViewport;
    [SerializeField] private DemoPageSwitcher pageSwitcher;
    [SerializeField] private DemoSourcePanel sourcePanel;
    [SerializeField] private KinemaMockPopupController popupController;

    [Header("Command")]
    [SerializeField] private KinemaCommandBridge commandBridge;
    [SerializeField] private bool sendMechaCommand = true;

    [Header("Page")]
    [SerializeField] private DemoPageId openingPage = DemoPageId.Welcome;
    [SerializeField] private DemoPageId drivePage = DemoPageId.NormalDrive;
    [SerializeField] private DemoPageId rearPage = DemoPageId.RearView;

    [Header("Parking")]
    [SerializeField] private DemoSourceId parkingDefaultSource = DemoSourceId.Setting;

    [Header("Opening")]
    [SerializeField] private float openingDuration = 3.5f;

    public KinemaMockDisplayMode CurrentDisplayMode { get; private set; }
    public bool IsIgnOn { get; private set; }

    private Coroutine openingCoroutine;

    private void Awake()
    {
        if (commandBridge == null)
        {
            commandBridge = FindFirstObjectByType<KinemaCommandBridge>();
        }
    }

    private void Start()
    {
        ApplyCloseMode();
    }

    public void ToggleIgn()
    {
        if (IsIgnOn)
        {
            IgnOff();
            return;
        }

        IgnOn();
    }

    public void IgnOn()
    {
        if (IsIgnOn)
        {
            return;
        }

        IsIgnOn = true;
        EnterOpeningMode();
    }

    public void IgnOff()
    {
        if (!IsIgnOn)
        {
            return;
        }

        IsIgnOn = false;
        StopOpeningCoroutine();
        ApplyCloseMode();
        SendCloseModeCommand();
    }

    public void ShiftP()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        EnterFullMode(true);
    }

    public void ShiftD()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        EnterHalfMode(true);
    }

    public void ShiftR()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        EnterRearViewMode(true);
    }

    public void ToggleAutoPopup()
    {
        if (!IsIgnOn)
        {
            return;
        }

        if (CurrentDisplayMode != KinemaMockDisplayMode.Full)
        {
            HidePopup();
            return;
        }

        if (popupController != null)
        {
            popupController.TogglePopup();
        }
    }

    private bool CanAcceptShiftInput()
    {
        if (!IsIgnOn)
        {
            return false;
        }

        if (CurrentDisplayMode == KinemaMockDisplayMode.Opening)
        {
            return false;
        }

        return true;
    }

    private void ApplyCloseMode()
    {
        CurrentDisplayMode = KinemaMockDisplayMode.Close;

        HidePopup();

        if (sourcePanel != null)
        {
            sourcePanel.ResetFullSource(parkingDefaultSource);
        }

        SetActive(screenViewportRoot, false);
        SetActive(sourcePanelObject, false);
    }

    private void EnterOpeningMode()
    {
        StopOpeningCoroutine();

        CurrentDisplayMode = KinemaMockDisplayMode.Opening;

        HidePopup();

        SetActive(screenViewportRoot, true);
        SetActive(sourcePanelObject, false);

        if (screenViewport != null)
        {
            screenViewport.SetMode(DemoScreenOpenMode.SemiOpen);
        }

        if (pageSwitcher != null)
        {
            pageSwitcher.ShowPage(openingPage);
        }

        SendHalfModeCommand();

        openingCoroutine = StartCoroutine(OpeningRoutine());
    }

    private IEnumerator OpeningRoutine()
    {
        yield return new WaitForSeconds(openingDuration);

        if (!IsIgnOn)
        {
            yield break;
        }

        if (CurrentDisplayMode != KinemaMockDisplayMode.Opening)
        {
            yield break;
        }

        EnterFullMode(true);
    }

    private void EnterFullMode(bool shouldSendCommand)
    {
        CurrentDisplayMode = KinemaMockDisplayMode.Full;

        SetActive(screenViewportRoot, true);
        SetActive(sourcePanelObject, true);

        if (screenViewport != null)
        {
            screenViewport.SetMode(DemoScreenOpenMode.FullOpen);
        }

        if (sourcePanel != null)
        {
            sourcePanel.ApplyVehicleMode(DemoVehicleMode.Parking);
            sourcePanel.ShowCurrentFullSource();
        }
        else if (pageSwitcher != null)
        {
            pageSwitcher.ShowPage(DemoPageId.LightingColorChange);
        }

        if (shouldSendCommand)
        {
            SendFullModeCommand();
        }
    }

    private void EnterHalfMode(bool shouldSendCommand)
    {
        CurrentDisplayMode = KinemaMockDisplayMode.Half;

        HidePopup();

        SetActive(screenViewportRoot, true);
        SetActive(sourcePanelObject, true);

        if (screenViewport != null)
        {
            screenViewport.SetMode(DemoScreenOpenMode.SemiOpen);
        }

        if (pageSwitcher != null)
        {
            pageSwitcher.ShowPage(drivePage);
        }

        if (sourcePanel != null)
        {
            sourcePanel.ApplyVehicleMode(DemoVehicleMode.Drive);
        }

        if (shouldSendCommand)
        {
            SendHalfModeCommand();
        }
    }

    private void EnterRearViewMode(bool shouldSendCommand)
    {
        CurrentDisplayMode = KinemaMockDisplayMode.RearView;

        HidePopup();

        SetActive(screenViewportRoot, true);
        SetActive(sourcePanelObject, true);

        if (screenViewport != null)
        {
            screenViewport.SetMode(DemoScreenOpenMode.SemiOpen);
        }

        if (pageSwitcher != null)
        {
            pageSwitcher.ShowPage(rearPage);
        }

        if (sourcePanel != null)
        {
            sourcePanel.ApplyVehicleMode(DemoVehicleMode.Rear);
        }

        if (shouldSendCommand)
        {
            SendHalfModeCommand();
        }
    }

    private void SendFullModeCommand()
    {
        if (!sendMechaCommand || commandBridge == null)
        {
            return;
        }

        commandBridge.SendFullModeCommand();
    }

    private void SendHalfModeCommand()
    {
        if (!sendMechaCommand || commandBridge == null)
        {
            return;
        }

        commandBridge.SendHalfModeCommand();
    }

    private void SendCloseModeCommand()
    {
        if (!sendMechaCommand || commandBridge == null)
        {
            return;
        }

        commandBridge.SendCloseModeCommand();
    }

    private void HidePopup()
    {
        if (popupController == null)
        {
            return;
        }

        popupController.HidePopup();
    }

    private void StopOpeningCoroutine()
    {
        if (openingCoroutine == null)
        {
            return;
        }

        StopCoroutine(openingCoroutine);
        openingCoroutine = null;
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(isActive);
    }
}
