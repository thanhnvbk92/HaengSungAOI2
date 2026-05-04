using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Linq;
using HaengSungAOI_WPF;
using HaengSungAOI_WPF.Services.Machine;

namespace HaengSungAOI_WPF.Core.PLC
{
    /// <summary>
    /// Defines the servo axes available in the system based on Servo para.csv
    /// </summary>
    public enum ServoAxis
    {
        X1 = 0,   // PCB Infeed Robot X
        Y1 = 1,   // PCB Infeed Robot Y
        C1 = 2,   // PCB Infeed Robot C (Rotation)
        X2 = 3,   // PCB Transfer X
        Z2 = 4,   // PCB Transfer Z
        X3 = 5,   // Out Feed Robot X
        Y3 = 6,   // Out Feed Robot Y
        Z4 = 7,   // Inspect 1 Z
        C4 = 8,   // Inspect 1 C (Rotation)
        Z5 = 9,// Inspect 2 Z
        C5 = 10,  // Inspect 2 C (Rotation)
        Z61 = 11, // Lifting Tray In
        Z62 = 12, // Lifting Tray Out
        CV7 = 13  // NG CV
    }

    /// <summary>
    /// Defines the parameter types for servo status/data registers
    /// </summary>
    public enum ServoParameter
    {
        CurrentPosition = 0,      // Offset +0, LREAL (4 bytes)
        CurrentSpeed = 4,   // Offset +4, LREAL
        ErrorCode = 8,         // Offset +8, LREAL
        OperationStatus = 12,     // Offset +12, LREAL
        ORGFound = 16,     // Offset +16, BOOL
        MoveCompleted = 17, // Offset +17, BOOL
        Acceleration = 20, // Offset +20, LREAL
        Deceleration = 24,   // Offset +24, LREAL
        ORGSpeedFast = 28, // Offset +28, LREAL
        JogSpeed = 32,         // Offset +32, LREAL
        InchingDistance = 36,     // Offset +36, LREAL
        InchingSpeed = 40,        // Offset +40, LREAL
        TargetPosition = 50,      // Offset +50, LREAL
        TargetSpeed = 54,         // Offset +54, LREAL
        TargetPoint = 58,         // Offset +58, INT
        CurrentPoint = 60 // Offset +60, INT
    }

    /// <summary>
    /// Defines HMI push button types for servo control
    /// </summary>
    public enum ServoHMIButton
    {
        ServoON = 0,
         ORG = 1,
        JogPlus = 2,
        JogMinus = 3,
        JogPlusHispeed = 4,
        JogMinusHispeed = 5,
        InchingPlus = 6,
        InchingMinus = 7,
        StepPlus = 8,
        StepMinus = 9,
        Move = 10,
        // Positions 11-15 reserved
        Pos1 = 16,
        Pos2 = 17,
        Pos3 = 18,
        Pos4 = 19,
        Pos5 = 20,
        Pos6 = 21,
        Pos7 = 22,
        Pos8 = 23,
        Pos9 = 24,
        Pos10 = 25,
        Pos11 = 26
    }

    /// <summary>
    /// Calculates PLC addresses for servo parameters based on the Servo para.csv structure
    /// </summary>
public static class ServoAddressCalculator
    {
        // Base addresses for each axis (from CSV: MW6000, MW6200, MW6400, etc.)
      private static readonly Dictionary<ServoAxis, ushort> AxisBaseAddresses = new Dictionary<ServoAxis, ushort>
 {
        { ServoAxis.X1, 6000 },
        { ServoAxis.Y1, 6200 },
        { ServoAxis.C1, 6400 },
        { ServoAxis.X2, 6600 },
        { ServoAxis.Z2, 6800 },
        { ServoAxis.X3, 7000 },
        { ServoAxis.Y3, 7200 },
        { ServoAxis.Z4, 7400 },
        { ServoAxis.C4, 7600 },
        { ServoAxis.Z5, 7800 },
        { ServoAxis.C5, 8000 },
        { ServoAxis.Z61, 8200 },
        { ServoAxis.Z62, 8400 },
        { ServoAxis.CV7, 8600 }
        };

        // Offset from base address to HMI push buttons section
    private const ushort HMI_BUTTON_OFFSET = 100;

        // Offset from base address to HMI lamp section
     private const ushort HMI_LAMP_OFFSET = 150;

