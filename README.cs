using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OledTouchPointerBridge : MonoBehaviour
{
    private sealed class PointerState
    {
        public int pointerId;
        public PointerEventData eventData;
        public GameObject currentOverObject;
        public GameObject pointerPress;
        public GameObject rawPointerPress;
        public GameObject pointerDrag;
        public bool isPressed;
        public bool isDragging;
        public bool clickTriggeredOnDown;
        public Vector2 lastScreenPosition;
        public Camera eventCamera;
    }

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
    [SerializeField] private bool yOriginTop = true;
    [SerializeField] private bool clampCoordinate = true;

    [Header("Pointer")]
    [SerializeField] private bool enablePointerEvents = true;
    [SerializeField] private int driverPointerId = 1001;
    [SerializeField] private int passengerPointerId = 1002;
    [SerializeField] private float dragThresholdPixels = 8f;

    [Header("Touch Trigger")]
    [SerializeField] private bool triggerClickOnDown = true;
    [SerializeField] private bool triggerClickOnDragTarget = false;

    [Header("Debug")]
    [SerializeField] private bool logTouch = true;
    [SerializeField] private bool logRaycastHit = true;
    [SerializeField] private bool logPointerEvent = true;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private readonly List<Graphic> graphicBuffer = new List<Graphic>();

    private PointerState driverPointer;
    private PointerState passengerPointer;

    private void Awake()
    {
        ResolveReferences();
        InitializePointerStates();
    }

    private void OnEnable()
    {
        ResolveReferences();
        InitializePointerStates();

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

        ResetPointerState(driverPointer);
        ResetPointerState(passengerPointer);
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

        PointerState pointerState = GetPointerState(sourceText);
        Camera sourceCamera = GetRaycastCamera(sourceText);

        if (pointerState == null || sourceCamera == null)
        {
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

        if (eventText == "move")
        {
            HandleTouchMove(pointerState, screenPoint, canvasLocalPoint, sourceCamera);
            return;
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

        if (eventText == "down")
        {
            HandlePointerDown(pointerState, screenPoint, topResult, sourceCamera);
            return;
        }

        if (eventText == "up")
        {
            HandlePointerUp(pointerState, screenPoint, topResult);
        }
    }

    private void HandleTouchMove(
        PointerState pointerState,
        Vector2 screenPoint,
        Vector2 canvasLocalPoint,
        Camera sourceCamera
    )
    {
        if (pointerState.isDragging)
        {
            HandlePointerMove(pointerState, screenPoint, default);
            return;
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

        HandlePointerMove(pointerState, screenPoint, topResult);
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

        if (clampCoordinate)
        {
            x = Mathf.Clamp(x, 0f, driverWidth);
            y = Mathf.Clamp(y, 0f, rect.height);
        }

        float globalX = x;

        if (normalizedSource == "passenger")
        {
            globalX = passengerOffsetX + x;
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
                return eventSystemHit;
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

    private void HandlePointerDown(
        PointerState state,
        Vector2 screenPoint,
        RaycastResult raycastResult,
        Camera sourceCamera
    )
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            return;
        }

        ResetPointerState(state);

        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.pointerId = state.pointerId;
        eventData.button = PointerEventData.InputButton.Left;
        eventData.position = screenPoint;
        eventData.pressPosition = screenPoint;
        eventData.delta = Vector2.zero;
        eventData.clickTime = Time.unscaledTime;
        eventData.clickCount = 1;
        eventData.pointerCurrentRaycast = raycastResult;
        eventData.pointerPressRaycast = raycastResult;
        eventData.useDragThreshold = true;

        GameObject currentOver = raycastResult.gameObject;

        state.eventData = eventData;
        state.currentOverObject = currentOver;
        state.lastScreenPosition = screenPoint;
        state.isPressed = true;
        state.eventCamera = sourceCamera;

        if (currentOver == null)
        {
            LogPointer("Down hit none.");
            return;
        }

        GameObject pointerPress = ExecuteEvents.ExecuteHierarchy(
            currentOver,
            eventData,
            ExecuteEvents.pointerDownHandler
        );

        if (pointerPress == null)
        {
            pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOver);
        }

        GameObject pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOver);

        eventData.pointerPress = pointerPress;
        eventData.rawPointerPress = currentOver;
        eventData.pointerDrag = pointerDrag;

        state.pointerPress = pointerPress;
        state.rawPointerPress = currentOver;
        state.pointerDrag = pointerDrag;

        if (pointerDrag != null)
        {
            ExecuteEvents.Execute(
                pointerDrag,
                eventData,
                ExecuteEvents.initializePotentialDrag
            );
        }

        TryTriggerClickOnDown(state, eventData, pointerPress, pointerDrag);

        LogPointer(
            "Down current="
            + GetObjectName(currentOver)
            + " press="
            + GetObjectName(pointerPress)
            + " drag="
            + GetObjectName(pointerDrag)
            + " clickOnDown="
            + state.clickTriggeredOnDown
        );
    }

    private void TryTriggerClickOnDown(
        PointerState state,
        PointerEventData eventData,
        GameObject pointerPress,
        GameObject pointerDrag
    )
    {
        if (!triggerClickOnDown)
        {
            return;
        }

        if (state == null || eventData == null || pointerPress == null)
        {
            return;
        }

        if (!triggerClickOnDragTarget && pointerDrag != null)
        {
            return;
        }

        GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointerPress);

        if (clickHandler == null)
        {
            return;
        }

        ExecuteEvents.Execute(
            clickHandler,
            eventData,
            ExecuteEvents.pointerClickHandler
        );

        state.clickTriggeredOnDown = true;
        eventData.eligibleForClick = false;

        LogPointer("ClickOnDown target=" + GetObjectName(clickHandler));
    }

    private void HandlePointerMove(
        PointerState state,
        Vector2 screenPoint,
        RaycastResult raycastResult
    )
    {
        if (state == null || !state.isPressed || state.eventData == null)
        {
            return;
        }

        PointerEventData eventData = state.eventData;

        Vector2 delta = screenPoint - state.lastScreenPosition;

        eventData.position = screenPoint;
        eventData.delta = delta;
        eventData.pointerCurrentRaycast = raycastResult;

        state.currentOverObject = raycastResult.gameObject;

        if (state.pointerDrag != null)
        {
            if (!state.isDragging)
            {
                float movedDistance = Vector2.Distance(
                    eventData.pressPosition,
                    eventData.position
                );

                if (movedDistance >= dragThresholdPixels)
                {
                    ExecuteEvents.Execute(
                        state.pointerDrag,
                        eventData,
                        ExecuteEvents.beginDragHandler
                    );

                    eventData.dragging = true;
                    state.isDragging = true;

                    LogPointer("BeginDrag target=" + GetObjectName(state.pointerDrag));
                }
            }

            if (state.isDragging)
            {
                ExecuteEvents.Execute(
                    state.pointerDrag,
                    eventData,
                    ExecuteEvents.dragHandler
                );
            }
        }

        state.lastScreenPosition = screenPoint;
    }

    private void HandlePointerUp(
        PointerState state,
        Vector2 screenPoint,
        RaycastResult raycastResult
    )
    {
        if (state == null || !state.isPressed || state.eventData == null)
        {
            return;
        }

        PointerEventData eventData = state.eventData;

        eventData.position = screenPoint;
        eventData.delta = screenPoint - state.lastScreenPosition;
        eventData.pointerCurrentRaycast = raycastResult;

        GameObject currentOver = raycastResult.gameObject;

        if (state.pointerPress != null)
        {
            ExecuteEvents.Execute(
                state.pointerPress,
                eventData,
                ExecuteEvents.pointerUpHandler
            );
        }

        GameObject clickHandler = currentOver == null
            ? null
            : ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOver);

        if (!state.clickTriggeredOnDown
            && !state.isDragging
            && state.pointerPress != null
            && state.pointerPress == clickHandler)
        {
            ExecuteEvents.Execute(
                state.pointerPress,
                eventData,
                ExecuteEvents.pointerClickHandler
            );

            LogPointer("Click target=" + GetObjectName(state.pointerPress));
        }

        if (state.isDragging && state.pointerDrag != null)
        {
            ExecuteEvents.Execute(
                state.pointerDrag,
                eventData,
                ExecuteEvents.endDragHandler
            );

            LogPointer("EndDrag target=" + GetObjectName(state.pointerDrag));
        }

        ResetPointerState(state);
    }

    private void ResetPointerState(PointerState state)
    {
        if (state == null)
        {
            return;
        }

        state.eventData = null;
        state.currentOverObject = null;
        state.pointerPress = null;
        state.rawPointerPress = null;
        state.pointerDrag = null;
        state.isPressed = false;
        state.isDragging = false;
        state.clickTriggeredOnDown = false;
        state.lastScreenPosition = Vector2.zero;
        state.eventCamera = null;
    }

    private PointerState GetPointerState(string source)
    {
        if (source == "driver")
        {
            return driverPointer;
        }

        if (source == "passenger")
        {
            return passengerPointer;
        }

        return null;
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

    private void InitializePointerStates()
    {
        if (driverPointer == null)
        {
            driverPointer = new PointerState
            {
                pointerId = driverPointerId
            };
        }

        if (passengerPointer == null)
        {
            passengerPointer = new PointerState
            {
                pointerId = passengerPointerId
            };
        }
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
