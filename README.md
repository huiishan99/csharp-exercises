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

    public DemoPageId CurrentPage { get; private set; }

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
    }

    public void HideAllPages()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            PageBinding binding = pages[i];

            if (binding == null || binding.pageObject == null)
            {
                continue;
            }

            binding.pageObject.SetActive(false);
        }
    }
}
