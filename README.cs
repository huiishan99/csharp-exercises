using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DemoDelayedPageContentView : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private CanvasGroup targetCanvasGroup;

    [Header("Timing")]
    [SerializeField] private float showDelaySec = 0.2f;
    [SerializeField] private float fadeDurationSec = 0.12f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Interaction")]
    [SerializeField] private bool disableInteractionWhileHidden = true;

    [Header("Disable")]
    [SerializeField] private bool hideOnDisable = true;

    [Header("Debug")]
    [SerializeField] private bool logState = false;

    private Coroutine showRoutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        StartShowRoutine();
    }

    private void OnDisable()
    {
        StopShowRoutine();

        if (hideOnDisable)
        {
            ApplyHiddenImmediate();
        }
    }

    public void RestartShow()
    {
        StartShowRoutine();
    }

    public void ShowImmediate()
    {
        StopShowRoutine();
        ApplyVisibleImmediate();
    }

    public void HideImmediate()
    {
        StopShowRoutine();
        ApplyHiddenImmediate();
    }

    private void StartShowRoutine()
    {
        StopShowRoutine();

        if (targetCanvasGroup == null)
        {
            return;
        }

        showRoutine = StartCoroutine(ShowRoutine());
    }

    private void StopShowRoutine()
    {
        if (showRoutine == null)
        {
            return;
        }

        StopCoroutine(showRoutine);
        showRoutine = null;
    }

    private IEnumerator ShowRoutine()
    {
        ApplyHiddenImmediate();

        Log("Hidden. Waiting delay: " + showDelaySec);

        if (showDelaySec > 0f)
        {
            yield return Wait(showDelaySec);
        }

        if (fadeDurationSec <= 0f)
        {
            ApplyVisibleImmediate();
            showRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDurationSec)
        {
            elapsed += GetDeltaTime();

            float rate = Mathf.Clamp01(elapsed / fadeDurationSec);
            targetCanvasGroup.alpha = rate;

            yield return null;
        }

        ApplyVisibleImmediate();

        Log("Visible.");
        showRoutine = null;
    }

    private IEnumerator Wait(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }

    private void ApplyHiddenImmediate()
    {
        if (targetCanvasGroup == null)
        {
            return;
        }

        targetCanvasGroup.alpha = 0f;

        if (disableInteractionWhileHidden)
        {
            targetCanvasGroup.interactable = false;
            targetCanvasGroup.blocksRaycasts = false;
        }
    }

    private void ApplyVisibleImmediate()
    {
        if (targetCanvasGroup == null)
        {
            return;
        }

        targetCanvasGroup.alpha = 1f;

        if (disableInteractionWhileHidden)
        {
            targetCanvasGroup.interactable = true;
            targetCanvasGroup.blocksRaycasts = true;
        }
    }

    private void ResolveReferences()
    {
        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Log(string message)
    {
        if (!logState)
        {
            return;
        }

        Debug.Log("[DelayedPageContent] " + message + " object=" + gameObject.name);
    }
}
