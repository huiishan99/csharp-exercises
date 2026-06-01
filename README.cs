using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DemoMusicCarouselView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Serializable]
    private class LayerLayout
    {
        public float xDistance;
        public float y;
        public float scale;
        public float alpha;
    }

    [SerializeField] private DemoMusicState musicState;
    [SerializeField] private DemoPageSwitcher pageSwitcher;

    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [SerializeField] private DemoMusicAlbumCardView[] cardViews = new DemoMusicAlbumCardView[9];

    [SerializeField] private LayerLayout[] layerLayouts =
    {
        new LayerLayout { xDistance = 0f,   y = 0f,    scale = 1.00f, alpha = 1.00f },
        new LayerLayout { xDistance = 300f, y = -30f,  scale = 0.68f, alpha = 1.00f },
        new LayerLayout { xDistance = 500f, y = -110f, scale = 0.52f, alpha = 0.90f },
        new LayerLayout { xDistance = 650f, y = -190f, scale = 0.38f, alpha = 0.35f },
        new LayerLayout { xDistance = 760f, y = -260f, scale = 0.28f, alpha = 0.18f }
    };

    [SerializeField] private float imageVisibleDistance = 2.25f;
    [SerializeField] private float songTitleVisibleDistance = 1.25f;
    [SerializeField] private int clickableDistance = 2;
    [SerializeField] private int pushEnabledDistance = 1;

    [SerializeField] private float dragDistancePerStep = 260f;
    [SerializeField] private float minSwipeStep = 0.35f;
    [SerializeField] private int maxCommitStep = 6;
    [SerializeField] private float animationDuration = 0.25f;

    private float rawOffset;
    private bool isDragging;
    private bool isChangingTrackByAnimation;

    private Vector2 dragStartPosition;
    private Coroutine animationRoutine;
    private Coroutine refreshRoutine;

    public event Action<float, bool> CarouselMotionChanged;

    private void Awake()
    {
        ResolveReferences();
        RegisterButtons();
        InitializeCards();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
        ForceRefresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StopAnimation();
        StopRefreshRoutine();

        isDragging = false;
        isChangingTrackByAnimation = false;
    }

    public void MovePrevious()
    {
        MoveByStep(-1);
    }

    public void MoveNext()
    {
        MoveByStep(1);
    }

    public void OnCardClicked(int slotOffset)
    {
        if (slotOffset == 0)
        {
            return;
        }

        if (Mathf.Abs(slotOffset) > clickableDistance)
        {
            return;
        }

        MoveByStep(slotOffset);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanOperate())
        {
            return;
        }

        StopAnimation();

        isDragging = true;
        dragStartPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        float dragX = eventData.position.x - dragStartPosition.x;
        rawOffset = dragX / dragDistancePerStep;

        ApplyRawOffset(rawOffset, true);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        int step = -Mathf.RoundToInt(rawOffset);

        if (Mathf.Abs(rawOffset) < minSwipeStep || step == 0)
        {
            AnimateBack(rawOffset);
            return;
        }

        step = Mathf.Clamp(step, -maxCommitStep, maxCommitStep);
        MoveByStep(step, rawOffset);
    }

    public void ForceRefresh()
    {
        StopAnimation();

        isDragging = false;
        rawOffset = 0f;

        ApplyPreview(0, 0f, false);
    }

    private void MoveByStep(int step)
    {
        MoveByStep(step, rawOffset);
    }

    private void MoveByStep(int step, float startOffset)
    {
        if (!CanOperate())
        {
            return;
        }

        if (step == 0)
        {
            AnimateBack(startOffset);
            return;
        }

        step = Mathf.Clamp(step, -maxCommitStep, maxCommitStep);

        float targetRawOffset = -step;

        StopAnimation();

        animationRoutine = StartCoroutine(
            AnimateRawOffset(
                startOffset,
                targetRawOffset,
                () =>
                {
                    isChangingTrackByAnimation = true;
                    musicState.SelectRelative(step);
                    isChangingTrackByAnimation = false;

                    rawOffset = 0f;
                    ApplyPreview(0, 0f, false);
                }
            )
        );
    }

    private void RegisterButtons()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(MovePrevious);
            leftButton.onClick.AddListener(MovePrevious);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(MoveNext);
            rightButton.onClick.AddListener(MoveNext);
        }
    }

    private void InitializeCards()
    {
        if (cardViews == null)
        {
            return;
        }

        int count = cardViews.Length;

        for (int i = 0; i < count; i++)
        {
            DemoMusicAlbumCardView card = cardViews[i];

            if (card == null)
            {
                continue;
            }

            int slotOffset = GetSlotOffset(i, count);
            card.Initialize(this, slotOffset);
        }
    }

    private void ApplyRawOffset(float offset, bool moving)
    {
        int previewStep = -Mathf.RoundToInt(offset);
        float residualOffset = offset + previewStep;

        ApplyPreview(previewStep, residualOffset, moving);
    }

    private void ApplyPreview(int previewStep, float residualOffset, bool moving)
    {
        if (musicState == null || cardViews == null)
        {
            return;
        }

        int count = cardViews.Length;

        for (int i = 0; i < count; i++)
        {
            DemoMusicAlbumCardView card = cardViews[i];

            if (card == null)
            {
                continue;
            }

            int slotOffset = GetSlotOffset(i, count);
            float visualIndex = slotOffset + residualOffset;
            float visualDistance = Mathf.Abs(visualIndex);

            int trackOffset = previewStep + slotOffset;

            bool showImage = visualDistance <= imageVisibleDistance;
            bool showSongTitle = visualDistance <= songTitleVisibleDistance;
            bool useSelectedSprite = visualDistance < 0.55f;
            bool enablePush = Mathf.Abs(slotOffset) <= pushEnabledDistance && !moving;

            Vector2 position = CalculatePosition(visualIndex);
            float scale = CalculateScale(visualIndex);
            float alpha = CalculateAlpha(visualIndex);

            bool canClick = !moving
                && animationRoutine == null
                && Mathf.Abs(residualOffset) < 0.05f
                && Mathf.Abs(slotOffset) <= clickableDistance;

            card.Initialize(this, slotOffset);
            card.SetSlotOffset(slotOffset);
            card.SetTrack(
                musicState.GetTrackByOffset(trackOffset),
                showImage,
                showSongTitle,
                useSelectedSprite,
                enablePush
            );

            card.ApplyVisual(position, scale, alpha, canClick);
        }

        UpdateSiblingOrder(residualOffset);
        CarouselMotionChanged?.Invoke(residualOffset, moving);
    }

    private Vector2 CalculatePosition(float visualIndex)
    {
        float sign = Mathf.Sign(visualIndex);
        float distance = Mathf.Abs(visualIndex);

        LayerLayout layout = GetInterpolatedLayout(distance);

        float x = layout.xDistance * sign;
        float y = layout.y;

        return new Vector2(x, y);
    }

    private float CalculateScale(float visualIndex)
    {
        return GetInterpolatedLayout(Mathf.Abs(visualIndex)).scale;
    }

    private float CalculateAlpha(float visualIndex)
    {
        return GetInterpolatedLayout(Mathf.Abs(visualIndex)).alpha;
    }

    private LayerLayout GetInterpolatedLayout(float distance)
    {
        if (layerLayouts == null || layerLayouts.Length == 0)
        {
            return new LayerLayout
            {
                xDistance = 0f,
                y = 0f,
                scale = 1f,
                alpha = 1f
            };
        }

        float maxIndex = layerLayouts.Length - 1;
        float clampedDistance = Mathf.Clamp(distance, 0f, maxIndex);

        int lowerIndex = Mathf.FloorToInt(clampedDistance);
        int upperIndex = Mathf.Min(lowerIndex + 1, layerLayouts.Length - 1);
        float rate = clampedDistance - lowerIndex;

        LayerLayout lower = layerLayouts[lowerIndex];
        LayerLayout upper = layerLayouts[upperIndex];

        return new LayerLayout
        {
            xDistance = Mathf.Lerp(lower.xDistance, upper.xDistance, rate),
            y = Mathf.Lerp(lower.y, upper.y, rate),
            scale = Mathf.Lerp(lower.scale, upper.scale, rate),
            alpha = Mathf.Lerp(lower.alpha, upper.alpha, rate)
        };
    }

    private void UpdateSiblingOrder(float residualOffset)
    {
        List<DemoMusicAlbumCardView> validCards = new List<DemoMusicAlbumCardView>();

        for (int i = 0; i < cardViews.Length; i++)
        {
            if (cardViews[i] != null)
            {
                validCards.Add(cardViews[i]);
            }
        }

        validCards.Sort(
            (a, b) =>
            {
                float distanceA = Mathf.Abs(a.SlotOffset + residualOffset);
                float distanceB = Mathf.Abs(b.SlotOffset + residualOffset);

                return distanceB.CompareTo(distanceA);
            }
        );

        for (int i = 0; i < validCards.Count; i++)
        {
            validCards[i].transform.SetSiblingIndex(i);
        }
    }

    private int GetSlotOffset(int cardIndex, int cardCount)
    {
        int centerIndex = cardCount / 2;
        return cardIndex - centerIndex;
    }

    private void OnTrackChanged(int index, DemoMusicTrack track)
    {
        if (isChangingTrackByAnimation)
        {
            return;
        }

        if (isDragging || animationRoutine != null)
        {
            return;
        }

        ForceRefresh();
    }

    private void OnPageChanged(DemoPageId pageId)
    {
        RequestRefreshNextFrame();
    }

    private void RequestRefreshNextFrame()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        StopRefreshRoutine();
        refreshRoutine = StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;

        refreshRoutine = null;
        ForceRefresh();
    }

    private IEnumerator AnimateRawOffset(float from, float to, Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float rate = Mathf.Clamp01(elapsed / animationDuration);
            float smoothRate = Mathf.SmoothStep(0f, 1f, rate);

            rawOffset = Mathf.Lerp(from, to, smoothRate);
            ApplyRawOffset(rawOffset, true);

            yield return null;
        }

        rawOffset = to;
        ApplyRawOffset(rawOffset, true);

        animationRoutine = null;
        onComplete?.Invoke();
    }

    private void AnimateBack(float startOffset)
    {
        StopAnimation();

        animationRoutine = StartCoroutine(
            AnimateRawOffset(
                startOffset,
                0f,
                () =>
                {
                    rawOffset = 0f;
                    ApplyPreview(0, 0f, false);
                }
            )
        );
    }

    private bool CanOperate()
    {
        return musicState != null
            && musicState.TrackCount > 0
            && animationRoutine == null;
    }

    private void ResolveReferences()
    {
        if (musicState == null)
        {
            musicState = FindFirstObjectByType<DemoMusicState>();
        }

        if (pageSwitcher == null)
        {
            pageSwitcher = FindFirstObjectByType<DemoPageSwitcher>();
        }
    }

    private void SubscribeEvents()
    {
        if (musicState != null)
        {
            musicState.TrackChanged -= OnTrackChanged;
            musicState.TrackChanged += OnTrackChanged;
        }

        if (pageSwitcher != null)
        {
            pageSwitcher.PageChanged -= OnPageChanged;
            pageSwitcher.PageChanged += OnPageChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (musicState != null)
        {
            musicState.TrackChanged -= OnTrackChanged;
        }

        if (pageSwitcher != null)
        {
            pageSwitcher.PageChanged -= OnPageChanged;
        }
    }

    private void StopAnimation()
    {
        if (animationRoutine == null)
        {
            return;
        }

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }

    private void StopRefreshRoutine()
    {
        if (refreshRoutine == null)
        {
            return;
        }

        StopCoroutine(refreshRoutine);
        refreshRoutine = null;
    }
}