        // Address increment between axes
        private const ushort AXIS_ADDRESS_INCREMENT = 200;

        /// <summary>
        /// Gets the base address for a servo axis
        /// </summary>
        public static ushort GetAxisBaseAddress(ServoAxis axis)
    {
    return AxisBaseAddresses[axis];
        }

        /// <summary>
        /// Calculates the base address for any axis using the formula
  /// </summary>
     public static ushort CalculateAxisBaseAddress(ServoAxis axis)
        {
          return (ushort)(6000 + ((int)axis * AXIS_ADDRESS_INCREMENT));
        }

        /// <summary>
        /// Gets the address for a specific servo parameter
      /// </summary>
  /// <param name="axis">The servo axis</param>
        /// <param name="parameter">The parameter type</param>
     /// <returns>The MW address for the parameter</returns>
        public static ushort GetParameterAddress(ServoAxis axis, ServoParameter parameter)
{
        return (ushort)(GetAxisBaseAddress(axis) + (int)parameter);
        }

    /// <summary>
        /// Gets the address for an HMI push button
   /// </summary>
        /// <param name="axis">The servo axis</param>
      /// <param name="button">The button type</param>
    /// <returns>The MW address for the push button</returns>
        public static ushort GetHMIButtonAddress(ServoAxis axis, ServoHMIButton button)
        {
 return (ushort)(GetAxisBaseAddress(axis) + HMI_BUTTON_OFFSET + (int)button);
        }

        /// <summary>
 /// Gets the address for an HMI lamp indicator
        /// </summary>
    /// <param name="axis">The servo axis</param>
     /// <param name="button">The button type (lamp corresponds to button)</param>
        /// <returns>The MW address for the lamp indicator</returns>
   public static ushort GetHMILampAddress(ServoAxis axis, ServoHMIButton button)
   {
            return (ushort)(GetAxisBaseAddress(axis) + HMI_LAMP_OFFSET + (int)button);
   }

        /// <summary>
  /// Gets the address for a specific position button (Pos1-Pos11)
  /// </summary>
      /// <param name="axis">The servo axis</param>
    /// <param name="positionNumber">Position number (1-11)</param>
      /// <returns>The MW address for the position button</returns>
        public static ushort GetPositionButtonAddress(ServoAxis axis, int positionNumber)
        {
            if (positionNumber < 1 || positionNumber > 11)
 throw new ArgumentOutOfRangeException(nameof(positionNumber), "Position number must be between 1 and 11");

            return (ushort)(GetAxisBaseAddress(axis) + HMI_BUTTON_OFFSET + 15 + positionNumber);
     }

 /// <summary>
      /// Gets the address for a specific position lamp (Pos1-Pos11)
        /// </summary>
        /// <param name="axis">The servo axis</param>
        /// <param name="positionNumber">Position number (1-11)</param>
        /// <returns>The MW address for the position lamp</returns>
        public static ushort GetPositionLampAddress(ServoAxis axis, int positionNumber)
        {
            if (positionNumber < 1 || positionNumber > 11)
    throw new ArgumentOutOfRangeException(nameof(positionNumber), "Position number must be between 1 and 11");

            return (ushort)(GetAxisBaseAddress(axis) + HMI_LAMP_OFFSET + 15 + positionNumber);
        }

 /// <summary>
        /// Gets the axis name as displayed in the CSV (e.g., "AX1", "AY1", "AC1")
        /// </summary>
        public static string GetAxisDisplayName(ServoAxis axis)
        {
            switch (axis)
            {
                case ServoAxis.X1: return "AX1";
                case ServoAxis.Y1: return "AY1";
                case ServoAxis.C1: return "AC1";
                case ServoAxis.X2: return "AX2";
                case ServoAxis.Z2: return "AZ2";
                case ServoAxis.X3: return "AX3";
                case ServoAxis.Y3: return "AY3";
                case ServoAxis.Z4: return "AZ4";
                case ServoAxis.C4: return "AC4";
                case ServoAxis.Z5: return "AZ5";
                case ServoAxis.C5: return "AC5";
                case ServoAxis.Z61: return "AZ61";
                case ServoAxis.Z62: return "AZ62";
                case ServoAxis.CV7: return "CV7";
                default: return axis.ToString();
      }
        }

