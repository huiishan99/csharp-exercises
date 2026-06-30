using UnityEngine;

[DisallowMultipleComponent]
public class CursorVisibilityController : MonoBehaviour
{
    [SerializeField] private bool hideCursorOnStart = true;
    [SerializeField] private bool reapplyOnFocus = true;

    [Header("Debug")]
    [SerializeField] private bool allowToggleWithKey = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;

    private bool cursorHidden;

    private void Awake()
    {
        SetCursorHidden(hideCursorOnStart);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            return;
        }

        if (!reapplyOnFocus)
        {
            return;
        }

        ApplyCursorState();
    }

    private void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (!allowToggleWithKey)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            SetCursorHidden(!cursorHidden);
        }
#endif
    }

    public void SetCursorHidden(bool hidden)
    {
        cursorHidden = hidden;
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        Cursor.visible = !cursorHidden;

        // Locked はWindowsキーやデバッグ操作に悪影響が出やすいため使わない。
        Cursor.lockState = CursorLockMode.None;
    }
}
