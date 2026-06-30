using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OledTouchPointerBridge : MonoBehaviour
{
    [Header("Event")]
    [SerializeField] private GuiEventDispatcher eventDispatcher;

    [Header("Canvas")]
    [SerializeField] private Canvas displayCanvas;
    [SerializeField] private RectTransform touchSurfaceRect;

    [Header("Raycast Cameras")]
    [SerializeField] private Camera driverRaycastCamera;
    [SerializeField] private Camera passengerRaycastCamera;

    [Header("Raycast Mode")]
    [SerializeField] private bool useLocalGraphicRaycast = true;
    [SerializeField] private bool useEventSystemRaycastFallback = false;

    [Tooltip("通常はfalse。CanvasEventCameraSwitcherと競合させない。")]
    [SerializeField] private bool setCanvasWorldCameraOnTouch = false;

    [Header("OLED Layout")]
    [SerializeField] private float driverWidth = 2650f;
    [SerializeField] private float passengerOffsetX = 2650f;

    // 物理OLEDの高さ。Full/Semiに関係なくDisplay自体は1392。
    [SerializeField] private float physicalDisplayHeight = 1392f;

    [SerializeField] private bool yOriginTop = true;
    [SerializeField] private bool clampCoordinate = true;

    // Semi時に表示されていない領域を誤って端にClampしないため。
    [SerializeField] private bool ignoreTouchOutsideVisibleArea = true;

    [Header("Touch Rotation")]
    [SerializeField] private bool rotateDriverTouch180 = false;
    [SerializeField] private bool rotatePassengerTouch180 = false;

    [Header("Tap On Down")]
    [SerializeField] private bool enablePointerEvents = true;

    // 実機Touchはdown時点で「Tap」として扱う。
    [SerializeField] private bool triggerTapOnDown = true;

    // down時に一瞬だけpointerDown/pointerUpを流して、Buttonの押下表示を残さない。
    [SerializeField] private bool executePointerDownBeforeClick = true;
    [SerializeField] private bool executePointerUpImmediately = true;

    // up/moveはclick判定に使わない。
    [SerializeField] private bool ignoreMoveEvent = true;
    [SerializeField] private bool ignoreUpEvent = true;

    [Header("Pointer Id")]
    [SerializeField] private int driverPointerId = 1001;
    [SerializeField] private int passengerPointerId = 1002;

    [Header("Debug")]
    [SerializeField] private bool logTouch = true;
    [SerializeField] private bool logRaycastHit = true;
    [SerializeField] private bool logPointerEvent = true;
    [SerializeField] private bool logIgnoredEvents = false;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private readonly List<Graphic> graphicBuffer = new List<Graphic>();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (eventDispatcher != null)
        {
            eventDispatcher.TouchReceived -= OnTouchReceived;
            eventDispatcher.TouchReceived += OnTouchReceived;
        }
    }

    private void OnDisable()
    {
        if (eventDispatcher != null)
        {
            eventDispatcher.TouchReceived -= OnTouchReceived;
        }
    }

    private void OnTouchReceived(GuiEventMessage message)
    {
        if (message == null || message.TouchPayload == null)
        {
            return;
        }

        GuiEventTouchPayload payload = message.TouchPayload;

        string sourceText = NormalizeText(payload.source);
        string eventText = NormalizeText(payload.GetTouchEventText());

        if (eventText == "move" && ignoreMoveEvent)
        {
            LogIgnoredEvent(sourceText, eventText, payload);
            return;
        }

        if (eventText == "up" && ignoreUpEvent)
        {
            LogIgnoredEvent(sourceText, eventText, payload);
            return;
        }

        // 現仕様ではdownのみをTap triggerとして扱う。
        if (triggerTapOnDown && eventText != "down")
        {
            LogIgnoredEvent(sourceText, eventText, payload);
            return;
        }

        Camera sourceCamera = GetRaycastCamera(sourceText);

        if (sourceCamera == null)
        {
            Debug.LogWarning("[OLED Pointer] Raycast camera is not assigned for source=" + sourceText);
            return;
        }

        if (!TryConvertToPoints(
                payload,
                sourceText,
                sourceCamera,
                out Vector2 screenPoint,
                out Vector2 canvasLocalPoint,
                out Vector2 sourceLocalPoint
            ))
        {
            return;
        }

        if (logTouch)
        {
            Debug.Log(
                "[OLED Pointer] source="
                + sourceText
                + " event="
                + eventText
                + " touch=("
                + payload.x
                + ", "
                + payload.y
                + ") sourceLocal="
                + sourceLocalPoint
                + " canvasLocal="
                + canvasLocalPoint
                + " screen="
                + screenPoint
                + " camera="
                + sourceCamera.name
            );
        }

        GameObject hitObject = RaycastTop(
            screenPoint,
            canvasLocalPoint,
            sourceCamera,
            out RaycastResult topResult,
            out string hitMethod
        );

        if (logRaycastHit)
        {
            string hitName = hitObject == null ? "None" : GetHierarchyPath(hitObject);
            Debug.Log("[OLED Pointer Raycast] hit=" + hitName + " hitMethod=" + hitMethod);
        }

        if (!enablePointerEvents)
        {
            return;
        }

        if (triggerTapOnDown && eventText == "down")
        {
            ExecuteTapOnDown(
                sourceText,
                screenPoint,
                topResult
            );
        }
    }

    private bool TryConvertToPoints(
        GuiEventTouchPayload payload,
        string normalizedSource,
        Camera sourceCamera,
        out Vector2 screenPoint,
        out Vector2 canvasLocalPoint,
        out Vector2 sourceLocalPoint
    )
    {
        screenPoint = Vector2.zero;
        canvasLocalPoint = Vector2.zero;
        sourceLocalPoint = Vector2.zero;

        if (touchSurfaceRect == null || sourceCamera == null)
        {
            return false;
        }

        Rect rect = touchSurfaceRect.rect;

        float x = payload.x;
        float y = payload.y;

        // まず物理Display座標としてClampする。
        // Semi時でもTouch入力は物理Display基準で来る可能性があるため、
        // ここではrect.height(720)ではなくphysicalDisplayHeight(1392)を使う。
        if (clampCoordinate)
        {
            x = Mathf.Clamp(x, 0f, driverWidth);
            y = Mathf.Clamp(y, 0f, physicalDisplayHeight);
        }

        ApplyTouchRotationIfNeeded(normalizedSource, ref x, ref y);

        // 180度回転後、GUI可視領域外にあるTouchは無視する。
        // 例: Semi 720px表示で、変換後yが720を超える場合。
        if (ignoreTouchOutsideVisibleArea)
        {
            if (x < 0f || x > driverWidth)
            {
                LogOutOfVisibleArea(normalizedSource, x, y, rect);
                return false;
            }

            if (y < 0f || y > rect.height)
            {
                LogOutOfVisibleArea(normalizedSource, x, y, rect);
                return false;
            }
        }
        else if (clampCoordinate)
        {
            x = Mathf.Clamp(x, 0f, driverWidth);
            y = Mathf.Clamp(y, 0f, rect.height);
        }

        float globalX = x;

        if (normalizedSource == "passenger")
        {
            globalX = passengerOffsetX + x;
        }
        else if (normalizedSource != "driver")
        {
            Debug.LogWarning("[OLED Pointer] Unknown source: " + normalizedSource);
            return false;
        }

        float canvasLocalX = rect.xMin + globalX;
        float canvasLocalY = yOriginTop
            ? rect.yMax - y
            : rect.yMin + y;

        sourceLocalPoint = new Vector2(x, canvasLocalY);
        canvasLocalPoint = new Vector2(canvasLocalX, canvasLocalY);

        Vector3 worldPoint = touchSurfaceRect.TransformPoint(
            new Vector3(canvasLocalPoint.x, canvasLocalPoint.y, 0f)
        );

        screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPoint);
        return true;
    }

    private void ApplyTouchRotationIfNeeded(
        string normalizedSource,
        ref float x,
        ref float y
    )
    {
        bool shouldRotate = false;

        if (normalizedSource == "driver")
        {
            shouldRotate = rotateDriverTouch180;
        }
        else if (normalizedSource == "passenger")
        {
            shouldRotate = rotatePassengerTouch180;
        }

        if (!shouldRotate)
        {
            return;
        }

        // 180度回転は物理Display全体を基準にする。
        // Semi時のrect.height=720を使うと、Touchだけ上側に残る。
        x = driverWidth - x;
        y = physicalDisplayHeight - y;
    }

    private GameObject RaycastTop(
        Vector2 screenPoint,
        Vector2 canvasLocalPoint,
        Camera sourceCamera,
        out RaycastResult topResult,
        out string hitMethod
    )
    {
        topResult = new RaycastResult();
        hitMethod = "None";

        if (useLocalGraphicRaycast)
        {
            bool localHit = UiLocalGraphicRaycastUtility.TryRaycastTopGraphic(
                displayCanvas,
                touchSurfaceRect,
                canvasLocalPoint,
                screenPoint,
                graphicBuffer,
                out topResult
            );

            if (localHit)
            {
                hitMethod = "LocalGraphic";
                return topResult.gameObject;
            }
        }

        if (useEventSystemRaycastFallback)
        {
            GameObject eventSystemHit = RaycastByEventSystem(
                screenPoint,
                sourceCamera,
                out topResult
            );

            if (eventSystemHit != null)
            {
                hitMethod = "EventSystem";
                return topResult.gameObject;
            }
        }

        return null;
    }

    private GameObject RaycastByEventSystem(
        Vector2 screenPoint,
        Camera sourceCamera,
        out RaycastResult topResult
    )
    {
        topResult = new RaycastResult();

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            return null;
        }

        Camera previousCamera = null;

        if (setCanvasWorldCameraOnTouch && displayCanvas != null)
        {
            previousCamera = displayCanvas.worldCamera;
            displayCanvas.worldCamera = sourceCamera;
        }

        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.position = screenPoint;
        eventData.button = PointerEventData.InputButton.Left;

        raycastResults.Clear();
        eventSystem.RaycastAll(eventData, raycastResults);

        if (setCanvasWorldCameraOnTouch && displayCanvas != null)
        {
            displayCanvas.worldCamera = previousCamera;
        }

        if (raycastResults.Count == 0)
        {
            return null;
        }

        topResult = raycastResults[0];
        return topResult.gameObject;
    }

    private void ExecuteTapOnDown(
        string sourceText,
        Vector2 screenPoint,
        RaycastResult raycastResult
    )
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            return;
        }

        GameObject currentOver = raycastResult.gameObject;

        if (currentOver == null)
        {
            LogPointer("TapOnDown hit none.");
            return;
        }

        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.pointerId = GetPointerId(sourceText);
        eventData.button = PointerEventData.InputButton.Left;
        eventData.position = screenPoint;
        eventData.pressPosition = screenPoint;
        eventData.delta = Vector2.zero;
        eventData.clickTime = Time.unscaledTime;
        eventData.clickCount = 1;
        eventData.pointerCurrentRaycast = raycastResult;
        eventData.pointerPressRaycast = raycastResult;
        eventData.useDragThreshold = false;
        eventData.eligibleForClick = true;

        GameObject pointerDownTarget = null;

        if (executePointerDownBeforeClick)
        {
            pointerDownTarget = ExecuteEvents.ExecuteHierarchy(
                currentOver,
                eventData,
                ExecuteEvents.pointerDownHandler
            );
        }

        GameObject clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOver);

        if (clickTarget == null)
        {
            clickTarget = pointerDownTarget;
        }

        eventData.pointerPress = clickTarget;
        eventData.rawPointerPress = currentOver;

        if (executePointerUpImmediately && pointerDownTarget != null)
        {
            ExecuteEvents.Execute(
                pointerDownTarget,
                eventData,
                ExecuteEvents.pointerUpHandler
            );
        }

        if (clickTarget != null)
        {
            ExecuteEvents.Execute(
                clickTarget,
                eventData,
                ExecuteEvents.pointerClickHandler
            );

            LogPointer(
                "TapOnDown current="
                + GetObjectName(currentOver)
                + " down="
                + GetObjectName(pointerDownTarget)
                + " click="
                + GetObjectName(clickTarget)
            );

            return;
        }

        LogPointer(
            "TapOnDown no click handler. current="
            + GetObjectName(currentOver)
            + " down="
            + GetObjectName(pointerDownTarget)
        );
    }

    private int GetPointerId(string sourceText)
    {
        if (sourceText == "passenger")
        {
            return passengerPointerId;
        }

        return driverPointerId;
    }

    private Camera GetRaycastCamera(string source)
    {
        if (source == "driver")
        {
            return driverRaycastCamera;
        }

        if (source == "passenger")
        {
            return passengerRaycastCamera;
        }

        return null;
    }

    private void ResolveReferences()
    {
        if (eventDispatcher == null)
        {
            eventDispatcher = FindFirstObjectByType<GuiEventDispatcher>();
        }

        if (displayCanvas == null)
        {
            displayCanvas = FindFirstObjectByType<Canvas>();
        }

        if (touchSurfaceRect == null && displayCanvas != null)
        {
            touchSurfaceRect = displayCanvas.GetComponent<RectTransform>();
        }
    }

    private void LogIgnoredEvent(string sourceText, string eventText, GuiEventTouchPayload payload)
    {
        if (!logIgnoredEvents)
        {
            return;
        }

        Debug.Log(
            "[OLED Pointer] ignored event source="
            + sourceText
            + " event="
            + eventText
            + " touch=("
            + payload.x
            + ", "
            + payload.y
            + ")"
        );
    }

    private void LogOutOfVisibleArea(string sourceText, float x, float y, Rect rect)
    {
        if (!logIgnoredEvents)
        {
            return;
        }

        Debug.Log(
            "[OLED Pointer] ignored outside visible area source="
            + sourceText
            + " converted=("
            + x.ToString("0.###")
            + ", "
            + y.ToString("0.###")
            + ") visibleSize=("
            + driverWidth.ToString("0.###")
            + ", "
            + rect.height.ToString("0.###")
            + ") physicalHeight="
            + physicalDisplayHeight.ToString("0.###")
        );
    }

    private string NormalizeText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "");
    }

    private void LogPointer(string message)
    {
        if (!logPointerEvent)
        {
            return;
        }

        Debug.Log("[OLED Pointer] " + message);
    }

    private string GetObjectName(GameObject target)
    {
        if (target == null)
        {
            return "None";
        }

        return target.name;
    }

    private string GetHierarchyPath(GameObject target)
    {
        if (target == null)
        {
            return "None";
        }

        string path = target.name;
        Transform current = target.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
