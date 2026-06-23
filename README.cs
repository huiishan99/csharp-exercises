using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class DemoViewportTopAligner : MonoBehaviour
{
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private float viewportWidth = 5300f;

    [Header("Apply")]
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool applyEveryLateUpdate = true;

    private void Awake()
    {
        ResolveReferences();

        if (applyOnEnable)
        {
            ApplyTopAlignment();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (applyOnEnable)
        {
            ApplyTopAlignment();
        }
    }

    private void LateUpdate()
    {
        if (!applyEveryLateUpdate)
        {
            return;
        }

        ApplyTopAlignment();
    }

    [ContextMenu("Apply Top Alignment")]
    public void ApplyTopAlignment()
    {
        ResolveReferences();

        if (viewportRect == null)
        {
            return;
        }

        float currentHeight = viewportRect.rect.height;

        viewportRect.anchorMin = new Vector2(0.5f, 1f);
        viewportRect.anchorMax = new Vector2(0.5f, 1f);
        viewportRect.pivot = new Vector2(0.5f, 1f);

        viewportRect.anchoredPosition = Vector2.zero;

        viewportRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewportWidth);
        viewportRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);
    }

    private void ResolveReferences()
    {
        if (viewportRect == null)
        {
            viewportRect = GetComponent<RectTransform>();
        }
    }
}
