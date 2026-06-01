using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Button))]
public class DemoMusicAlbumCardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Image songTitleImage;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Button button;

    private DemoMusicCarouselView owner;
    private DemoMusicTrack currentTrack;

    private int slotOffset;
    private bool showImage;
    private bool showSongTitle;
    private bool isCenter;
    private bool pushEnabled;
    private bool isPressed;

    public int SlotOffset
    {
        get { return slotOffset; }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        button = GetComponent<Button>();

        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(HandleClicked);

        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
    }

    public void Initialize(DemoMusicCarouselView carousel, int offset)
    {
        owner = carousel;
        slotOffset = offset;
    }

    public void SetSlotOffset(int offset)
    {
        slotOffset = offset;
    }

    public void SetTrack(
        DemoMusicTrack track,
        bool shouldShowImage,
        bool shouldShowSongTitle,
        bool shouldUseSelectedSprite,
        bool shouldEnablePush
    )
    {
        currentTrack = track;
        showImage = shouldShowImage;
        showSongTitle = shouldShowSongTitle;
        isCenter = shouldUseSelectedSprite;
        pushEnabled = shouldEnablePush;

        isPressed = false;

        ApplyCardSprite();
        ApplySongTitle();
    }

    public void ApplyVisual(Vector2 position, float scale, float alpha, bool canClick)
    {
        rectTransform.anchoredPosition = position;
        rectTransform.localScale = Vector3.one * scale;

        canvasGroup.alpha = alpha;
        button.interactable = canClick;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!pushEnabled)
        {
            return;
        }

        if (currentTrack == null || currentTrack.pushedSprite == null)
        {
            return;
        }

        isPressed = true;
        ApplyCardSprite();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        ApplyCardSprite();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        ApplyCardSprite();
    }

    private void HandleClicked()
    {
        if (owner == null)
        {
            return;
        }

        owner.OnCardClicked(slotOffset);
    }

    private void ApplyCardSprite()
    {
        if (cardImage == null)
        {
            return;
        }

        bool shouldShow = showImage && currentTrack != null;
        cardImage.gameObject.SetActive(shouldShow);

        if (!shouldShow)
        {
            return;
        }

        if (isPressed && currentTrack.pushedSprite != null)
        {
            cardImage.sprite = currentTrack.pushedSprite;
        }
        else if (isCenter && currentTrack.selectedSprite != null)
        {
            cardImage.sprite = currentTrack.selectedSprite;
        }
        else
        {
            cardImage.sprite = currentTrack.normalSprite;
        }

        cardImage.preserveAspect = true;
    }

    private void ApplySongTitle()
    {
        if (songTitleImage == null)
        {
            return;
        }

        bool shouldShow = showSongTitle
            && currentTrack != null
            && currentTrack.songTitleSprite != null;

        songTitleImage.gameObject.SetActive(shouldShow);

        if (!shouldShow)
        {
            return;
        }

        songTitleImage.sprite = currentTrack.songTitleSprite;
        songTitleImage.preserveAspect = true;
    }
}
