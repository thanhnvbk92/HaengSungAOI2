using HaengSungAOI_WPF.Core.PLC;
using HaengSungAOI_WPF.Services.Machine;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VM.Core;
using CommunityToolkit.Mvvm.Messaging;

namespace HaengSungAOI_WPF.Core
{
    public enum PLCWorkType
    {
        Vision,
        ProductLog,
        TrayUpdate,
        Alarm
    }

    public class PLCWorkItem
    {
        public PLCWorkType Type { get; set; }
        public string TagName { get; set; }
        public object NewValue { get; set; }
        public string ProcedureName { get; set; }
        public ushort Address { get; set; }
    }

    public partial class Machine
    {
        #region Fields and Properties

        private ActionBlock<PLCWorkItem> _visionProcessor;
        private ActionBlock<PLCWorkItem> _ScanOutAndPackingProcessor;
        private ActionBlock<PLCWorkItem> _MachineAlarmProcessor;
        private readonly SemaphoreSlim _productLogLock = new SemaphoreSlim(1, 1);

        private readonly ConcurrentDictionary<string, ushort> _previousVisionTriggerValues = new ConcurrentDictionary<string, ushort>();
        private readonly ConcurrentDictionary<string, ushort> _previousScanoutTriggerValues = new ConcurrentDictionary<string, ushort>();
        private readonly ConcurrentDictionary<string, ushort> _previousTrayTriggerValues = new ConcurrentDictionary<string, ushort>();

        private readonly HashSet<string> _proceduresInFlight = new HashSet<string>();
        private readonly object _flightLock = new object();

        private readonly HashSet<string> _activeAlarms = new HashSet<string>();
        private readonly object _alarmLock = new object();

        // Vision trigger tags mapping
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

        // Vision result tags mapping
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

