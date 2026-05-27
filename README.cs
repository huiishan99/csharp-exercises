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
    }

    public void ShiftP()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        EnterFullMode();
    }

    public void ShiftD()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        EnterHalfMode();
    }

    public void ShiftR()
    {
        if (!CanAcceptShiftInput())
        {
            return;
        }

        EnterRearViewMode();
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

        EnterFullMode();
    }

    private void EnterFullMode()
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
            return;
        }

        if (pageSwitcher != null)
        {
            pageSwitcher.ShowPage(DemoPageId.LightingColorChange);
        }
    }

    private void EnterHalfMode()
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
    }

    private void EnterRearViewMode()
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
