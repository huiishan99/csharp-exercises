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
    private enum KinemaMechaState
    {
        Unknown,
        Close,
        Half,
        Full,
        Other
    }

    private enum PendingMechaAction
    {
        None,
        WaitHalfForOpening,
        WaitFullAfterWelcome,
        WaitFullForParking,
        WaitHalfForDrive,
        WaitHalfForRear,
        WaitClose
    }

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

    [Header("Debug")]
    [SerializeField] private bool logState = true;

    public KinemaMockDisplayMode CurrentDisplayMode { get; private set; }
    public bool IsIgnOn { get; private set; }

    private KinemaMechaState currentMechaState = KinemaMechaState.Close;
    private PendingMechaAction pendingMechaAction = PendingMechaAction.None;
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
        currentMechaState = KinemaMechaState.Close;
        pendingMechaAction = PendingMechaAction.None;
        ApplyCloseView();
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

    /// <summary>
    /// IG ON入力。ここではWelcomeをまだ表示しない。
    /// half_mode_cmdを送信し、half_mode_stsを待つ。
    /// </summary>
    public void IgnOn()
    {
        if (IsIgnOn)
        {
            return;
        }

        IsIgnOn = true;
        HidePopup();
        StopOpeningCoroutine();

        if (currentMechaState == KinemaMechaState.Half)
        {
            ApplyOpeningViewAndStartTimer();
            return;
        }

        pendingMechaAction = PendingMechaAction.WaitHalfForOpening;
        LogState("IG_ON requested. Waiting half_mode_sts.");
        SendHalfModeCommand();
    }

    /// <summary>
    /// IG OFF入力。close_mode_cmdを送信し、close_mode_stsを待つ。
    /// </summary>
    public void IgnOff()
    {
        if (!IsIgnOn && CurrentDisplayMode == KinemaMockDisplayMode.Close)
        {
            return;
        }

        IsIgnOn = false;
        StopOpeningCoroutine();
        HidePopup();

        if (currentMechaState == KinemaMechaState.Close)
        {
            pendingMechaAction = PendingMechaAction.None;
            ApplyCloseView();
            return;
        }

        pendingMechaAction = PendingMechaAction.WaitClose;
        LogState("IG_OFF requested. Waiting close_mode_sts.");
        SendCloseModeCommand();
    }

    /// <summary>
    /// P入力。full_mode_cmdを送信し、full_mode_stsを待つ。
    /// 既にFullなら画面だけ同期する。
    /// </summary>
    public void ShiftP()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        RequestFullForParking();
    }

    /// <summary>
    /// D入力。half_mode_cmdを送信し、half_mode_stsを待つ。
    /// 既にHalfなら直接NormalDrive表示に切り替える。
    /// </summary>
    public void ShiftD()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        RequestHalfForDrive();
    }

    /// <summary>
    /// R入力。half_mode_cmdを送信し、half_mode_stsを待つ。
    /// 既にHalfなら直接RearView表示に切り替える。
    /// </summary>
    public void ShiftR()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        RequestHalfForRear();
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

    public void OnMechaHalfModeStatus()
    {
        currentMechaState = KinemaMechaState.Half;
        LogState("Received half_mode_sts. Pending=" + pendingMechaAction);

        switch (pendingMechaAction)
        {
            case PendingMechaAction.WaitHalfForOpening:
                pendingMechaAction = PendingMechaAction.None;
                ApplyOpeningViewAndStartTimer();
                break;

            case PendingMechaAction.WaitHalfForDrive:
                pendingMechaAction = PendingMechaAction.None;
                ApplyHalfDriveView();
                break;

            case PendingMechaAction.WaitHalfForRear:
                pendingMechaAction = PendingMechaAction.None;
                ApplyRearView();
                break;

            default:
                // 想定外Statusは機械状態だけ同期し、画面は勝手に変えない。
                LogState("half_mode_sts received without matching pending action.");
                break;
        }
    }

    public void OnMechaFullModeStatus()
    {
        currentMechaState = KinemaMechaState.Full;
        LogState("Received full_mode_sts. Pending=" + pendingMechaAction);

        switch (pendingMechaAction)
        {
            case PendingMechaAction.WaitFullAfterWelcome:
            case PendingMechaAction.WaitFullForParking:
                pendingMechaAction = PendingMechaAction.None;
                ApplyFullView();
                break;

            default:
                LogState("full_mode_sts received without matching pending action.");
                break;
        }
    }

    public void OnMechaCloseModeStatus()
    {
        currentMechaState = KinemaMechaState.Close;
        LogState("Received close_mode_sts. Pending=" + pendingMechaAction);

        pendingMechaAction = PendingMechaAction.None;
        IsIgnOn = false;
        StopOpeningCoroutine();
        ApplyCloseView();
    }

    public void OnMechaOtherModeStatus()
    {
        currentMechaState = KinemaMechaState.Other;
        pendingMechaAction = PendingMechaAction.None;
        StopOpeningCoroutine();
        Debug.LogWarning("[KinemaDisplay] Received other_mode_sts. Pending action has been cleared.");
    }

    private bool CanAcceptShiftInput()
    {
        if (!IsIgnOn)
        {
            return false;
        }

        if (pendingMechaAction != PendingMechaAction.None)
        {
            LogState("Shift ignored because pending action exists: " + pendingMechaAction);
            return false;
        }

        if (CurrentDisplayMode == KinemaMockDisplayMode.Opening)
        {
            LogState("Shift ignored during Opening.");
            return false;
        }

        return true;
    }

    private void RequestFullForParking()
    {
        HidePopup();

        if (currentMechaState == KinemaMechaState.Full)
        {
            ApplyFullView();
            return;
        }

        pendingMechaAction = PendingMechaAction.WaitFullForParking;
        LogState("Full requested. Waiting full_mode_sts.");
        SendFullModeCommand();
    }

    private void RequestHalfForDrive()
    {
        HidePopup();

        if (currentMechaState == KinemaMechaState.Half)
        {
            ApplyHalfDriveView();
            return;
        }

        pendingMechaAction = PendingMechaAction.WaitHalfForDrive;
        LogState("Half for Drive requested. Waiting half_mode_sts.");
        SendHalfModeCommand();
    }

    private void RequestHalfForRear()
    {
        HidePopup();

        if (currentMechaState == KinemaMechaState.Half)
        {
            ApplyRearView();
            return;
        }

        pendingMechaAction = PendingMechaAction.WaitHalfForRear;
        LogState("Half for Rear requested. Waiting half_mode_sts.");
        SendHalfModeCommand();
    }

    private void ApplyCloseView()
    {
        CurrentDisplayMode = KinemaMockDisplayMode.Close;

        HidePopup();

        if (sourcePanel != null)
        {
            sourcePanel.ResetFullSource(parkingDefaultSource);
        }

        SetActive(screenViewportRoot, false);
        SetActive(sourcePanelObject, false);
        LogState("ApplyCloseView");
    }

    private void ApplyOpeningViewAndStartTimer()
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

        LogState("ApplyOpeningView. Start welcome timer.");
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

        pendingMechaAction = PendingMechaAction.WaitFullAfterWelcome;
        LogState("Welcome finished. Full requested. Waiting full_mode_sts.");
        SendFullModeCommand();
    }

    private void ApplyFullView()
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

        LogState("ApplyFullView");
    }

    private void ApplyHalfDriveView()
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

        LogState("ApplyHalfDriveView");
    }

    private void ApplyRearView()
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

        LogState("ApplyRearView");
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

    private void LogState(string message)
    {
        if (!logState)
        {
            return;
        }

        Debug.Log(
            "[KinemaDisplay] "
            + message
            + " | Display="
            + CurrentDisplayMode
            + " | Mecha="
            + currentMechaState
            + " | Pending="
            + pendingMechaAction
            + " | IG="
            + IsIgnOn
        );
    }
}
