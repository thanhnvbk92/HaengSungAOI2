using System;
using System.Text;
using System.Timers;
using HaengSungAOI_WPF.Utils;

namespace HaengSungAOI_WPF.Machine.PLC
{
    /// <summary>
    /// PLC-Based Axis Control - Replacement for EtherCAT-based Axis
    /// Uses PLCController and PLCConfiguration for axis control via Modbus TCP
    /// Provides same interface as Axis class for compatibility
    /// </summary>
    public class PLCAxis
    {
        #region Properties

        public string Name { get; set; }
        public ushort cardIndex { get; set; } // Kept for compatibility, not used in PLC mode
        public ushort Index { get; set; } // Kept for compatibility, not used in PLC mode
        public double CurrentPosition { get; set; }
        public double TargetPosition { get; set; }
        public double Speed { get; set; }
        public double AccTime { get; set; }
        public double DecTime { get; set; }
        public double STime { get; set; }

        public bool IsMoving { get; set; }
        public bool IsHoming { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsAlarm { get; set; }
        public ushort HomingMode { get; set; }
        public double HomingSpeed { get; private set; }

        public int statusword { get; set; }
        public int controlword { get; set; }

        // Status bits
        public bool isReady { get; set; }
        public bool isOn { get; set; }
        public bool OperationEnabled { get; set; }
        public bool isError { get; set; }
        public bool VoltageOutputEnabled { get; set; }
        public bool QuickStop { get; set; }
        public bool AxisDisabled { get; set; }
        public bool Warning { get; set; }
        public bool Remote { get; set; }
        public bool TargetReached { get; set; }

        // Control bits
        public bool SwitchOn { get; set; }
        public bool VoltageOutput { get; set; }
        public bool QuickStopActive { get; set; }
        public bool EnableOperation { get; set; }
        public bool ResetError { get; set; }
        public bool Halt { get; set; }

        public double LowSpeed { get; private set; }
        public double HighSpeed { get; private set; }

        #endregion

        #region Events

        public event EventHandler MovementStarted;
        public event EventHandler AlarmActivated;
        public event EventHandler AlarmCleared;
        public event EventHandler ErrorActivated;
        public event EventHandler ErrorCleared;
        public event EventHandler WarningActivated;
        public event EventHandler WarningCleared;

        #endregion

        #region Private Fields

        private readonly PLCController _plc;
        private readonly string _plcAxisPrefix; // e.g., "HMI_X1", "HMI_Z1", "HMI_C1"
        private readonly Timer _timer;
        private readonly MachineErrorList _errorList;

        private bool _wasMoving = false;
        private bool _wasHoming = false;
        private bool _previousAlarmState = false;
        private bool _previousErrorState = false;
        private bool _previousWarningState = false;

        private double offsetPos;
        private double pulse_per_unit;
        private double neg_softlimit;
        private double pos_softlimit;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new PLC-based axis
        /// </summary>
        /// <param name="name">Axis name (e.g., "InfeedAxisX1")</param>
        /// <param name="plc">PLCController instance</param>
        /// <param name="plcAxisPrefix">PLC axis prefix (e.g., "X1", "Z1", "C1")</param>
        /// <param name="card">Card index (kept for compatibility, not used)</param>
        /// <param name="index">Axis index (kept for compatibility, not used)</param>
        /// <param name="homingMode">Homing mode</param>
        public PLCAxis(string name, PLCController plc, string plcAxisPrefix, ushort card = 0, ushort index = 0, ushort homingMode = 0)
        {
            if (plc == null)
                throw new ArgumentNullException(nameof(plc), "PLCController cannot be null");

            if (string.IsNullOrEmpty(plcAxisPrefix))
                throw new ArgumentException("PLC axis prefix cannot be null or empty", nameof(plcAxisPrefix));

            Name = name;
            _plc = plc;
            _plcAxisPrefix = plcAxisPrefix;
            cardIndex = card;
            Index = index;
            HomingMode = homingMode;

            // Initialize default values
            CurrentPosition = 0.0;
            TargetPosition = 0.0;
            Speed = 0.0;
            AccTime = 0.1;
            DecTime = 0.1;
            STime = 0.1;
            IsMoving = false;
            IsHoming = false;
            IsEnabled = true;
            IsAlarm = false;
            statusword = 0;
            controlword = 0;

            _errorList = MachineErrorList.Instance;

            // Subscribe to PLC data changes for lamp feedback
            _plc.DataChanged += OnPLCDataChanged;

            // Start monitoring timer
            _timer = new Timer(100); // Update every 100 ms
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();

            Logger.Info("PLCAxis", $"Initialized PLC axis: {Name} with prefix {_plcAxisPrefix}");
        }

        #endregion

        #region Initialization

        public void Init(double ppu, double min, double max, double lowSpd, double highSpd, double homeOffset)
        {
            pulse_per_unit = ppu;
            neg_softlimit = min;
            pos_softlimit = max;
            LowSpeed = lowSpd;
            HighSpeed = highSpd;
            offsetPos = homeOffset;

            Logger.Info("PLCAxis", $"Initialized {Name}: PPU={ppu}, Limits=[{min},{max}], Speeds=[{lowSpd},{highSpd}], Offset={homeOffset}");
        }

        #endregion

        #region Timer and Status Updates

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                UpdateStatusFromPLC();
                CheckAlarmEvents();
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error in timer elapsed for {Name}: {ex.Message}", ex);
            }
        }

