using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DemoSourceButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("State View")]
    [SerializeField] private Image stateOverlay;
    [SerializeField] private Graphic labelGraphic;

    [Header("Push Button Visual")]
    [SerializeField] private PushButtonSliderLite.PushButtonInput pushButtonInput;
    [SerializeField] private PushButtonSliderLite.PressVisualEffect pressVisualEffect;
    [SerializeField] private bool disableInputWhenSelected = true;
    [SerializeField] private bool releaseOnPointerExit = true;

    [Header("Overlay Color")]
    [SerializeField] private Color normalOverlayColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color selectedOverlayColor = new Color(0.1f, 0.55f, 1f, 0.45f);
    [SerializeField] private Color disabledOverlayColor = new Color(0f, 0f, 0f, 0.6f);

    [Header("Label Color")]
    [SerializeField] private Color normalLabelColor = Color.white;
    [SerializeField] private Color selectedLabelColor = Color.white;
    [SerializeField] private Color disabledLabelColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    private Button button;
    private bool canReceiveInput;
    private bool isPressed;

    public Button Button
    {
        get
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            return button;
        }
    }

    private void Reset()
    {
        AutoFindReferences();
    }

    private void Awake()
    {
        button = GetComponent<Button>();

        // Button 自带色变化会和我们的状态显示冲突。
        button.transition = Selectable.Transition.None;

        AutoFindReferences();
        DisablePushButtonInput();
        ReleasePressEffect();

        if (stateOverlay != null)
        {
            stateOverlay.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        DisablePushButtonInput();
        ReleasePressEffect();
    }

    private void OnDisable()
    {
        ReleasePressEffect();
    }

    public void SetVisible(bool isVisible)
    {
        if (!isVisible)
        {
            ReleasePressEffect();
        }

        gameObject.SetActive(isVisible);
    }

    // 新接口：SourcePanel 推荐使用这个。
    public void SetState(bool isSelected, bool isClickable)
    {
        bool shouldReceiveInput = isClickable;

        if (disableInputWhenSelected && isSelected)
        {
            shouldReceiveInput = false;
        }

        canReceiveInput = shouldReceiveInput;
        Button.interactable = canReceiveInput;

        // 子对象 PushButtonInput 不直接接收输入，避免 disabled / selected 时还有特效。
        DisablePushButtonInput();

        if (!canReceiveInput)
        {
            ReleasePressEffect();
        }

        ApplyOverlay(isSelected, isClickable);
        ApplyLabelColor(isSelected, isClickable);
    }

    // 旧接口兼容：如果 DemoSourcePanel.cs 还在用 enum，也不会报错。
    public void SetState(DemoSourceButtonVisualState visualState, bool isClickable)
    {
        bool isSelected = visualState == DemoSourceButtonVisualState.Selected;

        if (visualState == DemoSourceButtonVisualState.Disabled)
        {
            isClickable = false;
        }

        SetState(isSelected, isClickable);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!canReceiveInput)
        {
            return;
        }

        isPressed = true;

        if (pressVisualEffect != null)
        {
            pressVisualEffect.SetPressed(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ReleasePressEffect();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (releaseOnPointerExit)
        {
            ReleasePressEffect();
        }
    }

    private void AutoFindReferences()
    {
        if (pushButtonInput == null)
        {
            pushButtonInput = GetComponentInChildren<PushButtonSliderLite.PushButtonInput>(true);
        }

        if (pressVisualEffect == null)
        {
            pressVisualEffect = GetComponentInChildren<PushButtonSliderLite.PressVisualEffect>(true);
        }

        if (stateOverlay == null)
        {
            Transform overlayTransform = transform.Find("StateOverlay");

            if (overlayTransform != null)
            {
                stateOverlay = overlayTransform.GetComponent<Image>();
            }
        }
    }

    private void DisablePushButtonInput()
    {
        if (pushButtonInput == null)
        {
            return;
        }

        pushButtonInput.enabled = false;
    }

    private void ReleasePressEffect()
    {
        if (!isPressed && pressVisualEffect == null)
        {
            return;
        }

        isPressed = false;

        if (pressVisualEffect != null)
        {
            pressVisualEffect.SetPressed(false);
        }
    }

    private void ApplyOverlay(bool isSelected, bool isClickable)
    {
        if (stateOverlay == null)
        {
            return;
        }

        if (isSelected)
        {
            stateOverlay.enabled = true;
            stateOverlay.color = selectedOverlayColor;
            return;
        }

        if (!isClickable)
        {
            stateOverlay.enabled = true;
            stateOverlay.color = disabledOverlayColor;
            return;
        }

        stateOverlay.enabled = false;
        stateOverlay.color = normalOverlayColor;
    }

    private void ApplyLabelColor(bool isSelected, bool isClickable)
    {
        if (labelGraphic == null)
        {
            return;
        }

        if (isSelected)
        {
            labelGraphic.color = selectedLabelColor;
            return;
        }

        labelGraphic.color = isClickable ? normalLabelColor : disabledLabelColor;
    }
}
