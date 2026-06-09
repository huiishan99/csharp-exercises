using System;
using UnityEngine;
using UnityEngine.Events;

namespace PushButtonSliderLite
{
    [DisallowMultipleComponent]
    public sealed class ThemeButtonGroup : MonoBehaviour
    {
        [Serializable]
        public sealed class IntEvent : UnityEvent<int> { }

        [Header("按钮列表：顺序对应 0-5")]
        [SerializeField] private ThemeSelectButton[] buttons = new ThemeSelectButton[6];

        [Header("默认选中项")]
        [SerializeField] private bool selectDefaultOnStart = true;
        [SerializeField, Min(0)] private int defaultSelectedIndex = 5;

        [Header("所有选择变化事件：用于视觉更新")]
        public IntEvent onSelectedIndexChanged = new IntEvent();

        [Header("用户点击选择事件：用于Command送信")]
        public IntEvent onUserSelectedIndexChanged = new IntEvent();

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
            {
                SelectIndexInternal(defaultSelectedIndex, false);
            }
        }

        private void OnValidate()
        {
            if (defaultSelectedIndex < 0)
            {
                defaultSelectedIndex = 0;
            }
        }

        private void InitializeButtons()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            if (buttons == null)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }

                buttons[i].Initialize(this, i);
                buttons[i].SetSelected(false);
            }
        }

        public void SelectIndex(int index)
        {
            SelectIndexInternal(index, false);
        }

        public void SelectIndexFromUser(int index)
        {
            SelectIndexInternal(index, true);
        }

        public void NotifyCurrentSelection()
        {
            if (selectedIndex < 0)
            {
                return;
            }

            onSelectedIndexChanged.Invoke(selectedIndex);
        }

        private void SelectIndexInternal(int index, bool isUserAction)
        {
            InitializeButtons();

            if (buttons == null || buttons.Length == 0)
            {
                return;
            }

            if (index < 0 || index >= buttons.Length)
            {
                Debug.LogWarning("ThemeButtonGroup: index " + index + " is out of range.", this);
                return;
            }

            if (buttons[index] == null)
            {
                Debug.LogWarning("ThemeButtonGroup: button at index " + index + " is not assigned.", this);
                return;
            }

            if (selectedIndex == index)
            {
                return;
            }

            selectedIndex = index;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }

                buttons[i].SetSelected(i == selectedIndex);
            }

            onSelectedIndexChanged.Invoke(selectedIndex);

            if (isUserAction)
            {
                onUserSelectedIndexChanged.Invoke(selectedIndex);
            }
        }
    }
}