        private void UpdateStatusFromPLC()
        {
            try
            {
                // Read servo ON lamp state
                bool? servoLamp = _plc.GetBoolValue($"HMI_Lamp_{_plcAxisPrefix}_Servo_ON_PB");
                if (servoLamp.HasValue)
                {
                    isOn = servoLamp.Value;
                    isReady = servoLamp.Value;
                    OperationEnabled = servoLamp.Value;
                }

                // Read jog lamp states to detect movement
                bool? jogPlusLamp = _plc.GetBoolValue($"HMI_Lamp_{_plcAxisPrefix}_Jog_Plus_PB");
                bool? jogMinusLamp = _plc.GetBoolValue($"HMI_Lamp_{_plcAxisPrefix}_Jog_Minus_PB");
                
                if (jogPlusLamp.HasValue || jogMinusLamp.HasValue)
                {
                    IsMoving = (jogPlusLamp.GetValueOrDefault(false) || jogMinusLamp.GetValueOrDefault(false));
                }

                // Read homing lamp state
                bool? homingLamp = _plc.GetBoolValue($"HMI_Lamp_{_plcAxisPrefix}_ORG_PB");
                if (homingLamp.HasValue)
                {
                    IsHoming = homingLamp.Value;
                }

                // TODO: Read position from PLC if position registers are available
                // For now, position tracking would need to be added to PLC program

            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error updating status from PLC for {Name}: {ex.Message}", ex);
            }
        }

