using System;
using UnityEngine;

public class DemoPageSwitcher : MonoBehaviour
{
    [Serializable]
    private class PageBinding
    {
        public DemoPageId pageId;
        public GameObject pageObject;
        public DemoScreenOpenMode screenOpenMode = DemoScreenOpenMode.FullOpen;
    }

    [SerializeField] private PageBinding[] pages;
    [SerializeField] private DemoScreenViewport screenViewport;
    [SerializeField] private DemoPageId firstPage = DemoPageId.NormalDrive;

    public event Action<DemoPageId> PageChanged;

    public DemoPageId CurrentPage { get; private set; }

    private void Start()
    {
        ShowPage(firstPage);
    }

    public void ShowPage(DemoPageId targetPage)
    {
        PageBinding targetBinding = null;

        for (int i = 0; i < pages.Length; i++)
        {
            PageBinding binding = pages[i];

            if (binding == null)
            {
                continue;
            }

            bool shouldShow = binding.pageId == targetPage;

            if (binding.pageObject != null)
            {
                binding.pageObject.SetActive(shouldShow);
            }

            if (shouldShow)
            {
                targetBinding = binding;
            }
        }

        if (screenViewport != null && targetBinding != null)
        {
            screenViewport.SetMode(targetBinding.screenOpenMode);
        }

        CurrentPage = targetPage;
        PageChanged?.Invoke(targetPage);
    }
}
