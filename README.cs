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

    [Header("System Sound")]
    [SerializeField] private KinemaSystemSoundPlayer systemSoundPlayer;
    [SerializeField] private bool playOpeningSound = true;
    [SerializeField] private bool playClosingSound = true;
    [SerializeField] private bool stopOpeningSoundWhenOpeningFinished = true;
    [SerializeField] private bool stopClosingSoundOnCloseStatus = true;

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
        ResolveReferences();
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
    /// IGN ON入力。
    /// half_mode_cmdとLED Power ON / Shifter Startを送信し、
    /// half_mode_sts受信後にOpening画面とOpening音を開始する。
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
        StopClosingSound();

        if (currentMechaState == KinemaMechaState.Half)
        {
            SendSystemStartRelatedCommands();
            ApplyOpeningViewAndStartTimer();
            return;
        }

        pendingMechaAction = PendingMechaAction.WaitHalfForOpening;
        LogState("IG_ON requested. Waiting half_mode_sts.");

        SendHalfModeCommand();
        SendSystemStartRelatedCommands();
    }

    /// <summary>
    /// IGN OFF入力。
    /// close_mode_cmdとLED Power OFF / Shifter Stopを送信し、
    /// close_mode_stsまでClosing音を再生する。
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

        PlayClosingSound();

        if (currentMechaState == KinemaMechaState.Close)
        {
            pendingMechaAction = PendingMechaAction.None;
            SendSystemStopRelatedCommands();
            ApplyCloseView();

            if (stopClosingSoundOnCloseStatus)
            {
                StopClosingSound();
            }

            return;
        }

        pendingMechaAction = PendingMechaAction.WaitClose;
        LogState("IG_OFF requested. Waiting close_mode_sts.");

        SendCloseModeCommand();
        SendSystemStopRelatedCommands();
    }

    public void ShiftP()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        RequestFullForParking();
    }

    public void ShiftD()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        RequestHalfForDrive();
    }

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

        if (stopClosingSoundOnCloseStatus)
        {
            StopClosingSound();
        }
    }

    public void OnMechaOtherModeStatus()
    {
        currentMechaState = KinemaMechaState.Other;
        pendingMechaAction = PendingMechaAction.None;
        StopOpeningCoroutine();
        StopAllSystemSounds();

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

        PlayOpeningSound();

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

        if (stopOpeningSoundWhenOpeningFinished)
        {
            StopOpeningSound();
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

        // Semi modeでは左側操作Panelを表示しない。
        SetActive(sourcePanelObject, false);

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

        // Rear modeもsemi-open表示のため、左側操作Panelを表示しない。
        SetActive(sourcePanelObject, false);

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

    private void SendSystemStartRelatedCommands()
    {
        if (!sendMechaCommand || commandBridge == null)
        {
            return;
        }

        commandBridge.SendSystemStartRelatedCommands();
    }

    private void SendSystemStopRelatedCommands()
    {
        if (!sendMechaCommand || commandBridge == null)
        {
            return;
        }

        commandBridge.SendSystemStopRelatedCommands();
    }

    private void PlayOpeningSound()
    {
        if (!playOpeningSound)
        {
            return;
        }

        ResolveReferences();

        if (systemSoundPlayer == null)
        {
            return;
        }

        systemSoundPlayer.PlayOpeningSound();
    }

    private void PlayClosingSound()
    {
        if (!playClosingSound)
        {
            return;
        }

        ResolveReferences();

        if (systemSoundPlayer == null)
        {
            return;
        }

        systemSoundPlayer.PlayClosingSound();
    }

    private void StopOpeningSound()
    {
        ResolveReferences();

        if (systemSoundPlayer == null)
        {
            return;
        }

        systemSoundPlayer.StopOpeningSound();
    }

    private void StopClosingSound()
    {
        ResolveReferences();

        if (systemSoundPlayer == null)
        {
            return;
        }

        systemSoundPlayer.StopClosingSound();
    }

    private void StopAllSystemSounds()
    {
        ResolveReferences();

        if (systemSoundPlayer == null)
        {
            return;
        }

        systemSoundPlayer.StopAllSystemSounds();
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

    private void ResolveReferences()
    {
        if (commandBridge == null)
        {
            commandBridge = FindFirstObjectByType<KinemaCommandBridge>();
        }

        if (systemSoundPlayer == null)
        {
            systemSoundPlayer = FindFirstObjectByType<KinemaSystemSoundPlayer>();
        }
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
