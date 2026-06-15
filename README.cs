using System;
using System.Collections;
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
        [SerializeField, Min(0)] private int defaultSelectedIndex = 0;

        [Header("选择变化事件：视觉更新用")]
        public IntEvent onSelectedIndexChanged = new IntEvent();

        [Header("用户选择事件：Command发送用")]
        public IntEvent onUserSelectedIndexChanged = new IntEvent();

        private int selectedIndex = -1;
        private bool initialized;
        private Coroutine reapplyRoutine;

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

        private void OnEnable()
        {
            InitializeButtons();
            RequestReapplySelectionVisual();
        }

        private void Start()
        {
            if (selectDefaultOnStart && selectedIndex < 0)
            {
                // 初期選択はCommandを送らない。
                SelectIndexInternal(defaultSelectedIndex, true, false);
            }

            RequestReapplySelectionVisual();
        }

        private void OnDisable()
        {
            if (reapplyRoutine != null)
            {
                StopCoroutine(reapplyRoutine);
                reapplyRoutine = null;
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
            SelectIndexInternal(index, true, false);
        }

        public void SelectIndexByUser(int index)
        {
            SelectIndexInternal(index, true, true);
        }

        public void NotifyCurrentSelection()
        {
            if (selectedIndex < 0)
            {
                return;
            }

            ReapplyButtonVisuals();
            onSelectedIndexChanged.Invoke(selectedIndex);
        }

        private void SelectIndexInternal(
            int index,
            bool notifyVisual,
            bool notifyUserCommand
        )
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

            bool indexChanged = selectedIndex != index;
            selectedIndex = index;

            ReapplyButtonVisuals();

            if (notifyVisual && indexChanged)
            {
                onSelectedIndexChanged.Invoke(selectedIndex);
            }

            if (notifyUserCommand && indexChanged)
            {
                onUserSelectedIndexChanged.Invoke(selectedIndex);
            }
        }

        private void RequestReapplySelectionVisual()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (reapplyRoutine != null)
            {
                StopCoroutine(reapplyRoutine);
            }

            reapplyRoutine = StartCoroutine(ReapplySelectionVisualNextFrame());
        }

        private IEnumerator ReapplySelectionVisualNextFrame()
        {
            // PressVisualEffect.OnEnable() が normal に戻した後で再適用する。
            yield return null;

            reapplyRoutine = null;

            if (selectedIndex < 0)
            {
                yield break;
            }

            ReapplyButtonVisuals();
            onSelectedIndexChanged.Invoke(selectedIndex);
        }

        private void ReapplyButtonVisuals()
        {
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

                buttons[i].SetSelected(i == selectedIndex);
            }
        }
    }
}
