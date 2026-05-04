using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LeadshineHmi.Services;
using LeadshineHmi.Core.Models;
using System.ComponentModel;
using HaengSungAOI_WPF.Machine.PLC;

namespace HaengSungAOI_WPF.Services.Machine
{
    public class HmiPlcService : IPlcService
    {
        private readonly ILogger<HmiPlcService> _logger;
        private readonly IGlobalStateService _globalState;
        private readonly IHmiService _hmiService;

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

        private readonly Dictionary<string, ushort> _previousTriggerValues = new Dictionary<string, ushort>();
        private readonly Dictionary<string, string> _alarmMessages = new Dictionary<string, string>();

        public bool IsConnected => _hmiService.Engine?.IsRunning ?? false;

        public event EventHandler<VisionTriggerEventArgs> VisionTriggered;
        public event EventHandler<AlarmEventArgs> AlarmChanged;
        public event EventHandler<TrayUpdateEventArgs> TrayUpdated;
        public event EventHandler<bool> ConnectionStatusChanged;
        public event EventHandler<Dictionary<string, bool>> HmiLampStateChanged;

        private bool _tagsInitialized = false;

        public HmiPlcService(ILogger<HmiPlcService> logger, IGlobalStateService globalState, IHmiService hmiService)
        {
            _logger = logger;
            _globalState = globalState;
            _hmiService = hmiService;
        }

