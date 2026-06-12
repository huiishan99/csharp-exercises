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
        public Vector2 lastScreenPosition;
    }

    [Header("Event")]
    [SerializeField] private GuiEventDispatcher eventDispatcher;

    [Header("Canvas / Camera")]
    [SerializeField] private Canvas displayCanvas;
    [SerializeField] private RectTransform touchSurfaceRect;
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private bool assignCanvasEventCamera = true;

    [Header("OLED Layout")]
    [SerializeField] private float driverWidth = 2650f;
    [SerializeField] private float passengerOffsetX = 2650f;
    [SerializeField] private bool yOriginTop = true;
    [SerializeField] private bool clampCoordinate = true;

    [Header("Pointer")]
    [SerializeField] private bool enablePointerEvents = false;
    [SerializeField] private int driverPointerId = 1001;
    [SerializeField] private int passengerPointerId = 1002;
    [SerializeField] private float dragThresholdPixels = 8f;

    [Header("Debug")]
    [SerializeField] private bool logTouch = true;
    [SerializeField] private bool logRaycastHit = true;
    [SerializeField] private bool logPointerEvent = true;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private PointerState driverPointer;
    private PointerState passengerPointer;

    private void Awake()
    {
        ResolveReferences();
        InitializePointerStates();
        ApplyCanvasEventCamera();
    }

    private void OnEnable()
    {
        ResolveReferences();
        InitializePointerStates();
        ApplyCanvasEventCamera();

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
            Debug.LogWarning("[OLED Pointer] Touch payload is null.");
            return;
        }

        GuiEventTouchPayload payload = message.TouchPayload;

        string sourceText = NormalizeText(payload.source);
        string eventText = NormalizeText(payload.GetTouchEventText());

        PointerState pointerState = GetPointerState(sourceText);

        if (pointerState == null)
        {
            Debug.LogWarning("[OLED Pointer] Unknown source: " + payload.source);
            return;
        }

        if (!TryConvertToScreenPoint(payload, sourceText, out Vector2 screenPoint, out Vector2 localPoint))
        {
            Debug.LogWarning("[OLED Pointer] Failed to convert touch coordinate.");
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
                + ") local="
                + localPoint
                + " screen="
                + screenPoint
            );
        }

        GameObject hitObject = RaycastTop(screenPoint, out RaycastResult topResult);

        if (logRaycastHit)
        {
            string hitName = hitObject == null ? "None" : GetHierarchyPath(hitObject);
            Debug.Log("[OLED Pointer Raycast] hit=" + hitName);
        }

        if (!enablePointerEvents)
        {
            return;
        }

        if (eventText == "down")
        {
            HandlePointerDown(pointerState, screenPoint, topResult);
            return;
        }

        if (eventText == "move")
        {
            HandlePointerMove(pointerState, screenPoint, topResult);
            return;
        }

        if (eventText == "up")
        {
            HandlePointerUp(pointerState, screenPoint, topResult);
            return;
        }

        Debug.LogWarning("[OLED Pointer] Unknown touch event: " + payload.GetTouchEventText());
    }

    private bool TryConvertToScreenPoint(
        GuiEventTouchPayload payload,
        string normalizedSource,
        out Vector2 screenPoint,
        out Vector2 localPoint
    )
    {
        screenPoint = Vector2.zero;
        localPoint = Vector2.zero;

        if (touchSurfaceRect == null || raycastCamera == null)
        {
            return false;
        }

        Rect rect = touchSurfaceRect.rect;

        float x = payload.x;
        float y = payload.y;

        float activeHeight = rect.height;

        if (clampCoordinate)
        {
            x = Mathf.Clamp(x, 0f, driverWidth);
            y = Mathf.Clamp(y, 0f, activeHeight);
        }

        float globalX = x;

        if (normalizedSource == "passenger")
        {
            globalX = passengerOffsetX + x;
        }

        float localX = rect.xMin + globalX;
        float localY = yOriginTop
            ? rect.yMax - y
            : rect.yMin + y;

        localPoint = new Vector2(localX, localY);

        Vector3 worldPoint = touchSurfaceRect.TransformPoint(new Vector3(localX, localY, 0f));
        screenPoint = RectTransformUtility.WorldToScreenPoint(raycastCamera, worldPoint);

        return true;
    }

    private GameObject RaycastTop(Vector2 screenPoint, out RaycastResult topResult)
    {
        topResult = new RaycastResult();

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            Debug.LogWarning("[OLED Pointer] EventSystem.current is null.");
            return null;
        }

        PointerEventData tempEventData = new PointerEventData(eventSystem);
        tempEventData.position = screenPoint;

        raycastResults.Clear();
        eventSystem.RaycastAll(tempEventData, raycastResults);

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
        RaycastResult raycastResult
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

        LogPointer(
            "Down current="
            + GetObjectName(currentOver)
            + " press="
            + GetObjectName(pointerPress)
            + " drag="
            + GetObjectName(pointerDrag)
        );
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
                float movedDistance = Vector2.Distance(eventData.pressPosition, eventData.position);

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

        if (!state.isDragging && state.pointerPress != null && state.pointerPress == clickHandler)
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
        state.lastScreenPosition = Vector2.zero;
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

        if (raycastCamera == null && displayCanvas != null)
        {
            raycastCamera = displayCanvas.worldCamera;
        }

        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (touchSurfaceRect == null && displayCanvas != null)
        {
            touchSurfaceRect = displayCanvas.GetComponent<RectTransform>();
        }
    }

    private void ApplyCanvasEventCamera()
    {
        if (!assignCanvasEventCamera)
        {
            return;
        }

        if (displayCanvas == null || raycastCamera == null)
        {
            return;
        }

        displayCanvas.worldCamera = raycastCamera;
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
