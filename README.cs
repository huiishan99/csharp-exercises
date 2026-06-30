using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OledTouchDebugOverlay : MonoBehaviour
{
    [Header("Event")]
    [SerializeField] private GuiEventDispatcher eventDispatcher;

    [Header("Canvas")]
    [SerializeField] private Canvas displayCanvas;
    [SerializeField] private RectTransform touchSurfaceRect;

    [Header("OLED Layout")]
    [SerializeField] private float driverWidth = 2650f;
    [SerializeField] private float passengerOffsetX = 2650f;

    // 物理OLEDの高さ。Full/Semiに関係なくDisplay自体は1392。
    [SerializeField] private float physicalDisplayHeight = 1392f;

    [SerializeField] private bool yOriginTop = true;
    [SerializeField] private bool clampCoordinate = true;
    [SerializeField] private bool ignoreTouchOutsideVisibleArea = true;

    [Header("Touch Rotation")]
    [SerializeField] private bool rotateDriverTouch180 = false;
    [SerializeField] private bool rotatePassengerTouch180 = false;

    [Header("Debug Circle")]
    [SerializeField] private bool showDebugCircle = true;
    [SerializeField] private bool showDown = true;
    [SerializeField] private bool showMove = false;
    [SerializeField] private bool showUp = false;
    [SerializeField] private float circleSize = 80f;
    [SerializeField] private float lifeTime = 1.0f;
    [SerializeField] private Color circleColor = new Color(1f, 0f, 0f, 0.8f);

    [Header("Log")]
    [SerializeField] private bool logTouchPosition = true;
    [SerializeField] private bool logIgnoredTouch = false;

    private RectTransform debugLayer;
    private Sprite circleSprite;

    private void Awake()
    {
        ResolveReferences();
        EnsureDebugLayer();
        EnsureCircleSprite();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureDebugLayer();
        EnsureCircleSprite();

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
        if (!showDebugCircle)
        {
            return;
        }

        if (message == null || message.TouchPayload == null)
        {
            return;
        }

        GuiEventTouchPayload payload = message.TouchPayload;

        string sourceText = NormalizeText(payload.source);
        string eventText = NormalizeText(payload.GetTouchEventText());

        if (!ShouldShowEvent(eventText))
        {
            return;
        }

        if (!TryConvertToCanvasLocal(
                payload,
                sourceText,
                out Vector2 canvasLocalPoint,
                out Vector2 sourceLocalPoint,
                out string interpretedHalf
            ))
        {
            return;
        }

        if (logTouchPosition)
        {
            Debug.Log(
                "[OLED Touch Debug Circle] source="
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
                + " interpretedHalf="
                + interpretedHalf
                + " rectWidth="
                + touchSurfaceRect.rect.width
                + " rectHeight="
                + touchSurfaceRect.rect.height
                + " physicalHeight="
                + physicalDisplayHeight
            );
        }

        ShowCircle(canvasLocalPoint);
    }

    private bool ShouldShowEvent(string eventText)
    {
        if (eventText == "down")
        {
            return showDown;
        }

        if (eventText == "move")
        {
            return showMove;
        }

        if (eventText == "up")
        {
            return showUp;
        }

        return true;
    }

    private bool TryConvertToCanvasLocal(
        GuiEventTouchPayload payload,
        string normalizedSource,
        out Vector2 canvasLocalPoint,
        out Vector2 sourceLocalPoint,
        out string interpretedHalf
    )
    {
        canvasLocalPoint = Vector2.zero;
        sourceLocalPoint = Vector2.zero;
        interpretedHalf = "Unknown";

        if (touchSurfaceRect == null)
        {
            Debug.LogWarning("[OLED Touch Debug Circle] Touch Surface Rect is not assigned.");
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
            Debug.LogWarning("[OLED Touch Debug Circle] Unknown source: " + payload.source);
            return false;
        }

        float canvasLocalX = rect.xMin + globalX;
        float canvasLocalY = yOriginTop
            ? rect.yMax - y
            : rect.yMin + y;

        canvasLocalPoint = new Vector2(canvasLocalX, canvasLocalY);
        sourceLocalPoint = new Vector2(x, canvasLocalY);

        interpretedHalf = canvasLocalX < 0f
            ? "DriverHalf"
            : "PassengerHalf";

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

    private void ShowCircle(Vector2 canvasLocalPoint)
    {
        EnsureDebugLayer();
        EnsureCircleSprite();

        if (debugLayer == null || circleSprite == null)
        {
            return;
        }

        GameObject circleObject = new GameObject(
            "TouchDebugCircle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        circleObject.transform.SetParent(debugLayer, false);

        RectTransform rectTransform = circleObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(circleSize, circleSize);
        rectTransform.anchoredPosition = canvasLocalPoint;

        Image image = circleObject.GetComponent<Image>();
        image.sprite = circleSprite;
        image.color = circleColor;
        image.raycastTarget = false;

        circleObject.transform.SetAsLastSibling();

        StartCoroutine(FadeAndDestroy(image, lifeTime));
    }

    private IEnumerator FadeAndDestroy(Image image, float duration)
    {
        if (image == null)
        {
            yield break;
        }

        float elapsed = 0f;
        Color startColor = image.color;

        while (elapsed < duration)
        {
            if (image == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            float rate = duration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / duration);

            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, rate);
            image.color = color;

            yield return null;
        }

        if (image != null)
        {
            Destroy(image.gameObject);
        }
    }

    private void EnsureDebugLayer()
    {
        if (debugLayer != null)
        {
            return;
        }

        if (touchSurfaceRect == null)
        {
            return;
        }

        Transform existing = touchSurfaceRect.Find("OledTouchDebugOverlayLayer");

        if (existing != null)
        {
            debugLayer = existing.GetComponent<RectTransform>();
            return;
        }

        GameObject layerObject = new GameObject(
            "OledTouchDebugOverlayLayer",
            typeof(RectTransform)
        );

        layerObject.transform.SetParent(touchSurfaceRect, false);

        debugLayer = layerObject.GetComponent<RectTransform>();
        debugLayer.anchorMin = Vector2.zero;
        debugLayer.anchorMax = Vector2.one;
        debugLayer.offsetMin = Vector2.zero;
        debugLayer.offsetMax = Vector2.zero;
        debugLayer.pivot = new Vector2(0.5f, 0.5f);

        layerObject.transform.SetAsLastSibling();
    }

    private void EnsureCircleSprite()
    {
        if (circleSprite != null)
        {
            return;
        }

        int size = 64;
        float radius = size * 0.5f - 2f;
        float center = (size - 1) * 0.5f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeTouchDebugCircleTexture";

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                Color color = distance <= radius
                    ? Color.white
                    : Color.clear;

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        circleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );

        circleSprite.name = "RuntimeTouchDebugCircleSprite";
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

    private void LogOutOfVisibleArea(string sourceText, float x, float y, Rect rect)
    {
        if (!logIgnoredTouch)
        {
            return;
        }

        Debug.Log(
            "[OLED Touch Debug Circle] ignored outside visible area source="
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
}
