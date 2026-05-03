using FrontendUI.WPF;
using HaengSungAOI_WPF.Machine.PLC;
using HaengSungAOI_WPF.Machine.PLC.PLC;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Utils;
using System;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using VM.Core;
using VMControls.WPF.Release;

namespace HaengSungAOI_WPF.Machine
{
    public enum MachineMode
    {
        Manual,
        Auto,
        Maintenance
    }

    public enum ScanOutResult
    {
        OK,
        NGQuantity,
        NG
    }

    /// <summary>
    /// Machine class - Core fields, properties, constructor, and initialization
    /// All hardware I/O now handled through PLCController via Modbus TCP
    /// This is a partial class - see other Machine.*.cs files for additional functionality:
    /// - Machine.PLC.cs - PLC communication and vision triggers
    /// - Machine.Safety.cs - Safety operations
    /// - Machine.Operations.cs - Machine operations (Start/Stop)
    /// - Machine.Vision.cs - Vision solution and camera procedures
    /// - Machine.Init.cs - Initialization methods
    /// - Machine.Events.cs - Event handlers and logic setup
    /// </summary>
    public partial class Machine
    {
        #region Fields

        // Vision Procedures
        public VmProcedure Camera_align;
        public VmProcedure Camera_inspect1;
        public VmProcedure Camera_inspect2;
        public VmProcedure Camera_inspect3;
        public VmProcedure Camera_inspect4;
        public VmProcedure Camera_inspect5;
        public VmProcedure Camera_inspect6;

        int triggerCount = 0;
        public MachineMode Mode { get; set; }

        // Machine variables
        public float PCBAlign_X;
        public float PCBAlign_Y;
        public float PCBAlign_Angle;
        public int PCB_Quantity = 0;
        public int PCBTrayQuantity = 0;
        public int BlankTrayQuantity = 0;

        private SerialPort ScanoutSerialPort;

        // PCB Model
        public PCBModel _PCBModel;
        public PCBModel CurrentModel => _PCBModel;

        // Vision UI Controls
        public VmFrontendControl frontendControl;
        public FrontendUI.WPF.Controls.ImageControl imageAlign;
        public FrontendUI.WPF.Controls.ImageControl imageInspect1;
        public FrontendUI.WPF.Controls.ImageControl imageInspect2;
        public FrontendUI.WPF.Controls.ImageControl imageInspect3;
        public FrontendUI.WPF.Controls.ImageControl imageInspect4;
        public FrontendUI.WPF.Controls.ImageControl imageInspect5;
        public FrontendUI.WPF.Controls.ImageControl imageInspect6;

        // Vision Solution Manager
        private VisionSolutionManager _visionManager;
        private string _currentVisionSolutionPath = "";

        public MachineSequenceState SequenceState { get; set; } = MachineSequenceState.Idle;

        // Machine state
        private bool _isMachineEnabled = false;
        private bool _isInitialized = false;
        public bool overideInspection = false;
        public bool EnableScanOut { get; set; } = true; // Enable/disable scan-out feature
        public bool IsByPass { get; set; } = false; // By Pass mode

        public bool IsMachineEnabled
        {
            get => _isMachineEnabled;
            private set
            {
                _isMachineEnabled = value;
                //Logger.Info("Machine", $"Machine enabled state changed to: {value}");
                OnMachineEnabledStateChanged?.Invoke(value);
            }
        }

