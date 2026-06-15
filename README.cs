using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OledMouseTouchCoordinateLogger : MonoBehaviour
{
    public enum SourceMode
    {
        AutoByDisplay,
        ForceDriver,
        ForcePassenger
    }

    private struct TouchCoord
    {
        public string source;
        public int x;
        public int y;
        public Vector2 canvasLocal;
        public Vector2 screenPoint;
        public GameObject hitObject;
    }

    [Header("Source")]
    [SerializeField] private SourceMode sourceMode = SourceMode.AutoByDisplay;
    [SerializeField] private SourceMode fallbackSourceMode = SourceMode.ForceDriver;

    [Header("Canvas / Camera")]
    [SerializeField] private Canvas displayCanvas;
    [SerializeField] private RectTransform touchSurfaceRect;
    [SerializeField] private Camera driverCamera;
    [SerializeField] private Camera passengerCamera;

    [Header("Raycast")]
    [SerializeField] private bool raycastHitObject = true;
    [SerializeField] private bool temporarilySetCanvasCameraForRaycast = true;

    [Header("OLED Layout")]
    [SerializeField] private float driverWidth = 2650f;
    [SerializeField] private float passengerOffsetX = 2650f;
    [SerializeField] private bool yOriginTop = true;
    [SerializeField] private bool clampCoordinate = true;

    [Header("Output")]
    [SerializeField] private bool logMouseDown = true;
    [SerializeField] private bool logMouseUp = true;
    [SerializeField] private bool logMouseDragMove = false;
    [SerializeField] private bool logPythonCommand = true;
    [SerializeField] private float dragDetectThreshold = 8f;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private bool isPressed;
    private TouchCoord downCoord;
    private TouchCoord latestCoord;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (IsMouseDownThisFrame())
        {
            HandleMouseDown();
        }

        if (IsMousePressed())
        {
            HandleMouseMoveWhilePressed();
        }

        if (IsMouseUpThisFrame())
        {
            HandleMouseUp();
        }
    }

    private void HandleMouseDown()
    {
        if (!TryGetCurrentTouchCoord(out TouchCoord coord))
        {
            return;
        }

        isPressed = true;
        downCoord = coord;
        latestCoord = coord;

        if (logMouseDown)
        {
            LogCoord("DOWN", coord);
        }

        if (logPythonCommand)
        {
            Debug.Log("[Mouse→OLED] Python: td " + coord.x + " " + coord.y + " " + coord.source);
        }
    }

    private void HandleMouseMoveWhilePressed()
    {
        if (!isPressed)
        {
            return;
        }

        if (!TryGetCurrentTouchCoord(out TouchCoord coord))
        {
            return;
        }

        latestCoord = coord;

        if (!logMouseDragMove)
        {
            return;
        }

        LogCoord("MOVE", coord);

        if (logPythonCommand)
        {
            Debug.Log("[Mouse→OLED] Python: tm " + coord.x + " " + coord.y + " " + coord.source);
        }
    }

    private void HandleMouseUp()
    {
        if (!isPressed)
        {
            return;
        }

        if (!TryGetCurrentTouchCoord(out TouchCoord upCoord))
        {
            upCoord = latestCoord;
        }

        isPressed = false;

        if (logMouseUp)
        {
            LogCoord("UP", upCoord);
        }

        if (!logPythonCommand)
        {
            return;
        }

        Debug.Log("[Mouse→OLED] Python: tu " + upCoord.x + " " + upCoord.y + " " + upCoord.source);

        float distance = Vector2.Distance(
            new Vector2(downCoord.x, downCoord.y),
            new Vector2(upCoord.x, upCoord.y)
        );

        if (distance < dragDetectThreshold && downCoord.source == upCoord.source)
        {
            Debug.Log(
                "[Mouse→OLED] Python Tap Command: tap "
                + downCoord.x
                + " "
                + downCoord.y
                + " "
                + downCoord.source
            );

            return;
        }

        if (downCoord.source == upCoord.source)
        {
            Debug.Log(
                "[Mouse→OLED] Python Drag Command: drag "
                + downCoord.x
                + " "
                + downCoord.y
                + " "
                + upCoord.x
                + " "
                + upCoord.y
                + " "
                + downCoord.source
            );
        }
        else
        {
            Debug.LogWarning(
                "[Mouse→OLED] Drag source changed from "
                + downCoord.source
                + " to "
                + upCoord.source
                + ". Command not generated."
            );
        }
    }

    private bool TryGetCurrentTouchCoord(out TouchCoord coord)
    {
        coord = new TouchCoord();

        Vector2 rawMousePosition = GetMousePosition();

        if (!TryResolveSourceAndCamera(
                rawMousePosition,
                out string source,
                out Camera targetCamera,
                out Vector2 cameraScreenPoint
            ))
        {
            return false;
        }

        if (targetCamera == null || touchSurfaceRect == null)
        {
            Debug.LogWarning("[Mouse→OLED] Camera or TouchSurfaceRect is not assigned.");
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                touchSurfaceRect,
                cameraScreenPoint,
                targetCamera,
                out Vector2 canvasLocalPoint
            ))
        {
            Debug.LogWarning("[Mouse→OLED] Failed to convert mouse position to canvas local point.");
            return false;
        }

        if (!TryCanvasLocalToTouchCoord(
                source,
                canvasLocalPoint,
                out int touchX,
                out int touchY
            ))
        {
            return false;
        }

        GameObject hitObject = null;

        if (raycastHitObject)
        {
            hitObject = RaycastTop(cameraScreenPoint, targetCamera);
        }

        coord = new TouchCoord
        {
            source = source,
            x = touchX,
            y = touchY,
            canvasLocal = canvasLocalPoint,
            screenPoint = cameraScreenPoint,
            hitObject = hitObject
        };

        return true;
    }

    private bool TryResolveSourceAndCamera(
        Vector2 rawMousePosition,
        out string source,
        out Camera targetCamera,
        out Vector2 cameraScreenPoint
    )
    {
        source = "";
        targetCamera = null;
        cameraScreenPoint = rawMousePosition;

        if (sourceMode == SourceMode.ForceDriver)
        {
            source = "driver";
            targetCamera = driverCamera;
            cameraScreenPoint = GetScreenPointForCamera(rawMousePosition, driverCamera);
            return targetCamera != null;
        }

        if (sourceMode == SourceMode.ForcePassenger)
        {
            source = "passenger";
            targetCamera = passengerCamera;
            cameraScreenPoint = GetScreenPointForCamera(rawMousePosition, passengerCamera);
            return targetCamera != null;
        }

        if (TryResolveByDisplay(
                rawMousePosition,
                out source,
                out targetCamera,
                out cameraScreenPoint
            ))
        {
            return true;
        }

        if (fallbackSourceMode == SourceMode.ForcePassenger)
        {
            source = "passenger";
            targetCamera = passengerCamera;
            cameraScreenPoint = GetScreenPointForCamera(rawMousePosition, passengerCamera);
            return targetCamera != null;
        }

        source = "driver";
        targetCamera = driverCamera;
        cameraScreenPoint = GetScreenPointForCamera(rawMousePosition, driverCamera);
        return targetCamera != null;
    }

    private bool TryResolveByDisplay(
        Vector2 rawMousePosition,
        out string source,
        out Camera targetCamera,
        out Vector2 cameraScreenPoint
    )
    {
        source = "";
        targetCamera = null;
        cameraScreenPoint = rawMousePosition;

        Vector3 relativeMouse = Display.RelativeMouseAt(rawMousePosition);

        bool hasRelativeMouse =
            Mathf.Abs(relativeMouse.x) > 0.001f
            || Mathf.Abs(relativeMouse.y) > 0.001f
            || Mathf.Abs(relativeMouse.z) > 0.001f;

        if (!hasRelativeMouse)
        {
            return false;
        }

        int displayIndex = Mathf.RoundToInt(relativeMouse.z);

        if (driverCamera != null && displayIndex == driverCamera.targetDisplay)
        {
            source = "driver";
            targetCamera = driverCamera;
            cameraScreenPoint = new Vector2(relativeMouse.x, relativeMouse.y);
            return true;
        }

        if (passengerCamera != null && displayIndex == passengerCamera.targetDisplay)
        {
            source = "passenger";
            targetCamera = passengerCamera;
            cameraScreenPoint = new Vector2(relativeMouse.x, relativeMouse.y);
            return true;
        }

        return false;
    }

    private Vector2 GetScreenPointForCamera(Vector2 rawMousePosition, Camera targetCamera)
    {
        if (targetCamera == null)
        {
            return rawMousePosition;
        }

        Vector3 relativeMouse = Display.RelativeMouseAt(rawMousePosition);

        bool hasRelativeMouse =
            Mathf.Abs(relativeMouse.x) > 0.001f
            || Mathf.Abs(relativeMouse.y) > 0.001f
            || Mathf.Abs(relativeMouse.z) > 0.001f;

        if (!hasRelativeMouse)
        {
            return rawMousePosition;
        }

        int displayIndex = Mathf.RoundToInt(relativeMouse.z);

        if (displayIndex == targetCamera.targetDisplay)
        {
            return new Vector2(relativeMouse.x, relativeMouse.y);
        }

        return rawMousePosition;
    }

    private bool TryCanvasLocalToTouchCoord(
        string source,
        Vector2 canvasLocalPoint,
        out int touchX,
        out int touchY
    )
    {
        touchX = 0;
        touchY = 0;

        if (touchSurfaceRect == null)
        {
            return false;
        }

        Rect rect = touchSurfaceRect.rect;

        float globalX = canvasLocalPoint.x - rect.xMin;
        float localX = globalX;

        if (source == "passenger")
        {
            localX = globalX - passengerOffsetX;
        }

        float localY = yOriginTop
            ? rect.yMax - canvasLocalPoint.y
            : canvasLocalPoint.y - rect.yMin;

        if (clampCoordinate)
        {
            localX = Mathf.Clamp(localX, 0f, driverWidth);
            localY = Mathf.Clamp(localY, 0f, rect.height);
        }

        touchX = Mathf.RoundToInt(localX);
        touchY = Mathf.RoundToInt(localY);

        return true;
    }

    private GameObject RaycastTop(Vector2 screenPoint, Camera targetCamera)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            return null;
        }

        Camera previousWorldCamera = null;

        if (temporarilySetCanvasCameraForRaycast && displayCanvas != null)
        {
            previousWorldCamera = displayCanvas.worldCamera;
            displayCanvas.worldCamera = targetCamera;
        }

        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.position = screenPoint;

        raycastResults.Clear();
        eventSystem.RaycastAll(eventData, raycastResults);

        if (temporarilySetCanvasCameraForRaycast && displayCanvas != null)
        {
            displayCanvas.worldCamera = previousWorldCamera;
        }

        if (raycastResults.Count == 0)
        {
            return null;
        }

        return raycastResults[0].gameObject;
    }

    private void LogCoord(string phase, TouchCoord coord)
    {
        string hitName = coord.hitObject == null
            ? "None"
            : GetHierarchyPath(coord.hitObject);

        Debug.Log(
            "[Mouse→OLED] "
            + phase
            + " source="
            + coord.source
            + " touch=("
            + coord.x
            + ", "
            + coord.y
            + ") canvasLocal="
            + coord.canvasLocal
            + " screen="
            + coord.screenPoint
            + " hit="
            + hitName
        );
    }

    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private bool IsMouseDownThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private bool IsMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.isPressed;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    private bool IsMouseUpThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasReleasedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonUp(0);
#else
        return false;
#endif
    }

    private void ResolveReferences()
    {
        if (displayCanvas == null)
        {
            displayCanvas = FindFirstObjectByType<Canvas>();
        }

        if (touchSurfaceRect == null && displayCanvas != null)
        {
            touchSurfaceRect = displayCanvas.GetComponent<RectTransform>();
        }
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
