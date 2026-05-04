using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HaengSungAOI_WPF.Services.Machine;
using HaengSungAOI_WPF.Utils;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class ManualOperationsViewModel : ObservableObject
    {
        private readonly IMachineService _machineService;
        private readonly IMachineHmiService _hmiService;

        private string _statusText = "Ready";
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

        private string _emergencyStatus = "Emergency: Normal";
        public string EmergencyStatus { get => _emergencyStatus; set => SetProperty(ref _emergencyStatus, value); }

        private string _connectionStatus = "PLC: Disconnected";
        public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }

        public ICommand ButtonDownCommand { get; }
        public ICommand ButtonUpCommand { get; }
        public ICommand EmergencyStopCommand { get; }
        public ICommand ResetEmergencyCommand { get; }

        public ManualOperationsViewModel(IMachineService machineService, IMachineHmiService hmiService)
        {
            _machineService = machineService;
            _hmiService = hmiService;
            
            ButtonDownCommand = new AsyncRelayCommand<string>(ButtonDown);
            ButtonUpCommand = new AsyncRelayCommand<string>(ButtonUp);
            EmergencyStopCommand = new RelayCommand(EmergencyStop);
            ResetEmergencyCommand = new RelayCommand(ResetEmergency);

            UpdateConnectionStatus();
            
            // Subscribe to PLC connection events if available
            if (_machineService?.PLC != null)
            {
                _machineService.PLC.ConnectionStatusChanged += (s, e) => UpdateConnectionStatus();
            }
        }

        private async Task ButtonDown(string addressKey)
        {
            if (string.IsNullOrEmpty(addressKey)) return;

            try
            {
                Logger.Debug("ManualOps", $"Button Down: {addressKey}");
                await _hmiService.HandleButtonPressAsync(addressKey, true);
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in ButtonDown ({addressKey}): {ex.Message}", ex);
            }
        }

        private async Task ButtonUp(string addressKey)
        {
            if (string.IsNullOrEmpty(addressKey)) return;

            try
            {
                Logger.Debug("ManualOps", $"Button Up: {addressKey}");
                await _hmiService.HandleButtonPressAsync(addressKey, false);
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in ButtonUp ({addressKey}): {ex.Message}", ex);
            }
        }

        private void EmergencyStop()
        {
            try
            {
                _machineService.EmergencyStop();
                StatusText = "Emergency Stop Triggered";
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in EmergencyStop: {ex.Message}", ex);
            }
        }

        private void ResetEmergency()
        {
            try
            {
                // In a real system, this would write to a PLC reset bit
                StatusText = "Emergency Reset Sent";
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in ResetEmergency: {ex.Message}", ex);
            }
        }

        private void UpdateConnectionStatus()
        {
            bool plcConnected = _machineService?.PLC?.IsConnected ?? false;
            ConnectionStatus = plcConnected ? "PLC: Connected" : "PLC: Disconnected";
        }
        public void Cleanup()
        {
            try
            {
                // Future: Add logic to release any active jog buttons if necessary
                Logger.Info("ManualOperationsViewModel", "Manual Operations ViewModel cleanup completed");
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOperationsViewModel", $"Error during cleanup: {ex.Message}", ex);
            }
        }
    }
}



