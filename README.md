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

    private DemoVehicleMode currentVehicleMode = DemoVehicleMode.Parking;
    private DemoSourceId selectedFullSource;

    private void Start()
    {
        selectedFullSource = firstFullSource;
        RegisterButtonEvents();
        RefreshButtons();
    }

    private void RegisterButtonEvents()
    {
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

    public void SetFullSource(DemoSourceId sourceId, bool showTargetPage)
    {
        if (sourceId == DemoSourceId.Music)
        {
            return;
        }

        selectedFullSource = sourceId;

        if (showTargetPage && pageSwitcher != null)
        {
            DemoPageId targetPage = GetTargetPage(sourceId);
            pageSwitcher.ShowPage(targetPage);
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

        selectedFullSource = binding.sourceId;

        if (pageSwitcher != null)
        {
            pageSwitcher.ShowPage(binding.targetPage);
        }

        RefreshButtons();
    }

    private DemoPageId GetTargetPage(DemoSourceId sourceId)
    {
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
                ApplyParkingState(binding);
            }
            else
            {
                ApplySemiState(binding);
            }
        }
    }

    private void ApplyParkingState(SourceBinding binding)
    {
        bool isMusic = binding.sourceId == DemoSourceId.Music;
        bool isSelected = binding.sourceId == selectedFullSource && !isMusic;
        bool isClickable = !isMusic && !isSelected;

        binding.sourceButton.SetVisible(true);
        binding.sourceButton.SetState(isSelected, isClickable);
    }

    private void ApplySemiState(SourceBinding binding)
    {
        bool isMusic = binding.sourceId == DemoSourceId.Music;
        bool isSetting = binding.sourceId == DemoSourceId.Setting;

        if (isSetting)
        {
            binding.sourceButton.SetVisible(false);
            return;
        }

        binding.sourceButton.SetVisible(true);
        binding.sourceButton.SetState(isMusic, false);
    }
}
