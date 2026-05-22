using UnityEngine;

public class DemoSourcePanel : MonoBehaviour
{
    [SerializeField] private DemoPageSwitcher pageSwitcher;

    [SerializeField] private DemoSourceButton musicButton;
    [SerializeField] private DemoSourceButton workButton;
    [SerializeField] private DemoSourceButton movieButton;
    [SerializeField] private DemoSourceButton gameButton;
    [SerializeField] private DemoSourceButton settingButton;

    private void OnEnable()
    {
        if (pageSwitcher != null)
        {
            pageSwitcher.PageChanged += RefreshByPage;
        }
    }

    private void Start()
    {
        RegisterButtonEvents();

        if (pageSwitcher != null)
        {
            RefreshByPage(pageSwitcher.CurrentPage);
        }
    }

    private void OnDisable()
    {
        if (pageSwitcher != null)
        {
            pageSwitcher.PageChanged -= RefreshByPage;
        }
    }

    private void RegisterButtonEvents()
    {
        if (workButton != null)
        {
            workButton.Button.onClick.RemoveListener(OnWorkClicked);
            workButton.Button.onClick.AddListener(OnWorkClicked);
        }

        if (movieButton != null)
        {
            movieButton.Button.onClick.RemoveListener(OnMovieClicked);
            movieButton.Button.onClick.AddListener(OnMovieClicked);
        }

        if (gameButton != null)
        {
            gameButton.Button.onClick.RemoveListener(OnGameClicked);
            gameButton.Button.onClick.AddListener(OnGameClicked);
        }

        if (settingButton != null)
        {
            settingButton.Button.onClick.RemoveListener(OnSettingClicked);
            settingButton.Button.onClick.AddListener(OnSettingClicked);
        }

        // Music 是显示用，不注册点击事件。
    }

    private void OnWorkClicked()
    {
        TryShowFullPage(DemoPageId.Work);
    }

    private void OnMovieClicked()
    {
        TryShowFullPage(DemoPageId.Movie);
    }

    private void OnGameClicked()
    {
        TryShowFullPage(DemoPageId.Game);
    }

    private void OnSettingClicked()
    {
        TryShowFullPage(DemoPageId.LightingColorChange);
    }

    private void TryShowFullPage(DemoPageId targetPage)
    {
        if (pageSwitcher == null)
        {
            return;
        }

        if (!IsFullModePage(pageSwitcher.CurrentPage))
        {
            return;
        }

        pageSwitcher.ShowPage(targetPage);
    }

    private void RefreshByPage(DemoPageId currentPage)
    {
        if (currentPage == DemoPageId.Welcome)
        {
            ApplyWelcomeMode();
            return;
        }

        if (IsSemiModePage(currentPage))
        {
            ApplySemiMode();
            return;
        }

        ApplyFullMode(currentPage);
    }

    private bool IsSemiModePage(DemoPageId pageId)
    {
        return pageId == DemoPageId.NormalDrive
            || pageId == DemoPageId.RearView;
    }

    private bool IsFullModePage(DemoPageId pageId)
    {
        return pageId == DemoPageId.Work
            || pageId == DemoPageId.Movie
            || pageId == DemoPageId.Game
            || pageId == DemoPageId.LightingColorChange;
    }

    private void ApplyWelcomeMode()
    {
        SetButtonVisible(musicButton, false);
        SetButtonVisible(workButton, false);
        SetButtonVisible(movieButton, false);
        SetButtonVisible(gameButton, false);
        SetButtonVisible(settingButton, false);
    }

    private void ApplySemiMode()
    {
        SetButtonVisible(musicButton, true);
        SetButtonVisible(workButton, true);
        SetButtonVisible(movieButton, true);
        SetButtonVisible(gameButton, true);
        SetButtonVisible(settingButton, false);

        SetButtonState(musicButton, DemoSourceButtonVisualState.Selected, false);
        SetButtonState(workButton, DemoSourceButtonVisualState.Disabled, false);
        SetButtonState(movieButton, DemoSourceButtonVisualState.Disabled, false);
        SetButtonState(gameButton, DemoSourceButtonVisualState.Disabled, false);
    }

    private void ApplyFullMode(DemoPageId currentPage)
    {
        SetButtonVisible(musicButton, true);
        SetButtonVisible(workButton, true);
        SetButtonVisible(movieButton, true);
        SetButtonVisible(gameButton, true);
        SetButtonVisible(settingButton, true);

        SetButtonState(musicButton, DemoSourceButtonVisualState.Disabled, false);

        SetButtonState(
            workButton,
            currentPage == DemoPageId.Work
                ? DemoSourceButtonVisualState.Selected
                : DemoSourceButtonVisualState.Normal,
            true
        );

        SetButtonState(
            movieButton,
            currentPage == DemoPageId.Movie
                ? DemoSourceButtonVisualState.Selected
                : DemoSourceButtonVisualState.Normal,
            true
        );

        SetButtonState(
            gameButton,
            currentPage == DemoPageId.Game
                ? DemoSourceButtonVisualState.Selected
                : DemoSourceButtonVisualState.Normal,
            true
        );

        SetButtonState(
            settingButton,
            currentPage == DemoPageId.LightingColorChange
                ? DemoSourceButtonVisualState.Selected
                : DemoSourceButtonVisualState.Normal,
            true
        );
    }

    private void SetButtonVisible(DemoSourceButton sourceButton, bool isVisible)
    {
        if (sourceButton == null)
        {
            return;
        }

        sourceButton.SetVisible(isVisible);
    }

    private void SetButtonState(
        DemoSourceButton sourceButton,
        DemoSourceButtonVisualState visualState,
        bool canClick
    )
    {
        if (sourceButton == null)
        {
            return;
        }

        sourceButton.SetState(visualState, canClick);
    }
}