        private void InitializeTags()
        {
            if (_tagsInitialized) return;
            try
            {
                // Register Vision Trigger Tags
                foreach (var kvp in _visionTriggerTags)
                {
                    var tag = _hmiService.Engine.AddTag(kvp.Key, kvp.Key, DataType.Int16, $"Vision Trigger for {kvp.Value}");
                    _hmiService.Engine.GetTag(kvp.Key).PropertyChanged += OnTagPropertyChanged;
                }

                // Register Vision Result Tags
                foreach (var kvp in _visionResultTags)
                {
                    _hmiService.Engine.AddTag(kvp.Value, kvp.Value, DataType.Int16, $"Vision Result for {kvp.Key}");
                }

                // Register Align Positions
                _hmiService.Engine.AddTag(PLCConstants.ALIGN_X_TAG, PLCConstants.ALIGN_X_TAG, DataType.Float, "Align X Position");
                _hmiService.Engine.AddTag(PLCConstants.ALIGN_Y_TAG, PLCConstants.ALIGN_Y_TAG, DataType.Float, "Align Y Position");
                _hmiService.Engine.AddTag(PLCConstants.ALIGN_R_TAG, PLCConstants.ALIGN_R_TAG, DataType.Float, "Align R/Angle Position");

                // Register HMI Lamps
                foreach (var kvp in PLCAddresses.HMI_Lamps)
                {
                    string addr = $"MW{kvp.Value}";
                    _hmiService.Engine.AddTag(kvp.Key, addr, DataType.Int16, $"HMI Lamp {kvp.Key}");
                    _hmiService.Engine.GetTag(kvp.Key).PropertyChanged += OnTagPropertyChanged;
                }

                // Register HMI PushButtons
                foreach (var kvp in PLCAddresses.HMI_PushButtons)
                {
                    string addr = $"MW{kvp.Value}";
                    _hmiService.Engine.AddTag(kvp.Key, addr, DataType.Int16, $"HMI Button {kvp.Key}");
                }

                // Register Alarms
                foreach (var kvp in PLCAddresses.Alarm_Registers)
                {
                    string addr = $"MW{kvp.Value}";
                    _hmiService.Engine.AddTag(kvp.Key, addr, DataType.Int16, $"Alarm {kvp.Key}");
                    _hmiService.Engine.GetTag(kvp.Key).PropertyChanged += OnTagPropertyChanged;
                }

                // Register HMI Select
                foreach (var kvp in PLCAddresses.HMI_Select_Registers)
                {
                    _hmiService.Engine.AddTag(kvp.Key, $"MW{kvp.Value}", DataType.Int16, $"HMI Select {kvp.Key}");
                }
                foreach (var kvp in PLCAddresses.HMI_Select_Lamp_Registers)
                {
                    _hmiService.Engine.AddTag(kvp.Key, $"MW{kvp.Value}", DataType.Int16, $"HMI Select Lamp {kvp.Key}");
                }

                // Register Tray and Product Logging
                foreach (var kvp in PLCAddresses.TrayQuantity_Registers)
                {
                    _hmiService.Engine.AddTag(kvp.Key, $"MW{kvp.Value}", DataType.Int16, $"Tray Data: {kvp.Key}");
                    _hmiService.Engine.GetTag(kvp.Key).PropertyChanged += OnTagPropertyChanged;
                }
                foreach (var kvp in PLCAddresses.ProductLog_Registers)
                {
                    _hmiService.Engine.AddTag(kvp.Key, $"MW{kvp.Value}", DataType.Int16, $"Product Log: {kvp.Key}");
                    _hmiService.Engine.GetTag(kvp.Key).PropertyChanged += OnTagPropertyChanged;
                }

                // Register Barcodes
                foreach (var kvp in PLCAddresses.OKBarcode_Registers)
                    _hmiService.Engine.AddTag(kvp.Key, $"MW{kvp.Value}", DataType.Int16, $"OK Barcode {kvp.Key}");
                foreach (var kvp in PLCAddresses.NGBarcode_Registers)
                    _hmiService.Engine.AddTag(kvp.Key, $"MW{kvp.Value}", DataType.Int16, $"NG Barcode {kvp.Key}");

                // Register Servo Status Coils (as Int16 for simplicity if they are MW or mapped)
                foreach (var kvp in PLCAddresses.Servo_Status_Coils)
                {
                    _hmiService.Engine.AddTag(kvp.Key, $"MW{kvp.Value}", DataType.Int16, $"Servo Status: {kvp.Key}");
                }

                // Register Robot Positions (LREAL - 8 bytes)
                foreach (var kvp in PLCAddresses.ServoPositionData)
                {
                    string addr = $"MW{kvp.Value}";
                    DataType type = kvp.Key.Contains("Speed") || kvp.Key.Contains("Pos") || kvp.Key.Contains("Offset") 
                        ? DataType.Double : DataType.Int16;
                    
                    _hmiService.Engine.AddTag(kvp.Key, addr, type, $"Robot Data: {kvp.Key}");
                }

                // Register Servo Monitoring and HMI Controls
                foreach (ServoAxis axis in Enum.GetValues(typeof(ServoAxis)))
                {
                    string axisName = ServoAddressCalculator.GetAxisDisplayName(axis);
                    
                    // Parameters
                    _hmiService.Engine.AddTag($"{axisName}_CurrentPosition", $"MW{ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.CurrentPosition)}", DataType.Double, $"{axisName} Current Position");
                    _hmiService.Engine.AddTag($"{axisName}_CurrentSpeed", $"MW{ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.CurrentSpeed)}", DataType.Double, $"{axisName} Current Speed");
                    _hmiService.Engine.AddTag($"{axisName}_ErrorCode", $"MW{ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.ErrorCode)}", DataType.Double, $"{axisName} Error Code");
                    _hmiService.Engine.AddTag($"{axisName}_OperationStatus", $"MW{ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.OperationStatus)}", DataType.Double, $"{axisName} Operation Status");
                    _hmiService.Engine.AddTag($"{axisName}_TargetPosition", $"MW{ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.TargetPosition)}", DataType.Double, $"{axisName} Target Position");
                    _hmiService.Engine.AddTag($"{axisName}_TargetSpeed", $"MW{ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.TargetSpeed)}", DataType.Double, $"{axisName} Target Speed");
                    
                    // Axis HMI Buttons
                    _hmiService.Engine.AddTag($"HMI_{axisName}_ServoON_PB", $"MW{ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.ServoON)}", DataType.Int16);
                    _hmiService.Engine.AddTag($"HMI_{axisName}_ORG_PB", $"MW{ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.ORG)}", DataType.Int16);
                    _hmiService.Engine.AddTag($"HMI_{axisName}_JogPlus_PB", $"MW{ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.JogPlus)}", DataType.Int16);
                    _hmiService.Engine.AddTag($"HMI_{axisName}_JogMinus_PB", $"MW{ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.JogMinus)}", DataType.Int16);
                    _hmiService.Engine.AddTag($"HMI_{axisName}_Move_PB", $"MW{ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.Move)}", DataType.Int16);

                    // Axis HMI Lamps
                    _hmiService.Engine.AddTag($"HMI_Lamp_{axisName}_ServoON_PB", $"MW{ServoAddressCalculator.GetHMILampAddress(axis, ServoHMIButton.ServoON)}", DataType.Int16);
                    _hmiService.Engine.AddTag($"HMI_Lamp_{axisName}_ORG_PB", $"MW{ServoAddressCalculator.GetHMILampAddress(axis, ServoHMIButton.ORG)}", DataType.Int16);
                }

                _logger.LogInformation("HmiPlcService Tags Initialized");
                _tagsInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing HMI Tags");
            }
        }