        public bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                _isInitialized = value;
                //Logger.Info("Machine", $"Machine initialization state changed to: {value}");
            }
        }

        public bool MachineHomed { get; set; } = false;

        // Events
        public event Action<bool> OnMachineEnabledStateChanged;

        // Error list
        private readonly MachineErrorList _errorList;

        // PLC - centralized hardware communication
        public PLCController PLC { get; private set; }

        // Inspection History Manager for database logging
        private InspectionHistoryManager _historyManager;

        #endregion

        #region Constructor

        public Machine()
        {
            _errorList = MachineErrorList.Instance;
            //Logger.Info("Machine", "Machine constructor called - PLC-based communication");

            _ScanoutResonposeProcessor = new ActionBlock<ScanOutResponseData>(
                async data => await ProcessScanOutResponseAsync(data),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true }
            );
            // Single vision processor - VisionMaster does not support concurrent Run() calls
            // PLC also triggers procedures sequentially, so 1 queue is sufficient
            _visionProcessor = new ActionBlock<PLCWorkItem>(async item =>
            {
                try
                {
                    await ProcessVisionTriggerAsync(item.TagName, item.ProcedureName, Convert.ToUInt16(item.NewValue));
                }
                catch (Exception ex)
                {
                    Logger.Error("Machine_VisionProcessor", $"Fatal error handled internally to prevent block faulting for tag {item.TagName}", ex);
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true });


            // ProductLog và TrayUpdate vào cùng 1 block tuần tự để đảm bảo
            // HandleProductLogTrigger() luôn chạy xong trước UpdateTrayQuantitiesFromPLC()
            // Alarm xử lý ngay lập tức không cần đợi block này
            _ScanOutAndPackingProcessor = new ActionBlock<PLCWorkItem>(async item =>
            {
                try
                {
                    switch (item.Type)
                    {
                        case PLCWorkType.ProductLog:
                            //HandleProductLogTrigger(item.TagName, item.NewValue);
                            await HandleProductLogTriggerAsync(item.TagName, item.NewValue);
                            break;
                        case PLCWorkType.TrayUpdate:
                            UpdateTrayQuantitiesFromPLC(item.TagName, item.NewValue);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Machine_ScanOutProcessor", $"Fatal error handled internally to prevent block faulting for type {item.Type}", ex);
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true });

            _MachineAlarmProcessor = new ActionBlock<PLCWorkItem>(item =>
            {
                try
                {
                    switch (item.Type)
                    {
                        case PLCWorkType.Alarm:
                            HandleAlarmChanged(item.TagName, item.NewValue, item.Address);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Machine_AlarmProcessor", $"Fatal error handled internally to prevent block faulting for tag {item.TagName}", ex);
                }
                return Task.CompletedTask;
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true });
        }

        #endregion

        #region Initialize

        public void Initialize()
        {
            try
            {

                // Initialize PLC controller first - all hardware communication goes through this
                InitializePLCController();

                // Initialize scan out
                InitializeScanOut();

                // Initialize vision system
                _visionManager = new VisionSolutionManager();
                LoadActiveModelFromDatabase();
                LoadVisionSolution(_PCBModel);
                SubscribeToVisionProcedureCallbacks();



                // Read initial tray quantities from PLC
                ReadTrayQuantitiesFromPLC();

                // Initialize inspection history manager
                _historyManager = new InspectionHistoryManager();
                //Logger.Info("Machine", "Inspection history manager initialized");

                IsInitialized = true;
                //Logger.Info("Machine", "Machine initialization completed successfully (PLC-based)");
                Logger.Info("Machine", "End initialization");

                _errorList.AddError(ErrorType.Information, "Machine", "Machine initialized successfully");
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                Logger.Error("Machine", "Failed to initialize machine", ex);
                _errorList.AddException("Machine", "Machine initialization failed", ex);
                throw;
            }
        }
        private void LoadActiveModelFromDatabase()
        {
            try
            {
                //Logger.Info("Machine", "Loading active PCB model from database");

                var modelDatabase = new ModelDatabaseManager();
                _PCBModel = modelDatabase.GetActiveModel();

                if (_PCBModel == null)
                {
                    Logger.Warning("Machine", "No active model found in database, creating default model");
                    _errorList.AddError(ErrorType.Warning, "Machine", "No active model found, creating default");

                    // Create default model as fallback
                    _PCBModel = new PCBModel
                    {
                        Name = "Default Model",
                        Description = "Default configuration",
                        IsActive = true,

                        // Set default values - these will be the actual defaults
                        PCBInfeedPick_Speed = 100000.0f,  // Now you can set your desired default speed!
                        PCBInfeedPlace_Speed = 100000.0f,
                        PCBInfeedPick_Acceleration = 0.1f,
                        PCBInfeedPlace_Acceleration = 0.1f,
                        PCBInfeedPick_Deceleration = 0.1f,
                        PCBInfeedPlace_Deceleration = 0.1f,

                        // Default positions
                        PCBInfeed_IdleX = 0.0f,
                        PCBInfeed_IdleY = 0.0f,
                        PCBInfeed_IdleZ = 50.0f,
                        PCBInfeed_IdleR = 0.0f,

                        PCBInfeed_PickupX = 100.0f,
                        PCBInfeed_PickupY = 100.0f,
                        PCBInfeed_PickupZ = 10.0f,
                        PCBInfeed_PickupR = 0.0f,

                        PCBInfeed_PreparePlaceX = 200.0f,
                        PCBInfeed_PreparePlaceY = 200.0f,
                        PCBInfeed_PreparePlaceZ = 50.0f,
                        PCBInfeed_PreparePlaceR = 0.0f,

                        PCBInfeed_PlaceX = 200.0f,
                        PCBInfeed_PlaceY = 200.0f,
                        PCBInfeed_PlaceZ = 10.0f,
                        PCBInfeed_PlaceR = 0.0f,

                        // Transfer Robot defaults
                        PCBTransfer_IdleX = 0.0f,
                        PCBTransfer_IdleZ = 50.0f,
                        PCBTransfer_PreparePickupX = 120.0f,
                        PCBTransfer_PreparePickupZ = 30.0f,
                        PCBTransfer_PickupX = 150.0f,
                        PCBTransfer_PickupZ = 10.0f,
                        PCBTransfer_PreparePlaceX = 220.0f,
                        PCBTransfer_PreparePlaceZ = 30.0f,
                        PCBTransfer_PlaceX = 250.0f,
                        PCBTransfer_PlaceZ = 10.0f,
                        PCBTransfer_Speed = 100000.0f,
                        PCBTransfer_Acceleration = 0.1f,
                        PCBTransfer_Deceleration = 0.1f,

                        // Outfeed Robot defaults
                        PCBOutfeed_IdleX = 0.0f,
                        PCBOutfeed_IdleY = 0.0f,
                        PCBOutfeed_IdleZ = 50.0f,

                        PCBOutfeed_PickupX = 200.0f,
                        PCBOutfeed_PickupY = 200.0f,
                        PCBOutfeed_PickupZ = 10.0f,

                        PCBOutfeed_PlaceNGX = 400.0f,
                        PCBOutfeed_PlaceNGY = 400.0f,
                        PCBOutfeed_PlaceNGZ = 10.0f,

                        PCBOutfeed_PlaceX = 300.0f,
                        PCBOutfeed_PlaceY = 300.0f,
                        PCBOutfeed_PlaceZ = 10.0f,

                        // Tray pickup/place positions
                        PCBOutfeed_PickupTrayX = 350.0f,
                        PCBOutfeed_PickupTrayY = 350.0f,
                        PCBOutfeed_PickupTrayZ = 10.0f,
                        PCBOutfeed_PlaceTrayX = 360.0f,
                        PCBOutfeed_PlaceTrayY = 360.0f,
                        PCBOutfeed_PlaceTrayZ = 10.0f,

                        PCBOutfeed_Speed = 100000.0f,
                        PCBOutfeed_Acceleration = 0.1f,
                        PCBOutfeed_Deceleration = 0.1f
                    };

                    // Save the default model to database
                    modelDatabase.SaveModel(_PCBModel);
                    modelDatabase.SetActiveModel(_PCBModel.Id);
                }

                Logger.Info("Machine", $"Loaded PCB model from database: {_PCBModel.Name} (Speed: {_PCBModel.PCBInfeedPick_Speed})");
                _errorList.AddError(ErrorType.Information, "Machine", $"Loaded model: {_PCBModel.Name}");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error loading active model from database", ex);
                _errorList.AddException("Machine", "Failed to load PCB model", ex);

                // Create minimal fallback model
                _PCBModel = new PCBModel
                {
                    Name = "Emergency Fallback",
                    PCBInfeedPick_Speed = 100000.0f,  // Your desired speed
                    PCBOutfeed_Speed = 100000.0f,
                    PCBTransfer_Speed = 100000.0f
                };
            }
        }

        private void InitializeScanOut()
        {
            try
            {
                //list all serial ports
                string[] ports = SerialPort.GetPortNames();
                if (ports.Length == 0)
                {
                    Logger.Warning("Machine", "No serial ports found for ScanOut device");
                    _errorList.AddError(ErrorType.Warning, "Machine", "No serial ports found for ScanOut device");
                    return;
                }
                ScanoutSerialPort = new SerialPort("COM7", 115200, Parity.None, 8, StopBits.One);
                ScanoutSerialPort.DataReceived += ScanoutSerialPort_DataReceived;
                ScanoutSerialPort.Open();
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Failed to initialize ScanOut serial port", ex);
                _errorList.AddException("Machine", "ScanOut initialization failed", ex);
            }

        }
        #endregion

        #region Dispose

        public void Dispose()
        {
            try
            {
                //Logger.Info("Machine", "Disposing machine resources");

                if (PLC != null)
                {
                    PLC.DataChanged -= OnPLCDataChanged;
                    PLC.ConnectionStatusChanged -= OnPLCConnectionStatusChanged;
                    PLC.ErrorOccurred -= OnPLCErrorOccurred;
                    PLC.Stop();
                    PLC.Disconnect();
                    PLC.Dispose();
                    PLC = null;
                }

                Logger.Info("Machine", "Machine disposal completed");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error during machine disposal", ex);
            }
        }

        #endregion

        #region Stub Methods (To be implemented with PLC-based logic)


        public Task<ScanOutResult> performScanOut_v2(string PID, int slot)
        {
            try
            {
                // Đẩy trước một bản ghi NG (giả định scanner không phản hồi) vào queue.
                // ActionBlock xử lý tuần tự nên bản ghi này sẽ được Insert vào DB trước.
                // Nếu scanner trả về kết quả thật (OK hoặc NG), nó sẽ được xử lý NGAY SAU ĐÓ và tiến hành chạy lệnh UPDATE trong DB.
                // Cách này hoàn toàn "fire and forget", không cần thiết lập block timeout đợi, không gây treo hay lọt thông tin.
                _ScanoutResonposeProcessor.Post(new ScanOutResponseData
                {
                    RawResponse = $"NG|{PID}|||Tạm block chờ kq scanout thật"
                });

                string dataToSend = $"{PID}|{slot}";
                if (ScanoutSerialPort != null && ScanoutSerialPort.IsOpen)
                {
                    ScanoutSerialPort.WriteLine(dataToSend + "\r");
                }
                //else
                //{
                //    Logger.Warning("ScanOut", "Serial port is not open or initialized.");
                //    _ScanoutResonposeProcessor.Post(new ScanOutResponseData 
                //    { 
                //        RawResponse = $"NG|{PID}|||Port is not open" 
                //    });
                //}

                // Luôn báo OK với PLC để hệ thống chạy tiếp ngay (nghiệp vụ ngầm sẽ xử lý sau)
                return Task.FromResult(ScanOutResult.OK);
            }
            catch (Exception ex)
            {
                Logger.Error("ScanOut", "Error sending scan out trigger", ex);
                _errorList.AddException("ScanOut", "Scan out trigger failed", ex);
                return Task.FromResult(ScanOutResult.NG);
            }
        }

        // Biến toàn cục để theo dõi Magazine hiện tại đang quét
        public static string CurrentMagazineNo { get; set; } = null;

        // Biến toàn cục để lưu trữ EBR hiện tại, tránh đọc từ giao diện UI mỗi lần quét
        public static string CurrentEbr { get; set; } = "";

        // Dữ liệu luân chuyển từ SerialPort sang _scanOutActionBlock
        private class ScanOutResponseData
        {
            public string RawResponse { get; set; }
        }

        // ActionBlock đảm bảo FIFO theo danh sách, chạy trên nền bằng ThreadPool ngầm 
        // MaxDegreeOfParallelism = 1 đảm bảo không xảy ra đụng độ (Race Condition) khi cập nhật Database
        private readonly ActionBlock<ScanOutResponseData> _ScanoutResonposeProcessor;

        // --- Bắt đầu thực hiện nghiệp vụ ngầm (Background Task) ---
        private void ScanoutSerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Dùng ReadLine để chặn đọc tới khi gặp ký tự ngắt dòng từ Barcode reader
                string response = ScanoutSerialPort.ReadLine().Trim();
                if (string.IsNullOrEmpty(response)) return;

                // Chuyển dữ liệu vào Dataflow Queue và giải phóng lập tức Thread COM Port
                _ScanoutResonposeProcessor.Post(new ScanOutResponseData { RawResponse = response });
            }
            catch (Exception serialEx)
            {
                Logger.Error("ScanOut", $"Serial Port Error: {serialEx.Message}", serialEx);
            }
        }

        private async Task ProcessScanOutResponseAsync(ScanOutResponseData data)
        {
            try
            {
                string response = data.RawResponse;
                var dbService = new AutoVisionDbService();

                ScanOutResult scanOutRes = ScanOutResult.NG;
                string PID = "";
                string errorMsg = "";
                string ebr = "";
                string wo = "";

                var parts = response.Split('|');
                if (parts.Length >= 2)
                {
                    PID = parts[1]?.Trim() ?? PID;

                    if (parts.Length >= 3) ebr = parts[2]?.Trim();
                    if (parts.Length >= 4) wo = parts[3]?.Trim();
                    if (parts.Length >= 5) errorMsg = parts[4]?.Trim();

                    try
                    {
                        var scanoutData = new TbAutoVisionScanout
                        {
                            Pid = PID,
                            ScanoutStatus = parts[0],
                            ErrorMessage = errorMsg,
                            ScanoutTime = DateTime.Now,
                            ebr = ebr, // Có thể null hoặc trống
                            wo = wo    // Có thể null hoặc trống
                        };

                        // Xử lý HSMES (Oracle DB) TRƯỚC TIÊN để đảm bảo kiểm soát dây chuyền nhanh chóng
                        if (string.Equals(scanoutData.ScanoutStatus, "NG", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(PID))
                        {
                            //string blockReason = string.IsNullOrEmpty(errorMsg) ? "Scanout NG" : errorMsg;

                            // ❗ Skip nếu là Mismatch quantity
                            if (!errorMsg.ToLower().Contains("mismatch quantity"))
                            {
                                bool isBlockUpdated = await dbService.UpdateBlock(PID, errorMsg, "", CurrentMagazineNo, "Autovision");
                                if (!isBlockUpdated)
                                {
                                    Logger.Warning("ScanOut", $"Failed to upsert TBL_BLOCK for NG PID: {PID}");
                                }
                            }
                            //else
                            //{
                            //    Logger.Warning("ScanOut", $"{PID} is mismatch quantity. Stopping machine.");
                            //    await PLC.WriteHoldingRegisterAsync("HMI_End_Cycle", 1);
                            //}
                        }
                        else if (string.Equals(scanoutData.ScanoutStatus, "OK", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(PID))
                        {
                            bool isReleased = await dbService.ReleaseBlock(PID);
                            if (!isReleased)
                            {
                                // Ghi log debug nếu cần
                            }
                        }

                        // SAU KHI chạy HSMES xong, mới lưu thông tin vào Local DB (MMES)
                        bool isInserted = await dbService.UpdateVisionScanoutAsync(scanoutData);

                        if (!isInserted)
                        {
                            Logger.Warning("ScanOut", $"Failed to save Vision Scanout to DB for PID: {PID}");
                        }
                        else if (!string.IsNullOrEmpty(PID) && (!string.IsNullOrEmpty(ebr) || !string.IsNullOrEmpty(wo)))
                        {
                            bool isUpdated = await dbService.UpdateVisionResultEbrWoAsync(PID, ebr, wo);

                            if (!isUpdated)
                                Logger.Warning("ScanOut", $"UpdateVisionResultEbrWo: no rows updated for PID: {PID}");
                        }
                    }
                    catch (Exception dbEx)
                    {
                        Logger.Error("ScanOut", $"Error saving to DB: {dbEx.Message}", dbEx);
                    }
                } // Đóng if parts.Length >= 2
            }
            catch (Exception ex)
            {
                Logger.Error("ScanOut", "Error during background scan out operation", ex);
            }
        }


        /// <summary>
        /// Stop all robot sequences (stub - robots removed)
        /// </summary>
        public void StopAllRobotSequences()
        {
            // TODO: Implement PLC-based sequence stopping if needed
            Logger.Info("Machine", "StopAllRobotSequences called (stub - robots removed)");
        }

        /// <summary>
        /// Update machine model (stub - simplified)
        /// </summary>
        public void UpdateModel(PCBModel model)
        {
            if (model == null) return;

            _PCBModel = model;
            //Logger.Info("Machine", $"UpdateModel called for model: {model.Name}");

            // Reload vision solution if needed
            try
            {
                LoadVisionSolution(model);
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", $"Error loading vision solution during model update: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get machine status string (stub)
        /// </summary>
        public string GetMachineStatus()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine("=== Machine Status ===");
            status.AppendLine($"Initialized: {IsInitialized}");
            status.AppendLine($"Enabled: {IsMachineEnabled}");
            status.AppendLine($"Mode: {Mode}");
            status.AppendLine($"PLC Connected: {PLC?.IsConnected ?? false}");
            status.AppendLine($"Model: {_PCBModel?.Name ?? "None"}");
            return status.ToString();
        }

        /// <summary>
        /// Start machine (stub)
        /// </summary>
        public void StartMachine()
        {
            // TODO: Implement PLC-based machine start logic
            IsMachineEnabled = true;
            Mode = MachineMode.Auto;
            //Logger.Info("Machine", "StartMachine called - machine enabled");
        }

        /// <summary>
        /// Stop machine (stub)
        /// </summary>
        public void StopMachine()
        {
            // TODO: Implement PLC-based machine stop logic
            IsMachineEnabled = false;
            Mode = MachineMode.Manual;
            //Logger.Info("Machine", "StopMachine called - machine disabled");
        }

        /// <summary>
        /// Emergency stop (stub)
        /// </summary>
        public void EmergencyStop()
        {
            // TODO: Implement PLC-based emergency stop
            IsMachineEnabled = false;
            Mode = MachineMode.Manual;
            Logger.Critical("Machine", "EmergencyStop called");
            _errorList.AddError(ErrorType.Critical, "Machine", "Emergency stop activated");
        }

        /// <summary>
        /// Reset emergency (stub)
        /// </summary>
        public void ResetEmergency()
        {
            // TODO: Implement PLC-based emergency reset
            Logger.Info("Machine", "ResetEmergency called (stub)");
        }

        /// <summary>
        /// Reset buzzer (stub)
        /// </summary>
        public void ResetBuzzer()
        {
            // TODO: Implement PLC-based buzzer reset
            Logger.Info("Machine", "ResetBuzzer called (stub)");
        }

        #endregion
    }
}