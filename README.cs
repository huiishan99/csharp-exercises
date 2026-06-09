using UnityEngine;
using UnityEngine.EventSystems;

namespace PushButtonSliderLite
{
    [DisallowMultipleComponent]
    public sealed class ThemeSelectButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("视觉效果")]
        [SerializeField] private PressVisualEffect visualEffect;

        [Header("输入状态")]
        [SerializeField] private bool interactable = true;

        private ThemeButtonGroup ownerGroup;
        private int buttonIndex = -1;
        private bool isSelected;

        public int ButtonIndex
        {
            get { return buttonIndex; }
        }

        public bool IsSelected
        {
            get { return isSelected; }
        }

        private void Awake()
        {
            if (visualEffect == null)
            {
                visualEffect = GetComponent<PressVisualEffect>();
            }
        }

        public void Initialize(ThemeButtonGroup group, int index)
        {
            ownerGroup = group;
            buttonIndex = index;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable)
            {
                return;
            }

            if (ownerGroup == null)
            {
                return;
            }

            ownerGroup.SelectIndexFromUser(buttonIndex);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (visualEffect != null)
            {
                visualEffect.SetPressed(selected);
            }
        }

        public void SetInteractable(bool canInteract)
        {
            interactable = canInteract;
        }
    }
}
