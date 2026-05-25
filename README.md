using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DemoSourceButton : MonoBehaviour
{
    [SerializeField] private Image stateOverlay;
    [SerializeField] private Graphic labelGraphic;

    [SerializeField] private Color normalOverlayColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color selectedOverlayColor = new Color(0.1f, 0.55f, 1f, 0.45f);
    [SerializeField] private Color disabledOverlayColor = new Color(0f, 0f, 0f, 0.6f);

    [SerializeField] private Color normalLabelColor = Color.white;
    [SerializeField] private Color selectedLabelColor = Color.white;
    [SerializeField] private Color disabledLabelColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    private Button button;

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

    private void Awake()
    {
        button = GetComponent<Button>();

        // Button 自带的色变化会干扰我们自己的状态显示。
        button.transition = Selectable.Transition.None;

        if (stateOverlay != null)
        {
            stateOverlay.raycastTarget = false;
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void SetState(bool isSelected, bool isClickable)
    {
        Button.interactable = isClickable;

        ApplyOverlay(isSelected, isClickable);
        ApplyLabelColor(isSelected, isClickable);
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
