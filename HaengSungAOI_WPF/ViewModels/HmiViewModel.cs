using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HaengSungAOI_WPF.Services.Machine;
using Microsoft.Extensions.Logging;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class HmiViewModel : ObservableObject
    {
        private readonly ILogger<HmiViewModel> _logger;
        private readonly IHmiService _hmiService;

        [ObservableProperty]
        private bool _isAutoLampOn;

        [ObservableProperty]
        private bool _isManualLampOn;

        [ObservableProperty]
        private bool _isResetLampOn;

        [ObservableProperty]
        private bool _isStartLampOn;

        [ObservableProperty]
        private bool _isStopLampOn;

        [ObservableProperty]
        private bool _isOriginLampOn;

        [ObservableProperty]
        private int _pcbSlot;

        [ObservableProperty]
        private int _pcbTrayQuantity;

        [ObservableProperty]
        private int _blankTrayQuantity;

        public HmiViewModel(ILogger<HmiViewModel> logger, IHmiService hmiService)
        {
            _logger = logger;
            _hmiService = hmiService;
            _hmiService.LampStateChanged += OnLampStateChanged;
            _hmiService.QuantityChanged += OnQuantityChanged;
        }

        private void OnQuantityChanged(object sender, HmiQuantityChangedEventArgs e)
        {
            switch (e.TagName)
            {
                case "PCB_Slot": PcbSlot = e.Value; break;
                case "PCB_Tray_Qty": PcbTrayQuantity = e.Value; break;
                case "Blank_Tray_Qty": BlankTrayQuantity = e.Value; break;
            }
        }

        private void OnLampStateChanged(object sender, HmiLampStateChangedEventArgs e)
        {
            switch (e.LampName)
            {
                case "HMI_Lamp_Auto_PB": IsAutoLampOn = e.IsOn; break;
                case "HMI_Lamp_Manual_PB": IsManualLampOn = e.IsOn; break;
                case "HMI_Lamp_Reset_PB": IsResetLampOn = e.IsOn; break;
                case "HMI_Lamp_Start": IsStartLampOn = e.IsOn; break;
                case "HMI_Lamp_Stop": IsStopLampOn = e.IsOn; break;
                case "HMI_Lamp_Origin": IsOriginLampOn = e.IsOn; break;
            }
        }

        [RelayCommand]
        private async Task ButtonDown(string tag)
        {
            await _hmiService.HandleButtonPressAsync(tag, true);
        }

        [RelayCommand]
        private async Task ButtonUp(string tag)
        {
            await _hmiService.HandleButtonPressAsync(tag, false);
        }
    }
}