        /// <summary>
    /// Gets the robot/unit name for an axis
    /// </summary>
        public static string GetAxisUnitName(ServoAxis axis)
        {
       switch (axis)
        {
            case ServoAxis.X1:
            case ServoAxis.Y1:
            case ServoAxis.C1:
       return "PCB Infeed Robot";
            case ServoAxis.X2:
            case ServoAxis.Z2:
       return "PCB Transfer";
            case ServoAxis.X3:
            case ServoAxis.Y3:
       return "Out Feed Robot";
            case ServoAxis.Z4:
            case ServoAxis.C4:
       return "Inspect 1";
            case ServoAxis.Z5:
            case ServoAxis.C5:
       return "Inspect 2";
            case ServoAxis.Z61:
       return "Lifting Tray In";
            case ServoAxis.Z62:
       return "Lifting Tray Out";
             case ServoAxis.CV7:
       return "NG CV";
     default:
         return "Unknown";
    }
     }

        /// <summary>
        /// Generates a dictionary of all parameter addresses for a specific axis
        /// </summary>
        public static Dictionary<string, ushort> GetAllParameterAddresses(ServoAxis axis)
 {
    var addresses = new Dictionary<string, ushort>();
     string axisName = GetAxisDisplayName(axis);

         foreach (ServoParameter param in Enum.GetValues(typeof(ServoParameter)))
            {
        addresses[$"{axisName}.{param}"] = GetParameterAddress(axis, param);
            }

         return addresses;
  }

        /// <summary>
        /// Generates a dictionary of all HMI button addresses for a specific axis
        /// </summary>
        public static Dictionary<string, ushort> GetAllHMIButtonAddresses(ServoAxis axis)
        {
   var addresses = new Dictionary<string, ushort>();
            string axisName = GetAxisDisplayName(axis);

         foreach (ServoHMIButton button in Enum.GetValues(typeof(ServoHMIButton)))
       {
    addresses[$"HMI.{axisName} {button} PB"] = GetHMIButtonAddress(axis, button);
}

    return addresses;
        }

        /// <summary>
        /// Generates a dictionary of all HMI lamp addresses for a specific axis
        /// </summary>
        public static Dictionary<string, ushort> GetAllHMILampAddresses(ServoAxis axis)
        {
            var addresses = new Dictionary<string, ushort>();
        string axisName = GetAxisDisplayName(axis);

    foreach (ServoHMIButton button in Enum.GetValues(typeof(ServoHMIButton)))
   {
             addresses[$"HMI.Lamp {axisName} {button} PB"] = GetHMILampAddress(axis, button);
            }

            return addresses;
        }

        /// <summary>
        /// Gets the data type for a parameter
   /// </summary>
  public static Type GetParameterDataType(ServoParameter parameter)
      {
            switch (parameter)
            {
        case ServoParameter.ORGFound:
         case ServoParameter.MoveCompleted:
          return typeof(bool);
                case ServoParameter.TargetPoint:
           case ServoParameter.CurrentPoint:
      return typeof(short); // INT in PLC
    default:
                    return typeof(double); // LREAL in PLC
        }
        }

     /// <summary>
        /// Gets the size in bytes/words for a parameter
        /// </summary>
        public static int GetParameterSize(ServoParameter parameter)
        {
            switch (parameter)
      {
                case ServoParameter.ORGFound:
 case ServoParameter.MoveCompleted:
 return 1; // BOOL = 1 word
    case ServoParameter.TargetPoint:
   case ServoParameter.CurrentPoint:
             return 2; // INT = 2 words
             default:
   return 4; // LREAL = 4 words (8 bytes / 2)
            }
        }
    }

    /// <summary>
  /// Represents the current status of a servo axis for monitoring
    /// Implements INotifyPropertyChanged for UI binding
    /// </summary>
    public class ServoAxisStatus : INotifyPropertyChanged
    {
   private double _currentPosition;
        private double _currentSpeed;
        private double _errorCode;
      private double _operationStatus;
 private bool _orgFound;
        private bool _moveCompleted;
        private double _targetPosition;
    private double _targetSpeed;
      private int _currentPoint;
        private DateTime _lastUpdated;

        public ServoAxis Axis { get; }
   public string AxisName { get; }
        public string UnitName { get; }
        public ushort BaseAddress { get; }

   public ServoAxisStatus(ServoAxis axis)
        {
Axis = axis;
   AxisName = ServoAddressCalculator.GetAxisDisplayName(axis);
            UnitName = ServoAddressCalculator.GetAxisUnitName(axis);
            BaseAddress = ServoAddressCalculator.GetAxisBaseAddress(axis);
         _lastUpdated = DateTime.MinValue;
        }

