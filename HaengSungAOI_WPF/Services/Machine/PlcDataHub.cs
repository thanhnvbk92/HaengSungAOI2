using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HaengSungAOI_WPF.Core.PLC;

namespace HaengSungAOI_WPF.Services.Machine
{
    public interface IPlcDataHub : INotifyPropertyChanged, IMachineHmiService
    {
        // System Status
        bool IsConnected { get; }
        
        // Machine Counters / Quantities
        int PcbSlot { get; }
        int PcbTrays { get; }
        int BlankTrays { get; }
        
        // HMI Control States
        bool IsAutoMode { get; }
        bool IsManualMode { get; }
        bool IsRunning { get; }
        
        // Lamps / Indicators
        bool GetLampState(string lampName);
        
        // Generic Data Access
        object GetValue(string tagName);
        int GetQuantity(string tagName);

        // Control Actions
        Task WriteTagAsync(string tagName, object value);
        Task HandleButtonPressAsync(string tag, bool isPressed);

        // Events for legacy/reactive support
        event EventHandler<HmiLampStateChangedEventArgs> LampStateChanged;
        event EventHandler<HmiQuantityChangedEventArgs> QuantityChanged;
    }

    public class PlcDataHub : IPlcDataHub
    {
        private readonly IPlcService _plc;
        private readonly Dictionary<string, object> _tagValues = new Dictionary<string, object>();
        private readonly Dictionary<string, bool> _lampStates = new Dictionary<string, bool>();

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<HmiLampStateChangedEventArgs> LampStateChanged;
        public event EventHandler<HmiQuantityChangedEventArgs> QuantityChanged;

        public bool IsConnected => _plc.IsConnected;

        // Mapped Properties
        public int PcbSlot => GetInt("PCB_Slot");
        public int PcbTrays => GetInt("PCB_Trays");
        public int BlankTrays => GetInt("Blank_Trays");

        public bool IsAutoMode => GetBool("HMI_Lamp_Auto_PB");
        public bool IsManualMode => GetBool("HMI_Lamp_Manual_PB");
        public bool IsRunning => GetBool("HMI_Lamp_Start");

        public PlcDataHub(IPlcService plc)
        {
            _plc = plc;
            _plc.TagChanged += OnTagChanged;
            _plc.ConnectionStatusChanged += (s, e) => OnPropertyChanged(nameof(IsConnected));
            _plc.HmiLampStateChanged += OnLampStateChanged;
            _plc.TrayUpdated += OnTrayUpdated;
        }

        public bool GetLampState(string lampName)
        {
            return _lampStates.TryGetValue(lampName, out bool state) && state;
        }

        public int GetQuantity(string tagName)
        {
            return GetInt(tagName);
        }

        public object GetValue(string tagName)
        {
            return _tagValues.TryGetValue(tagName, out object val) ? val : null;
        }

        public async Task WriteTagAsync(string tagName, object value)
        {
            if (value is ushort us) await _plc.WriteRegisterAsync(tagName, us);
            else if (value is double d) await _plc.WriteDoubleAsync(tagName, d);
            else if (value is bool b) await _plc.SetHmiButtonAsync(tagName, b);
        }

        public async Task HandleButtonPressAsync(string tag, bool isPressed)
        {
            await _plc.SetHmiButtonAsync(tag, isPressed);
        }

        private void OnTagChanged(object sender, TagChangedEventArgs e)
        {
            _tagValues[e.TagName] = e.NewValue;

            // Notify specific properties if they change
            switch (e.TagName)
            {
                case "PCB_Slot": OnPropertyChanged(nameof(PcbSlot)); break;
                case "PCB_Trays": OnPropertyChanged(nameof(PcbTrays)); break;
                case "Blank_Trays": OnPropertyChanged(nameof(BlankTrays)); break;
                case "HMI_Lamp_Auto_PB": OnPropertyChanged(nameof(IsAutoMode)); break;
                case "HMI_Lamp_Manual_PB": OnPropertyChanged(nameof(IsManualMode)); break;
                case "HMI_Lamp_Start": OnPropertyChanged(nameof(IsRunning)); break;
            }
            
            OnPropertyChanged($"Tag_{e.TagName}");
        }

        private void OnTrayUpdated(object sender, TrayUpdateEventArgs e)
        {
            QuantityChanged?.Invoke(this, new HmiQuantityChangedEventArgs(e.TagName, e.NewValue));
        }

        private void OnLampStateChanged(object sender, Dictionary<string, bool> lamps)
        {
            foreach (var kvp in lamps)
            {
                _lampStates[kvp.Key] = kvp.Value;
                LampStateChanged?.Invoke(this, new HmiLampStateChangedEventArgs(kvp.Key, kvp.Value));
                OnPropertyChanged($"Lamp_{kvp.Key}");
            }
        }

        private int GetInt(string tagName)
        {
            return _tagValues.TryGetValue(tagName, out object val) ? Convert.ToInt32(val) : 0;
        }

        private bool GetBool(string tagName)
        {
            return _tagValues.TryGetValue(tagName, out object val) ? Convert.ToBoolean(val) : false;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