        private void OnTagPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Value") return;
            
            var tag = (HmiTag)sender;
            
            // Handle Vision Triggers
            if (_visionTriggerTags.TryGetValue(tag.Name, out string procName))
            {
                ushort val = Convert.ToUInt16(tag.Value ?? 0);
                _previousTriggerValues.TryGetValue(tag.Name, out ushort prevVal);
                _previousTriggerValues[tag.Name] = val;

                if (val == 1 && prevVal == 0) // Robust rising edge check
                {
                    VisionTriggered?.Invoke(this, new VisionTriggerEventArgs(tag.Name, procName, val));
                }
            }
            // Handle Alarms
            else if (tag.Name.StartsWith("Alarm_"))
            {
                ushort val = Convert.ToUInt16(tag.Value ?? 0);
                _previousTriggerValues.TryGetValue(tag.Name, out ushort prevVal);
                _previousTriggerValues[tag.Name] = val;

                if (val != prevVal)
                {
                    bool isActive = val != 0;
                    AlarmChanged?.Invoke(this, new AlarmEventArgs(tag.Name, tag.Description, isActive, tag.ModbusAddress));
                }
            }
            // Handle Tray Updates
            else if (tag.Name == "PCB_Slot" || tag.Name == "PCB_Trays" || tag.Name == "Blank_Trays" || tag.Name.Contains("Trigger"))
            {
                ushort val = Convert.ToUInt16(tag.Value ?? 0);
                _previousTriggerValues.TryGetValue(tag.Name, out ushort prevVal);
                _previousTriggerValues[tag.Name] = val;

                if (val != prevVal)
                {
                    TrayUpdated?.Invoke(this, new TrayUpdateEventArgs(tag.Name, val));
                }
            }
            // Handle Lamps
            else if (tag.Name.StartsWith("HMI_Lamp_"))
            {
                bool isOn = Convert.ToUInt16(tag.Value ?? 0) != 0;
                HmiLampStateChanged?.Invoke(this, new Dictionary<string, bool> { { tag.Name, isOn } });
            }
        }

        public void Start() => _hmiService.StartAsync();
        public void Stop() => _hmiService.StopAsync();
        
        public bool Connect() 
        {
            _hmiService.InitializeAsync(PLCConstants.PLC_IP_ADDRESS, PLCConstants.PLC_PORT).GetAwaiter().GetResult();
            InitializeTags();
            _hmiService.StartAsync();
            return true; 
        }

        public void Disconnect() => _hmiService.StopAsync();

        public void WriteVisionResult(string procedureName, bool isOK)
        {
            if (_visionResultTags.TryGetValue(procedureName, out string tagName))
            {
                ushort value = isOK ? (ushort)1 : (ushort)2;
                _hmiService.WriteTagAsync(tagName, value);
            }
        }

