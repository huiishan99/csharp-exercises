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

    [SerializeField] private int imageVisibleDistance = 2;
    [SerializeField] private int songTitleVisibleDistance = 1;
    [SerializeField] private int clickableDistance = 2;
    [SerializeField] private int pushEnabledDistance = 1;

    [SerializeField] private float dragDistancePerStep = 260f;
    [SerializeField] private float minSwipeStep = 0.35f;
    [SerializeField] private int maxSwipeStep = 4;
    [SerializeField] private float animationDuration = 0.25f;

    private float dragOffset;
    private bool isDragging;
    private bool isChangingTrackByAnimation;

    private Vector2 dragStartPosition;
    private Coroutine animationRoutine;
    private Coroutine refreshRoutine;

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
        float rawOffset = dragX / dragDistancePerStep;

        dragOffset = Mathf.Clamp(rawOffset, -maxSwipeStep, maxSwipeStep);
        ApplyOffset(dragOffset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        int step = Mathf.RoundToInt(-dragOffset);

        if (Mathf.Abs(dragOffset) < minSwipeStep || step == 0)
        {
            AnimateBack(dragOffset);
            return;
        }

        step = Mathf.Clamp(step, -maxSwipeStep, maxSwipeStep);
        MoveByStep(step, dragOffset);
    }

    public void ForceRefresh()
    {
        StopAnimation();

        isDragging = false;
        RefreshCards();
        ApplyOffset(0f);
    }

    private void MoveByStep(int step)
    {
        MoveByStep(step, dragOffset);
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

        step = Mathf.Clamp(step, -maxSwipeStep, maxSwipeStep);

        float targetOffset = -step;

        StopAnimation();

        animationRoutine = StartCoroutine(
            AnimateOffset(
                startOffset,
                targetOffset,
                () =>
                {
                    isChangingTrackByAnimation = true;
                    musicState.SelectRelative(step);
                    isChangingTrackByAnimation = false;

                    RefreshCards();
                    ApplyOffset(0f);
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

    private void RefreshCards()
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
            int distance = Mathf.Abs(slotOffset);

            bool showImage = distance <= imageVisibleDistance;
            bool showSongTitle = distance <= songTitleVisibleDistance;
            bool showFlow = distance == 0;
            bool enablePush = distance <= pushEnabledDistance;

            card.Initialize(this, slotOffset);
            card.SetSlotOffset(slotOffset);
            card.SetTrack(
                musicState.GetTrackByOffset(slotOffset),
                showImage,
                showSongTitle,
                showFlow,
                enablePush
            );
        }
    }

    private void ApplyOffset(float offset)
    {
        dragOffset = offset;

        if (cardViews == null)
        {
            return;
        }

        for (int i = 0; i < cardViews.Length; i++)
        {
            DemoMusicAlbumCardView card = cardViews[i];

            if (card == null)
            {
                continue;
            }

            int slotOffset = GetSlotOffset(i, cardViews.Length);
            float positionIndex = slotOffset + offset;

            Vector2 position = CalculatePosition(positionIndex);
            float scale = CalculateScale(positionIndex);
            float alpha = CalculateAlpha(positionIndex);

            bool canClick = !isDragging
                && animationRoutine == null
                && Mathf.Abs(offset) < 0.05f
                && Mathf.Abs(slotOffset) <= clickableDistance;

            card.ApplyVisual(position, scale, alpha, canClick);
        }

        UpdateSiblingOrder(offset);
    }

    private Vector2 CalculatePosition(float positionIndex)
    {
        float sign = Mathf.Sign(positionIndex);
        float distance = Mathf.Abs(positionIndex);

        LayerLayout layout = GetInterpolatedLayout(distance);

        float x = layout.xDistance * sign;
        float y = layout.y;

        return new Vector2(x, y);
    }

    private float CalculateScale(float positionIndex)
    {
        return GetInterpolatedLayout(Mathf.Abs(positionIndex)).scale;
    }

    private float CalculateAlpha(float positionIndex)
    {
        return GetInterpolatedLayout(Mathf.Abs(positionIndex)).alpha;
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

        float clamped = Mathf.Clamp(distance, 0f, layerLayouts.Length - 1);
        int lowerIndex = Mathf.FloorToInt(clamped);
        int upperIndex = Mathf.Min(lowerIndex + 1, layerLayouts.Length - 1);
        float rate = clamped - lowerIndex;

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

    private void UpdateSiblingOrder(float offset)
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
                float distanceA = Mathf.Abs(a.SlotOffset + offset);
                float distanceB = Mathf.Abs(b.SlotOffset + offset);
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

    private IEnumerator AnimateOffset(float from, float to, Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float rate = Mathf.Clamp01(elapsed / animationDuration);
            float smoothRate = Mathf.SmoothStep(0f, 1f, rate);

            ApplyOffset(Mathf.Lerp(from, to, smoothRate));

            yield return null;
        }

        ApplyOffset(to);

        animationRoutine = null;
        onComplete?.Invoke();
    }

    private void AnimateBack(float startOffset)
    {
        StopAnimation();

        animationRoutine = StartCoroutine(
            AnimateOffset(
                startOffset,
                0f,
                () =>
                {
                    RefreshCards();
                    ApplyOffset(0f);
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
