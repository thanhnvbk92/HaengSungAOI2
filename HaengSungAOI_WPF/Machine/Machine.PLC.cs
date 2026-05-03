using HaengSungAOI_WPF.Machine.PLC;
using HaengSungAOI_WPF.Machine.PLC.PLC;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Utils;
using HaengSungAOI_WPF.ViewModels;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VM.Core;
using VM.PlatformSDKCS;
using ZXing.QrCode.Internal;

namespace HaengSungAOI_WPF.Machine
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
    /// <summary>
    /// Event arguments for vision trigger events from PLC
    /// </summary>
    public class VisionTriggerEventArgs : EventArgs
    {
        /// <summary>
        /// The PLC tag name that was triggered (e.g., "MW400")
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// The procedure name associated with the trigger (e.g., "Align")
        /// </summary>
        public string ProcedureName { get; }

        /// <summary>
        /// The trigger value (typically 1 when triggered)
        /// </summary>
        public ushort TriggerValue { get; }

        /// <summary>
        /// Timestamp when the trigger was detected
        /// </summary>
        public DateTime Timestamp { get; }

        public VisionTriggerEventArgs(string tagName, string procedureName, ushort triggerValue)
        {
            TagName = tagName;
            ProcedureName = procedureName;
            TriggerValue = triggerValue;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event arguments for vision procedure completion events
    /// </summary>
    public class VisionProcedureCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the procedure that completed (e.g., "Align", "Inspect1")
        /// </summary>
        public string ProcedureName { get; }

        /// <summary>
        /// The VmProcedure that completed
        /// </summary>
        public VmProcedure Procedure { get; }

        /// <summary>
        /// Timestamp when the procedure completed
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Whether the inspection result is OK (true) or NG (false)
        /// </summary>
        public bool IsOK { get; }

        /// <summary>
        /// Align X position (only for Align procedure)
        /// </summary>
        public float AlignX { get; }

        /// <summary>
        /// Align Y position (only for Align procedure)
        /// </summary>
        public float AlignY { get; }

        /// <summary>
        /// Align Angle/Rotation (only for Align procedure)
        /// </summary>
        public float AlignAngle { get; }

        public VisionProcedureCompletedEventArgs(string procedureName, VmProcedure procedure, bool isOK,
            float alignX = 0, float alignY = 0, float alignAngle = 0)
        {
            ProcedureName = procedureName;
            Procedure = procedure;
            Timestamp = DateTime.Now;
            IsOK = isOK;
            AlignX = alignX;
            AlignY = alignY;
            AlignAngle = alignAngle;
        }
    }

    /// <summary>
    /// Represents the working data for a single PCB as it moves through the machine
    /// </summary>
    public class WipData
    {
        public DateTime StartTime { get; set; } = DateTime.Now;
        public string PID { get; set; }

    }

    /// <summary>
    /// Machine partial class - PLC communication and vision trigger handling
    /// </summary>
    public partial class Machine
    {

        private readonly ConcurrentQueue<WipData> _wipQueue1 = new ConcurrentQueue<WipData>();
        private readonly ConcurrentQueue<WipData> _wipQueue2 = new ConcurrentQueue<WipData>();
        private readonly ConcurrentQueue<WipData> _wipQueueScanout = new ConcurrentQueue<WipData>();

        string saveDir = AppConfig.SaveDir;

        private readonly ActionBlock<PLCWorkItem> _visionProcessor;
        private readonly ActionBlock<PLCWorkItem> _ScanOutAndPackingProcessor;
        private readonly ActionBlock<PLCWorkItem> _MachineAlarmProcessor;

        // MaxDegreeOfParallelism = 1 đảm bảo: Xong cái này mới tới cái tiếp theo


        // Camera Busy Flags và SemaphoreSlim đã được loại bỏ.
        // ActionBlock phân cụm (alignProcessor / cam2Processor / cam3Processor) đã đảm bảo
        // sequential access trong mỗi cụm camera, nên không cần lock thêm.

        // Track inspection start time per procedure to measure tack time
        private readonly ConcurrentDictionary<string, DateTime> _inspectionStartTimes = new ConcurrentDictionary<string, DateTime>();

        // Rising-edge detection: only trigger when value changes 0→1, NOT on every poll cycle
        private readonly ConcurrentDictionary<string, ushort> _previousVisionTriggerValues = new ConcurrentDictionary<string, ushort>();
        private readonly ConcurrentDictionary<string, ushort> _previousScanoutTriggerValues = new ConcurrentDictionary<string, ushort>();
        private readonly ConcurrentDictionary<string, ushort> _previousTrayTriggerValues = new ConcurrentDictionary<string, ushort>();


        // Procedure in-flight guard: reject new trigger if previous callback not yet completed
        // Tracks procedures from Run() call until OnWorkEndStatusCallBack finishes
        private readonly HashSet<string> _proceduresInFlight = new HashSet<string>();
        private readonly object _flightLock = new object();

        /// <summary>
        /// Xóa sạch toàn bộ WIP Queue và in-flight guard khi dừng máy.
        /// Phải gọi khi Stop Machine để tránh PID cũ lậm sang chu kỳ sản xuất tiếp theo.
        /// </summary>
        public void ClearAllQueues()
        {
            // Xóa WIP Queue (các board đang dở trong machine)
            while (_wipQueue1.TryDequeue(out _)) { }
            while (_wipQueue2.TryDequeue(out _)) { }
            while (_wipQueueScanout.TryDequeue(out _)) { }

            // Xóa In-Flight guard — cho phép Vision trigger chạy lại khi Start mới
            lock (_flightLock)
            {
                _proceduresInFlight.Clear();
            }

            // Reset edge-detection — tránh "edge cũ" bị detect khi PLC start lại
            _previousVisionTriggerValues.Clear();
            _previousScanoutTriggerValues.Clear();
            _previousTrayTriggerValues.Clear();

            Logger.Info("Machine", "ClearAllQueues: WIP Queue, In-Flight, Edge-Detection đã được xóa sạch");
        }

        // Vision trigger tags dictionary - maps PLC tag names to VmProcedure names
        // Relates to PLCConstants
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

        // Vision result tags dictionary - maps procedure names to PLC tag addresses for writing results
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

        // Alarm messages dictionary - maps alarm register names to user-friendly messages
        private readonly Dictionary<string, string> _alarmMessages = new Dictionary<string, string>
        {
            // System Alarms
            { "Alarm_EMG_Stop", "Emergency Stop Activated" },
            { "Alarm_Main_Pressure", "Main Air Pressure Low" },
            { "Alarm_Door_1_Open", "Safety Door 1 is Open" },
            { "Alarm_Door_2_Open", "Safety Door 2 is Open" },
            
            // Axis Alarms
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
            
            // Cylinder Alarms
            { "Alarm_Cyl_Infeed_Up", "Infeed Cylinder Up Error" },
            { "Alarm_Cyl_Infeed_Down", "Infeed Cylinder Down Error" },
            { "Alarm_Cyl_NG_Up", "NG Cylinder Up Error" },
            { "Alarm_Cyl_NG_Down", "NG Cylinder Down Error" },
            { "Alarm_Cyl_Outfeed_Up", "Outfeed Cylinder Up Error" },
            { "Alarm_Cyl_Outfeed_Down", "Outfeed Cylinder Down Error" },
            { "Alarm_Cyl_Pickup_Tray_Up", "Pickup Tray Cylinder Up Error" },
            { "Alarm_Cyl_Pickup_Tray_Down", "Pickup Tray Cylinder Down Error" },
            
            // Vacuum Alarms
            { "Alarm_Vacuum_Infeed", "Infeed Vacuum Error" },
            { "Alarm_Vacuum_NG", "NG Vacuum Error" },
            { "Alarm_Vacuum_Outfeed", "Outfeed Vacuum Error" },
            { "Alarm_Vacuum_Pickup_Tray", "Pickup Tray Vacuum Error" },
            { "Alarm_Vacuum_Inspect_1", "Inspect 1 Vacuum Error" },
            { "Alarm_Vacuum_Inspect_2", "Inspect 2 Vacuum Error" },
            
            // Unit Alarms
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
            
            // Tray Supply Alarms
            { "Alarm_Supply_Tray_Unit", "Tray Supply Unit Error" },
            { "Alarm_Supply_Tray_Input_Empty", "Input Tray Supply Empty" },
            { "Alarm_Supply_Tray_Input_Over", "Input Tray Supply Overfilled" },
            { "Alarm_Supply_Tray_Output_Empty", "Output Tray Supply Empty" },
            { "Alarm_Supply_Tray_Output_Full", "Output Tray Supply Full" },
            { "Alarm_Supply_Tray_Unit_ORG_Timeout", "Tray Supply Unit ORG Timeout" },
            
            // NG Conveyor Alarms
            { "Alarm_NG_CV_Unit", "NG Conveyor Unit Error" },
            { "Alarm_NG_CV_Full", "NG Conveyor Full - Empty Required" },
        };

        // Track active alarms to avoid duplicate error messages
        private readonly HashSet<string> _activeAlarms = new HashSet<string>();
        private readonly object _alarmLock = new object();

        // Serialize product log processing to protect serial port and PLC scan out registers
        private readonly object _productLogLock = new object();


        // Align position tags - use constants from PLCConstants
        private const string ALIGN_X_TAG = PLCConstants.ALIGN_X_TAG;
        private const string ALIGN_Y_TAG = PLCConstants.ALIGN_Y_TAG;
        private const string ALIGN_R_TAG = PLCConstants.ALIGN_R_TAG;
        private const ushort ALIGN_X_ADDRESS = PLCConstants.ALIGN_X_ADDRESS;
        private const ushort ALIGN_Y_ADDRESS = PLCConstants.ALIGN_Y_ADDRESS;
        private const ushort ALIGN_R_ADDRESS = PLCConstants.ALIGN_R_ADDRESS;

        // Events for vision triggers
        public event EventHandler<VisionTriggerEventArgs> AlignTriggered;
        public event EventHandler<VisionTriggerEventArgs> Inspect1Triggered;
        public event EventHandler<VisionTriggerEventArgs> Inspect2Triggered;
        public event EventHandler<VisionTriggerEventArgs> Inspect3Triggered;
        public event EventHandler<VisionTriggerEventArgs> Inspect4Triggered;
        public event EventHandler<VisionTriggerEventArgs> Inspect5Triggered;
        public event EventHandler<VisionTriggerEventArgs> Inspect6Triggered;

        // Events for vision procedure completion (OnWorkEndStatusCallBack)
        public event EventHandler<VisionProcedureCompletedEventArgs> AlignCompleted;
        public event EventHandler<VisionProcedureCompletedEventArgs> Inspect1Completed;
        public event EventHandler<VisionProcedureCompletedEventArgs> Inspect2Completed;
        public event EventHandler<VisionProcedureCompletedEventArgs> Inspect3Completed;
        public event EventHandler<VisionProcedureCompletedEventArgs> Inspect4Completed;
        public event EventHandler<VisionProcedureCompletedEventArgs> Inspect5Completed;
        public event EventHandler<VisionProcedureCompletedEventArgs> Inspect6Completed;


        /// <summary>
        /// Initialize PLC Controller for HMI and axis control
        /// </summary>
        private void InitializePLCController()
        {
            try
            {
                Logger.Info("Machine", "Initializing PLC Controller");

                // Create PLC controller instance using constants
                PLC = new PLCController(
                    PLCConstants.PLC_IP_ADDRESS,
                    PLCConstants.PLC_PORT,
                    PLCConstants.PLC_UNIT_IDENTIFIER);

                // Configure all PLC data points from constant dictionaries
                int configuredCount = PLCConfiguration.ConfigureFromConstants(PLC);
                Logger.Info("Machine", $"Configured {configuredCount} PLC data points");

                // Configure vision trigger data points
                ConfigureVisionTriggerTags();

                // Subscribe to PLC events
                PLC.DataChanged += OnPLCDataChanged;
                PLC.ConnectionStatusChanged += OnPLCConnectionStatusChanged;
                PLC.ErrorOccurred += OnPLCErrorOccurred;

                // Connect to PLC
                if (PLC.Connect())
                {
                    // Start polling PLC data
                    PLC.Start();
                    Logger.Info("Machine", "PLC Controller initialized and started successfully");
                    _errorList.AddError(ErrorType.Information, "Machine", "PLC Controller connected");

                    // Log performance analysis after configuration
                    System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
                    {
                        PLC.LogPerformanceReport();
                    });
                }
                else
                {
                    Logger.Error("Machine", "Failed to connect to PLC");
                    _errorList.AddError(ErrorType.Error, "Machine", "PLC connection failed");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error initializing PLC Controller", ex);
                _errorList.AddException("Machine", "PLC Controller initialization failed", ex);
            }
        }

        /// <summary>
        /// Configure vision trigger PLC tags for monitoring
        /// </summary>
        private void ConfigureVisionTriggerTags()
        {
            try
            {
                Logger.Info("Machine", "Configuring vision trigger PLC tags");

                // Configure vision trigger tags (for reading)
                foreach (var kvp in _visionTriggerTags)
                {
                    string tagName = kvp.Key;
                    string procedureName = kvp.Value;

                    // Extract the address number from tag name (e.g., "MW400" -> 400)
                    int address = int.Parse(tagName.Substring(2));

                    // Configure as holding register with polling using AddHoldingRegister
                    PLC.AddHoldingRegister(tagName, (ushort)address, 1, $"Vision Trigger for {procedureName}");

                    Logger.Debug("Machine", $"Configured vision trigger: {tagName} -> {procedureName} at address {address}");
                }

                // Configure vision result tags (for writing)
                foreach (var kvp in _visionResultTags)
                {
                    string procedureName = kvp.Key;
                    string tagName = kvp.Value;

                    // Extract the address number from tag name (e.g., "MW410" -> 410)
                    int address = int.Parse(tagName.Substring(2));

                    // Configure as holding register for writing results
                    PLC.AddHoldingRegister(tagName, (ushort)address, 1, $"Vision Result for {procedureName}");

                    Logger.Debug("Machine", $"Configured vision result: {tagName} <- {procedureName} at address {address}");
                }

                // Configure Align position tags (LREAL = 8 bytes = 4 registers each)
                PLC.AddHoldingRegister(ALIGN_X_TAG, ALIGN_X_ADDRESS, 4, "Align X Position (LREAL)");
                PLC.AddHoldingRegister(ALIGN_Y_TAG, ALIGN_Y_ADDRESS, 4, "Align Y Position (LREAL)");
                PLC.AddHoldingRegister(ALIGN_R_TAG, ALIGN_R_ADDRESS, 4, "Align R/Angle Position (LREAL)");

                Logger.Debug("Machine", $"Configured Align position tags: {ALIGN_X_TAG}, {ALIGN_Y_TAG}, {ALIGN_R_TAG}");

                // Note: SubscribeToVisionProcedureCallbacks is called separately after vision solution is loaded

                Logger.Info("Machine", $"Configured {_visionTriggerTags.Count} vision trigger tags, {_visionResultTags.Count} result tags, and 3 align position tags");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error configuring vision trigger tags", ex);
                _errorList.AddException("Machine", "Vision trigger tag configuration failed", ex);
            }
        }

        /// <summary>
        /// Write vision inspection result to PLC
        /// </summary>
        /// <param name="procedureName">Name of the procedure (e.g., "Align", "Inspect1")</param>
        /// <param name="isOK">True if inspection passed (OK), False if failed (NG)</param>
        public void WriteVisionResult(string procedureName, bool isOK)
        {
            try
            {
                if (!_visionResultTags.TryGetValue(procedureName, out string tagName))
                {
                    Logger.Warning("Machine", $"No result tag configured for procedure: {procedureName}");
                    return;
                }

                if (PLC == null || !PLC.IsConnected)
                {
                    Logger.Warning("Machine", $"Cannot write vision result for {procedureName}: PLC not connected");
                    return;
                }

                // Write 1 for OK, 0 for NG
                ushort resultValue = isOK ? (ushort)1 : (ushort)2;
                PLC.WriteHoldingRegister(tagName, resultValue);

                string resultText = isOK ? "OK (1)" : "NG (2)";
                Logger.Info("Machine", $"Wrote vision result: {procedureName} -> {tagName} = {resultText}");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error writing vision result for {procedureName}", ex);
            }
        }

        /// <summary>
        /// Read the OK result from a VmProcedure's ModuResult
        /// </summary>
        /// <param name="procedure">The VmProcedure to read result from</param>
        /// <returns>True if OK, False if NG</returns>
        private bool ReadProcedureResult(VmProcedure procedure)
        {
            try
            {
                if (procedure?.ModuResult == null)
                    return false;

                int okValue = procedure.ModuResult.GetOutputInt("OK").pIntVal[0];
                if (okValue == 0) okValue = 2; // NG
                return okValue == 1;
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error reading procedure result", ex);
                return false;
            }
        }

        /// <summary>
        /// Write LREAL (double) value to PLC using 4 holding registers
        /// </summary>
        /// <param name="tagName">Tag name for the first register</param>
        /// <param name="value">Double value to write</param>
        private void WriteLRealToPLC(string tagName, double value)
        {
            try
            {
                if (PLC == null || !PLC.IsConnected)
                {
                    Logger.Warning("Machine", $"Cannot write LREAL {tagName}: PLC not connected");
                    return;
                }

                // Convert double to bytes (LREAL is 8 bytes)
                byte[] bytes = BitConverter.GetBytes(value);

                // Convert to 4 ushort values (each register is 2 bytes)
                ushort[] registers = new ushort[4];
                for (int i = 0; i < 4; i++)
                {
                    // Big-endian byte order for Modbus
                    registers[i] = (ushort)((bytes[i * 2 + 1] << 8) | bytes[i * 2]);
                }

                PLC.WriteHoldingRegisters(tagName, registers);

                Logger.Debug("Machine", $"Wrote LREAL to PLC: {tagName} = {value}");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error writing LREAL {tagName} to PLC", ex);
            }
        }

        /// <summary>
        /// Write Align position results to PLC (X, Y, Angle as LREAL)
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="angle">Angle/Rotation</param>
        public void WriteAlignPositionToPLC(double x, double y, double angle)
        {
            try
            {
                WriteLRealToPLC(ALIGN_X_TAG, x);
                WriteLRealToPLC(ALIGN_Y_TAG, y);
                WriteLRealToPLC(ALIGN_R_TAG, angle);

                //Logger.Info("Machine", $"Wrote Align position to PLC: X={x:F3}, Y={y:F3}, R={angle:F3}");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error writing Align position to PLC", ex);
            }
        }

        /// <summary>
        /// Subscribe to OnWorkEndStatusCallBack for all VmProcedure objects
        /// </summary>
        private void SubscribeToVisionProcedureCallbacks()
        {
            try
            {
                if (Camera_align != null)
                {
                    Camera_align.OnWorkEndStatusCallBack += OnAlignWorkEndStatusCallBack;
                    //Logger.Debug("Machine", "Subscribed to Camera_align OnWorkEndStatusCallBack");
                }

                if (Camera_inspect1 != null)
                {
                    Camera_inspect1.OnWorkEndStatusCallBack += OnInspect1WorkEndStatusCallBack;
                    //Logger.Debug("Machine", "Subscribed to Camera_inspect1 OnWorkEndStatusCallBack");
                }

                if (Camera_inspect2 != null)
                {
                    Camera_inspect2.OnWorkEndStatusCallBack += OnInspect2WorkEndStatusCallBack;
                    //Logger.Debug("Machine", "Subscribed to Camera_inspect2 OnWorkEndStatusCallBack");
                }

                if (Camera_inspect3 != null)
                {
                    Camera_inspect3.OnWorkEndStatusCallBack += OnInspect3WorkEndStatusCallBack;
                    //Logger.Debug("Machine", "Subscribed to Camera_inspect3 OnWorkEndStatusCallBack");
                }

                if (Camera_inspect4 != null)
                {
                    Camera_inspect4.OnWorkEndStatusCallBack += OnInspect4WorkEndStatusCallBack;
                    //Logger.Debug("Machine", "Subscribed to Camera_inspect4 OnWorkEndStatusCallBack");
                }

                if (Camera_inspect5 != null)
                {
                    Camera_inspect5.OnWorkEndStatusCallBack += OnInspect5WorkEndStatusCallBack;
                    //Logger.Debug("Machine", "Subscribed to Camera_inspect5 OnWorkEndStatusCallBack");
                }

                if (Camera_inspect6 != null)
                {
                    Camera_inspect6.OnWorkEndStatusCallBack += OnInspect6WorkEndStatusCallBack;
                    //Logger.Debug("Machine", "Subscribed to Camera_inspect6 OnWorkEndStatusCallBack");
                }

                //Logger.Info("Machine", "Subscribed to all VmProcedure OnWorkEndStatusCallBack events");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error subscribing to VmProcedure callbacks", ex);
            }
        }

        /// <summary>
        /// Unsubscribe from OnWorkEndStatusCallBack for all VmProcedure objects
        /// </summary>
        private void UnsubscribeFromVisionProcedureCallbacks()
        {
            try
            {
                if (Camera_align != null)
                    Camera_align.OnWorkEndStatusCallBack -= OnAlignWorkEndStatusCallBack;

                if (Camera_inspect1 != null)
                    Camera_inspect1.OnWorkEndStatusCallBack -= OnInspect1WorkEndStatusCallBack;

                if (Camera_inspect2 != null)
                    Camera_inspect2.OnWorkEndStatusCallBack -= OnInspect2WorkEndStatusCallBack;

                if (Camera_inspect3 != null)
                    Camera_inspect3.OnWorkEndStatusCallBack -= OnInspect3WorkEndStatusCallBack;

                if (Camera_inspect4 != null)
                    Camera_inspect4.OnWorkEndStatusCallBack -= OnInspect4WorkEndStatusCallBack;

                if (Camera_inspect5 != null)
                    Camera_inspect5.OnWorkEndStatusCallBack -= OnInspect5WorkEndStatusCallBack;

                if (Camera_inspect6 != null)
                    Camera_inspect6.OnWorkEndStatusCallBack -= OnInspect6WorkEndStatusCallBack;

                Logger.Info("Machine", "Unsubscribed from all VmProcedure OnWorkEndStatusCallBack events");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error unsubscribing from VmProcedure callbacks", ex);
            }
        }

        /// <summary>
        /// Handle PLC data changed events
        /// </summary>



        private void OnPLCDataChanged(object sender, PLCDataChangedEventArgs e)
        {
            try
            {
                PLCWorkItem item = null;

                if (_visionTriggerTags.ContainsKey(e.DataPointName))
                {

                    ushort triggerValue = Convert.ToUInt16(e.NewValue);
                    ushort prevValue = _previousVisionTriggerValues.GetOrAdd(e.DataPointName, 0);
                    _previousVisionTriggerValues[e.DataPointName] = triggerValue;

                    // Rising-edge only: 0→1 transition
                    if (triggerValue == 1 && prevValue == 0)
                    {
                        string procedureName = _visionTriggerTags[e.DataPointName];

                        // Reject if previous callback for this procedure has not completed yet
                        lock (_flightLock)
                        {
                            if (_proceduresInFlight.Contains(procedureName))
                            {
                                //Logger.Warning("PLC", $"[SKIP] {procedureName} still in flight (callback not done). PLC re-trigger ignored.");
                                return;
                            }
                            _proceduresInFlight.Add(procedureName);
                        }

                        item = new PLCWorkItem
                        {
                            Type = PLCWorkType.Vision,
                            TagName = e.DataPointName,
                            NewValue = triggerValue,
                            ProcedureName = procedureName
                        };
                        _visionProcessor.Post(item);
                    }
                }
                else if (e.DataPointName == "Product_OK_Trigger" || e.DataPointName == "Product_NG_Trigger")
                {
                    ushort triggerValue = Convert.ToUInt16(e.NewValue);
                    ushort prevValue = _previousScanoutTriggerValues.GetOrAdd(e.DataPointName, 0);
                    _previousScanoutTriggerValues[e.DataPointName] = triggerValue;
                    if (triggerValue == 1 && prevValue == 0)
                    {
                        item = new PLCWorkItem
                        {
                            Type = PLCWorkType.ProductLog,
                            TagName = e.DataPointName,
                            NewValue = e.NewValue
                        };
                        _ScanOutAndPackingProcessor.Post(item);
                    }

                }
                else if (e.DataPointName == "PCB_Slot" || e.DataPointName == "PCB_Trays" || e.DataPointName == "Blank_Trays")
                {
                    ushort currentValue = Convert.ToUInt16(e.NewValue);
                    ushort prevValue = _previousTrayTriggerValues.GetOrAdd(e.DataPointName, ushort.MaxValue); // Khởi tạo giá trị rác ban đầu

                    // ⚡ SỬA: Với các thanh ghi chứa Số Lượng khay/slot, ta chỉ cần bắt sự thay đổi giá trị (Value Changed) chứ không phải sườn lên
                    if (currentValue != prevValue)
                    {
                        _previousTrayTriggerValues[e.DataPointName] = currentValue;

                        item = new PLCWorkItem
                        {
                            Type = PLCWorkType.TrayUpdate,
                            TagName = e.DataPointName,
                            NewValue = e.NewValue
                        };
                        _ScanOutAndPackingProcessor.Post(item);
                    }
                }
                else if (e.DataPointName.StartsWith("Alarm_"))
                {
                    // LỖI TRƯỚC ĐÓ Ở ĐÂY: Bạn hãy kiểm tra kỹ dòng dưới này
                    item = new PLCWorkItem // Đảm bảo khởi tạo đúng class PLCWorkItem
                    {
                        Type = PLCWorkType.Alarm,
                        TagName = e.DataPointName,
                        NewValue = e.NewValue,
                        Address = e.Address,
                    };
                    _MachineAlarmProcessor.Post(item);
                }

            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error: {e.DataPointName}", ex);
            }
        }


        /// <summary>
        /// Handle alarm register changes from PLC
        /// </summary>
        private void HandleAlarmChanged(string alarmName, object newValue, ushort Address)
        {
            try
            {
                // Convert value to alarm state (1 = alarm active, 0 = alarm cleared)
                bool isAlarmActive = false;
                if (newValue is ushort usVal)
                {
                    isAlarmActive = usVal != 0;
                }
                else if (newValue is int intVal)
                {
                    isAlarmActive = intVal != 0;
                }

                lock (_alarmLock)
                {
                    // Check if alarm state changed
                    bool wasActive = _activeAlarms.Contains(alarmName);

                    if (isAlarmActive && !wasActive)
                    {
                        // Alarm activated - add to error list
                        _activeAlarms.Add(alarmName);

                        // Get user-friendly message
                        string message = _alarmMessages.ContainsKey(alarmName)
                            ? _alarmMessages[alarmName]
                            : $"Unknown Alarm: {alarmName}";

                        // Determine error type based on alarm severity
                        ErrorType errorType = GetAlarmErrorType(alarmName);

                        // Add to error list
                        _errorList.AddError(errorType, "PLC Alarm", message);

                        Logger.Warning("Machine", $"PLC Alarm activated: {alarmName} - {message}");

                        //// Log Vision Error to database using App.ActualMachineId
                        //if (App.ActualMachineId.HasValue)
                        //{
                        //    System.Threading.Tasks.Task.Run(async () =>
                        //    {
                        //        try
                        //        {
                        //            var dbService = new Services.Database.AutoVisionDbService();
                        //            await dbService.InsertStartVisionErrorAsync(App.ActualMachineId.Value, Address);
                        //        }
                        //        catch (Exception ex)
                        //        {
                        //            Logger.Error("Machine", $"Failed to log vision error for {alarmName}: {ex.Message}");
                        //        }
                        //    });
                        //}

                        if (App.ActualMachineId.HasValue)
                        {
                            _ = LogVisionErrorAsync(App.ActualMachineId.Value, Address);
                        }

                        // Handle critical alarms (EMG Stop, Door Open, Main Pressure)
                        if (errorType == ErrorType.Critical)
                        {
                            //Logger.Critical("Machine", $"Critical alarm detected: {message}");

                            // Stop machine if it's running
                            if (IsMachineEnabled)
                            {
                                //Logger.Critical("Machine", "Stopping machine due to critical alarm");

                                StopMachine();
                            }
                        }


                    }
                    else if (!isAlarmActive && wasActive)
                    {
                        // Alarm cleared
                        _activeAlarms.Remove(alarmName);

                        string message = _alarmMessages.ContainsKey(alarmName)
                            ? _alarmMessages[alarmName]
                            : $"Unknown Alarm: {alarmName}";

                        //Logger.Info("Machine", $"PLC Alarm cleared: {alarmName} - {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error handling alarm change for {alarmName}", ex);
            }
        }
        async Task LogVisionErrorAsync(int machineId, ushort address)
        {
            try
            {
                var dbService = new Services.Database.AutoVisionDbService();
                await dbService.InsertStartVisionErrorAsync(machineId, address);
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Failed to log vision error: {ex.Message}");
            }
        }
        /// <summary>
        /// Determine error type based on alarm severity
        /// </summary>
        private ErrorType GetAlarmErrorType(string alarmName)
        {
            // Critical alarms that require immediate machine stop
            if (alarmName == "Alarm_EMG_Stop" ||
                alarmName == "Alarm_Main_Pressure" ||
                alarmName == "Alarm_Door_1_Open" ||
                alarmName == "Alarm_Door_2_Open")
            {
                return ErrorType.Critical;
            }

            // Axis alarms are errors
            if (alarmName.Contains("_Axis"))
            {
                return ErrorType.Error;
            }

            // Unit/product handling alarms are warnings
            if (alarmName.Contains("Cannot_Pick") ||
                alarmName.Contains("Cannot_Hold") ||
                alarmName.Contains("Product_Falled"))
            {
                return ErrorType.Warning;
            }

            // Tray supply alarms are warnings
            if (alarmName.Contains("Supply_Tray"))
            {
                return ErrorType.Warning;
            }

            // Default to Error for other alarms
            return ErrorType.Error;
        }

        /// <summary>
        /// Read current tray quantities from PLC (for initial values or on-demand updates)
        /// </summary>
        public void ReadTrayQuantitiesFromPLC()
        {
            try
            {
                if (PLC == null || !PLC.IsConnected)
                {
                    Logger.Warning("Machine", "Cannot read tray quantities: PLC not connected");
                    return;
                }

                // Read PCB Slot count (MW498)
                var pcbSlotData = PLC.GetDataPoint("PCB_Slot");
                if (pcbSlotData != null && pcbSlotData.Value is ushort pcbSlotValue)
                {
                    PCB_Quantity = pcbSlotValue;
                    //Logger.Info("Machine", $"Read PCB Slot from PLC: {PCB_Quantity}");
                }

                // Read PCB Tray quantity (MW499)
                var pcbTrayData = PLC.GetDataPoint("PCB_Trays");
                if (pcbTrayData != null && pcbTrayData.Value is ushort pcbTrayValue)
                {
                    PCBTrayQuantity = pcbTrayValue;
                    //Logger.Info("Machine", $"Read PCB Tray Quantity from PLC: {PCBTrayQuantity}");
                }

                // Read Blank Tray quantity (MW4410)
                var blankTrayData = PLC.GetDataPoint("Blank_Trays");
                if (blankTrayData != null && blankTrayData.Value is ushort blankTrayValue)
                {
                    BlankTrayQuantity = blankTrayValue;
                    //Logger.Info("Machine", $"Read Blank Tray Quantity from PLC: {BlankTrayQuantity}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error reading tray quantities from PLC", ex);
            }
        }

        /// <summary>
        /// Update tray quantity properties from PLC data change events
        /// </summary>
        private void UpdateTrayQuantitiesFromPLC(string dataPointName, object newValue)
        {
            try
            {
                // Convert value to int
                int quantity = 0;
                if (newValue is ushort usVal)
                {
                    quantity = usVal;
                }
                else if (newValue is int intVal)
                {
                    quantity = intVal;
                }

                // Update corresponding property
                switch (dataPointName)
                {
                    case "PCB_Slot":
                        PCB_Quantity = quantity;
                        //Logger.Debug("Machine", $"PCB Slot updated from PLC: {quantity}");
                        break;

                    case "PCB_Trays":
                        PCBTrayQuantity = quantity;
                        //Logger.Debug("Machine", $"PCB Tray Quantity updated from PLC: {quantity}");
                        break;

                    case "Blank_Trays":
                        BlankTrayQuantity = quantity;
                       // Logger.Debug("Machine", $"Blank Tray Quantity updated from PLC: {quantity}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error updating tray quantity from PLC data point {dataPointName}", ex);
            }
        }


        /// <summary>
        /// Process a vision trigger from PLC and run the corresponding VmProcedure
        /// </summary>
        private async Task ProcessVisionTriggerAsync(string tagName, string procedureName, ushort triggerValue)
        {
            _inspectionStartTimes[procedureName] = DateTime.Now;
            try
            {
                var eventArgs = new VisionTriggerEventArgs(tagName, procedureName, triggerValue);
                ResetVisionTrigger(tagName);

                // Run the corresponding VmProcedure and raise the event
                switch (procedureName)
                {
                    case "Align":
                        if (Camera_align != null)
                        {
                            await WaitAndRunProcedureAsync(Camera_align, "Align");
                            //Logger.Info("Machine", "Camera_align procedure started from PLC trigger");
                            AlignTriggered?.Invoke(this, eventArgs);
                        }
                        break;

                    case "Inspect1":
                        if (Camera_inspect1 != null)
                        {
                            await WaitAndRunProcedureAsync(Camera_inspect1, "Inspect1");
                            //Logger.Info("Machine", "Camera_inspect1 procedure started from PLC trigger");
                            Inspect1Triggered?.Invoke(this, eventArgs);
                        }
                        break;

                    case "Inspect2":
                        if (Camera_inspect2 != null)
                        {
                            await WaitAndRunProcedureAsync(Camera_inspect2, "Inspect2");
                            //Logger.Info("Machine", "Camera_inspect2 procedure started from PLC trigger");
                            Inspect2Triggered?.Invoke(this, eventArgs);
                        }
                        break;

                    case "Inspect3":
                        if (Camera_inspect3 != null)
                        {
                            await WaitAndRunProcedureAsync(Camera_inspect3, "Inspect3");
                            //Logger.Info("Machine", "Camera_inspect3 procedure started from PLC trigger");
                            Inspect3Triggered?.Invoke(this, eventArgs);
                        }
                        break;

                    case "Inspect4":
                        if (Camera_inspect4 != null)
                        {
                            await WaitAndRunProcedureAsync(Camera_inspect4, "Inspect4");
                            //Logger.Info("Machine", "Camera_inspect4 procedure started from PLC trigger");
                            Inspect4Triggered?.Invoke(this, eventArgs);
                        }
                        break;

                    case "Inspect5":
                        if (Camera_inspect5 != null)
                        {
                            await WaitAndRunProcedureAsync(Camera_inspect5, "Inspect5");
                            //Logger.Info("Machine", "Camera_inspect5 procedure started from PLC trigger");
                            Inspect5Triggered?.Invoke(this, eventArgs);
                        }
                        break;

                    case "Inspect6":
                        if (Camera_inspect6 != null)
                        {
                            await WaitAndRunProcedureAsync(Camera_inspect6, "Inspect6");
                            //Logger.Info("Machine", "Camera_inspect6 procedure started from PLC trigger");
                            Inspect6Triggered?.Invoke(this, eventArgs);
                        }
                        break;

                    default:
                        Logger.Warning("Machine", $"Unknown vision procedure: {procedureName}");
                        break;
                }

                // Reset the trigger in PLC after processing (write 0 back)
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error processing vision trigger {tagName} -> {procedureName}", ex);
                _errorList.AddException("VisionTrigger", $"Failed to process {procedureName} trigger", ex);
            }
        }

        /// <summary>
        /// Waits until the VisionMaster procedure is internally free (IsRunning=false) before running it again.
        /// </summary>
        private async Task WaitAndRunProcedureAsync(VmProcedure procedure, string procedureName)
        {
            int timeoutMs = 3000;
            int elapsedMs = 0;

            // Đảm bảo core engine của VisionMaster đã thực sự dừng trước khi Run lại
            while (procedure.IsRunning)
            {
                if (elapsedMs >= timeoutMs)
                {
                    Logger.Warning("Machine", $"Wait {procedureName} free (IsRunning=false).");
                    break;
                }
                await Task.Delay(50);
                elapsedMs += 50;
            }

            procedure.Run();
        }

        /// <summary>
        /// Reset a vision trigger in PLC by writing 0 to the tag
        /// </summary>
        private void ResetVisionTrigger(string tagName)
        {
            try
            {
                if (PLC != null && PLC.IsConnected)
                {
                    PLC.WriteHoldingRegister(tagName, 0);
                    //Logger.Debug("Machine", $"Reset vision trigger: {tagName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error resetting vision trigger {tagName}", ex);
            }
        }

        /// <summary>
        /// Handle PLC connection status changes
        /// </summary>
        private void OnPLCConnectionStatusChanged(object sender, PLCConnectionEventArgs e)
        {
            try
            {
                if (e.IsConnected)
                {
                    Logger.Info("Machine", "PLC connection established");
                    _errorList.AddError(ErrorType.Information, "Machine", "PLC connected");
                }
                else
                {
                    Logger.Warning("Machine", "PLC connection lost");
                    _errorList.AddError(ErrorType.Warning, "Machine", "PLC connection lost");

                    // If machine is running and PLC disconnects, consider stopping for safety
                    if (IsMachineEnabled)
                    {
                        Logger.Critical("Machine", "Stopping machine due to PLC disconnect");

                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error handling PLC connection status change", ex);
            }
        }

        /// <summary>
        /// Handle PLC errors
        /// </summary>
        private void OnPLCErrorOccurred(object sender, string errorMessage)
        {
            try
            {
                Logger.Error("Machine", $"PLC Error: {errorMessage}");
                _errorList.AddError(ErrorType.Error, "PLC", errorMessage);
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error handling PLC error event", ex);
            }
        }

        #region VmProcedure OnWorkEndStatusCallBack Handlers

        private void OnAlignWorkEndStatusCallBack(object sender, EventArgs e)
        {
            try
            {
                var procedure = sender as VmProcedure;
                bool isOK = ReadProcedureResult(procedure);
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error in OnAlignWorkEndStatusCallBack", ex);
            }
            finally
            {
                lock (_flightLock) _proceduresInFlight.Remove("Align");
            }
        }
      

      
        private async void OnInspect1WorkEndStatusCallBack(object sender, EventArgs e)
        {
            var procedure = sender as VmProcedure;
            var dbService = new AutoVisionDbService();

            try
            {
                bool isOK = ReadProcedureResult(procedure);

                //var pid = GetPidSafe(procedure);
                var pid = ReadBarcodeFromPLC(dataType.station1);
                //Logger.Info("Inspect1: ", $"PID: {pid}");
                //// Enqueue vào WIP ngay lập tức để Inspect2/3 có thể nhận PID
                //_wipQueue1.Enqueue(new WipData { PID = pid });

                // Chạy nền: ghi thời gian vào DB (fire & forget)
                _ = dbService.InsertVisionInputTimeAsync(pid, App.ActualMachineId.GetValueOrDefault());

                // ── 1. Kiểm tra PID hợp lệ (ví dụ: phải chứa "hs") ──────────────────
                if (!pid.ToLower().Contains("hs"))
                {
                    isOK = false;
                    if (IsByPass) isOK = true;
                    Logger.Warning("Inspect1", $"PID không hợp lệ (không chứa 'hs'): {pid}");
                    await dbService.UpdateVisionScanoutAsync(new TbAutoVisionScanout
                    {
                        Pid = pid,
                        ScanoutStatus = "NG",
                        ErrorMessage = "PID không hợp lệ",
                        ScanoutTime = DateTime.Now
                    });
                    _errorList.AddError(ErrorType.Information, "ProductLog", $" {pid} wrong format");
                    //_wipQueue1.TryDequeue(out _);
                    SaveImageForFlow(procedure, pid, "Inspect1", saveDir, false, "Image Source1_ImageData", CurrentEbr, $" wrong format");
                    WriteVisionResult("Inspect1", false);
                    return;
                }

                // ── 2. Chạy song song: IsBlock + IsScanOut + EBR (nếu có) ────────────
                var rfService = new BlockRFService();
                var blockTask = dbService.IsBlock(pid);
                var rfBlockTask = rfService.IsBlockAsync(pid);
                var scanoutTask = dbService.IsScanOut(pid);


                string currentEbr = CurrentEbr; // đọc từ RAM
                Task<(bool isSameEbr, string foundEbr)> ebrTask = null;
                Task<string> getEbrTask = null;

                var sw = System.Diagnostics.Stopwatch.StartNew();
                if (!string.IsNullOrEmpty(currentEbr))
                {
                    // Có EBR sẵn → kiểm tra match
                    ebrTask = dbService.IsSameEbr(pid, currentEbr);
                    await Task.WhenAll(blockTask, rfBlockTask, scanoutTask, ebrTask);
                }
                else
                {
                    // Chưa có EBR → tự tra cứu EBR từ klas
                    getEbrTask = dbService.GetEbrForPid(pid);
                    await Task.WhenAll(blockTask, rfBlockTask, scanoutTask, getEbrTask);
                }
                sw.Stop();
                Logger.Info("Inspect1", $"Time to check precondition for {pid} took {sw.ElapsedMilliseconds}ms");

                // ── 3. Xử lý kết quả ─────────────────────────────────────────────────
                var blockResult = blockTask.Result;
                var rfBlockResult = rfBlockTask.Result;
                bool alreadyScanOut = scanoutTask.Result;

                string errorMsg = string.Empty;
                
                // --- Nhóm 1: Xử lý EBR ---
                if (ebrTask != null)
                {
                    var (isSameEbr, foundEbr) = ebrTask.Result;
                    if (!isSameEbr)
                    {
                        errorMsg += $"{pid} is different with {currentEbr}\n";
                        _errorList.AddError(ErrorType.Information, "ProductLog", $" {pid} wrong EBR");
                    }
                    else if (!string.IsNullOrEmpty(foundEbr) && string.IsNullOrEmpty(CurrentEbr))
                    {
                        CurrentEbr = foundEbr;
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow is HaengSungAOI_WPF.MainWindow mw)
                                mw.SetEbrFromBackend(foundEbr);
                        });
                    }
                }
                else if (getEbrTask != null)
                {
                    string foundEbr = getEbrTask.Result;
                    if (!string.IsNullOrEmpty(foundEbr) && string.IsNullOrEmpty(CurrentEbr))
                    {
                        CurrentEbr = foundEbr;
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow is HaengSungAOI_WPF.MainWindow mw)
                                mw.SetEbrFromBackend(foundEbr);
                        });
                    }
                    else if (string.IsNullOrEmpty(foundEbr))
                    {
                        errorMsg += $"Not found EBR for {pid} in tb_klas\n";
                    }
                }

                // --- Nhóm 2: Kiểm tra Scan-out ---
                if (alreadyScanOut)
                {
                    errorMsg += $"{pid} already scanout in HSMES\n";
                    _errorList.AddError(ErrorType.Information, "ProductLog", $" {pid} already in HSMES");
                }

                // --- Nhóm 3: Kiểm tra Block HSMES ---
                if (blockResult.isBlock)
                {
                    errorMsg += blockResult.reason ?? $"{pid} is blocked in HSMES\n";
                }

                // --- Nhóm 4: Kiểm tra Block RF Service ---
                if (rfBlockResult != null)
                {
                    errorMsg += $"RF Blocked: Band {rfBlockResult.Band}, IP {rfBlockResult.MachineIP}\n";
                }
                // 3c. Kiểm tra EBR


                // ── 4. Nếu có lỗi → đánh NG và dequeue ──────────────────────────────
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    isOK = false;

                    Logger.Warning("Inspect1", $"[NG] {pid}: {errorMsg}");
                    await dbService.UpdateVisionScanoutAsync(new TbAutoVisionScanout
                    {
                        Pid = pid,
                        ScanoutStatus = "NG",
                        ErrorMessage = errorMsg,
                        ScanoutTime = DateTime.Now
                    });
                    //_wipQueue1.TryDequeue(out _);
                }
                if (IsByPass) isOK = true;
                //_ = dbService.UpdateBlock(pid, "Scanout NG hoặc chưa chạy vision -> Thả lại vision", "", "", "Autovision");

                // ── 5. Lưu ảnh & trả kết quả về PLC ─────────────────────────────────
                SaveImageForFlow(procedure, pid, "Inspect1", saveDir, isOK, "Image Source1_ImageData", CurrentEbr, errorMsg);
                WriteVisionResult("Inspect1", isOK);
            }
            catch (Exception ex)
            {
                Logger.Error("Inspect1", ex.ToString());
            }
            finally
            {
                lock (_flightLock) _proceduresInFlight.Remove("Inspect1");
            }
        }
        private void OnInspect2WorkEndStatusCallBack(object sender, EventArgs e)
        {
            var procedure = sender as VmProcedure;
            try
            {
                bool isOK = ReadProcedureResult(procedure);
                if (IsByPass) isOK = true;

                var pid = ReadBarcodeFromPLC(dataType.station1);
                //Logger.Info("Inspect2: ", $"PID: {pid}");

                //string pidToSave = "";

                //if (_wipQueue1.TryPeek(out WipData current)) pidToSave = current.PID;
                //else pidToSave = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                //if (!pidToSave.ToLower().Contains("hs")) { isOK = false; }

                SaveImageForFlow(procedure, pid, "Inspect2", saveDir, isOK, "Image Source2_ImageData", CurrentEbr, "");

                //if (!isOK) _wipQueue1.TryDequeue(out _);

                WriteVisionResult("Inspect2", isOK);
            }
            catch (Exception ex)
            {
                Logger.Error("OnInspect2WorkEndStatusCallBack", ex.Message);
            }
            finally
            {
                lock (_flightLock) _proceduresInFlight.Remove("Inspect2");
            }
        }

        private void OnInspect3WorkEndStatusCallBack(object sender, EventArgs e)
        {
            var procedure = sender as VmProcedure;
            try
            {
                bool isOK = ReadProcedureResult(procedure);
                if (IsByPass) isOK = true;
                //string pidToSave = "";
                //if (_wipQueue1.TryPeek(out WipData current)) pidToSave = current.PID;
                //else pidToSave = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                //if (!pidToSave.ToLower().Contains("hs")) { isOK = false; }


                var pid = ReadBarcodeFromPLC(dataType.station1);
                //Logger.Info("Inspect3: ", $"PID: {pid}");

                SaveImageForFlow(procedure, pid, "Inspect3", saveDir, isOK, "Image Source3_ImageData", CurrentEbr, "");

                //_wipQueue1.TryDequeue(out _);
                //var wip = new WipData() { PID = pidToSave };
                //if (isOK) _wipQueue2.Enqueue(wip);

                WriteVisionResult("Inspect3", isOK);
            }
            catch (Exception ex)
            {
                Logger.Error("OnInspect3WorkEndStatusCallBack", ex.Message);
            }
            finally
            {
                lock (_flightLock) _proceduresInFlight.Remove("Inspect3");
            }
        }
        private void OnInspect4WorkEndStatusCallBack(object sender, EventArgs e)
        {
            var procedure = sender as VmProcedure;
            try
            {
                bool isOK = ReadProcedureResult(procedure);
                if (IsByPass) isOK = true;

                //string pidToSave = "";
                //if (_wipQueue2.TryPeek(out WipData current)) pidToSave = current.PID;
                //else pidToSave = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                //if (!pidToSave.ToLower().Contains("hs")) { isOK = false; }

                var pid = ReadBarcodeFromPLC(dataType.station2);
                //Logger.Info("Inspect4: ", $"PID: {pid}");

                SaveImageForFlow(procedure, pid, "Inspect4", saveDir, isOK, "Image Source4_ImageData", CurrentEbr, "");

                //if (!isOK) _wipQueue2.TryDequeue(out _);

                WriteVisionResult("Inspect4", isOK);
            }
            catch (Exception ex)
            {
                Logger.Error("OnInspect4WorkEndStatusCallBack", ex.Message);
            }
            finally
            {
                lock (_flightLock) _proceduresInFlight.Remove("Inspect4");
            }
        }
        private void OnInspect5WorkEndStatusCallBack(object sender, EventArgs e)
        {
            var procedure = sender as VmProcedure;
            try
            {
                bool isOK = ReadProcedureResult(procedure);
                if (IsByPass) isOK = true;

                //string pidToSave = "";
                //if (_wipQueue2.TryPeek(out WipData current)) pidToSave = current.PID;
                //else pidToSave = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                //if (!pidToSave.ToLower().Contains("hs")) { isOK = false; }

                var pid = ReadBarcodeFromPLC(dataType.station2);
                //Logger.Info("Inspect5: ", $"PID: {pid}");

                SaveImageForFlow(procedure, pid, "Inspect5", saveDir, isOK, "Image Source5_ImageData", CurrentEbr, "");

                //if (!isOK) _wipQueue2.TryDequeue(out _);

                WriteVisionResult("Inspect5", isOK);
            }
            catch (Exception ex)
            {
                Logger.Error("OnInspect5WorkEndStatusCallBack", ex.Message);
            }
            finally
            {
                lock (_flightLock) _proceduresInFlight.Remove("Inspect5");
            }
        }
        private void OnInspect6WorkEndStatusCallBack(object sender, EventArgs e)
        {
            var procedure = sender as VmProcedure;
            try
            {
                bool isOK = ReadProcedureResult(procedure);
                if (IsByPass) isOK = true;

                //// Inspect6 là cuối nhóm Inspect4-5-6 → LUÔN dequeue _wipQueue2
                //// Giống Inspect3 luôn dequeue _wipQueue1 để tránh PID cũ ảnh hưởng PCB tiếp theo
                //string pidToSave = "";
                //if (_wipQueue2.TryDequeue(out WipData current))
                //    pidToSave = current.PID;
                //else
                //    pidToSave = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                //if (!pidToSave.ToLower().Contains("hs")) { isOK = false; }


                //// enqueue để xử lý scanout
                //if (isOK)
                //{
                //    var wip = new WipData() { PID = pidToSave };
                //    if (isOK) _wipQueueScanout.Enqueue(wip);
                //}


                ////double tackTime = 0;
                ////if (_inspectionStartTimes.TryGetValue("Inspect6", out DateTime startTime))
                ////    tackTime = (DateTime.Now - startTime).TotalSeconds;


                var pid = ReadBarcodeFromPLC(dataType.station2);
                //Logger.Info("Inspect6: ", $"PID: {pid}");

                SaveImageForFlow(procedure, pid, "Inspect6", saveDir, isOK, "Image Source6_ImageData", CurrentEbr, "");
                WriteVisionResult("Inspect6", isOK);
            }
            catch (Exception ex)
            {
                Logger.Error("OnInspect6WorkEndStatusCallBack", ex.Message);
            }
            finally
            {
                lock (_flightLock) _proceduresInFlight.Remove("Inspect6");
            }
        }



        public BitmapSource ConvertRawToBitmapSource(IntPtr pData, int width, int height, VMPixelFormat pixelFormat)
        {
            if (pData == IntPtr.Zero) return null;

            System.Windows.Media.PixelFormat wpfFormat;
            int bytesPerPixel;

            // 1. Nhận diện Format chuẩn từ Vision Master
            // Lưu ý: Nếu ảnh bị ngược màu Đỏ/Xanh, hãy đổi Rgb24 <-> Bgr24
            switch (pixelFormat)
            {
                case VMPixelFormat.VM_PIXEL_MONO_08:
                    wpfFormat = PixelFormats.Gray8;
                    bytesPerPixel = 1;
                    break;
                case VMPixelFormat.VM_PIXEL_RGB24_C3:
                    wpfFormat = PixelFormats.Rgb24;
                    bytesPerPixel = 3;
                    break;
                default:
                    wpfFormat = PixelFormats.Gray8;
                    bytesPerPixel = 1;
                    break;
            }

            // 2. Tối ưu Stride: Vision Master thường không có padding (stride = width * bytesPerPixel)
            // Nhưng WPF yêu cầu stride phải khớp với dữ liệu nguồn truyền vào.
            int rawStride = width * bytesPerPixel;

            try
            {
                // 3. Tạo BitmapSource trực tiếp từ con trỏ pData
                // Việc này cực nhanh vì không tốn công tạo mảng byte[] trung gian thủ công
                BitmapSource bmpSource = BitmapSource.Create(
                    width,
                    height,
                    96, 96,
                    wpfFormat,
                    null,
                    pData,
                    rawStride * height,
                    rawStride
                );

                // 4. QUAN TRỌNG NHẤT: Đóng băng dữ liệu (Deep Copy ngầm)
                // Khi gọi Freeze(), WPF sẽ tự động copy toàn bộ dữ liệu từ Unmanaged Memory (pData)
                // vào Managed Memory của nó. Sau lệnh này, pData của VM có bị giải phóng 
                // hay ghi đè thì bmpSource vẫn giữ nguyên dữ liệu ảnh cũ.
                if (bmpSource.CanFreeze)
                {
                    bmpSource.Freeze();
                }

                return bmpSource;
            }
            catch (Exception ex)
            {
                Logger.Error("Vision", $"Convert Image Error: {ex.Message}");
                return null;
            }
        }

        private void SaveImageForFlow(VmProcedure procedure, string pid, string flowName, string rootSaveDir, bool isOK, string imageModuleName = "Image Source1_ImageData0", string ebr = "", string note = "")
        {
            if (string.IsNullOrWhiteSpace(pid) || procedure == null) return;

            int width = 0, height = 0, stride = 0;
            System.Windows.Media.PixelFormat format = PixelFormats.Gray8;
            byte[] imageBytes = null;

            try
            {
                // PHẦN 1: COPY DỮ LIỆU TỐC ĐỘ CAO (Chạy đồng bộ để đảm bảo an toàn vùng nhớ)
                var imgDataV2 = procedure.ModuResult.GetOutputImageV2(imageModuleName);
                if (imgDataV2 == null || imgDataV2.ImageData == IntPtr.Zero) return;

                width = imgDataV2.Width;
                height = imgDataV2.Height;
                int bytesPerPixel = (imgDataV2.Pixelformat == VMPixelFormat.VM_PIXEL_RGB24_C3) ? 3 : 1;
                format = bytesPerPixel == 3 ? PixelFormats.Rgb24 : PixelFormats.Gray8;
                stride = width * bytesPerPixel;

                // Copy mảng byte cực nhanh không qua thư viện UI WPF
                imageBytes = new byte[stride * height];
                System.Runtime.InteropServices.Marshal.Copy(imgDataV2.ImageData, imageBytes, 0, imageBytes.Length);
            }
            catch (Exception ex)
            {
                Logger.Error("Vision", $"Copy Image Error: {ex.Message}");
                return; // Thoát sớm nếu không copy được, tránh Task.Run bị lỗi
            }

            // PHẦN 2: LƯU FILE & Result
            Task.Run(async () =>
            {
                try
                {
                    // Tạo đường dẫn thư mục theo ngày
                    string dateFolder = DateTime.Now.ToString("yyyyMMdd");
                    string flowFolder = Path.Combine(rootSaveDir, dateFolder, flowName);

                    if (!Directory.Exists(flowFolder))
                        Directory.CreateDirectory(flowFolder);
                    string visionResult = isOK ? "OK" : "NG";
                    // Tạo tên file
                    string fileName = $"{pid}_{flowName}_{DateTime.Now:HHmmssfff}_{visionResult}.jpg";
                    string fullPath = Path.Combine(flowFolder, fileName);

                    // Tạo BitmapSource từ mảng byte (Thực hiện chạy ngầm)
                    BitmapSource uiImage = BitmapSource.Create(
                        width, height, 96, 96, format, null, imageBytes, stride
                    );

                    using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        int jpegQuality = 85;
                        string qualitySetting = ConfigurationManager.AppSettings["JpegQualityLevel"];
                        if (!string.IsNullOrEmpty(qualitySetting) && int.TryParse(qualitySetting, out int parsedQuality) && parsedQuality >= 1 && parsedQuality <= 100)
                            jpegQuality = parsedQuality;

                        JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = jpegQuality };
                        encoder.Frames.Add(BitmapFrame.Create(uiImage));
                        encoder.Save(fileStream);
                    }

                    // Đo thời gian hoàn thành (tack_time)
                    double tackTime = 0;
                    if (_inspectionStartTimes.TryGetValue(flowName, out DateTime startTime))
                    {
                        tackTime = (DateTime.Now - startTime).TotalSeconds;
                    }

                    ///////////////////////////////
                    // Lưu dữ liệu vào Database
                    ///////////////////////////////
                    var dbService = new AutoVisionDbService();

                    var resultData = new TbAutoVisionResult
                    {
                        MachineId = App.ActualMachineId.GetValueOrDefault(),
                        Pid = pid,
                        WorkOrder = "",
                        Station = flowName,
                        Result = visionResult,
                        Ebr = ebr,
                        ImagePath = fullPath,
                        InspectionTime = DateTime.Now,
                        TackTime = Math.Round(tackTime, 3),
                        Note = note
                    };

                    bool isResultInserted = await dbService.InsertVisionResultAsync(resultData);
                    if (!isResultInserted)
                    {
                        Logger.Warning("Vision", $"Failed to save Vision Result to DB for PID: {pid}");
                    }
                    else
                    {
                        // Thêm thành công => báo cho giao diện (MainWindow) cập nhật History DataGrid
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                            {
                                mainWindow.AddVisionResultToHistory(resultData);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning("Vision", $"IO/DB Save Error: {ex.Message}");
                }
            });
        }


        #endregion

        #region Product Logging Methods

        /// <summary>
        /// Handle product logging trigger from PLC (MW481 for OK, MW482 for NG)
        /// </summary>
       
        private async Task HandleProductLogTriggerAsync(string dataPointName, object newValue)
        {
            try
            {
                if (dataPointName == "Product_OK_Trigger")
                {
                    await ProcessOKProductLogAsync();
                }
                else if (dataPointName == "Product_NG_Trigger")
                {
                    await ProcessNGProductLogAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error handling product log trigger {dataPointName}", ex);
                _errorList.AddException("ProductLog", $"Failed to process product log trigger", ex);
            }
        }
        /// <summary>
        /// Process OK product logging when MW481 is triggered
        /// Reads barcode from MW460-MW469, slot from MW448, performs scan out, and sets result registers
        /// </summary>


        private readonly SemaphoreSlim _productLogLock_v2 = new SemaphoreSlim(1, 1);
        private async Task ProcessOKProductLogAsync()
        {
            await _productLogLock_v2.WaitAsync(); // 🔒 lock async
            try
            {

                //Logger.Info("Machine", "Processing OK product log trigger (MW481)");

                string barcode = ReadBarcodeFromPLC(dataType.FinalOk);
                int slot = ReadSlotNumberFromPLC();

                Logger.Info("Machine", $" Process OK Product Barcode: {barcode} - Slot: {slot}");


                ScanOutResult result = ScanOutResult.OK;

                if (EnableScanOut)
                {
                    //result = await performScanOut_v2(barcode, slot);
                    try
                    {
                        string dataToSend = $"{barcode}|{slot}";
                        if (ScanoutSerialPort != null && ScanoutSerialPort.IsOpen)
                        {
                            ScanoutSerialPort.WriteLine(dataToSend + "\r");
                        }
                        else
                        {
                            Logger.Warning("ScanOut", "Serial port is not open or initialized.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ScanOut", "Error sending scan out trigger", ex);
                        _errorList.AddException("ScanOut", "Scan out trigger failed", ex);
                    }
                }


                WriteScanOutResultToPLC(result);


                //_errorList.AddError(ErrorType.Information, "ProductLog",
                //    $"OK Product logged: {barcode}, Slot: {slot}, Send Scanout: {result}");

                ClearProductLogTrigger("Product_OK_Trigger");

                Logger.Info("Machine", $" Completed OK Product Barcode: {barcode} - Slot: {slot}");

            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error processing OK product log", ex);
                _errorList.AddException("ProductLog", "Failed to process OK product", ex);

                ClearProductLogTrigger("Product_OK_Trigger");
            }
            finally
            {
                _productLogLock_v2.Release(); // 🔓 unlock
            }
        }

        /// <summary>
        /// Process NG product logging when MW482 is triggered
        /// Reads barcode from MW470-MW479 and logs the NG product
        /// </summary>
        private async Task ProcessNGProductLogAsync()
        {
            await _productLogLock_v2.WaitAsync(); // 🔒 lock async
            try
            {
                // Clear the trigger (write 0 to MW482)


                //Logger.Info("Machine", "Processing NG product log trigger (MW482)");

                // Read barcode from MW470-MW479
                string barcode = ReadBarcodeFromPLC(dataType.FinalNg);
                Logger.Info("Machine", $"NG Product Barcode: {barcode}");

                // Log the NG product
                //Logger.Info("ProductLog", $"NG Product - Barcode: {barcode}");



                //_errorList.AddError(ErrorType.Information, "ProductLog", $"Vision NG: {barcode}");

                ClearProductLogTrigger("Product_NG_Trigger");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error processing NG product log", ex);
                _errorList.AddException("ProductLog", "Failed to process NG product", ex);

                // Clear the trigger even on error
                ClearProductLogTrigger("Product_NG_Trigger");
            }
            finally
            {
                _productLogLock_v2.Release(); // 🔓 unlock
            }
        }

        /// <summary>
        /// Read barcode string from PLC registers - performs a DIRECT read from PLC
        /// to ensure we get the latest data, not cached polling values
        /// </summary>
        /// <param name="isOK">True for OK barcode (MW460-MW469), False for NG barcode (MW470-MW479)</param>
        /// <returns>Barcode string</returns>
        private string ReadBarcodeFromPLC(bool isOK)
        {
            try
            {
                if (PLC == null || !PLC.IsConnected)
                {
                    Logger.Warning("Machine", "Cannot read barcode: PLC not connected");
                    return string.Empty;
                }

                // Determine start address based on OK/NG
                // OK barcode: MW460-MW469, NG barcode: MW470-MW479
                ushort startAddress = isOK ? (ushort)460 : (ushort)470;
                ushort registerCount = 10; // 10 registers for barcode

                // Perform DIRECT read from PLC to get latest values (not cached)
                // This ensures we read the barcode AFTER it's been written by PLC
                ushort[] registers = PLC.ReadHoldingRegistersDirect(startAddress, registerCount);

                if (registers == null || registers.Length == 0)
                {
                    Logger.Warning("Machine", $"Direct read of barcode registers returned empty (isOK={isOK})");
                    return string.Empty;
                }

                var barcodeBuilder = new System.Text.StringBuilder();

                // Each register holds 2 ASCII characters
                for (int i = 0; i < registers.Length; i++)
                {
                    ushort regValue = registers[i];

                    // Extract 2 ASCII characters from the register
                    // High byte is first character, low byte is second character
                    byte highByte = (byte)((regValue >> 8) & 0xFF);
                    byte lowByte = (byte)(regValue & 0xFF);

                    // Add characters if they are valid ASCII printable characters
                    if (highByte >= 0x20 && highByte <= 0x7E)
                    {
                        barcodeBuilder.Append((char)highByte);
                    }
                    if (lowByte >= 0x20 && lowByte <= 0x7E)
                    {
                        barcodeBuilder.Append((char)lowByte);
                    }
                }

                string barcode = barcodeBuilder.ToString().Trim();
                Logger.Debug("Machine", $"Direct read barcode (isOK={isOK}): '{barcode}' from registers {startAddress}-{startAddress + registerCount - 1}");

                return barcode;
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error reading barcode from PLC (isOK={isOK})", ex);
                return string.Empty;
            }
        }
        enum dataType
        {
            station1,
            transfer,
            station2,
            FinalOk,
            FinalNg
        }
        private string ReadBarcodeFromPLC(dataType dataType)
        {
            try
            {
                if (PLC == null || !PLC.IsConnected)
                {
                    Logger.Warning("Machine", "Cannot read barcode: PLC not connected");
                    return string.Empty;
                }

                ushort startAddress = (ushort)450;
                switch (dataType)
                {
                    case dataType.station1: startAddress = (ushort)450; break;
                    case dataType.transfer: startAddress = (ushort)750; break;
                    case dataType.station2: startAddress = (ushort)760; break;
                    case dataType.FinalOk: startAddress = (ushort)460; break;
                    case dataType.FinalNg: startAddress = (ushort)470; break;
                    default:
                        break;
                }
                ushort registerCount = 10; // 10 registers for barcode

                // Perform DIRECT read from PLC to get latest values (not cached)
                // This ensures we read the barcode AFTER it's been written by PLC
                ushort[] registers = PLC.ReadHoldingRegistersDirect(startAddress, registerCount);

                if (registers == null || registers.Length == 0)
                {
                    Logger.Warning("Machine", $"Direct read of barcode registers {dataType.ToString()} returned empty");
                    return string.Empty;
                }

                var barcodeBuilder = new System.Text.StringBuilder();

                // Each register holds 2 ASCII characters
                for (int i = 0; i < registers.Length; i++)
                {
                    ushort regValue = registers[i];

                    // Extract 2 ASCII characters from the register
                    // High byte is first character, low byte is second character
                    byte highByte = (byte)((regValue >> 8) & 0xFF);
                    byte lowByte = (byte)(regValue & 0xFF);

                    // Add characters if they are valid ASCII printable characters
                    if (highByte >= 0x20 && highByte <= 0x7E)
                    {
                        barcodeBuilder.Append((char)highByte);
                    }
                    if (lowByte >= 0x20 && lowByte <= 0x7E)
                    {
                        barcodeBuilder.Append((char)lowByte);
                    }
                }

                string barcode = barcodeBuilder.ToString().Trim();
                //Logger.Debug("Machine", $"Direct read barcode: '{barcode}' from registers {startAddress}-{startAddress + registerCount - 1}");

                return barcode;
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error reading barcode from PLC {dataType.ToString()}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Read slot number from PLC register MW448 - performs a DIRECT read from PLC
        /// </summary>
        /// <returns>Slot number</returns>
        private int ReadSlotNumberFromPLC()
        {
            try
            {
                if (PLC == null || !PLC.IsConnected)
                {
                    Logger.Warning("Machine", "Cannot read slot number: PLC not connected");
                    return 0;
                }

                // Perform DIRECT read from PLC to get latest value
                ushort[] registers = PLC.ReadHoldingRegistersDirect(448, 1);

                if (registers != null && registers.Length > 0)
                {
                    Logger.Debug("Machine", $"Direct read slot number: {registers[0]}");
                    return registers[0];
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error reading slot number from PLC", ex);
                return 0;
            }
        }

        /// <summary>
        /// Write scan out result to PLC registers (MW432, MW433, MW434)
        /// </summary>
        /// <param name="result">Scan out result</param>
        private void WriteScanOutResultToPLC(ScanOutResult result)
        {
            try
            {
                if (PLC == null || !PLC.IsConnected)
                {
                    Logger.Warning("Machine", "Cannot write scan out result: PLC not connected");
                    return;
                }

                // Clear all result registers first
                PLC.WriteHoldingRegister("ScanOut_OK", 0);
                PLC.WriteHoldingRegister("ScanOut_NG", 0);
                PLC.WriteHoldingRegister("ScanOut_NGQuantity", 0);

                Thread.Sleep(100);

                // Set the appropriate result register
                switch (result)
                {
                    case ScanOutResult.OK:
                        PLC.WriteHoldingRegister("ScanOut_OK", 1);
                        
                        break;

                    case ScanOutResult.NG:
                        PLC.WriteHoldingRegister("ScanOut_NG", 1);
                        //Logger.Info("Machine", "Wrote ScanOut result: NG (MW433 = 1)");
                        break;

                    case ScanOutResult.NGQuantity:
                        PLC.WriteHoldingRegister("ScanOut_NGQuantity", 1);
                       // Logger.Info("Machine", "Wrote ScanOut result: NGQuantity (MW434 = 1)");
                        break;
                }
                Logger.Info("Machine", $"WriteScanOutResultToPLC {result.ToString()} (1)");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error writing scan out result to PLC: {result}", ex);
            }
        }

        /// <summary>
        /// Clear product log trigger by writing 0 to the register
        /// </summary>
        /// <param name="triggerName">Name of the trigger register to clear</param>
        private void ClearProductLogTrigger(string triggerName)
        {
            try
            {
                if (PLC != null && PLC.IsConnected)
                {
                    PLC.WriteHoldingRegister(triggerName, 0);
                    Logger.Debug("Machine", $"Cleared product log trigger: {triggerName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error clearing product log trigger {triggerName}", ex);
            }
        }

        #endregion
    }

    public class BlockRFService
    {
        private static readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public async Task<RFInfo> IsBlockAsync(string pid)
        {
            if (string.IsNullOrWhiteSpace(pid))
                return null;

            try
            {
                string url = $"http://10.221.191.183:8081/api/TraceBackHistory/getErorrLogByPid/{pid}";
                Logger.Info("BlockRFService", $"Checking RF Block for {pid} via {url}");

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warning("BlockRFService", $"API returned error code: {response.StatusCode} for {pid}");
                    return null;
                }

                string jsonString = await response.Content.ReadAsStringAsync();
                Logger.Info("BlockRFService", $"Raw Response for {pid}: {jsonString}");

                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    Logger.Debug("BlockRFService", $"Empty response for {pid}");
                    return null;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var rfInfos = JsonSerializer.Deserialize<List<RFInfo>>(jsonString, options);

                if (rfInfos != null && rfInfos.Count > 0)
                {
                    var firstMatch = rfInfos[0];
                    Logger.Info("BlockRFService", $"Found RF Error for {pid}: Band={firstMatch.Band}, IP={firstMatch.MachineIP}, Cleared={firstMatch.Cleared}");
                    return firstMatch;
                }

                Logger.Info("BlockRFService", $"No RF Error record found for {pid} (Passed)");
                return null;
            }
            catch (Exception ex)
            {
                // Thêm log để dễ troubleshoot trong môi trường máy chạy thực tế
                Logger.Error("BlockRFService", $"Lỗi khi kiểm tra Block RF cho PID {pid}: {ex.Message}");
                return null;
            }
        }

        public class RFInfo
        {
            public int Id { get; set; }
            public string Pid { get; set; }
            public string Model { get; set; }
            public string Supplier { get; set; }

            [JsonPropertyName("valuE_NG")]
            public string ValueNgRaw { get; set; }
            
            public double ValueNg 
            {
                get 
                {
                    if (double.TryParse(ValueNgRaw, out double val)) return val;
                    return 0;
                }
            }

            public string Freq { get; set; }
            public string Band { get; set; }
            public string Subband { get; set; }

            [JsonPropertyName("gaiN_STATE")]
            public string GainState { get; set; }

            public string Ucl { get; set; }
            public string Lcl { get; set; }

            [JsonPropertyName("clienT_CHECKED")]
            [JsonConverter(typeof(IntToBoolConverter))]
            public bool ClientChecked { get; set; }

            [JsonPropertyName("weB_CHECKED")]
            [JsonConverter(typeof(IntToBoolConverter))]
            public bool WebChecked { get; set; }

            [JsonPropertyName("anT_NUMBER")]
            public string AntNumber { get; set; }

            public string Market { get; set; }
            public string Station { get; set; }

            public string Signpath { get; set; }

            [JsonPropertyName("creatE_USER")]
            public string CreateUser { get; set; }
            
            public string MachineIP 
            {
                get 
                {
                    if (string.IsNullOrEmpty(CreateUser)) return "Unknown";
                    string[] parts = CreateUser.Split('-');
                    return parts.Length > 0 ? parts[0] : CreateUser;
                }
            }

            [JsonPropertyName("creatE_TIME")]
            [JsonConverter(typeof(CustomDateTimeConverter))]
            public DateTime CreateTime { get; set; }

            [JsonPropertyName("cleared")]
            [JsonConverter(typeof(IntToBoolConverter))]
            public bool Cleared { get; set; }
        }

        /// <summary>
        /// Converter cho DateTime định dạng dd-MM-yyyy HH:mm:ss phù hợp với System.Text.Json (.NET 4.8.1)
        /// </summary>
        public class CustomDateTimeConverter : JsonConverter<DateTime>
        {
            private const string DateFormat = "dd-MM-yyyy HH:mm:ss";

            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string dateStr = reader.GetString();
                if (string.IsNullOrEmpty(dateStr)) return DateTime.MinValue;

                if (DateTime.TryParseExact(dateStr, DateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                {
                    return dt;
                }

                return DateTime.MinValue;
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString(DateFormat));
            }
        }

        /// <summary>
        /// Converter chuyển đổi giá trị số (1/0) hoặc chuỗi ("1"/"0") sang Boolean
        /// </summary>
        public class IntToBoolConverter : JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    return reader.GetString() == "1";
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    return reader.GetInt32() == 1;
                }
                return false;
            }

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            {
                // Ghi lại dưới dạng số nếu cần, ở đây giữ nguyên logic ghi chuỗi "1"/"0"
                writer.WriteStringValue(value ? "1" : "0");
            }
        }
    }
}
