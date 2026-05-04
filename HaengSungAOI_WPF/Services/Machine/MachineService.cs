using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using HaengSungAOI_WPF.Machine;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Services.UI;
using HaengSungAOI_WPF.Utils;

namespace HaengSungAOI_WPF.Services.Machine
{
    public class MachineService : IMachineService
    {
        private readonly ILogger<MachineService> _logger;
        private readonly IPlcService _plcService;
        private readonly IVisionService _visionService;
        private readonly IScanOutService _scanOutService;
        private readonly AutoVisionDbService _dbService;
        private readonly IImageDisplayService _imageDisplayService;
        private readonly IGlobalStateService _globalState;
        private readonly IMachineHmiService _hmiService;
        private readonly IErrorService _errorService;

        private HaengSungAOI_WPF.Machine.Machine _machine;
        public HaengSungAOI_WPF.Machine.Machine Machine => _machine;
        public IPlcService PLC => _plcService;
        public IMachineHmiService HMI => _hmiService;

        private bool _isInitialized;
        private bool _isRunning;
        private MachineMode _mode = MachineMode.Manual;
        private PCBModel _currentModel;


        private readonly ActionBlock<VisionTriggerEventArgs> _visionProcessor;
        private readonly ActionBlock<ScanOutReceivedEventArgs> _scanOutResponseProcessor;
        private readonly ActionBlock<PLCWorkItem> _trayAndProductProcessor;

        public bool IsRunning => _isRunning;
        public bool IsInitialized => _isInitialized;
        public MachineMode Mode { get => _mode; set { _mode = value; } }
        public PCBModel CurrentModel => _currentModel;
        public object FrontendControl { get; set; }

        public bool EnableScanOut 
        { 
            get => _machine?.EnableScanOut ?? true; 
            set { if (_machine != null) _machine.EnableScanOut = value; } 
        }

        public bool OverrideInspection 
        { 
            get => _machine?.IsByPass ?? false; 
            set { if (_machine != null) _machine.IsByPass = value; } 
        }

        public event Action<bool> OnRunningStateChanged;
        public event Action<string> OnStatusMessageChanged;

        public MachineService(
            ILogger<MachineService> logger,
            IPlcService plcService,
            IVisionService visionService,
            IScanOutService scanOutService,
            AutoVisionDbService dbService,
            IImageDisplayService imageDisplayService,
            IGlobalStateService globalState,
            IErrorService errorService,
            IMachineHmiService hmiService = null)
        {
            _logger = logger;
            _plcService = plcService;
            _visionService = visionService;
            _scanOutService = scanOutService;
            _dbService = dbService;
            _imageDisplayService = imageDisplayService;
            _globalState = globalState;
            _hmiService = hmiService;
            _errorService = errorService;

            // Initialize Processors (ActionBlocks)
            _visionProcessor = new ActionBlock<VisionTriggerEventArgs>(
                async e => await ProcessVisionTriggerAsync(e),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true }
            );

            _scanOutResponseProcessor = new ActionBlock<ScanOutReceivedEventArgs>(
                async e => await ProcessScanOutResponseAsync(e),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true }
            );

