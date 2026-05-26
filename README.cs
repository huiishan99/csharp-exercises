using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DemoMusicCarouselView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private DemoMusicState musicState;
    [SerializeField] private DemoPageSwitcher pageSwitcher;
    [SerializeField] private TMP_Text currentTrackText;
    [SerializeField] private DemoMusicAlbumCardView[] cardViews;

    [SerializeField] private float cardSpacing = 360f;
    [SerializeField] private float centerScale = 1.0f;
    [SerializeField] private float sideScale = 0.65f;
    [SerializeField] private float farScale = 0.45f;
    [SerializeField] private float farAlpha = 0.0f;
    [SerializeField] private float sideYOffset = 24f;

    [SerializeField] private float swipeDistance = 280f;
    [SerializeField] private float selectThreshold = 0.35f;
    [SerializeField] private float animationDuration = 0.25f;

    private Vector2 dragStartPosition;
    private float currentOffset;
    private bool isDragging;
    private bool isChangingTrackByAnimation;

    private Coroutine animationRoutine;
    private Coroutine refreshRoutine;

    private void Awake()
    {
        ResolveReferences();
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

    public void OnCardClicked(int slotOffset)
    {
        if (musicState == null || musicState.TrackCount == 0)
        {
            return;
        }

        if (animationRoutine != null || isDragging)
        {
            return;
        }

        if (slotOffset == -1)
        {
            AnimateAndSelect(1f);
            return;
        }

        if (slotOffset == 1)
        {
            AnimateAndSelect(-1f);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (musicState == null || musicState.TrackCount == 0)
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
        currentOffset = Mathf.Clamp(dragX / swipeDistance, -1f, 1f);

        ApplyOffset(currentOffset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        if (currentOffset >= selectThreshold)
        {
            AnimateAndSelect(1f, currentOffset);
            return;
        }

        if (currentOffset <= -selectThreshold)
        {
            AnimateAndSelect(-1f, currentOffset);
            return;
        }

        AnimateBack(currentOffset);
    }

    public void ForceRefresh()
    {
        StopAnimation();

        isDragging = false;
        RefreshCards();
        ApplyOffset(0f);
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

    private void InitializeCards()
    {
        if (cardViews == null)
        {
            return;
        }

        int count = cardViews.Length;

        for (int i = 0; i < count; i++)
        {
            if (cardViews[i] == null)
            {
                continue;
            }

            int slotOffset = GetSlotOffset(i, count);
            cardViews[i].Initialize(this, slotOffset);
        }
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

    private void RefreshCards()
    {
        UpdateCurrentTrackText();

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

            card.Initialize(this, slotOffset);
            card.SetSlotOffset(slotOffset);
            card.SetTrack(musicState.GetTrackByOffset(slotOffset));
        }
    }

    private void UpdateCurrentTrackText()
    {
        if (currentTrackText == null || musicState == null)
        {
            return;
        }

        DemoMusicTrack track = musicState.GetSelectedTrack();

        if (track == null)
        {
            currentTrackText.text = "";
            return;
        }

        currentTrackText.text = track.albumTitle + "\n" + track.songTitle;
    }

    private int GetSlotOffset(int cardIndex, int cardCount)
    {
        int centerIndex = cardCount / 2;
        return cardIndex - centerIndex;
    }

    private void ApplyOffset(float offset)
    {
        currentOffset = offset;

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
            float positionIndex = slotOffset + offset;
            float distance = Mathf.Abs(positionIndex);

            float x = positionIndex * cardSpacing;
            float y = -sideYOffset * Mathf.Clamp01(distance);
            float scale = CalculateScale(distance);
            float alpha = CalculateAlpha(distance);

            bool isSideCard = Mathf.Abs(slotOffset) == 1;
            bool canClick = !isDragging && Mathf.Abs(offset) < 0.05f && isSideCard;

            card.ApplyVisual(x, y, scale, alpha, canClick);
        }
    }

    private float CalculateScale(float distance)
    {
        float clampedDistance = Mathf.Clamp(distance, 0f, 2f);

        if (clampedDistance <= 1f)
        {
            return Mathf.Lerp(centerScale, sideScale, clampedDistance);
        }

        return Mathf.Lerp(sideScale, farScale, clampedDistance - 1f);
    }

    private float CalculateAlpha(float distance)
    {
        float clampedDistance = Mathf.Clamp(distance, 0f, 2f);

        if (clampedDistance <= 1f)
        {
            return 1f;
        }

        return Mathf.Lerp(1f, farAlpha, clampedDistance - 1f);
    }

    private void AnimateAndSelect(float targetOffset)
    {
        AnimateAndSelect(targetOffset, currentOffset);
    }

    private void AnimateAndSelect(float targetOffset, float startOffset)
    {
        StopAnimation();

        animationRoutine = StartCoroutine(
            AnimateOffset(
                startOffset,
                targetOffset,
                () =>
                {
                    if (musicState == null)
                    {
                        return;
                    }

                    isChangingTrackByAnimation = true;

                    if (targetOffset > 0f)
                    {
                        musicState.SelectPrevious();
                    }
                    else
                    {
                        musicState.SelectNext();
                    }

                    isChangingTrackByAnimation = false;

                    RefreshCards();
                    ApplyOffset(0f);
                }
            )
        );
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

    private IEnumerator AnimateOffset(float from, float to, Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float rate = Mathf.Clamp01(elapsed / animationDuration);
            float smoothRate = Mathf.SmoothStep(0f, 1f, rate);
            float offset = Mathf.Lerp(from, to, smoothRate);

            ApplyOffset(offset);

            yield return null;
        }

        ApplyOffset(to);

        animationRoutine = null;
        onComplete?.Invoke();
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
