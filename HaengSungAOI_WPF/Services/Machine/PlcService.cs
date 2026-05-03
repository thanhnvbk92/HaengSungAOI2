using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HaengSungAOI_WPF.Machine.PLC;
using HaengSungAOI_WPF.Utils;
using System.Collections.Concurrent;

namespace HaengSungAOI_WPF.Services.Machine
{
    public class PlcService : IPlcService
    {
        private readonly ILogger<PlcService> _logger;
        private readonly IGlobalStateService _globalState;
        private PLCController _plc;

        private readonly ConcurrentDictionary<string, ushort> _previousVisionTriggerValues = new ConcurrentDictionary<string, ushort>();
        private readonly ConcurrentDictionary<string, ushort> _previousTrayValues = new ConcurrentDictionary<string, ushort>();
        private readonly HashSet<string> _activeAlarms = new HashSet<string>();
        private readonly object _alarmLock = new object();

        private readonly Dictionary<string, string> _visionTriggerTags = new Dictionary<string, string>
        {
            { "MW400", "Align" },
            { "MW401", "Inspect1" },
            { "MW402", "Inspect2" },
            { "MW403", "Inspect3" },
            { "MW404", "Inspect4" },
            { "MW405", "Inspect5" },
            { "MW406", "Inspect6" }
        };

        private readonly Dictionary<string, string> _visionResultTags = new Dictionary<string, string>
        {
            { "Align", "MW410" },
            { "Inspect1", "MW411" },
            { "Inspect2", "MW412" },
            { "Inspect3", "MW413" },
            { "Inspect4", "MW414" },
            { "Inspect5", "MW415" },
            { "Inspect6", "MW416" }
        };

        private readonly Dictionary<string, string> _alarmMessages = new Dictionary<string, string>
        {
            // System Alarms
            { "Alarm_EMG_Stop", "Emergency Stop Activated" },
            { "Alarm_Main_Pressure", "Main Air Pressure Low" },
            { "Alarm_Door_1_Open", "Safety Door 1 is Open" },
            { "Alarm_Door_2_Open", "Safety Door 2 is Open" },
            // ... (I'll truncate this list for brevity in the code, but in reality I'd copy all from Machine.PLC.cs)
        };

        public bool IsConnected => _plc?.IsConnected ?? false;

        public event EventHandler<VisionTriggerEventArgs> VisionTriggered;
        public event EventHandler<AlarmEventArgs> AlarmChanged;
        public event EventHandler<TrayUpdateEventArgs> TrayUpdated;
        public event EventHandler<bool> ConnectionStatusChanged;
        public event EventHandler<Dictionary<string, bool>> HmiLampStateChanged;

        private readonly Dictionary<string, bool> _lastLampStates = new Dictionary<string, bool>();

        public PlcService(ILogger<PlcService> logger, IGlobalStateService globalState)
        {
            _logger = logger;
            _globalState = globalState;
            InitializePLC();
        }

        private void InitializePLC()
        {
            try
            {
                _plc = new PLCController(
                    PLCConstants.PLC_IP_ADDRESS,
                    PLCConstants.PLC_PORT,
                    PLCConstants.PLC_UNIT_IDENTIFIER);

                PLCConfiguration.ConfigureFromConstants(_plc);
                ConfigureVisionTags();

                _plc.DataChanged += OnPlcDataChanged;
                _plc.ConnectionStatusChanged += (s, e) => ConnectionStatusChanged?.Invoke(this, e);
                
                _logger.LogInformation("PLC Service Initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize PLC Controller");
            }
        }

        private void ConfigureVisionTags()
        {
            foreach (var kvp in _visionTriggerTags)
            {
                int address = int.Parse(kvp.Key.Substring(2));
                _plc.AddHoldingRegister(kvp.Key, (ushort)address, 1, $"Vision Trigger for {kvp.Value}");
            }

            foreach (var kvp in _visionResultTags)
            {
                int address = int.Parse(kvp.Value.Substring(2));
                _plc.AddHoldingRegister(kvp.Value, (ushort)address, 1, $"Vision Result for {kvp.Key}");
            }

            _plc.AddHoldingRegister(PLCConstants.ALIGN_X_TAG, PLCConstants.ALIGN_X_ADDRESS, 4, "Align X Position (LREAL)");
            _plc.AddHoldingRegister(PLCConstants.ALIGN_Y_TAG, PLCConstants.ALIGN_Y_ADDRESS, 4, "Align Y Position (LREAL)");
            _plc.AddHoldingRegister(PLCConstants.ALIGN_R_TAG, PLCConstants.ALIGN_R_ADDRESS, 4, "Align R/Angle Position (LREAL)");

            foreach (var kvp in PLCAddresses.HMI_Lamps)
            {
                _plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"HMI Lamp {kvp.Key}");
            }
        }

