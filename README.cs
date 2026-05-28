using UnityEngine;
using UnityEngine.EventSystems;

namespace PushButtonSliderLite
{
    /// <summary>
    /// 只负责单个主题按钮的点击输入和自身选中视觉。
    /// 不负责切换图片、不负责管理其他按钮。
    /// </summary>
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
                visualEffect = GetComponent<PressVisualEffect>();
        }

        /// <summary>
        /// 由 ThemeButtonGroup 初始化。不要在 Inspector 手动调用。
        /// </summary>
        public void Initialize(ThemeButtonGroup group, int index)
        {
            ownerGroup = group;
            buttonIndex = index;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable)
                return;

            if (ownerGroup == null)
                return;

            ownerGroup.SelectIndex(buttonIndex);
        }

        /// <summary>
        /// 设置是否处于“按住/选中”状态。
        /// 这里使用 PressVisualEffect 的 pressed 表现作为 selected 表现。
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (visualEffect != null)
                visualEffect.SetPressed(selected);
        }

        public void SetInteractable(bool canInteract)
        {
            interactable = canInteract;
        }
    }
}
