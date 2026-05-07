using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HaengSungAOI_WPF.Services.Machine;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.ViewModels
{
    public class AlarmViewModel : ObservableObject
    {
        private readonly IPlcService _plcService;

        private string _alarmName;
        public string AlarmName
        {
            get => _alarmName;
            set => SetProperty(ref _alarmName, value);
        }

        private string _alarmMessage;
        public string AlarmMessage
        {
            get => _alarmMessage;
            set => SetProperty(ref _alarmMessage, value);
        }

        private string _alarmSource;
        public string AlarmSource
        {
            get => _alarmSource;
            set => SetProperty(ref _alarmSource, value);
        }

        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        public event EventHandler RequestClose;

        public AlarmViewModel(IPlcService plcService)
        {
            _plcService = plcService;
            AlarmName = "MACHINE ALARM";
            Timestamp = DateTime.Now;
        }

        public void Initialize(string name, string message, string source)
        {
            AlarmName = name;
            AlarmMessage = message;
            AlarmSource = source;
            Timestamp = DateTime.Now;
        }

        public void SetAlarm(string title, string message, string source = "PLC")
        {
            AlarmName = title ?? "MACHINE ALARM";
            AlarmMessage = message;
            AlarmSource = source;
            Timestamp = DateTime.Now;
        }

        private ICommand _resetCommand;
        public ICommand ResetCommand => _resetCommand ?? (_resetCommand = new AsyncRelayCommand(Reset));

        private ICommand _closeCommand;
        public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new RelayCommand(Close));

        private async Task Reset()
        {
            try
            {
                Utils.Logger.Info("AlarmViewModel", "Reset button clicked");
                
                // Send Reset command to PLC
                if (_plcService != null)
                {
                    await _plcService.SetHmiButtonAsync("HMI_Reset_PB", true);
                    await Task.Delay(500);
                    await _plcService.SetHmiButtonAsync("HMI_Reset_PB", false);
                }
                
                Utils.Logger.Info("AlarmViewModel", "Sent Alarm Reset command to PLC");
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Utils.Logger.Error("AlarmViewModel", $"Error sending reset: {ex.Message}");
                // Close anyway to allow the user to continue
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Close()
        {
            Utils.Logger.Info("AlarmViewModel", "Close button clicked");
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