        public double CurrentPosition
 {
      get => _currentPosition;
    set { if (_currentPosition != value) { _currentPosition = value; OnPropertyChanged(); } }
        }

        public double CurrentSpeed
        {
   get => _currentSpeed;
        set { if (_currentSpeed != value) { _currentSpeed = value; OnPropertyChanged(); } }
        }

        public double ErrorCode
      {
            get => _errorCode;
            set { if (_errorCode != value) { _errorCode = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); } }
   }

        public double OperationStatus
   {
            get => _operationStatus;
      set { if (_operationStatus != value) { _operationStatus = value; OnPropertyChanged(); } }
        }

        public bool ORGFound
        {
      get => _orgFound;
            set { if (_orgFound != value) { _orgFound = value; OnPropertyChanged(); } }
      }

        public bool MoveCompleted
        {
     get => _moveCompleted;
       set { if (_moveCompleted != value) { _moveCompleted = value; OnPropertyChanged(); } }
        }

      public double TargetPosition
        {
            get => _targetPosition;
    set { if (_targetPosition != value) { _targetPosition = value; OnPropertyChanged(); } }
        }

   public double TargetSpeed
      {
    get => _targetSpeed;
      set { if (_targetSpeed != value) { _targetSpeed = value; OnPropertyChanged(); } }
     }

        public int CurrentPoint
        {
   get => _currentPoint;
            set { if (_currentPoint != value) { _currentPoint = value; OnPropertyChanged(); } }
    }

        public DateTime LastUpdated
{
        get => _lastUpdated;
            set { _lastUpdated = value; OnPropertyChanged(); }
    }

      public bool HasError => ErrorCode != 0;

