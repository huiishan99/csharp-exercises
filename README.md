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
        button.transition = Selectable.Transition.None;

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public void SetState(bool isSelected, bool isClickable)
    {
        Button.interactable = isClickable;

        if (targetImage == null)
        {
            return;
        }

        if (isSelected)
        {
            targetImage.color = selectedColor;
            return;
        }

        targetImage.color = isClickable ? normalColor : disabledColor;
    }
}
