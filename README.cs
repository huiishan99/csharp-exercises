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
    [SerializeField] private GameObject flowObject;

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

    public float CurrentDistance
    {
        get { return Mathf.Abs(slotOffset); }
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
        bool shouldShowFlow,
        bool shouldEnablePush
    )
    {
        currentTrack = track;
        showImage = shouldShowImage;
        showSongTitle = shouldShowSongTitle;
        isCenter = shouldShowFlow;
        pushEnabled = shouldEnablePush;

        isPressed = false;

        ApplySprite();

        if (songTitleImage != null)
        {
            songTitleImage.gameObject.SetActive(showSongTitle && track != null && track.songTitleSprite != null);
            songTitleImage.sprite = track == null ? null : track.songTitleSprite;
            songTitleImage.preserveAspect = true;
        }

        if (flowObject != null)
        {
            flowObject.SetActive(shouldShowFlow);
        }
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
        if (!pushEnabled || currentTrack == null || currentTrack.pushedSprite == null)
        {
            return;
        }

        isPressed = true;
        ApplySprite();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        ApplySprite();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        ApplySprite();
    }

    private void HandleClicked()
    {
        if (owner == null)
        {
            return;
        }

        owner.OnCardClicked(slotOffset);
    }

    private void ApplySprite()
    {
        if (cardImage == null)
        {
            return;
        }

        bool hasTrack = currentTrack != null;
        bool shouldShow = showImage && hasTrack;

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
}
