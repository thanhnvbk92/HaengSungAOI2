using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HaengSungAOI_WPF.Services.Machine
{
    public class MachineHmiService : IMachineHmiService
    {
        private readonly ILogger<MachineHmiService> _logger;
        private readonly IPlcService _plcService;
        private readonly Dictionary<string, bool> _lampStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _quantities = new Dictionary<string, int>();

        public event EventHandler<HmiLampStateChangedEventArgs> LampStateChanged;
        public event EventHandler<HmiQuantityChangedEventArgs> QuantityChanged;

        public MachineHmiService(ILogger<MachineHmiService> logger, IPlcService plcService)
        {
            _logger = logger;
            _plcService = plcService;
            _plcService.HmiLampStateChanged += OnHmiLampStateChanged;
            _plcService.TrayUpdated += OnTrayUpdated;
        }

        private void OnTrayUpdated(object sender, TrayUpdateEventArgs e)
        {
            _quantities[e.TagName] = e.NewValue;
            QuantityChanged?.Invoke(this, new HmiQuantityChangedEventArgs(e.TagName, e.NewValue));
        }

        private void OnHmiLampStateChanged(object sender, Dictionary<string, bool> changes)
        {
            foreach (var kvp in changes)
            {
                _lampStates[kvp.Key] = kvp.Value;
                LampStateChanged?.Invoke(this, new HmiLampStateChangedEventArgs(kvp.Key, kvp.Value));
            }
        }

        public async Task HandleButtonPressAsync(string tag, bool isPressed)
        {
            try
            {
                if (!_plcService.IsConnected) return;

                ushort value = isPressed ? (ushort)1 : (ushort)0;
                _plcService.WriteRegister(tag, value);
                // _logger.LogDebug($"HMI Button {tag} set to {value}"); // Reduced logging
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling HMI button press for {tag}");
            }
        }

        public bool GetLampState(string lampName)
        {
            return _lampStates.TryGetValue(lampName, out bool isOn) && isOn;
        }

        public int GetQuantity(string tagName)
        {
            return _quantities.TryGetValue(tagName, out int val) ? val : 0;
        }
    }
}