        private void OnPLCDataChanged(object sender, PLCDataChangedEventArgs e)
        {
            try
            {
                // React to specific lamp changes for this axis
                if (e.DataPointName.StartsWith($"HMI_Lamp_{_plcAxisPrefix}"))
                {
                    // Update status based on lamp changes
                    UpdateStatusFromPLC();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error handling PLC data change for {Name}: {ex.Message}", ex);
            }
        }


        private void CheckAlarmEvents()
        {
            try
            {
                if (!_previousAlarmState && IsAlarm)
                {
                    OnAlarmActivated();
                }
                else if (_previousAlarmState && !IsAlarm)
                {
                    OnAlarmCleared();
                }

                if (!_previousErrorState && isError)
                {
                    OnErrorActivated();
                }
                else if (_previousErrorState && !isError)
                {
                    OnErrorCleared();
                }

                if (!_previousWarningState && Warning)
                {
                    OnWarningActivated();
                }
                else if (_previousWarningState && !Warning)
                {
                    OnWarningCleared();
                }

                _previousAlarmState = IsAlarm;
                _previousErrorState = isError;
                _previousWarningState = Warning;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in axis alarm monitoring for {Name}: {ex.Message}");
            }
        }

        #endregion

        #region Movement Methods

        public void MoveTo(double position, double speed, double accTime, double decTime)
        {
            TargetPosition = position;
            Speed = speed;
            AccTime = accTime;
            DecTime = decTime;

            // Note: Direct position control via PLC would require position registers
            // For now, this is a placeholder - actual implementation depends on PLC program
            Logger.Warning("PLCAxis", $"{Name}: MoveTo not fully implemented for PLC control - requires position register support");
            
            IsMoving = true;
        }

        public void Home()
        {
            try
            {
                // Send momentary pulse to origin button
                string homingButton = $"HMI_{_plcAxisPrefix}_ORG_PB";
                _plc.WriteCoil(homingButton, true);
                
                System.Threading.Tasks.Task.Delay(100).ContinueWith(t =>
                {
                    _plc.WriteCoil(homingButton, false);
                });

                IsHoming = true;
                IsMoving = true;
                
                Logger.Info("PLCAxis", $"Initiated homing for {Name} via {homingButton}");
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error homing {Name}: {ex.Message}", ex);
            }
        }

        public void StartJog(int direction, double jogSpeed = 5000)
        {
            if (IsMoving)
            {
                Logger.Warning("PLCAxis", $"{Name} is already moving, cannot start jog");
                return;
            }

            try
            {
                string jogButton = direction == 1 
                    ? $"HMI_{_plcAxisPrefix}_Jog_Plus_PB"
                    : $"HMI_{_plcAxisPrefix}_Jog_Minus_PB";

                _plc.WriteCoil(jogButton, true);
                IsMoving = true;
                Speed = jogSpeed;

                Logger.Debug("PLCAxis", $"Started jogging {Name} in {(direction == 1 ? "positive" : "negative")} direction");
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error starting jog for {Name}: {ex.Message}", ex);
            }
        }

        public void StopJog()
        {
            try
            {
                // Release both jog buttons
                _plc.WriteCoil($"HMI_{_plcAxisPrefix}_Jog_Plus_PB", false);
                _plc.WriteCoil($"HMI_{_plcAxisPrefix}_Jog_Minus_PB", false);
                
                IsMoving = false;
                
                Logger.Debug("PLCAxis", $"Stopped jogging {Name}");
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error stopping jog for {Name}: {ex.Message}", ex);
            }
        }

        public void JogPositive(double distance, double? jogSpeed = null)
        {
            // Step jog - would require step jog buttons in PLC
            Logger.Warning("PLCAxis", $"{Name}: JogPositive (step) not fully implemented for PLC control");
            
            // For now, use momentary jog
            StartJog(1, jogSpeed ?? Speed);
            System.Threading.Tasks.Task.Delay(100).ContinueWith(t => StopJog());
        }

        public void JogNegative(double distance, double? jogSpeed = null)
        {
            // Step jog - would require step jog buttons in PLC
            Logger.Warning("PLCAxis", $"{Name}: JogNegative (step) not fully implemented for PLC control");
            
            // For now, use momentary jog
            StartJog(0, jogSpeed ?? Speed);
            System.Threading.Tasks.Task.Delay(100).ContinueWith(t => StopJog());
        }

        public void Stop()
        {
            try
            {
                StopJog();
                Halt = true;
                IsMoving = false;
                
                Logger.Info("PLCAxis", $"Stopped {Name}");
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error stopping {Name}: {ex.Message}", ex);
                Halt = true;
                IsMoving = false;
            }
        }

        #endregion

        #region Servo Control

        public void Set_Servo_On()
        {
            try
            {
                string servoButton = $"HMI_{_plcAxisPrefix}_Servo_ON_PB";
                
                // Toggle servo ON (write true if currently off)
                bool? currentState = _plc.GetBoolValue($"HMI_Lamp_{servoButton.Replace("HMI_", "")}");
                if (!currentState.GetValueOrDefault(false))
                {
                    _plc.WriteCoil(servoButton, true);
                    Logger.Info("PLCAxis", $"Enabled servo for {Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error enabling servo for {Name}: {ex.Message}", ex);
            }
        }

        public void Set_Servo_Off()
        {
            try
            {
                string servoButton = $"HMI_{_plcAxisPrefix}_Servo_ON_PB";
                
                // Toggle servo OFF (write true if currently on)
                bool? currentState = _plc.GetBoolValue($"HMI_Lamp_{servoButton.Replace("HMI_", "")}");
                if (currentState.GetValueOrDefault(false))
                {
                    _plc.WriteCoil(servoButton, true);
                    Logger.Info("PLCAxis", $"Disabled servo for {Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error disabling servo for {Name}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Status and Info Methods

        public double GetSpeed()
        {
            return Speed;
        }

        public void getPosition()
        {
            // Position would need to be read from PLC registers if available
            // For now, this is a placeholder
        }

        public bool IsAtHome()
        {
            return Math.Abs(CurrentPosition) < 10;
        }

        public string GetStatusInfo()
        {
            return $"{Name}: Pos={CurrentPosition:F3}, Moving={IsMoving}, Homing={IsHoming}, Enabled={IsEnabled}, Error={isError}, Alarm={IsAlarm}, Warning={Warning}";
        }

        public string StatusInfo => GetStatusInfo();

        public string GetHealthStatus()
        {
            var health = new StringBuilder();
            health.AppendLine($"=== PLC Axis {Name} Health Status ===");
            health.AppendLine($"Position: {CurrentPosition:F3}");
            health.AppendLine($"Ready: {isReady}");
            health.AppendLine($"Servo On: {isOn}");
            health.AppendLine($"Error: {isError}");
            health.AppendLine($"Alarm: {IsAlarm}");
            health.AppendLine($"Warning: {Warning}");
            health.AppendLine($"Operation Enabled: {OperationEnabled}");
            health.AppendLine($"Moving: {IsMoving}");
            health.AppendLine($"Homing: {IsHoming}");
            health.AppendLine($"PLC Axis Prefix: {_plcAxisPrefix}");
            return health.ToString();
        }

        public void ClearAlarm()
        {
            try
            {
                ResetError = true;
                _errorList?.AddAxisInfo(Name, "Manual alarm clear initiated (PLC axis)", GetStatusDetails());
                Logger.Info("PLCAxis", $"Manual alarm clear initiated for {Name}");
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error clearing alarm for {Name}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Private Helper Methods

        private void OnAlarmActivated()
        {
            string alarmDetails = GetAlarmDetails();
            _errorList?.AddAxisAlarm(Name, "PLC Axis alarm activated", alarmDetails);
            AlarmActivated?.Invoke(this, EventArgs.Empty);
            Logger.Warning("PLCAxis", $"ALARM: Axis {Name} - alarm activated");
        }

        private void OnAlarmCleared()
        {
            _errorList?.AddAxisInfo(Name, "PLC Axis alarm cleared", GetStatusDetails());
            AlarmCleared?.Invoke(this, EventArgs.Empty);
            Logger.Info("PLCAxis", $"INFO: Axis {Name} - alarm cleared");
        }

        private void OnErrorActivated()
        {
            string errorDetails = GetErrorDetails();
            _errorList?.AddAxisError(Name, "PLC Axis error detected", errorDetails);
            ErrorActivated?.Invoke(this, EventArgs.Empty);
            Logger.Error("PLCAxis", $"ERROR: Axis {Name} - error detected");
        }

        private void OnErrorCleared()
        {
            _errorList?.AddAxisInfo(Name, "PLC Axis error cleared", GetStatusDetails());
            ErrorCleared?.Invoke(this, EventArgs.Empty);
            Logger.Info("PLCAxis", $"INFO: Axis {Name} - error cleared");
        }

        private void OnWarningActivated()
        {
            string warningDetails = GetWarningDetails();
            _errorList?.AddAxisWarning(Name, "PLC Axis warning activated", warningDetails);
            WarningActivated?.Invoke(this, EventArgs.Empty);
            Logger.Warning("PLCAxis", $"WARNING: Axis {Name} - warning activated");
        }

        private void OnWarningCleared()
        {
            _errorList?.AddAxisInfo(Name, "PLC Axis warning cleared", GetStatusDetails());
            WarningCleared?.Invoke(this, EventArgs.Empty);
            Logger.Info("PLCAxis", $"INFO: Axis {Name} - warning cleared");
        }

        private string GetAlarmDetails()
        {
            var details = new StringBuilder();
            details.AppendLine($"PLC Axis: {Name} (Prefix: {_plcAxisPrefix})");
            details.AppendLine($"Position: {CurrentPosition:F3}");
            details.AppendLine("Status Flags:");
            details.AppendLine($"  - Ready: {isReady}");
            details.AppendLine($"  - Servo On: {isOn}");
            details.AppendLine($"  - Operation Enabled: {OperationEnabled}");
            details.AppendLine($"  - Error: {isError}");
            details.AppendLine($"  - Alarm: {IsAlarm}");
            details.AppendLine($"  - Warning: {Warning}");
            details.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            return details.ToString();
        }

        private string GetErrorDetails()
        {
            var details = new StringBuilder();
            details.AppendLine($"PLC Axis: {Name} (Prefix: {_plcAxisPrefix})");
            details.AppendLine($"Position: {CurrentPosition:F3}");
            details.AppendLine("Error Flags:");
            details.AppendLine($"  - Error: {isError}");
            details.AppendLine($"  - Operation Enabled: {OperationEnabled}");
            details.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            return details.ToString();
        }

        private string GetWarningDetails()
        {
            var details = new StringBuilder();
            details.AppendLine($"PLC Axis: {Name} (Prefix: {_plcAxisPrefix})");
            details.AppendLine($"Position: {CurrentPosition:F3}");
            details.AppendLine("Warning Information:");
            details.AppendLine($"  - Warning: {Warning}");
            details.AppendLine($"  - Ready: {isReady}");
            details.AppendLine($"  - Operation Enabled: {OperationEnabled}");
            details.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            return details.ToString();
        }

        private string GetStatusDetails()
        {
            return $"PLC Axis: {Name}, Position: {CurrentPosition:F3}, Prefix: {_plcAxisPrefix}, Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}";
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            try
            {
                _timer?.Stop();
                _timer?.Dispose();
                
                if (_plc != null)
                {
                    _plc.DataChanged -= OnPLCDataChanged;
                }
                
                Logger.Info("PLCAxis", $"Disposed PLC axis {Name}");
            }
            catch (Exception ex)
            {
                Logger.Error("PLCAxis", $"Error disposing {Name}: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
