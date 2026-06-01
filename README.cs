using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class DemoMusicScrollLinkedView : MonoBehaviour
{
    [SerializeField] private DemoMusicCarouselView carouselView;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float followX = 90f;
    [SerializeField] private float followY = -18f;
    [SerializeField] private float scaleReduction = 0.05f;
    [SerializeField] private float alphaReduction = 0.15f;

    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private Vector3 initialScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;
        initialScale = rectTransform.localScale;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (carouselView != null)
        {
            carouselView.CarouselMotionChanged -= OnCarouselMotionChanged;
            carouselView.CarouselMotionChanged += OnCarouselMotionChanged;
        }

        ResetVisual();
    }

    private void OnDisable()
    {
        if (carouselView != null)
        {
            carouselView.CarouselMotionChanged -= OnCarouselMotionChanged;
        }

        ResetVisual();
    }

    private void OnCarouselMotionChanged(float residualOffset, bool isMoving)
    {
        float amount = Mathf.Clamp01(Mathf.Abs(residualOffset) * 2f);

        Vector2 nextPosition = initialPosition;
        nextPosition.x += residualOffset * followX;
        nextPosition.y += amount * followY;

        rectTransform.anchoredPosition = nextPosition;

        float scale = 1f - amount * scaleReduction;
        rectTransform.localScale = initialScale * scale;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f - amount * alphaReduction;
        }
    }

    private void ResetVisual()
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = initialPosition;
        rectTransform.localScale = initialScale;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void ResolveReferences()
    {
        if (carouselView == null)
        {
            carouselView = GetComponentInParent<DemoMusicCarouselView>();
        }

        if (carouselView == null)
        {
            carouselView = FindFirstObjectByType<DemoMusicCarouselView>();
        }
    }
}