            _trayAndProductProcessor = new ActionBlock<PLCWorkItem>(
                async item => await ProcessTrayAndProductAsync(item),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, EnsureOrdered = true }
            );

            // Subscribe to Service Events
            _plcService.VisionTriggered += (s, e) => _visionProcessor.Post(e);
            _plcService.TrayUpdated += (s, e) => _trayAndProductProcessor.Post(new PLCWorkItem { Type = PLCWorkType.TrayUpdate, TagName = e.TagName, NewValue = e.NewValue });
            _plcService.AlarmChanged += OnAlarmChanged;
            _scanOutService.DataReceived += (s, e) => _scanOutResponseProcessor.Post(e);
            _visionService.ProcedureCompleted += OnVisionProcedureCompleted;
        }

        public void Initialize()
        {
            try
            {
                _logger.LogInformation("Initializing Machine Service...");
                
                // Initialize legacy Machine object
                _machine = new HaengSungAOI_WPF.Machine.Machine(_plcService, _errorService);
                _machine.Initialize();

                _visionService.FrontendControl = FrontendControl;
                _plcService.Connect();
                _plcService.Start();

                // Load Current Model
                var modelDb = new ModelDatabaseManager();
                _currentModel = modelDb.GetActiveModel();
                
                if (_currentModel != null)
                {
                    _visionService.LoadSolutionForModel(_currentModel);
                }

                _scanOutService.Open("COM7");

                _isInitialized = true;
                _logger.LogInformation("Machine Service Initialized Successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Machine Service");
                // Do not re-throw to avoid application crash during initialization
            }
        }

        public void Start()
        {
            _isRunning = true;
            _mode = MachineMode.Auto;
            _globalState.IsAutoMode = true;
            OnRunningStateChanged?.Invoke(true);
            _logger.LogInformation("Machine Started in Auto Mode");
        }

        public void Stop()
        {
            _isRunning = false;
            _mode = MachineMode.Manual;
            _globalState.IsAutoMode = false;
            OnRunningStateChanged?.Invoke(false);
            _logger.LogInformation("Machine Stopped");
        }

        public void EmergencyStop()
        {
            Stop();
            _machine?.EmergencyStop();
            _logger.LogCritical("Emergency Stop Activated");
        }

        public void ResetEmergency()
        {
            _machine?.ResetEmergency();
            _logger.LogInformation("Emergency Reset");
        }

        public void ClearQueues()
        {
            _logger.LogInformation("WIP queues cleared (No-op in new architecture)");
        }

        public void UpdateModel(PCBModel model)
        {
            _currentModel = model;
            _visionService.LoadSolutionForModel(model);
        }

        private async Task ProcessVisionTriggerAsync(VisionTriggerEventArgs e)
        {
            try
            {
                _logger.LogInformation($"Processing Vision Trigger: {e.ProcedureName}");
                

                await _visionService.RunProcedureAsync(e.ProcedureName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing vision trigger: {e.ProcedureName}");
            }
        }

        private void OnVisionProcedureCompleted(object sender, HaengSungAOI_WPF.Services.Vision.VisionProcedureCompletedEventArgs e)
        {
            // Write results to PLC
            _plcService.WriteVisionResult(e.ProcedureName, e.IsOK);
            
            if (e.ProcedureName == "Align")
            {
                _plcService.WriteAlignPosition(e.AlignX, e.AlignY, e.AlignAngle);
            }

            // Update UI Image via ImageDisplayService (If needed)
            // e.Procedure.GetOutputImage(...)
            
            _logger.LogInformation($"Vision Procedure {e.ProcedureName} Completed. Result: {(e.IsOK ? "OK" : "NG")}");
        }

        private async Task ProcessScanOutResponseAsync(ScanOutReceivedEventArgs e)
        {
            try
            {
                _logger.LogInformation($"Processing ScanOut Response for PID: {e.PID}");
                
                var scanoutData = new TbAutoVisionScanout
                {
                    Pid = e.PID,
                    ScanoutStatus = e.Status,
                    ErrorMessage = e.ErrorMessage,
                    ScanoutTime = DateTime.Now
                };

                // Database updates (Legacy logic)
                if (e.Status == "NG")
                {
                    await _dbService.UpdateBlock(e.PID, e.ErrorMessage, "", "", "Autovision");
                }
                else
                {
                    await _dbService.ReleaseBlock(e.PID);
                }

                await _dbService.UpdateVisionScanoutAsync(scanoutData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scan-out response");
            }
        }

        private async Task ProcessTrayAndProductAsync(PLCWorkItem item)
        {
            // Logic for Tray Update (Simplified)
            _logger.LogDebug($"Processing Tray/Product Update: {item.TagName} = {item.NewValue}");
        }

        private void OnAlarmChanged(object sender, AlarmEventArgs e)
        {
            if (e.IsActive)
            {
                _logger.LogWarning($"Alarm Activated: {e.Message}");
                OnStatusMessageChanged?.Invoke($"ALARM: {e.Message}");
            }
        }

        public void Dispose()
        {
            _plcService.Dispose();
            _visionService.Dispose();
            _scanOutService.Dispose();
        }
    }
}
