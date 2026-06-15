using System.Collections;
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
        private Coroutine reapplyRoutine;

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
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RequestReapplySelectedVisual();
        }

        private void OnDisable()
        {
            if (reapplyRoutine != null)
            {
                StopCoroutine(reapplyRoutine);
                reapplyRoutine = null;
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

            ownerGroup.SelectIndexByUser(buttonIndex);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            ApplySelectedVisual();
        }

        public void SetInteractable(bool canInteract)
        {
            interactable = canInteract;
        }

        private void RequestReapplySelectedVisual()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (reapplyRoutine != null)
            {
                StopCoroutine(reapplyRoutine);
            }

            reapplyRoutine = StartCoroutine(ReapplySelectedVisualNextFrame());
        }

        private IEnumerator ReapplySelectedVisualNextFrame()
        {
            // PressVisualEffect.OnEnable() が released 状態へ戻した後で再適用する。
            yield return null;

            reapplyRoutine = null;
            ApplySelectedVisual();
        }

        private void ApplySelectedVisual()
        {
            if (visualEffect != null)
            {
                visualEffect.SetPressed(isSelected);
            }
        }

        private void ResolveReferences()
        {
            if (visualEffect == null)
            {
                visualEffect = GetComponent<PressVisualEffect>();
            }
        }
    }
}
