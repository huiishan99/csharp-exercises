using System;
using UnityEngine;

public class DemoPageSwitcher : MonoBehaviour
{
    [Serializable]
    private class PageBinding
    {
        public DemoPageId pageId;
        public GameObject pageObject;
    }

    [SerializeField] private PageBinding[] pages;
    [SerializeField] private DemoPageId firstPage = DemoPageId.Work;
    [SerializeField] private bool showFirstPageOnStart = false;

    public event Action<DemoPageId> PageChanged;

    public DemoPageId CurrentPage { get; private set; }

    private void Start()
    {
        if (showFirstPageOnStart)
        {
            ShowPage(firstPage);
        }
    }

    public void ShowPage(DemoPageId targetPage)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            PageBinding binding = pages[i];

            if (binding == null || binding.pageObject == null)
            {
                continue;
            }

            bool shouldShow = binding.pageId == targetPage;
            binding.pageObject.SetActive(shouldShow);
        }

        CurrentPage = targetPage;
        PageChanged?.Invoke(targetPage);
    }
}