        // Alarm messages mapping
        private readonly Dictionary<string, string> _alarmMessages = new Dictionary<string, string>
        {
            { "Alarm_EMG_Stop", "Emergency Stop Activated" },
            { "Alarm_Main_Pressure", "Main Air Pressure Low" },
            { "Alarm_Door_1_Open", "Safety Door 1 is Open" },
            { "Alarm_Door_2_Open", "Safety Door 2 is Open" },
            { "Alarm_X1_Axis", "X1 Axis Error" },
            { "Alarm_Y1_Axis", "Y1 Axis Error" },
            { "Alarm_C1_Axis", "C1 Axis Error" },
            { "Alarm_X2_Axis", "X2 Axis Error" },
            { "Alarm_Z2_Axis", "Z2 Axis Error" },
            { "Alarm_X3_Axis", "X3 Axis Error" },
            { "Alarm_Y3_Axis", "Y3 Axis Error" },
            { "Alarm_Z4_Axis", "Z4 Axis Error" },
            { "Alarm_C4_Axis", "C4 Axis Error" },
            { "Alarm_Z5_Axis", "Z5 Axis Error" },
            { "Alarm_C5_Axis", "C5 Axis Error" },
            { "Alarm_Z61_Axis", "Z61 Axis Error" },
            { "Alarm_Z62_Axis", "Z62 Axis Error" },
            { "Alarm_NG_CV", "NG Conveyor Error" },
            { "Alarm_Cyl_Infeed_Up", "Infeed Cylinder Up Error" },
            { "Alarm_Cyl_Infeed_Down", "Infeed Cylinder Down Error" },
            { "Alarm_Cyl_NG_Up", "NG Cylinder Up Error" },
            { "Alarm_Cyl_NG_Down", "NG Cylinder Down Error" },
            { "Alarm_Cyl_Outfeed_Up", "Outfeed Cylinder Up Error" },
            { "Alarm_Cyl_Outfeed_Down", "Outfeed Cylinder Down Error" },
            { "Alarm_Cyl_Pickup_Tray_Up", "Pickup Tray Cylinder Up Error" },
            { "Alarm_Cyl_Pickup_Tray_Down", "Pickup Tray Cylinder Down Error" },
            { "Alarm_Vacuum_Infeed", "Infeed Vacuum Error" },
            { "Alarm_Vacuum_NG", "NG Vacuum Error" },
            { "Alarm_Vacuum_Outfeed", "Outfeed Vacuum Error" },
            { "Alarm_Vacuum_Pickup_Tray", "Pickup Tray Vacuum Error" },
            { "Alarm_Vacuum_Inspect_1", "Inspect 1 Vacuum Error" },
            { "Alarm_Vacuum_Inspect_2", "Inspect 2 Vacuum Error" },
            { "Alarm_Infeed_Unit", "Infeed Unit Error" },
            { "Alarm_Infeed_Cannot_Pick_Product", "Infeed Cannot Pick Product" },
            { "Alarm_Infeed_Product_Falled", "Infeed Product Dropped" },
            { "Alarm_Camera_1_Cannot_Take_Photo", "Camera 1 Cannot Take Photo" },
            { "Alarm_Product_Input_Error", "Product Input Error" },
            { "Alarm_Infeed_Unit_ORG_Timeout", "Infeed Unit ORG Timeout" },
            { "Alarm_Transfer_Unit", "Transfer Unit Error" },
            { "Alarm_Transfer_Cannot_Pick_Product", "Transfer Cannot Pick Product" },
            { "Alarm_Transfer_Product_Falled", "Transfer Product Dropped" },
            { "Alarm_Transfer_Unit_ORG_Timeout", "Transfer Unit ORG Timeout" },
            { "Alarm_Outfeed_Unit", "Outfeed Unit Error" },
            { "Alarm_Outfeed_Cannot_Pick_Product", "Outfeed Cannot Pick Product" },
            { "Alarm_Outfeed_Product_Falled", "Outfeed Product Dropped" },
            { "Alarm_Outfeed_Unit_ORG_Timeout", "Outfeed Unit ORG Timeout" },
            { "Alarm_Inspect_1_Unit", "Inspect 1 Unit Error" },
            { "Alarm_Inspect_1_Cannot_Hold_Product", "Inspect 1 Cannot Hold Product" },
            { "Alarm_Camera_2_Cannot_Take_Photo", "Camera 2 Cannot Take Photo" },
            { "Alarm_Inspect_1_Unit_ORG_Timeout", "Inspect 1 Unit ORG Timeout" },
            { "Alarm_Inspect_2_Unit", "Inspect 2 Unit Error" },
            { "Alarm_Inspect_2_Cannot_Hold_Product", "Inspect 2 Cannot Hold Product" },
            { "Alarm_Camera_3_Cannot_Take_Photo", "Camera 3 Cannot Take Photo" },
            { "Alarm_Inspect_2_Unit_ORG_Timeout", "Inspect 2 Unit ORG Timeout" },
            { "Alarm_Supply_Tray_Unit", "Tray Supply Unit Error" },
            { "Alarm_Supply_Tray_Input_Empty", "Input Tray Supply Empty" },
            { "Alarm_Supply_Tray_Input_Over", "Input Tray Supply Overfilled" },
            { "Alarm_Supply_Tray_Output_Empty", "Output Tray Supply Empty" },
            { "Alarm_Supply_Tray_Output_Full", "Output Tray Supply Full" },
            { "Alarm_Supply_Tray_Unit_ORG_Timeout", "Tray Supply Unit ORG Timeout" },
            { "Alarm_NG_CV_Unit", "NG Conveyor Unit Error" },
            { "Alarm_NG_CV_Full", "NG Conveyor Full - Empty Required" }
        };

        // Shared variables for quantities
        public int PCB_Quantity = 0;
        public int PCBTrayQuantity = 0;
        public int BlankTrayQuantity = 0;

        #endregion

        #region Initialization

