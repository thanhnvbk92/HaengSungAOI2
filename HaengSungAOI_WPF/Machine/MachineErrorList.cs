using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using HaengSungAOI_WPF.Services;

namespace HaengSungAOI_WPF.Machine
{

    /// <summary>
    /// Represents a single error event in the machine system
    /// </summary>
    public class MachineError : INotifyPropertyChanged
    {
        private DateTime _timestamp;
        private ErrorType _errorType;
        private string _source;
        private string _message;
        private string _details;
        private bool _acknowledged;
        private Exception _exception;

        /// <summary>
        /// When the error occurred
        /// </summary>
        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged(nameof(Timestamp));
            }
        }

        /// <summary>
        /// The type of error
        /// </summary>
        public ErrorType ErrorType
        {
            get => _errorType;
            set
            {
                _errorType = value;
                OnPropertyChanged(nameof(ErrorType));
            }
        }

        /// <summary>
        /// Source of the error (component, robot, subsystem)
        /// </summary>
        public string Source
        {
            get => _source;
            set
            {
                _source = value;
                OnPropertyChanged(nameof(Source));
            }
        }

        /// <summary>
        /// Short error message
        /// </summary>
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        /// <summary>
        /// Detailed error information
        /// </summary>
        public string Details
        {
            get => _details;
            set
            {
                _details = value;
                OnPropertyChanged(nameof(Details));
            }
        }

        /// <summary>
        /// Whether the error has been acknowledged by an operator
        /// </summary>
        public bool Acknowledged
        {
            get => _acknowledged;
            set
            {
                _acknowledged = value;
                OnPropertyChanged(nameof(Acknowledged));
            }
        }

        /// <summary>
        /// Associated exception (if any)
        /// </summary>
        public Exception Exception
        {
            get => _exception;
            set
            {
                _exception = value;
                OnPropertyChanged(nameof(Exception));
            }
        }

        /// <summary>
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Creates a formatted string representation of the error
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {ErrorType}: {Source} - {Message}";
        }
    }

    /// <summary>
    /// Event arguments for error events
    /// </summary>
    public class MachineErrorEventArgs : EventArgs
    {
        /// <summary>
        /// The error that occurred
        /// </summary>
        public MachineError Error { get; private set; }

        /// <summary>
        /// Creates a new instance of MachineErrorEventArgs
        /// </summary>
        /// <param name="error">The error that occurred</param>
        public MachineErrorEventArgs(MachineError error)
        {
            Error = error;
        }
    }

    /// <summary>
    /// Simplified error tracking system for the machine
    /// Errors are only stored in memory and not persisted when app is closed
    /// </summary>
    public class MachineErrorList : INotifyPropertyChanged
    {
        // Singleton instance
        private static MachineErrorList _instance;

        // Thread-safe lock for instance creation
        private static readonly object _instanceLock = new object();

        // Thread-safe collection for errors
        private readonly ObservableCollection<MachineError> _errors;

        // Backing field for the public Errors property
        private readonly ReadOnlyObservableCollection<MachineError> _readOnlyErrors;

        // Settings
        private int _maxErrorCount = 100;  // Reduced from 1000
        private readonly ReaderWriterLockSlim _errorsLock = new ReaderWriterLockSlim();

        // Dictionary to track active error signatures to avoid duplicates
        private readonly Dictionary<string, DateTime> _activeErrorSignatures = new Dictionary<string, DateTime>();
        private readonly ReaderWriterLockSlim _signaturesLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Read-only collection of all recorded errors
        /// </summary>
        public ReadOnlyObservableCollection<MachineError> Errors => _readOnlyErrors;

        /// <summary>
        /// Count of unacknowledged errors (Information level errors are automatically acknowledged when created)
        /// </summary>
        public int UnacknowledgedErrorCount
        {
            get
            {
                _errorsLock.EnterReadLock();
                try
                {
                    // Count ALL unacknowledged errors
                    // Note: Information level errors are automatically acknowledged when created
                    return _errors.Count(e => !e.Acknowledged);
                }
                finally
                {
                    _errorsLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Count of unacknowledged critical errors
        /// </summary>
        public int UnacknowledgedCriticalErrorCount
        {
            get
            {
                _errorsLock.EnterReadLock();
                try
                {
                    return _errors.Count(e => !e.Acknowledged && e.ErrorType == ErrorType.Critical);
                }
                finally
                {
                    _errorsLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Count of all errors
        /// </summary>
        public int ErrorCount
        {
            get
            {
                _errorsLock.EnterReadLock();
                try
                {
                    return _errors.Count;
                }
                finally
                {
                    _errorsLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Maximum number of errors to keep in memory
        /// </summary>
        public int MaxErrorCount
        {
            get => _maxErrorCount;
            set
            {
                _maxErrorCount = value;
                TrimErrorList();
                OnPropertyChanged(nameof(MaxErrorCount));
            }
        }

        /// <summary>
        /// Event fired when a new error is added
        /// </summary>
        public event EventHandler<MachineErrorEventArgs> ErrorAdded;

        /// <summary>
        /// Event fired when a critical error is added
        /// </summary>
        public event EventHandler<MachineErrorEventArgs> CriticalErrorAdded;

        /// <summary>
        /// Event for property changed notifications
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private MachineErrorList()
        {
            _errors = new ObservableCollection<MachineError>();
            _readOnlyErrors = new ReadOnlyObservableCollection<MachineError>(_errors);
        }

        /// <summary>
        /// Get the singleton instance of MachineErrorList
        /// </summary>
        public static MachineErrorList Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MachineErrorList();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Reset the singleton instance - useful for testing or resetting state
        /// </summary>
        public static void ResetInstance()
        {
            lock (_instanceLock)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Generate a signature for an error to detect duplicates
        /// </summary>
        private string GenerateErrorSignature(ErrorType errorType, string source, string message, Exception exception)
        {
            // We'll use a combination of the error type, source, message, and exception type if present
            string exceptionType = exception?.GetType().FullName ?? "NoException";

            // Handle null values gracefully
            source = source ?? "NoSource";
            message = message ?? "NoMessage";

            // Create a signature that uniquely identifies this error
            return $"{errorType}|{source}|{message}|{exceptionType}";
        }

        /// <summary>
        /// Check if an error with this signature already exists and is still active
        /// </summary>
        private bool IsErrorAlreadyActive(string signature)
        {
            _signaturesLock.EnterReadLock();
            try
            {
                return _activeErrorSignatures.ContainsKey(signature);
            }
            finally
            {
                _signaturesLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Add a new error to the list, but only if it's not a duplicate of an existing error
        /// </summary>
        /// <param name="errorType">Type of error</param>
        /// <param name="source">Source of the error (e.g., robot name, component)</param>
        /// <param name="message">Short error message</param>
        /// <param name="details">Detailed error information</param>
        /// <param name="exception">Associated exception (if any)</param>
        /// <returns>The added error instance, or null if the error was a duplicate</returns>
        public MachineError AddError(ErrorType errorType, string source, string message, string details = null, Exception exception = null)
        {
            // Information messages are always added, no need to track duplicates
            bool shouldCheckDuplicates = errorType >= ErrorType.Warning;
            string signature = null;

            // Only check for duplicates if this is an actual error (not just info)
            if (shouldCheckDuplicates)
            {
                signature = GenerateErrorSignature(errorType, source, message, exception);

                // Check if this error already exists
                if (IsErrorAlreadyActive(signature))
                {
                    // This error is already being tracked, don't add it again
                    Console.WriteLine($"Duplicate error ignored: {errorType}: {source} - {message}");
                    return null;
                }
            }

            var error = new MachineError
            {
                Timestamp = DateTime.Now,
                ErrorType = errorType,
                Source = source,
                Message = message,
                Details = details ?? string.Empty,
                Exception = exception,
                // Automatically acknowledge Information level errors, others require manual acknowledgment
                Acknowledged = errorType == ErrorType.Information
            };

            _errorsLock.EnterWriteLock();
            try
            {
                _errors.Add(error);
                TrimErrorList();
            }
            finally
            {
                _errorsLock.ExitWriteLock();
            }

            // If this is an error we need to track, add it to the signatures dictionary
            if (shouldCheckDuplicates && signature != null)
            {
                _signaturesLock.EnterWriteLock();
                try
                {
                    _activeErrorSignatures[signature] = DateTime.Now;
                }
                finally
                {
                    _signaturesLock.ExitWriteLock();
                }
            }

            // Log to console immediately with acknowledgment status
            Console.WriteLine($"{error.ToString()} - {(error.Acknowledged ? "Auto-Acknowledged" : "Requires Acknowledgment")}");

            // Raise events
            OnErrorAdded(error);
            if (errorType == ErrorType.Critical)
            {
                OnCriticalErrorAdded(error);
            }

            // Update property notifications
            OnPropertyChanged(nameof(UnacknowledgedErrorCount));
            OnPropertyChanged(nameof(UnacknowledgedCriticalErrorCount));
            OnPropertyChanged(nameof(ErrorCount));

            return error;
        }

        /// <summary>
        /// Add a collision error
        /// </summary>
        /// <param name="source">Source of the collision (e.g., "Axis X1 and X2")</param>
        /// <param name="message">Collision description</param>
        /// <param name="details">Additional collision details</param>
        /// <returns>The added error instance</returns>
        public MachineError AddCollision(string source, string message, string details = null)
        {
            return AddError(ErrorType.Collision, source, message, details);
        }

        /// <summary>
        /// Add a timeout error
        /// </summary>
        /// <param name="source">Source of the timeout</param>
        /// <param name="operation">Operation that timed out</param>
        /// <param name="timeoutMs">Timeout duration in milliseconds</param>
        /// <returns>The added error instance</returns>
        public MachineError AddTimeout(string source, string operation, int timeoutMs)
        {
            return AddError(
                ErrorType.Timeout,
                source,
                $"Operation timed out: {operation}",
                $"The operation exceeded the maximum allowed time of {timeoutMs}ms."
            );
        }

        /// <summary>
        /// Add a hardware error
        /// </summary>
        /// <param name="source">Source of the hardware error</param>
        /// <param name="message">Error message</param>
        /// <param name="details">Additional details</param>
        /// <param name="exception">Associated exception (if any)</param>
        /// <returns>The added error instance</returns>
        public MachineError AddHardwareError(string source, string message, string details = null, Exception exception = null)
        {
            return AddError(ErrorType.Hardware, source, message, details, exception);
        }

        /// <summary>
        /// Add a robot error
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <param name="message">Error message</param>
        /// <param name="details">Additional details</param>
        /// <param name="exception">Associated exception (if any)</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotError(string robotName, string message, string details = null, Exception exception = null)
        {
            return AddError(ErrorType.Robot, robotName, message, details, exception);
        }

        /// <summary>
        /// Add a vision system error
        /// </summary>
        /// <param name="cameraName">Name of the camera or vision procedure</param>
        /// <param name="message">Error message</param>
        /// <param name="details">Additional details</param>
        /// <param name="exception">Associated exception (if any)</param>
        /// <returns>The added error instance</returns>
        public MachineError AddVisionError(string cameraName, string message, string details = null, Exception exception = null)
        {
            return AddError(ErrorType.Vision, cameraName, message, details, exception);
        }

        /// <summary>
        /// Add a PLC system error
        /// </summary>
        /// <param name="source">Source within PLC system</param>
        /// <param name="message">Error message</param>
        /// <param name="details">Additional details</param>
        /// <param name="exception">Associated exception (if any)</param>
        /// <returns>The added error instance</returns>
        public MachineError AddPLCError(string source, string message, string details = null, Exception exception = null)
        {
            return AddError(ErrorType.PLC, source, message, details, exception);
        }

        /// <summary>
        /// Add a safety system error (critical)
        /// </summary>
        /// <param name="source">Source within safety system</param>
        /// <param name="message">Error message</param>
        /// <param name="details">Additional details</param>
        /// <param name="exception">Associated exception (if any)</param>
        /// <returns>The added error instance</returns>
        public MachineError AddSafetyError(string source, string message, string details = null, Exception exception = null)
        {
            return AddError(ErrorType.Safety, source, message, details, exception);
        }

        /// <summary>
        /// Add an information level message (automatically acknowledged)
        /// </summary>
        /// <param name="source">Source of the information</param>
        /// <param name="message">Information message</param>
        /// <param name="details">Additional details</param>
        /// <returns>The added error instance</returns>
        public MachineError AddInformation(string source, string message, string details = null)
        {
            return AddError(ErrorType.Information, source, message, details);
        }

        /// <summary>
        /// Add a general exception
        /// </summary>
        /// <param name="source">Source of the exception</param>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception that occurred</param>
        /// <returns>The added error instance</returns>
        public MachineError AddException(string source, string message, Exception exception)
        {
            // Determine error type based on exception
            var errorType = ErrorType.Error;

            if (exception is TimeoutException)
                errorType = ErrorType.Timeout;
            else if (exception is System.IO.IOException || exception is System.IO.FileNotFoundException)
                errorType = ErrorType.System;
            else if (exception is System.Net.WebException || exception is System.Net.Sockets.SocketException)
                errorType = ErrorType.Communication;

            string details = $"Exception type: {exception.GetType().Name}\n" +
                             $"Message: {exception.Message}\n" +
                             $"Stack trace: {exception.StackTrace}";

            return AddError(errorType, source, message, details, exception);
        }

        /// <summary>
        /// Mark all errors as acknowledged
        /// </summary>
        public void AcknowledgeAllErrors()
        {
            _errorsLock.EnterWriteLock();
            try
            {
                foreach (var error in _errors.Where(e => !e.Acknowledged))
                {
                    error.Acknowledged = true;
                }
            }
            finally
            {
                _errorsLock.ExitWriteLock();
            }

            // Clear the signature dictionary so all errors can be recognized again if they reoccur
            _signaturesLock.EnterWriteLock();
            try
            {
                _activeErrorSignatures.Clear();
            }
            finally
            {
                _signaturesLock.ExitWriteLock();
            }

            OnPropertyChanged(nameof(UnacknowledgedErrorCount));
            OnPropertyChanged(nameof(UnacknowledgedCriticalErrorCount));
        }

        /// <summary>
        /// Acknowledge a specific error
        /// </summary>
        /// <param name="error">The error to acknowledge</param>
        public void AcknowledgeError(MachineError error)
        {
            if (error == null)
                return;

            error.Acknowledged = true;
            
            // Remove from signature dictionary so this error can be recognized again if it reoccurs
            string signature = GenerateErrorSignature(error.ErrorType, error.Source, error.Message, error.Exception);
            _signaturesLock.EnterWriteLock();
            try
            {
                if (_activeErrorSignatures.ContainsKey(signature))
                {
                    _activeErrorSignatures.Remove(signature);
                }
            }
            finally
            {
                _signaturesLock.ExitWriteLock();
            }
            
            OnPropertyChanged(nameof(UnacknowledgedErrorCount));
            OnPropertyChanged(nameof(UnacknowledgedCriticalErrorCount));
        }

        /// <summary>
        /// Clear all errors from the list and reset the duplicate detection
        /// </summary>
        public void ClearAllErrors()
        {
            _errorsLock.EnterWriteLock();
            try
            {
                _errors.Clear();
            }
            finally
            {
                _errorsLock.ExitWriteLock();
            }

            // Clear the signature dictionary so errors can be recognized again
            _signaturesLock.EnterWriteLock();
            try
            {
                _activeErrorSignatures.Clear();
            }
            finally
            {
                _signaturesLock.ExitWriteLock();
            }

            OnPropertyChanged(nameof(UnacknowledgedErrorCount));
            OnPropertyChanged(nameof(UnacknowledgedCriticalErrorCount));
            OnPropertyChanged(nameof(ErrorCount));
        }

        /// <summary>
        /// Clear specific error and allow it to be recognized again
        /// </summary>
        /// <param name="error">The error to clear</param>
        public void ClearError(MachineError error)
        {
            if (error == null)
                return;

            string signature = GenerateErrorSignature(error.ErrorType, error.Source, error.Message, error.Exception);

            _errorsLock.EnterWriteLock();
            try
            {
                _errors.Remove(error);
            }
            finally
            {
                _errorsLock.ExitWriteLock();
            }

            // Remove from signature dictionary so this error can be recognized again
            _signaturesLock.EnterWriteLock();
            try
            {
                if (_activeErrorSignatures.ContainsKey(signature))
                {
                    _activeErrorSignatures.Remove(signature);
                }
            }
            finally
            {
                _signaturesLock.ExitWriteLock();
            }

            OnPropertyChanged(nameof(UnacknowledgedErrorCount));
            OnPropertyChanged(nameof(UnacknowledgedCriticalErrorCount));
            OnPropertyChanged(nameof(ErrorCount));
        }

        /// <summary>
        /// Get errors filtered by various criteria
        /// </summary>
        /// <param name="errorType">Optional error type filter</param>
        /// <param name="source">Optional source filter</param>
        /// <param name="acknowledgedState">Optional acknowledged state filter (null for both)</param>
        /// <param name="startTime">Optional start time for time range</param>
        /// <param name="endTime">Optional end time for time range</param>
        /// <returns>Filtered list of errors</returns>
        public IEnumerable<MachineError> GetFilteredErrors(
            ErrorType? errorType = null,
            string source = null,
            bool? acknowledgedState = null,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            _errorsLock.EnterReadLock();
            try
            {
                var query = _errors.AsEnumerable();

                if (errorType.HasValue)
                    query = query.Where(e => e.ErrorType == errorType.Value);

                if (!string.IsNullOrEmpty(source))
                    query = query.Where(e => e.Source.Contains(source));

                if (acknowledgedState.HasValue)
                    query = query.Where(e => e.Acknowledged == acknowledgedState.Value);

                if (startTime.HasValue)
                    query = query.Where(e => e.Timestamp >= startTime.Value);

                if (endTime.HasValue)
                    query = query.Where(e => e.Timestamp <= endTime.Value);

                return query.ToList(); // Create a copy to avoid thread safety issues
            }
            finally
            {
                _errorsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Check if any critical errors exist
        /// </summary>
        /// <returns>True if critical errors exist</returns>
        public bool HasCriticalErrors()
        {
            _errorsLock.EnterReadLock();
            try
            {
                return _errors.Any(e => e.ErrorType == ErrorType.Critical && !e.Acknowledged);
            }
            finally
            {
                _errorsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Check if a specific type of error exists from a source
        /// </summary>
        /// <param name="errorType">Type of error to check for</param>
        /// <param name="source">Source to filter by</param>
        /// <returns>True if matching errors exist</returns>
        public bool HasErrorOfType(ErrorType errorType, string source = null)
        {
            _errorsLock.EnterReadLock();
            try
            {
                var query = _errors.Where(e => e.ErrorType == errorType && !e.Acknowledged);

                if (!string.IsNullOrEmpty(source))
                    query = query.Where(e => e.Source.Contains(source));

                return query.Any();
            }
            finally
            {
                _errorsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Add an axis alarm error (critical level)
        /// </summary>
        /// <param name="axisName">Name of the axis</param>
        /// <param name="message">Alarm message</param>
        /// <param name="details">Detailed alarm information</param>
        /// <returns>The added error instance</returns>
        public MachineError AddAxisAlarm(string axisName, string message, string details = null)
        {
            return AddError(ErrorType.Critical, $"Axis {axisName}", $"ALARM: {message}", details);
        }

        /// <summary>
        /// Add an axis error
        /// </summary>
        /// <param name="axisName">Name of the axis</param>
        /// <param name="message">Error message</param>
        /// <param name="details">Detailed error information</param>
        /// <returns>The added error instance</returns>
        public MachineError AddAxisError(string axisName, string message, string details = null)
        {
            return AddError(ErrorType.Hardware, $"Axis {axisName}", $"ERROR: {message}", details);
        }

        /// <summary>
        /// Add an axis warning
        /// </summary>
        /// <param name="axisName">Name of the axis</param>
        /// <param name="message">Warning message</param>
        /// <param name="details">Detailed warning information</param>
        /// <returns>The added error instance</returns>
        public MachineError AddAxisWarning(string axisName, string message, string details = null)
        {
            return AddError(ErrorType.Warning, $"Axis {axisName}", $"WARNING: {message}", details);
        }

        /// <summary>
        /// Add an axis status information message
        /// </summary>
        /// <param name="axisName">Name of the axis</param>
        /// <param name="message">Information message</param>
        /// <param name="details">Additional details</param>
        /// <returns>The added error instance</returns>
        public MachineError AddAxisInfo(string axisName, string message, string details = null)
        {
            return AddError(ErrorType.Information, $"Axis {axisName}", message, details);
        }

        /// <summary>
        /// Add a robot critical alarm - used when robots encounter critical conditions that require immediate attention
        /// </summary>
        /// <param name="robotName">Name of the robot (e.g., "PCBInfeedRobot", "NGRobot")</param>
        /// <param name="alarmMessage">Critical alarm message</param>
        /// <param name="details">Detailed alarm information</param>
        /// <param name="exception">Associated exception (if any)</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotCriticalAlarm(string robotName, string alarmMessage, string details = null, Exception exception = null)
        {
            return AddError(ErrorType.Critical, $"Robot {robotName}", $"CRITICAL ALARM: {alarmMessage}", details, exception);
        }

        /// <summary>
        /// Add a robot safety alarm - used when robots detect safety violations (critical level)
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <param name="safetyMessage">Safety alarm message</param>
        /// <param name="details">Detailed safety information</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotSafetyAlarm(string robotName, string safetyMessage, string details = null)
        {
            return AddError(ErrorType.Safety, $"Robot {robotName}", $"SAFETY ALARM: {safetyMessage}", details);
        }

        /// <summary>
        /// Add a robot collision alarm (critical level)
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <param name="collisionDetails">Details about the collision</param>
        /// <param name="details">Additional collision information</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotCollisionAlarm(string robotName, string collisionDetails, string details = null)
        {
            return AddError(ErrorType.Collision, $"Robot {robotName}", $"COLLISION ALARM: {collisionDetails}", details);
        }

        /// <summary>
        /// Add a robot hardware failure alarm (critical level)
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <param name="hardwareComponent">Hardware component that failed</param>
        /// <param name="failureMessage">Failure message</param>
        /// <param name="details">Additional failure details</param>
        /// <param name="exception">Associated exception (if any)</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotHardwareFailure(string robotName, string hardwareComponent, string failureMessage, string details = null, Exception exception = null)
        {
            return AddError(ErrorType.Hardware, $"Robot {robotName}", $"HARDWARE FAILURE [{hardwareComponent}]: {failureMessage}", details, exception);
        }

        /// <summary>
        /// Add a robot timeout alarm (critical level for critical timeouts, error level for normal timeouts)
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <param name="operation">Operation that timed out</param>
        /// <param name="timeoutMs">Timeout duration in milliseconds</param>
        /// <param name="isCritical">Whether this timeout is critical (affects error type)</param>
        /// <param name="details">Additional timeout details</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotTimeoutAlarm(string robotName, string operation, int timeoutMs, bool isCritical = false, string details = null)
        {
            ErrorType errorType = isCritical ? ErrorType.Critical : ErrorType.Timeout;
            string message = isCritical ? $"CRITICAL TIMEOUT: {operation}" : $"TIMEOUT: {operation}";
            string timeoutDetails = $"The operation exceeded the maximum allowed time of {timeoutMs}ms.";
            
            if (!string.IsNullOrEmpty(details))
            {
                timeoutDetails += "\n" + details;
            }

            return AddError(errorType, $"Robot {robotName}", message, timeoutDetails);
        }

        /// <summary>
        /// Add a robot vacuum failure alarm (specific to vacuum-based robots)
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <param name="vacuumFailureType">Type of vacuum failure (e.g., "Lost Vacuum During Pickup", "Vacuum Not Detected")</param>
        /// <param name="details">Additional vacuum failure details</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotVacuumFailure(string robotName, string vacuumFailureType, string details = null)
        {
            return AddError(ErrorType.Error, $"Robot {robotName}", $"VACUUM FAILURE: {vacuumFailureType}", details);
        }

        /// <summary>
        /// Add a robot sequence error (when automatic sequences fail)
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <param name="sequenceName">Name of the sequence that failed</param>
        /// <param name="sequenceStep">Step where the sequence failed</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="details">Additional sequence error details</param>
        /// <param name="exception">Associated exception (if any)</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotSequenceError(string robotName, string sequenceName, string sequenceStep, string errorMessage, string details = null, Exception exception = null)
        {
            string message = $"SEQUENCE ERROR [{sequenceName}] at step [{sequenceStep}]: {errorMessage}";
            return AddError(ErrorType.Error, $"Robot {robotName}", message, details, exception);
        }

        /// <summary>
        /// Add a robot warning message
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <param name="warningMessage">Warning message</param>
        /// <param name="details">Additional warning details</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotWarning(string robotName, string warningMessage, string details = null)
        {
            return AddError(ErrorType.Warning, $"Robot {robotName}", $"WARNING: {warningMessage}", details);
        }

        /// <summary>
        /// Add a robot information message (automatically acknowledged)
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <param name="infoMessage">Information message</param>
        /// <param name="details">Additional information details</param>
        /// <returns>The added error instance</returns>
        public MachineError AddRobotInfo(string robotName, string infoMessage, string details = null)
        {
            return AddError(ErrorType.Information, $"Robot {robotName}", infoMessage, details);
        }

        /// <summary>
        /// Check if any axis has active alarms
        /// </summary>
        /// <returns>True if any axis alarm exists</returns>
        public bool HasAxisAlarms()
        {
            _errorsLock.EnterReadLock();
            try
            {
                return _errors.Any(e => e.Source.StartsWith("Axis ") && 
                                       e.ErrorType == ErrorType.Critical && 
                                       e.Message.StartsWith("ALARM:") && 
                                       !e.Acknowledged);
            }
            finally
            {
                _errorsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get all active axis alarms
        /// </summary>
        /// <returns>List of axis alarm errors</returns>
        public IEnumerable<MachineError> GetAxisAlarms()
        {
            return GetFilteredErrors(ErrorType.Critical)
                .Where(e => e.Source.StartsWith("Axis ") && e.Message.StartsWith("ALARM:") && !e.Acknowledged);
        }

        /// <summary>
        /// Get all axis-related errors (alarms, errors, warnings)
        /// </summary>
        /// <param name="axisName">Optional specific axis name filter</param>
        /// <returns>List of axis errors</returns>
        public IEnumerable<MachineError> GetAxisErrors(string axisName = null)
        {
            var query = Errors.Where(e => e.Source.StartsWith("Axis "));
            
            if (!string.IsNullOrEmpty(axisName))
            {
                query = query.Where(e => e.Source.Equals($"Axis {axisName}", StringComparison.OrdinalIgnoreCase));
            }
            
            return query.ToList();
        }

        /// <summary>
        /// Check if a specific robot has active critical alarms
        /// </summary>
        /// <param name="robotName">Name of the robot to check</param>
        /// <returns>True if the robot has active critical alarms</returns>
        public bool HasRobotCriticalAlarms(string robotName)
        {
            _errorsLock.EnterReadLock();
            try
            {
                return _errors.Any(e => e.Source.Equals($"Robot {robotName}", StringComparison.OrdinalIgnoreCase) && 
                                       (e.ErrorType == ErrorType.Critical || e.ErrorType == ErrorType.Safety) && 
                                       !e.Acknowledged);
            }
            finally
            {
                _errorsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Check if any robot has active critical alarms
        /// </summary>
        /// <returns>True if any robot has active critical alarms</returns>
        public bool HasAnyRobotCriticalAlarms()
        {
            _errorsLock.EnterReadLock();
            try
            {
                return _errors.Any(e => e.Source.StartsWith("Robot ") && 
                                       (e.ErrorType == ErrorType.Critical || e.ErrorType == ErrorType.Safety) && 
                                       !e.Acknowledged);
            }
            finally
            {
                _errorsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get all robot-related errors
        /// </summary>
        /// <param name="robotName">Optional specific robot name filter</param>
        /// <param name="errorType">Optional error type filter</param>
        /// <param name="acknowledgedState">Optional acknowledged state filter (null for both)</param>
        /// <returns>List of robot errors</returns>
        public IEnumerable<MachineError> GetRobotErrors(string robotName = null, ErrorType? errorType = null, bool? acknowledgedState = null)
        {
            _errorsLock.EnterReadLock();
            try
            {
                var query = _errors.Where(e => e.Source.StartsWith("Robot "));
                
                if (!string.IsNullOrEmpty(robotName))
                {
                    query = query.Where(e => e.Source.Equals($"Robot {robotName}", StringComparison.OrdinalIgnoreCase));
                }
                
                if (errorType.HasValue)
                {
                    query = query.Where(e => e.ErrorType == errorType.Value);
                }
                
                if (acknowledgedState.HasValue)
                {
                    query = query.Where(e => e.Acknowledged == acknowledgedState.Value);
                }
                
                return query.ToList(); // Create a copy to avoid thread safety issues
            }
            finally
            {
                _errorsLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get all active (unacknowledged) robot critical alarms
        /// </summary>
        /// <param name="robotName">Optional specific robot name filter</param>
        /// <returns>List of active robot critical alarms</returns>
        public IEnumerable<MachineError> GetActiveRobotCriticalAlarms(string robotName = null)
        {
            return GetRobotErrors(robotName, null, false)
                .Where(e => e.ErrorType == ErrorType.Critical || e.ErrorType == ErrorType.Safety);
        }

        /// <summary>
        /// Clear all robot errors for a specific robot
        /// </summary>
        /// <param name="robotName">Name of the robot to clear errors for</param>
        public void ClearRobotErrors(string robotName)
        {
            if (string.IsNullOrEmpty(robotName))
                return;

            var robotErrors = GetRobotErrors(robotName).ToList();
            
            foreach (var error in robotErrors)
            {
                ClearError(error);
            }
        }

        /// <summary>
        /// Acknowledge all robot errors for a specific robot
        /// </summary>
        /// <param name="robotName">Name of the robot to acknowledge errors for</param>
        public void AcknowledgeRobotErrors(string robotName)
        {
            if (string.IsNullOrEmpty(robotName))
                return;

            var robotErrors = GetRobotErrors(robotName, acknowledgedState: false).ToList();
            
            foreach (var error in robotErrors)
            {
                AcknowledgeError(error);
            }
        }

        /// <summary>
        /// Check if a robot can operate (no active critical alarms)
        /// </summary>
        /// <param name="robotName">Name of the robot to check</param>
        /// <returns>True if the robot can operate, false if it has active critical alarms</returns>
        public bool CanRobotOperate(string robotName)
        {
            return !HasRobotCriticalAlarms(robotName);
        }

        /// <summary>
        /// Get a summary of robot status (for diagnostics)
        /// </summary>
        /// <param name="robotName">Name of the robot</param>
        /// <returns>Summary string of robot error status</returns>
        public string GetRobotErrorSummary(string robotName)
        {
            if (string.IsNullOrEmpty(robotName))
                return "Invalid robot name";

            var robotErrors = GetRobotErrors(robotName).ToList();
            var criticalCount = robotErrors.Count(e => e.ErrorType == ErrorType.Critical && !e.Acknowledged);
            var safetyCount = robotErrors.Count(e => e.ErrorType == ErrorType.Safety && !e.Acknowledged);
            var errorCount = robotErrors.Count(e => e.ErrorType == ErrorType.Error && !e.Acknowledged);
            var warningCount = robotErrors.Count(e => e.ErrorType == ErrorType.Warning && !e.Acknowledged);
            var totalCount = robotErrors.Count;

            if (totalCount == 0)
                return $"Robot {robotName}: No errors";

            var summary = $"Robot {robotName}: {totalCount} total errors";
            if (criticalCount > 0) summary += $", {criticalCount} critical";
            if (safetyCount > 0) summary += $", {safetyCount} safety";
            if (errorCount > 0) summary += $", {errorCount} errors";
            if (warningCount > 0) summary += $", {warningCount} warnings";

            var canOperate = CanRobotOperate(robotName);
            summary += $" - {(canOperate ? "CAN OPERATE" : "CANNOT OPERATE")}";

            return summary;
        }

        // Helper method to trim the error list to the maximum size
        private void TrimErrorList()
        {
            if (_errors.Count > _maxErrorCount)
            {
                var excessCount = _errors.Count - _maxErrorCount;
                for (int i = 0; i < excessCount; i++)
                {
                    _errors.RemoveAt(0);
                }
            }
        }

        // Event invoker for ErrorAdded
        protected virtual void OnErrorAdded(MachineError error)
        {
            ErrorAdded?.Invoke(this, new MachineErrorEventArgs(error));
        }

        // Event invoker for CriticalErrorAdded
        protected virtual void OnCriticalErrorAdded(MachineError error)
        {
            CriticalErrorAdded?.Invoke(this, new MachineErrorEventArgs(error));
        }

        // Property changed notification
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}