        public void WriteAlignPosition(double x, double y, double angle)
        {
            _hmiService.WriteTagAsync(PLCConstants.ALIGN_X_TAG, (float)x);
            _hmiService.WriteTagAsync(PLCConstants.ALIGN_Y_TAG, (float)y);
            _hmiService.WriteTagAsync(PLCConstants.ALIGN_R_TAG, (float)angle);
        }

        public void WriteRegister(string tagName, ushort value)
        {
            _hmiService.WriteTagAsync(tagName, value);
        }

        public void WriteDouble(string tagName, double value)
        {
            _hmiService.WriteTagAsync(tagName, value);
        }

        public async Task WriteRegisterAsync(string tagName, ushort value)
        {
            await _hmiService.WriteTagAsync(tagName, value);
        }

        public async Task WriteDoubleAsync(string tagName, double value)
        {
            await _hmiService.WriteTagAsync(tagName, value);
        }

        public async Task WriteRobotPositionAsync(string posName, double value)
        {
            await _hmiService.WriteTagAsync(posName, value);
        }

        public async Task DownloadModelParametersAsync(IDictionary<string, object> parameters)
        {
            _logger.LogInformation("Starting model parameter download...");
            
            // To avoid overloading the PLC, we send in small batches or with delays
            // LeadshineHmi's WriteTagAsync already queues writes, but we can manage it here
            int count = 0;
            foreach (var param in parameters)
            {
                await _hmiService.WriteTagAsync(param.Key, param.Value);
                count++;
                
                // Add a small delay every 10 writes to prevent flooding the PLC network buffer
                if (count % 10 == 0)
                {
                    await Task.Delay(50);
                }
            }
            
            _logger.LogInformation($"Download complete. Sent {count} parameters.");
        }

        public ushort[] GetRegisterArrayValue(string dataPointName)
        {
            // Placeholder - LeadshineHmi doesn't expose direct array access easily for named tags
            // But we can get the value if it's a simple type
            var val = _hmiService.ReadTag(dataPointName);
            if (val is ushort u) return new[] { u };
            return new ushort[0];
        }

        public ushort[] GetRegisterArrayValue(ushort startAddress, ushort count)
        {
            // Direct protocol access
            // This is a bit hacky but for backward compatibility
            return new ushort[count]; // Need to implement in library if really needed
        }

        public void WriteHoldingRegisters(ushort address, ushort[] values)
        {
            // LeadshineHmi usually works with Tags, but we can implement direct write
        }

        public void WriteHoldingRegistersDirect(ushort address, ushort[] values)
        {
            // Implementation...
        }

        public void WriteCoil(string name, bool value)
        {
            _hmiService.WriteTagAsync(name, value);
        }

        public double GetDoubleValue(string tagName)
        {
            var val = _hmiService.ReadTag(tagName);
            return val != null ? Convert.ToDouble(val) : 0.0;
        }

        public ushort GetUInt16Value(string tagName)
        {
            var val = _hmiService.ReadTag(tagName);
            return val != null ? Convert.ToUInt16(val) : (ushort)0;
        }

        public short GetInt16Value(string tagName)
        {
            var val = _hmiService.ReadTag(tagName);
            return val != null ? Convert.ToInt16(val) : (short)0;
        }

        public async Task SetHmiButtonAsync(string tagName, bool value)
        {
            await _hmiService.WriteTagAsync(tagName, value);
        }

        public void SetActiveMonitoringGroups(HaengSungAOI_WPF.Machine.PLC.PLCMonitoringGroup groups)
        {
            // Note: This method would be used to dynamiclly enable/disable polling groups in HmiEngine.
            // For now, it's implemented as a stub to satisfy the IPlcService interface.
            _logger.LogInformation($"PLC Monitoring groups changed to: {groups}");
        }


        public void Dispose()
        {
            _hmiService.StopAsync();
        }
    }
}
