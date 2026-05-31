using UnityEngine;
using UnityEngine.UI;

public class DemoMusicProgressBarView : MonoBehaviour
{
    [SerializeField] private DemoMusicPlayer musicPlayer;

    [SerializeField] private RectTransform baseBarRect;
    [SerializeField] private RectTransform fillStartRect;
    [SerializeField] private RectTransform fillMiddleRect;
    [SerializeField] private RectTransform fillEndRect;

    [SerializeField] private Image fillStartImage;
    [SerializeField] private Image fillMiddleImage;
    [SerializeField] private Image fillEndImage;

    [SerializeField] private float totalWidth = 520f;
    [SerializeField] private float startWidth = 18f;
    [SerializeField] private float endWidth = 18f;
    [SerializeField] private bool hideFillWhenZero = true;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        float progress = musicPlayer == null
            ? 0f
            : musicPlayer.NormalizedProgress;

        SetProgress(progress);
    }

    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        float width = GetTotalWidth();
        float fillWidth = width * progress;

        bool hasProgress = fillWidth > 0.01f;

        SetFillVisible(!hideFillWhenZero || hasProgress);

        if (!hasProgress)
        {
            return;
        }

        float realStartWidth = Mathf.Min(startWidth, fillWidth);
        float remainingAfterStart = Mathf.Max(0f, fillWidth - realStartWidth);
        float realEndWidth = Mathf.Min(endWidth, remainingAfterStart);
        float middleWidth = Mathf.Max(0f, fillWidth - realStartWidth - realEndWidth);

        SetRectWidth(fillStartRect, realStartWidth);
        SetRectWidth(fillMiddleRect, middleWidth);
        SetRectWidth(fillEndRect, realEndWidth);

        SetRectX(fillStartRect, realStartWidth * 0.5f);
        SetRectX(fillMiddleRect, realStartWidth + middleWidth * 0.5f);
        SetRectX(fillEndRect, realStartWidth + middleWidth + realEndWidth * 0.5f);
    }

    private float GetTotalWidth()
    {
        if (baseBarRect != null && baseBarRect.rect.width > 1f)
        {
            return baseBarRect.rect.width;
        }

        return totalWidth;
    }

    private void SetRectWidth(RectTransform target, float width)
    {
        if (target == null)
        {
            return;
        }

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    private void SetRectX(RectTransform target, float x)
    {
        if (target == null)
        {
            return;
        }

        Vector2 position = target.anchoredPosition;
        position.x = x;
        target.anchoredPosition = position;
    }

    private void SetFillVisible(bool visible)
    {
        if (fillStartImage != null)
        {
            fillStartImage.enabled = visible;
        }

        if (fillMiddleImage != null)
        {
            fillMiddleImage.enabled = visible;
        }

        if (fillEndImage != null)
        {
            fillEndImage.enabled = visible;
        }
    }

    private void ResolveReferences()
    {
        if (musicPlayer == null)
        {
            musicPlayer = FindFirstObjectByType<DemoMusicPlayer>();
        }
    }
}
