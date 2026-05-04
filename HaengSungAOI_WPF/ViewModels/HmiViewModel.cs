using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HaengSungAOI_WPF.Services.Machine;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class HmiViewModel : ObservableObject
    {
        private readonly ILogger<HmiViewModel> _logger;
        private readonly IMachineHmiService _hmiService;

        private bool _isAutoLampOn;
        public bool IsAutoLampOn { get => _isAutoLampOn; set => SetProperty(ref _isAutoLampOn, value); }

        private bool _isManualLampOn;
        public bool IsManualLampOn { get => _isManualLampOn; set => SetProperty(ref _isManualLampOn, value); }

        private bool _isResetLampOn;
        public bool IsResetLampOn { get => _isResetLampOn; set => SetProperty(ref _isResetLampOn, value); }

        private bool _isStartLampOn;
        public bool IsStartLampOn { get => _isStartLampOn; set => SetProperty(ref _isStartLampOn, value); }

        private bool _isStopLampOn;
        public bool IsStopLampOn { get => _isStopLampOn; set => SetProperty(ref _isStopLampOn, value); }

        private bool _isOriginLampOn;
        public bool IsOriginLampOn { get => _isOriginLampOn; set => SetProperty(ref _isOriginLampOn, value); }

        private int _pcbSlot;
        public int PcbSlot { get => _pcbSlot; set => SetProperty(ref _pcbSlot, value); }

        private int _pcbTrayQuantity;
        public int PcbTrayQuantity { get => _pcbTrayQuantity; set => SetProperty(ref _pcbTrayQuantity, value); }

        private int _blankTrayQuantity;
        public int BlankTrayQuantity { get => _blankTrayQuantity; set => SetProperty(ref _blankTrayQuantity, value); }

        public HmiViewModel(ILogger<HmiViewModel> logger, IMachineHmiService hmiService)
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

        private System.Windows.Input.ICommand _buttonDownCommand;
        public System.Windows.Input.ICommand ButtonDownCommand => _buttonDownCommand ?? (_buttonDownCommand = new AsyncRelayCommand<string>(ButtonDown));

        private System.Windows.Input.ICommand _buttonUpCommand;
        public System.Windows.Input.ICommand ButtonUpCommand => _buttonUpCommand ?? (_buttonUpCommand = new AsyncRelayCommand<string>(ButtonUp));

        private async Task ButtonDown(string tag)
        {
            await _hmiService.HandleButtonPressAsync(tag, true);
        }

        private async Task ButtonUp(string tag)
        {
            await _hmiService.HandleButtonPressAsync(tag, false);
        }

    }
}