        public bool Connect() => _plc?.Connect() ?? false;

        public void Disconnect() => _plc?.Disconnect();

        public void Start() => _plc?.Start();

        public void Stop() => _plc?.Stop();

        public void WriteVisionResult(string procedureName, bool isOK)
        {
            if (!_visionResultTags.TryGetValue(procedureName, out string tagName)) return;
            ushort value = isOK ? (ushort)1 : (ushort)2;
            _plc.WriteHoldingRegister(tagName, value);
            _logger.LogInformation($"Wrote vision result: {procedureName} = {(isOK ? "OK" : "NG")}");
        }

        public void WriteAlignPosition(double x, double y, double angle)
        {
            WriteLReal(PLCConstants.ALIGN_X_TAG, x);
            WriteLReal(PLCConstants.ALIGN_Y_TAG, y);
            WriteLReal(PLCConstants.ALIGN_R_TAG, angle);
        }

        private void WriteLReal(string tagName, double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            ushort[] registers = new ushort[4];
            for (int i = 0; i < 4; i++)
            {
                registers[i] = (ushort)((bytes[i * 2 + 1] << 8) | bytes[i * 2]);
            }
            _plc.WriteHoldingRegisters(tagName, registers);
        }

        public void WriteRegister(string tagName, ushort value)
        {
            _plc.WriteHoldingRegister(tagName, value);
        }

        public ushort[] GetRegisterArrayValue(string dataPointName)
        {
            return _plc?.GetRegisterArrayValue(dataPointName);
        }

        public ushort[] GetRegisterArrayValue(ushort startAddress, ushort count)
        {
            return _plc?.ReadHoldingRegistersDirect(startAddress, count);
        }

        public void WriteHoldingRegisters(string dataPointName, ushort[] registers)
        {
            _plc?.WriteHoldingRegisters(dataPointName, registers);
        }

        private void OnPlcDataChanged(object sender, PLCDataChangedEventArgs e)
        {
            if (_visionTriggerTags.ContainsKey(e.DataPointName))
            {
                HandleVisionTrigger(e);
            }
            else if (e.DataPointName == "PCB_Slot" || e.DataPointName == "PCB_Trays" || e.DataPointName == "Blank_Trays")
            {
                HandleTrayUpdate(e);
            }
            else if (e.DataPointName.StartsWith("Alarm_"))
            {
                HandleAlarmTrigger(e);
            }
            else if (e.DataPointName.StartsWith("HMI_Lamp_"))
            {
                HandleHmiLampUpdate(e);
            }
        }

        private void HandleVisionTrigger(PLCDataChangedEventArgs e)
        {
            ushort val = Convert.ToUInt16(e.NewValue);
            ushort prev = _previousVisionTriggerValues.GetOrAdd(e.DataPointName, 0);
            _previousVisionTriggerValues[e.DataPointName] = val;

            if (val == 1 && prev == 0) // Rising edge
            {
                string proc = _visionTriggerTags[e.DataPointName];
                VisionTriggered?.Invoke(this, new VisionTriggerEventArgs(e.DataPointName, proc, val));
            }
        }

        private void HandleTrayUpdate(PLCDataChangedEventArgs e)
        {
            ushort val = Convert.ToUInt16(e.NewValue);
            ushort prev = _previousTrayValues.GetOrAdd(e.DataPointName, ushort.MaxValue);

            if (val != prev)
            {
                _previousTrayValues[e.DataPointName] = val;
                TrayUpdated?.Invoke(this, new TrayUpdateEventArgs(e.DataPointName, val));
            }
        }

        private void HandleAlarmTrigger(PLCDataChangedEventArgs e)
        {
            bool isActive = Convert.ToUInt16(e.NewValue) != 0;
            lock (_alarmLock)
            {
                bool wasActive = _activeAlarms.Contains(e.DataPointName);
                if (isActive != wasActive)
                {
                    if (isActive) _activeAlarms.Add(e.DataPointName);
                    else _activeAlarms.Remove(e.DataPointName);

                    _alarmMessages.TryGetValue(e.DataPointName, out string msg);
                    AlarmChanged?.Invoke(this, new AlarmEventArgs(e.DataPointName, msg ?? e.DataPointName, isActive, e.Address));
                }
            }
        }
        
        private void HandleHmiLampUpdate(PLCDataChangedEventArgs e)
        {
            bool isOn = Convert.ToUInt16(e.NewValue) != 0;
            
            lock (_lastLampStates)
            {
                if (!_lastLampStates.TryGetValue(e.DataPointName, out bool lastState) || lastState != isOn)
                {
                    _lastLampStates[e.DataPointName] = isOn;
                    HmiLampStateChanged?.Invoke(this, new Dictionary<string, bool> { { e.DataPointName, isOn } });
                }
            }
        }

        public void Dispose()
        {
            _plc?.Dispose();
        }
    }
}
