using System;
using UnityEngine;
using UnityEngine.Events;

namespace PushButtonSliderLite
{
    /// <summary>
    /// 只负责一组主题按钮的“六选一”状态管理。
    /// 不负责具体图片替换，也不负责按钮视觉细节。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThemeButtonGroup : MonoBehaviour
    {
        [Serializable]
        public sealed class IntEvent : UnityEvent<int> { }

        [Header("按钮列表：按 1,2,3,4,5,6 的顺序放入")]
        [SerializeField] private ThemeSelectButton[] buttons = new ThemeSelectButton[6];

        [Header("默认选中项")]
        [SerializeField] private bool selectDefaultOnStart = true;
        [SerializeField, Min(0)] private int defaultSelectedIndex = 5;

        [Header("选择变化事件")]
        public IntEvent onSelectedIndexChanged = new IntEvent();

        private int selectedIndex = -1;
        private bool initialized;

        public int SelectedIndex
        {
            get { return selectedIndex; }
        }

        public int ButtonCount
        {
            get { return buttons == null ? 0 : buttons.Length; }
        }

        private void Awake()
        {
            InitializeButtons();
        }

        private void Start()
        {
            if (selectDefaultOnStart)
                SelectIndex(defaultSelectedIndex);
        }

        private void OnValidate()
        {
            if (defaultSelectedIndex < 0)
                defaultSelectedIndex = 0;
        }

        private void InitializeButtons()
        {
            if (initialized)
                return;

            initialized = true;

            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;

                buttons[i].Initialize(this, i);
                buttons[i].SetSelected(false);
            }
        }

        /// <summary>
        /// 选择指定 index 的按钮。index 从 0 开始。
        /// 6号按钮对应 index = 5。
        /// </summary>
        public void SelectIndex(int index)
        {
            InitializeButtons();

            if (buttons == null || buttons.Length == 0)
                return;

            if (index < 0 || index >= buttons.Length)
            {
                Debug.LogWarning($"ThemeButtonGroup: index {index} is out of range.", this);
                return;
            }

            if (buttons[index] == null)
            {
                Debug.LogWarning($"ThemeButtonGroup: button at index {index} is not assigned.", this);
                return;
            }

            if (selectedIndex == index)
                return;

            selectedIndex = index;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;

                buttons[i].SetSelected(i == selectedIndex);
            }

            onSelectedIndexChanged.Invoke(selectedIndex);
        }

        /// <summary>
        /// 重新应用当前选中状态。用于手动刷新外部绑定。
        /// </summary>
        public void NotifyCurrentSelection()
        {
            if (selectedIndex < 0)
                return;

            onSelectedIndexChanged.Invoke(selectedIndex);
        }
    }
}
