using System;
using UnityEngine;

public class DemoSourcePanel : MonoBehaviour
{
    [Serializable]
    private class SourceBinding
    {
        public DemoSourceId sourceId;
        public DemoPageId targetPage;
        public DemoSourceButton sourceButton;
    }

    [SerializeField] private DemoPageSwitcher pageSwitcher;
    [SerializeField] private SourceBinding[] sources;
    [SerializeField] private DemoSourceId firstFullSource = DemoSourceId.Setting;

    [Header("Display Rule")]
    [SerializeField] private bool showMusicButtonInFullMode = false;
    [SerializeField] private bool showAnyButtonInSemiMode = false;

    private DemoVehicleMode currentVehicleMode = DemoVehicleMode.Parking;
    private DemoSourceId selectedFullSource;

    public DemoSourceId CurrentFullSource
    {
        get { return selectedFullSource; }
    }

    private void Start()
    {
        selectedFullSource = NormalizeFullSource(firstFullSource);
        RegisterButtonEvents();
        RefreshButtons();
    }

    private void RegisterButtonEvents()
    {
        if (sources == null)
        {
            return;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            SourceBinding binding = sources[i];

            if (binding == null || binding.sourceButton == null)
            {
                continue;
            }

            SourceBinding capturedBinding = binding;
            binding.sourceButton.Button.onClick.AddListener(() => OnSourceClicked(capturedBinding));
        }
    }

    public void ApplyVehicleMode(DemoVehicleMode vehicleMode)
    {
        currentVehicleMode = vehicleMode;
        RefreshButtons();
    }

    public void ResetFullSource(DemoSourceId sourceId)
    {
        selectedFullSource = NormalizeFullSource(sourceId);
        RefreshButtons();
    }

    public void SetFullSource(DemoSourceId sourceId, bool showTargetPage)
    {
        DemoSourceId normalizedSource = NormalizeFullSource(sourceId);

        selectedFullSource = normalizedSource;

        if (showTargetPage && pageSwitcher != null)
        {
            pageSwitcher.ShowPage(GetTargetPage(normalizedSource));
        }

        RefreshButtons();
    }

    public void ShowCurrentFullSource()
    {
        selectedFullSource = NormalizeFullSource(selectedFullSource);

        if (pageSwitcher != null)
        {
            pageSwitcher.ShowPage(GetTargetPage(selectedFullSource));
        }

        RefreshButtons();
    }

    private void OnSourceClicked(SourceBinding binding)
    {
        if (binding == null)
        {
            return;
        }

        if (currentVehicleMode != DemoVehicleMode.Parking)
        {
            return;
        }

        if (binding.sourceId == DemoSourceId.Music)
        {
            return;
        }

        if (binding.sourceId == selectedFullSource)
        {
            return;
        }

        selectedFullSource = NormalizeFullSource(binding.sourceId);

        if (pageSwitcher != null)
        {
            pageSwitcher.ShowPage(binding.targetPage);
        }

        RefreshButtons();
    }

    private DemoPageId GetTargetPage(DemoSourceId sourceId)
    {
        if (sources == null)
        {
            return DemoPageId.LightingColorChange;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            SourceBinding binding = sources[i];

            if (binding == null)
            {
                continue;
            }

            if (binding.sourceId == sourceId)
            {
                return binding.targetPage;
            }
        }

        return DemoPageId.LightingColorChange;
    }

    private void RefreshButtons()
    {
        if (sources == null)
        {
            return;
        }

        bool isParking = currentVehicleMode == DemoVehicleMode.Parking;

        for (int i = 0; i < sources.Length; i++)
        {
            SourceBinding binding = sources[i];

            if (binding == null || binding.sourceButton == null)
            {
                continue;
            }

            if (isParking)
            {
                ApplyFullModeState(binding);
            }
            else
            {
                ApplySemiModeState(binding);
            }
        }
    }

    private void ApplyFullModeState(SourceBinding binding)
    {
        bool isMusic = binding.sourceId == DemoSourceId.Music;

        if (isMusic && !showMusicButtonInFullMode)
        {
            binding.sourceButton.SetVisible(false);
            return;
        }

        bool isSelected = binding.sourceId == selectedFullSource && !isMusic;
        bool isClickable = !isMusic && !isSelected;

        binding.sourceButton.SetVisible(true);
        binding.sourceButton.SetState(isSelected, isClickable);
    }

    private void ApplySemiModeState(SourceBinding binding)
    {
        if (!showAnyButtonInSemiMode)
        {
            binding.sourceButton.SetVisible(false);
            return;
        }

        binding.sourceButton.SetVisible(false);
    }

    private DemoSourceId NormalizeFullSource(DemoSourceId sourceId)
    {
        if (sourceId == DemoSourceId.Music)
        {
            return DemoSourceId.Setting;
        }

        return sourceId;
    }
}
