using UnityEngine;

[ExecuteAlways]
public class OledTouchCoordinateDebugger : MonoBehaviour
{
    public enum TouchSource
    {
        Driver,
        Passenger
    }

    [SerializeField] private RectTransform touchSurfaceRect;
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private TouchSource source = TouchSource.Driver;

    [SerializeField] private float passengerOffsetX = 2650f;
    [SerializeField] private bool yOriginTop = true;

    [Header("Debug")]
    [SerializeField] private bool printOnStart = false;

    private readonly Vector3[] targetCorners = new Vector3[4];

    private void Start()
    {
        if (printOnStart)
        {
            PrintTouchCommand();
        }
    }

    [ContextMenu("Print Touch Command For Target")]
    public void PrintTouchCommand()
    {
        if (touchSurfaceRect == null)
        {
            Debug.LogWarning("[OLED Touch Coord] TouchSurfaceRect is not assigned.");
            return;
        }

        if (targetRect == null)
        {
            Debug.LogWarning("[OLED Touch Coord] TargetRect is not assigned.");
            return;
        }

        targetRect.GetWorldCorners(targetCorners);

        Vector2 local0 = WorldToTouchSurfaceLocal(targetCorners[0]);
        Vector2 local1 = WorldToTouchSurfaceLocal(targetCorners[1]);
        Vector2 local2 = WorldToTouchSurfaceLocal(targetCorners[2]);
        Vector2 local3 = WorldToTouchSurfaceLocal(targetCorners[3]);

        float minLocalX = Mathf.Min(local0.x, local1.x, local2.x, local3.x);
        float maxLocalX = Mathf.Max(local0.x, local1.x, local2.x, local3.x);
        float minLocalY = Mathf.Min(local0.y, local1.y, local2.y, local3.y);
        float maxLocalY = Mathf.Max(local0.y, local1.y, local2.y, local3.y);

        Vector2 centerLocal = new Vector2(
            (minLocalX + maxLocalX) * 0.5f,
            (minLocalY + maxLocalY) * 0.5f
        );

        Vector2 centerTouch = LocalToTouch(centerLocal);
        Vector2 minTouch = LocalToTouch(new Vector2(minLocalX, maxLocalY));
        Vector2 maxTouch = LocalToTouch(new Vector2(maxLocalX, minLocalY));

        string sourceText = source == TouchSource.Driver
            ? "driver"
            : "passenger";

        Debug.Log(
            "[OLED Touch Coord] Target="
            + targetRect.name
            + "\nCommand: tap "
            + Mathf.RoundToInt(centerTouch.x)
            + " "
            + Mathf.RoundToInt(centerTouch.y)
            + " "
            + sourceText
            + "\nX Range: "
            + Mathf.RoundToInt(Mathf.Min(minTouch.x, maxTouch.x))
            + " ~ "
            + Mathf.RoundToInt(Mathf.Max(minTouch.x, maxTouch.x))
            + "\nY Range: "
            + Mathf.RoundToInt(Mathf.Min(minTouch.y, maxTouch.y))
            + " ~ "
            + Mathf.RoundToInt(Mathf.Max(minTouch.y, maxTouch.y))
        );
    }

    private Vector2 WorldToTouchSurfaceLocal(Vector3 worldPosition)
    {
        return touchSurfaceRect.InverseTransformPoint(worldPosition);
    }

    private Vector2 LocalToTouch(Vector2 localPoint)
    {
        Rect rect = touchSurfaceRect.rect;

        float globalX = localPoint.x - rect.xMin;

        if (source == TouchSource.Passenger)
        {
            globalX -= passengerOffsetX;
        }

        float touchY = yOriginTop
            ? rect.yMax - localPoint.y
            : localPoint.y - rect.yMin;

        return new Vector2(globalX, touchY);
    }
}