        public bool IsMoving => !MoveCompleted && CurrentSpeed != 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    public override string ToString()
        {
        return $"{AxisName}: Pos={CurrentPosition:F3}, Speed={CurrentSpeed:F1}, Error={ErrorCode}, Status={OperationStatus}";
   }
    }

    /// <summary>
    /// Event args for servo status change notifications
    /// </summary>
    public class ServoStatusChangedEventArgs : EventArgs
    {
      public ServoAxis Axis { get; }
        public ServoAxisStatus Status { get; }
        public string ChangedProperty { get; }

        public ServoStatusChangedEventArgs(ServoAxis axis, ServoAxisStatus status, string changedProperty)
     {
    Axis = axis;
            Status = status;
            ChangedProperty = changedProperty;
        }
    }

    /// <summary>
    /// Event args for servo error notifications
    /// </summary>
    public class ServoErrorEventArgs : EventArgs
    {
    public ServoAxis Axis { get; }
        public double ErrorCode { get; }
        public string AxisName { get; }
 public DateTime Timestamp { get; }

      public ServoErrorEventArgs(ServoAxis axis, double errorCode)
        {
            Axis = axis;
         ErrorCode = errorCode;
        AxisName = ServoAddressCalculator.GetAxisDisplayName(axis);
  Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Monitors all servo axes continuously and provides status updates
    /// Integrates with IPlcService for data reading
    /// </summary>
        public class ServoMonitor : IServoMonitorService, IDisposable
    {
        private readonly IPlcService _plcService;
        private readonly Dictionary<ServoAxis, ServoAxisStatus> _axisStatuses;
        private bool _isMonitoring;
        private bool _disposed;

        public event EventHandler<ServoStatusChangedEventArgs> StatusChanged;
        public event EventHandler<ServoErrorEventArgs> ErrorDetected;
        public event EventHandler<ServoErrorEventArgs> ErrorCleared;
        public event EventHandler<ServoAxis> MoveCompleted;

        public IReadOnlyDictionary<ServoAxis, ServoAxisStatus> AxisStatuses => _axisStatuses;
        public bool IsMonitoring => _isMonitoring;

        public ServoMonitor() : this(App.PlcService) { }

        public ServoMonitor(IPlcService plcService, int updateIntervalMs = 100)
        {
            _plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
            _axisStatuses = new Dictionary<ServoAxis, ServoAxisStatus>();

            foreach (ServoAxis axis in Enum.GetValues(typeof(ServoAxis)))
            {
                var status = new ServoAxisStatus(axis);
                status.PropertyChanged += OnAxisPropertyChanged;
                _axisStatuses[axis] = status;
            }

            if (_plcService != null)
            {
                _plcService.TagChanged += OnPlcTagChanged;
            }
        }

        public void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
            ForceUpdate(); // Initial sync
            Utils.Logger.Info("ServoMonitor", "Started reactive servo monitoring");
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            Utils.Logger.Info("ServoMonitor", "Stopped servo monitoring");
        }

        public ServoAxisStatus GetAxisStatus(ServoAxis axis) => _axisStatuses.TryGetValue(axis, out var status) ? status : null;
        public double GetCurrentPosition(ServoAxis axis) => GetAxisStatus(axis)?.CurrentPosition ?? 0;
        public double GetCurrentSpeed(ServoAxis axis) => GetAxisStatus(axis)?.CurrentSpeed ?? 0;
        public double GetErrorCode(ServoAxis axis) => GetAxisStatus(axis)?.ErrorCode ?? 0;
        public bool HasError(ServoAxis axis) => GetAxisStatus(axis)?.HasError ?? false;
        
        public bool HasAnyError() => _axisStatuses.Values.Any(s => s.HasError);
        public List<ServoAxis> GetAxesWithErrors() => _axisStatuses.Where(kvp => kvp.Value.HasError).Select(kvp => kvp.Key).ToList();
        public bool IsAxisMoving(ServoAxis axis) => GetAxisStatus(axis)?.IsMoving ?? false;
        public bool IsAxisHomed(ServoAxis axis) => GetAxisStatus(axis)?.ORGFound ?? false;

        public void ForceUpdate()
        {
            foreach (ServoAxis axis in Enum.GetValues(typeof(ServoAxis)))
            {
                UpdateAxisFromPLC(axis);
            }
        }

        private void OnPlcTagChanged(object sender, TagChangedEventArgs e)
        {
            if (!_isMonitoring) return;

            string[] parts = e.TagName.Split('_');
            if (parts.Length < 2) return;

            string axisName = parts[0];
            string paramName = string.Join("_", parts.Skip(1));

            var axisStatus = _axisStatuses.Values.FirstOrDefault(s => s.AxisName == axisName);
            if (axisStatus == null) return;

            try { UpdateStatusFromTagValue(axisStatus, paramName, e.NewValue); }
            catch (Exception ex) { Utils.Logger.Error("ServoMonitor", $"Error updating status for {e.TagName}: {ex.Message}"); }
        }

        private void UpdateStatusFromTagValue(ServoAxisStatus status, string paramName, object value)
        {
            switch (paramName)
            {
                case "CurrentPosition": status.CurrentPosition = Convert.ToDouble(value); break;
                case "CurrentSpeed": status.CurrentSpeed = Convert.ToDouble(value); break;
                case "ErrorCode":
                    double oldError = status.ErrorCode;
                    double newError = Convert.ToDouble(value);
                    status.ErrorCode = newError;
                    if (oldError == 0 && newError != 0) ErrorDetected?.Invoke(this, new ServoErrorEventArgs(status.Axis, newError));
                    else if (oldError != 0 && newError == 0) ErrorCleared?.Invoke(this, new ServoErrorEventArgs(status.Axis, 0));
                    break;
                case "OperationStatus": status.OperationStatus = Convert.ToDouble(value); break;
                case "ORGFound": status.ORGFound = Convert.ToUInt16(value) != 0; break;
                case "MoveCompleted":
                    bool oldMoveCompleted = status.MoveCompleted;
                    bool newMoveCompleted = Convert.ToUInt16(value) != 0;
                    status.MoveCompleted = newMoveCompleted;
                    if (!oldMoveCompleted && newMoveCompleted) MoveCompleted?.Invoke(this, status.Axis);
                    break;
                case "TargetPosition": status.TargetPosition = Convert.ToDouble(value); break;
                case "TargetSpeed": status.TargetSpeed = Convert.ToDouble(value); break;
                case "CurrentPoint": status.CurrentPoint = Convert.ToInt32(value); break;
            }
            status.LastUpdated = DateTime.Now;
        }

        private void UpdateAxisFromPLC(ServoAxis axis)
        {
            if (_plcService == null || !_plcService.IsConnected || !_axisStatuses.TryGetValue(axis, out var status)) return;

            string axisName = status.AxisName;
            try
            {
                status.CurrentPosition = _plcService.GetDoubleValue($"{axisName}_CurrentPosition");
                status.CurrentSpeed = _plcService.GetDoubleValue($"{axisName}_CurrentSpeed");
                
                double oldError = status.ErrorCode;
                double newError = _plcService.GetDoubleValue($"{axisName}_ErrorCode");
                status.ErrorCode = newError;
                if (oldError == 0 && newError != 0) ErrorDetected?.Invoke(this, new ServoErrorEventArgs(axis, newError));
                else if (oldError != 0 && newError == 0) ErrorCleared?.Invoke(this, new ServoErrorEventArgs(axis, 0));

                status.OperationStatus = _plcService.GetDoubleValue($"{axisName}_OperationStatus");
                status.ORGFound = _plcService.GetUInt16Value($"{axisName}_ORGFound") != 0;
                
                bool oldMoveCompleted = status.MoveCompleted;
                bool newMoveCompleted = _plcService.GetUInt16Value($"{axisName}_MoveCompleted") != 0;
                status.MoveCompleted = newMoveCompleted;
                if (!oldMoveCompleted && newMoveCompleted) MoveCompleted?.Invoke(this, axis);

                status.TargetPosition = _plcService.GetDoubleValue($"{axisName}_TargetPosition");
                status.TargetSpeed = _plcService.GetDoubleValue($"{axisName}_TargetSpeed");
                status.CurrentPoint = _plcService.GetUInt16Value($"{axisName}_CurrentPoint");
                status.LastUpdated = DateTime.Now;
            }
            catch (Exception ex) { Utils.Logger.Error("ServoMonitor", $"Error updating axis {axisName}: {ex.Message}"); }
        }

        private void OnAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ServoAxisStatus status)
                StatusChanged?.Invoke(this, new ServoStatusChangedEventArgs(status.Axis, status, e.PropertyName));
        }

        public string GetStatusSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Servo Monitor Status ===");
            sb.AppendLine($"Monitoring: {_isMonitoring}");
            sb.AppendLine();
            foreach (var status in _axisStatuses.Values)
            {
                sb.AppendLine($"{status.AxisName} ({status.UnitName}):");
                sb.AppendLine($"  Position: {status.CurrentPosition:F3}");
                sb.AppendLine($"  Speed: {status.CurrentSpeed:F1}");
                sb.AppendLine($"  Error: {status.ErrorCode} {(status.HasError ? "[ERROR]" : "")}");
                sb.AppendLine($"  ORG: {(status.ORGFound ? "Yes" : "No")}");
                sb.AppendLine($"  Moving: {(status.IsMoving ? "Yes" : "No")}");
                sb.AppendLine($"  Last Update: {status.LastUpdated:HH:mm:ss.fff}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public float GetServoPosition(string robotName, string positionName, string axisName)
        {
            ServoAxis? axis = MapToServoAxis(robotName, axisName);
            return axis.HasValue ? (float)GetCurrentPosition(axis.Value) : 0f;
        }

        private ServoAxis? MapToServoAxis(string robotName, string axisName)
        {
            string robot = robotName?.ToLower() ?? "";
            string axis = axisName?.ToUpper() ?? "";
            switch (robot)
            {
                case "infeed":
                    switch (axis) { case "X": return ServoAxis.X1; case "Y": return ServoAxis.Y1; case "C": return ServoAxis.C1; }
                    break;
                case "transfer":
                    switch (axis) { case "X": return ServoAxis.X2; case "Z": return ServoAxis.Z2; }
                    break;
                case "outfeed":
                    switch (axis) { case "X": return ServoAxis.X3; case "Y": return ServoAxis.Y3; }
                    break;
                case "inspect1":
                    switch (axis) { case "Z": return ServoAxis.Z4; case "C": case "R": return ServoAxis.C4; }
                    break;
                case "inspect2":
                    switch (axis) { case "Z": return ServoAxis.Z5; case "C": case "R": return ServoAxis.C5; }
                    break;
            }
            return null;
        }

        public void Connect() { if (_plcService?.IsConnected == true) StartMonitoring(); }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopMonitoring();
            if (_plcService != null) _plcService.TagChanged -= OnPlcTagChanged;
            foreach (var status in _axisStatuses.Values) status.PropertyChanged -= OnAxisPropertyChanged;
            _axisStatuses.Clear();
        }
    }
}



