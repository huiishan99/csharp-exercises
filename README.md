using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DemoSourceButton : MonoBehaviour
{
    [SerializeField] private Image targetImage;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.7f, 1.0f, 1.0f);
    [SerializeField] private Color disabledColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);

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

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        // Button 默认的 Color Tint 会覆盖手动颜色，所以这里关闭。
        button.transition = Selectable.Transition.None;
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void SetState(DemoSourceButtonVisualState visualState, bool canClick)
    {
        Button.interactable = canClick;

        if (targetImage == null)
        {
            return;
        }

        switch (visualState)
        {
            case DemoSourceButtonVisualState.Selected:
                targetImage.color = selectedColor;
                break;

            case DemoSourceButtonVisualState.Disabled:
                targetImage.color = disabledColor;
                break;

            default:
                targetImage.color = normalColor;
                break;
        }
    }
}