        private void InitializeProcessors()
        {
            // Vision Trigger Processor
            _visionProcessor = new ActionBlock<PLCWorkItem>(async item =>
            {
                try
                {
                    await ProcessVisionTriggerAsync(item.TagName, item.ProcedureName, Convert.ToUInt16(item.NewValue));
                }
                catch (Exception ex)
                {
                    Logger.Error("Machine_VisionProcessor", $"Error processing vision trigger {item.TagName}", ex);
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true });

            // Scan-out and Packing Processor
            _ScanOutAndPackingProcessor = new ActionBlock<PLCWorkItem>(async item =>
            {
                try
                {
                    switch (item.Type)
                    {
                        case PLCWorkType.ProductLog:
                            await HandleProductLogTriggerAsync(item.TagName, item.NewValue);
                            break;
                        case PLCWorkType.TrayUpdate:
                            UpdateTrayQuantitiesFromPLC(item.TagName, item.NewValue);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Machine_PackingProcessor", $"Error processing item type {item.Type}", ex);
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true });

            // Alarm Processor
            _MachineAlarmProcessor = new ActionBlock<PLCWorkItem>(item =>
            {
                try
                {
                    HandleAlarmChanged(item.TagName, item.NewValue, item.Address);
                }
                catch (Exception ex)
                {
                    Logger.Error("Machine_AlarmProcessor", $"Error processing alarm {item.TagName}", ex);
                }
                return Task.CompletedTask;
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true });
        }

        private void InitializePlcEvents()
        {
            if (PLC == null) return;

            PLC.VisionTriggered += (s, e) =>
            {
                lock (_flightLock)
                {
                    if (_proceduresInFlight.Contains(e.ProcedureName)) return;
                    _proceduresInFlight.Add(e.ProcedureName);
                }

                _visionProcessor.Post(new PLCWorkItem
                {
                    Type = PLCWorkType.Vision,
                    TagName = e.TagName,
                    NewValue = e.TriggerValue,
                    ProcedureName = e.ProcedureName
                });
            };

            PLC.TrayUpdated += (s, e) =>
            {
                _ScanOutAndPackingProcessor.Post(new PLCWorkItem
                {
                    Type = PLCWorkType.TrayUpdate,
                    TagName = e.TagName,
                    NewValue = e.NewValue
                });
            };

            PLC.AlarmChanged += (s, e) =>
            {
                _MachineAlarmProcessor.Post(new PLCWorkItem
                {
                    Type = PLCWorkType.Alarm,
                    TagName = e.AlarmName,
                    NewValue = e.IsActive ? (ushort)1 : (ushort)0,
                    Address = e.Address
                });
            };

            PLC.ConnectionStatusChanged += OnPLCConnectionStatusChanged;

            // Subscribe to Vision Service events
            _visionService.ProcedureCompleted += OnVisionProcedureCompleted;
        }

        #endregion

        #region Vision Trigger and Result Handling

        private async Task ProcessVisionTriggerAsync(string tagName, string procedureName, ushort triggerValue)
        {
            try
            {
                // Reset trigger in PLC
                ResetVisionTrigger(tagName);

                // Run procedure via Vision Service
                await _visionService.RunProcedureAsync(procedureName);
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error processing vision trigger {tagName} -> {procedureName}", ex);
                _errorService.ReportError("VisionTrigger", $"Failed to process {procedureName} trigger", ex);
                
                // Clear in-flight guard on error
                lock (_flightLock) _proceduresInFlight.Remove(procedureName);
            }
        }

        private void OnVisionProcedureCompleted(object sender, VisionProcedureCompletedEventArgs e)
        {
            try
            {
                switch (e.ProcedureName)
                {
                    case "Align":
                        HandleAlignCompleted(e);
                        break;
                    case "Inspect1":
                        HandleInspect1Completed(e);
                        break;
                    case "Inspect2":
                        HandleInspect2Completed(e);
                        break;
                    case "Inspect3":
                        HandleInspect3Completed(e);
                        break;
                    case "Inspect4":
                        HandleInspect4Completed(e);
                        break;
                    case "Inspect5":
                        HandleInspect5Completed(e);
                        break;
                    case "Inspect6":
                        HandleInspect6Completed(e);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error handling completion of {e.ProcedureName}", ex);
            }
            finally
            {
                // Clear in-flight guard
                lock (_flightLock) _proceduresInFlight.Remove(e.ProcedureName);
            }
        }

        private void HandleAlignCompleted(VisionProcedureCompletedEventArgs e)
        {
            if (e.IsOK)
            {
                PLC.WriteAlignPosition(e.AlignX, e.AlignY, e.AlignAngle);
            }
            PLC.WriteVisionResult("Align", e.IsOK);
        }

        private async void HandleInspect1Completed(VisionProcedureCompletedEventArgs e)
        {
            var dbService = new AutoVisionDbService();
            bool isOK = e.IsOK;
            string pid = ReadBarcodeFromPLC(StationType.Station1);
            
            try
            {
                _ = dbService.InsertVisionInputTimeAsync(pid, App.ActualMachineId.GetValueOrDefault());

                if (!pid.ToLower().Contains("hs"))
                {
                    isOK = IsByPass;
                    if (!IsByPass)
                    {
                        await dbService.UpdateVisionScanoutAsync(new TbAutoVisionScanout
                        {
                            Pid = pid, ScanoutStatus = "NG", ErrorMessage = "PID không hợp lệ", ScanoutTime = DateTime.Now
                        });
                    }
                }
                else
                {
                    var rfService = new BlockRFService();
                    var blockResult = await dbService.IsBlock(pid);
                    var rfBlockResult = await rfService.IsBlockAsync(pid);
                    bool alreadyScanOut = await dbService.IsScanOut(pid);
                    string foundEbr = await dbService.GetEbrForPid(pid);

                    string errorMsg = "";
                    if (alreadyScanOut) errorMsg += "Already scanout; ";
                    if (blockResult.isBlock) errorMsg += (blockResult.reason ?? "Blocked in HSMES; ");
                    if (rfBlockResult != null) errorMsg += $"RF Blocked; ";
                    if (string.IsNullOrEmpty(foundEbr)) errorMsg += "EBR not found; ";

                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        isOK = IsByPass;
                        await dbService.UpdateVisionScanoutAsync(new TbAutoVisionScanout
                        {
                            Pid = pid, ScanoutStatus = "NG", ErrorMessage = errorMsg, ScanoutTime = DateTime.Now
                        });
                    }
                }

                _visionService.SaveImage(e.ProcedureName, pid, isOK, "");
                PLC.WriteVisionResult("Inspect1", isOK);
            }
            catch (Exception ex)
            {
                Logger.Error("Inspect1", ex.ToString());
            }
        }

        // Simpler handlers for other inspection stations
        private void HandleInspect2Completed(VisionProcedureCompletedEventArgs e) => PLC.WriteVisionResult("Inspect2", IsByPass || e.IsOK);
        private void HandleInspect3Completed(VisionProcedureCompletedEventArgs e) => PLC.WriteVisionResult("Inspect3", IsByPass || e.IsOK);
        private void HandleInspect4Completed(VisionProcedureCompletedEventArgs e) => PLC.WriteVisionResult("Inspect4", IsByPass || e.IsOK);
        private void HandleInspect5Completed(VisionProcedureCompletedEventArgs e) => PLC.WriteVisionResult("Inspect5", IsByPass || e.IsOK);
        private void HandleInspect6Completed(VisionProcedureCompletedEventArgs e) => PLC.WriteVisionResult("Inspect6", IsByPass || e.IsOK);

        private void ResetVisionTrigger(string tagName)
        {
            if (PLC?.IsConnected == true) PLC.WriteRegister(tagName, 0);
        }

        #endregion

        #region Alarm and Quantity Handling

        private void OnPLCConnectionStatusChanged(object sender, bool isConnected)
        {
            if (isConnected) _errorService.ReportError(ErrorType.Information, "Machine", "PLC connected");
            else _errorService.ReportError(ErrorType.Warning, "Machine", "PLC connection lost");
        }

        private void HandleAlarmChanged(string alarmName, object newValue, ushort address)
        {
            bool isAlarmActive = Convert.ToUInt16(newValue) != 0;
            lock (_alarmLock)
            {
                bool wasActive = _activeAlarms.Contains(alarmName);
                if (isAlarmActive && !wasActive)
                {
                    _activeAlarms.Add(alarmName);
                    string message = _alarmMessages.ContainsKey(alarmName) ? _alarmMessages[alarmName] : $"Unknown Alarm: {alarmName}";
                    ErrorType type = GetAlarmErrorType(alarmName);
                    _errorService.ReportError(type, "PLC Alarm", message);
                    if (type == ErrorType.Critical && IsMachineEnabled) StopMachine();
                }
                else if (!isAlarmActive && wasActive)
                {
                    _activeAlarms.Remove(alarmName);
                }
            }
        }

        private ErrorType GetAlarmErrorType(string alarmName)
        {
            if (alarmName.Contains("EMG") || alarmName.Contains("Door") || alarmName.Contains("Pressure")) return ErrorType.Critical;
            if (alarmName.Contains("_Axis")) return ErrorType.Error;
            return ErrorType.Warning;
        }

        public void ReadTrayQuantitiesFromPLC()
        {
            if (PLC?.IsConnected != true) return;
            PCB_Quantity = PLC.GetUInt16Value("PCB_Slot");
            PCBTrayQuantity = PLC.GetUInt16Value("PCB_Trays");
            BlankTrayQuantity = PLC.GetUInt16Value("Blank_Trays");
        }

        private void UpdateTrayQuantitiesFromPLC(string dataPointName, object newValue)
        {
            int quantity = Convert.ToInt32(newValue);
            switch (dataPointName)
            {
                case "PCB_Slot": PCB_Quantity = quantity; break;
                case "PCB_Trays": PCBTrayQuantity = quantity; break;
                case "Blank_Trays": BlankTrayQuantity = quantity; break;
            }
        }

        #endregion

        #region Product Logging

        private async Task HandleProductLogTriggerAsync(string dataPointName, object newValue)
        {
            if (dataPointName == "Product_OK_Trigger") await ProcessOKProductLogAsync();
            else if (dataPointName == "Product_NG_Trigger") await ProcessNGProductLogAsync();
        }

        private async Task ProcessOKProductLogAsync()
        {
            await _productLogLock.WaitAsync();
            try
            {
                string barcode = ReadBarcodeFromPLC(StationType.FinalOk);
                int slot = ReadSlotNumberFromPLC();
                Logger.Info("Machine", $"Process OK: {barcode} Slot: {slot}");
                
                if (EnableScanOut)
                {
                    // Scan-out logic handled here (delegated in future to IScanOutService)
                }
                
                PLC.WriteRegister("ScanOut_OK", 1);
                PLC.WriteRegister("Product_OK_Trigger", 0);
            }
            finally { _productLogLock.Release(); }
        }

        private async Task ProcessNGProductLogAsync()
        {
            await _productLogLock.WaitAsync();
            try
            {
                string barcode = ReadBarcodeFromPLC(StationType.FinalNg);
                Logger.Info("Machine", $"Process NG: {barcode}");
                PLC.WriteRegister("Product_NG_Trigger", 0);
            }
            finally { _productLogLock.Release(); }
        }

        #endregion

        #region Helper Methods

        public enum StationType { Station1, Transfer, Station2, FinalOk, FinalNg }

        private string ReadBarcodeFromPLC(StationType stationType)
        {
            if (PLC?.IsConnected != true) return "";
            ushort startAddress = stationType switch
            {
                StationType.Station1 => 450,
                StationType.Transfer => 750,
                StationType.Station2 => 760,
                StationType.FinalOk => 460,
                StationType.FinalNg => 470,
                _ => 450
            };
            ushort[] registers = PLC.GetRegisterArrayValue(startAddress, 10);
            if (registers == null) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var reg in registers)
            {
                byte h = (byte)((reg >> 8) & 0xFF), l = (byte)(reg & 0xFF);
                if (h >= 0x20 && h <= 0x7E) sb.Append((char)h);
                if (l >= 0x20 && l <= 0x7E) sb.Append((char)l);
            }
            return sb.ToString().Trim();
        }

        private int ReadSlotNumberFromPLC()
        {
            if (PLC?.IsConnected != true) return 0;
            ushort[] registers = PLC.GetRegisterArrayValue(448, 1);
            return (registers != null && registers.Length > 0) ? registers[0] : 0;
        }

        public void ClearQueues()
        {
            lock (_flightLock) _proceduresInFlight.Clear();
            _previousVisionTriggerValues.Clear();
            _previousScanoutTriggerValues.Clear();
            _previousTrayTriggerValues.Clear();
        }

        #endregion
    }

    public class BlockRFService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        public async Task<RFInfo> IsBlockAsync(string pid)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://10.221.191.183:8081/api/TraceBackHistory/getErorrLogByPid/{pid}");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                var rfInfos = JsonSerializer.Deserialize<List<RFInfo>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return rfInfos?.FirstOrDefault();
            }
            catch { return null; }
        }
    }

    public class RFInfo { public string Band { get; set; } public string MachineIP { get; set; } public bool Cleared { get; set; } }
}

