using HaengSungAOI_WPF.Core.PLC;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services;
using HaengSungAOI_WPF.Services.Machine;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Utils;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HaengSungAOI_WPF.Core
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
    /// Machine class - Core logic and state management.
    /// Relocated to the Core namespace and decoupled from direct hardware/vision SDK dependencies.
    /// Uses IVisionService for all vision operations.
    /// </summary>
    public partial class Machine
    {
        #region Fields

        private bool _isMachineEnabled = false;
        private bool _isInitialized = false;
        
        public MachineMode Mode { get; set; } = MachineMode.Manual;
        public bool EnableScanOut { get; set; } = true;
        public bool IsByPass { get; set; } = false;
        public bool MachineHomed { get; set; } = false;
        public static string CurrentEbr { get; set; } = "";
        public object FrontendControl { get; set; }
        public object frontendControl { get => FrontendControl; set => FrontendControl = value; }

        public bool IsMachineEnabled
        {
            get => _isMachineEnabled;
            private set
            {
                _isMachineEnabled = value;
                OnMachineEnabledStateChanged?.Invoke(value);
            }
        }

        public bool IsInitialized
        {
            get => _isInitialized;
            private set => _isInitialized = value;
        }

        // Services
        public IPlcService PLC { get; private set; }
        private readonly IErrorService _errorService;
        private readonly IVisionService _visionService;

        // Events
        public event Action<bool> OnMachineEnabledStateChanged;

        #endregion

        #region Constructor

        public Machine(IPlcService plc, IErrorService errorService, IVisionService visionService)
        {
            PLC = plc;
            _errorService = errorService;
            _visionService = visionService;

            // Initialize processors for PLC work
            InitializeProcessors();
        }

        #endregion

        #region Initialize

        public void Initialize()
        {
            try
            {
                Logger.Info("Machine", "Initializing Core Machine...");

                // Initialize PLC event subscriptions
                InitializePlcEvents();

                // Read initial tray quantities from PLC
                ReadTrayQuantitiesFromPLC();

                IsInitialized = true;
                Logger.Info("Machine", "Core Machine initialization completed");
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                Logger.Error("Machine", "Failed to initialize Core Machine", ex);
                _errorService.ReportError("Machine", "Machine initialization failed", ex);
                throw;
            }
        }

        #endregion

        #region Operations

        public void StartMachine()
        {
            IsMachineEnabled = true;
            Mode = MachineMode.Auto;
            Logger.Info("Machine", "Machine started (Core)");
        }

        public void StopMachine()
        {
            IsMachineEnabled = false;
            Mode = MachineMode.Manual;
            Logger.Info("Machine", "Machine stopped (Core)");
        }

        public void EmergencyStop()
        {
            IsMachineEnabled = false;
            Mode = MachineMode.Manual;
            Logger.Critical("Machine", "Emergency Stop Triggered");
            _errorService.ReportError(ErrorType.Critical, "Machine", "Emergency stop activated");
        }

        public void ResetEmergency()
        {
            Logger.Info("Machine", "Emergency Reset (Core)");
        }

        public void ResetBuzzer()
        {
            Logger.Info("Machine", "Buzzer Reset (Core)");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            try
            {
                if (PLC != null)
                {
                    PLC.ConnectionStatusChanged -= OnPLCConnectionStatusChanged;
                    PLC = null;
                }
                Logger.Info("Machine", "Core Machine disposed");
            }
            catch (Exception ex)
            {
                Logger.Error("Machine", "Error during Core Machine disposal", ex);
            }
        }

        #endregion
    }
}

