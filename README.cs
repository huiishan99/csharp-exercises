using System.Collections;
using UnityEngine;

public class KinemaMockPopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupObject;
    [SerializeField] private float autoCloseSeconds = 10f;

    private Coroutine autoCloseCoroutine;

    public bool IsVisible
    {
        get
        {
            return popupObject != null && popupObject.activeSelf;
        }
    }

    private void Awake()
    {
        HidePopup();
    }

    private void OnDisable()
    {
        StopAutoCloseTimer();
    }

    public void TogglePopup()
    {
        if (IsVisible)
        {
            HidePopup();
            return;
        }

        ShowPopup();
    }

    public void ShowPopup()
    {
        if (popupObject == null)
        {
            return;
        }

        popupObject.SetActive(true);
        RestartAutoCloseTimer();
    }

    public void HidePopup()
    {
        StopAutoCloseTimer();

        if (popupObject == null)
        {
            return;
        }

        popupObject.SetActive(false);
    }

    public void NotifyUserOperation()
    {
        if (!IsVisible)
        {
            return;
        }

        RestartAutoCloseTimer();
    }

    private void RestartAutoCloseTimer()
    {
        StopAutoCloseTimer();

        if (autoCloseSeconds <= 0f)
        {
            return;
        }

        autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseSeconds);
        HidePopup();
    }

    private void StopAutoCloseTimer()
    {
        if (autoCloseCoroutine == null)
        {
            return;
        }

        StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = null;
    }
}
