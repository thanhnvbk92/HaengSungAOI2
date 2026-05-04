using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace HaengSungAOI_WPF.Utils
{
    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Fatal,
        Critical
    }

    /// <summary>
    /// Log entry structure
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public string ThreadId { get; set; }

        public LogEntry()
        {
            Timestamp = DateTime.Now;
            ThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        }

        public override string ToString()
        {
            string exceptionInfo = Exception != null ? $" | Exception: {Exception.Message}" : "";
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} | {Level} | Thread-{ThreadId} | {Source} | {Message}{exceptionInfo}";
        }
    }

    /// <summary>
    /// Simple, thread-safe logging manager for the AOI system
    /// </summary>
    public class LogManager
    {
        private static LogManager _instance;
        private static readonly object _lock = new object();

        private readonly Queue<LogEntry> _logQueue = new Queue<LogEntry>();
        private readonly object _queueLock = new object();
        private readonly Timer _flushTimer;

        private string _logDirectory;
        private string _currentLogFile;
        private LogLevel _minimumLogLevel = LogLevel.Info;
        private bool _consoleLoggingEnabled = true;
        private bool _fileLoggingEnabled = true;
        private int _maxLogFiles = 30; // Keep 30 days of logs
        private volatile bool _isDisposed = false;


        public static event EventHandler<LogEntry> OnLogEntry;

        /// <summary>
        /// Get the singleton instance of LogManager
        /// </summary>
        public static LogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton pattern
        /// </summary>
        private LogManager()
        {
            try
            {
                // Read log directory from App.config if available
                string configDir = ConfigurationManager.AppSettings["VisionLogDir"];
                if (!string.IsNullOrEmpty(configDir))
                {
                    _logDirectory = configDir;
                }

                // Ensure log directory exists
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
                
                // Set up current log file
                UpdateLogFile();
                
                // Clean up old log files
                CleanupOldLogs();
                
                // Set up flush timer (every 5 seconds)
                _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                
                // Log startup
                LogInfo("LogManager", "Logging system initialized");
            }
            catch (Exception ex)
            {
                // If we can't initialize file logging, at least try console
                Console.WriteLine($"Failed to initialize LogManager: {ex.Message}");
                _fileLoggingEnabled = false;
            }
        }

        /// <summary>
        /// Log directory path
        /// </summary>
        public string LogDirectory 
        { 
            get => _logDirectory; 
            set 
            { 
                _logDirectory = value;
                Directory.CreateDirectory(_logDirectory);
                UpdateLogFile();
            } 
        }

        /// <summary>
        /// Minimum log level to record
        /// </summary>
        public LogLevel MinimumLogLevel 
        { 
            get => _minimumLogLevel; 
            set => _minimumLogLevel = value; 
        }

        /// <summary>
        /// Enable/disable console logging
        /// </summary>
        public bool ConsoleLoggingEnabled 
        { 
            get => _consoleLoggingEnabled; 
            set => _consoleLoggingEnabled = value; 
        }

        /// <summary>
        /// Enable/disable file logging
        /// </summary>
        public bool FileLoggingEnabled 
        { 
            get => _fileLoggingEnabled; 
            set => _fileLoggingEnabled = value; 
        }

        /// <summary>
        /// Maximum number of log files to keep
        /// </summary>
        public int MaxLogFiles 
        { 
            get => _maxLogFiles; 
            set => _maxLogFiles = value; 
        }

        /// <summary>
        /// Log a trace message
        /// </summary>
        public void LogTrace(string source, string message, Exception exception = null)
        {
            Log(LogLevel.Trace, source, message, exception);
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public void LogDebug(string source, string message, Exception exception = null)
        {
            Log(LogLevel.Debug, source, message, exception);
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        public void LogInfo(string source, string message, Exception exception = null)
        {
            Log(LogLevel.Info, source, message, exception);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void LogWarning(string source, string message, Exception exception = null)
        {
            Log(LogLevel.Warning, source, message, exception);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void LogError(string source, string message, Exception exception = null)
        {
            Log(LogLevel.Error, source, message, exception);
        }

        /// <summary>
        /// Log a fatal error message
        /// </summary>
        public void LogFatal(string source, string message, Exception exception = null)
        {
            Log(LogLevel.Fatal, source, message, exception);
        }

        /// <summary>
        /// Log a critical error message
        /// </summary>
        public void LogCritical(string source, string message, Exception exception = null)
        {
            Log(LogLevel.Critical, source, message, exception);
        }

        /// <summary>
        /// Main logging method
        /// </summary>
        private void Log(LogLevel level, string source, string message, Exception exception = null)
        {
            if (_isDisposed || level < _minimumLogLevel)
                return;

            try
            {
                var logEntry = new LogEntry
                {
                    Level = level,
                    Source = source ?? "Unknown",
                    Message = message ?? "No message",
                    Exception = exception
                };

                // Console logging (immediate)
                if (_consoleLoggingEnabled)
                {
                    Console.WriteLine(logEntry.ToString());
                }

                // File logging (queued)
                if (_fileLoggingEnabled)
                {
                    lock (_queueLock)
                    {
                        //_logQueue.Enqueue(logEntry);
                        if (_logQueue.Count < 10000)
                        {
                            _logQueue.Enqueue(logEntry);
                        }
                    }
                }

                // Raise event
                OnLogEntry?.Invoke(this, logEntry);
            }
            catch (Exception ex)
            {
                // Last resort - try to at least get it to console
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Flush pending log entries to file
        /// </summary>
        private void FlushLogs(object state)
        {
            if (_isDisposed || !_fileLoggingEnabled)
                return;

            List<LogEntry> entries;
            
            // Get all queued entries
            lock (_queueLock)
            {
                if (_logQueue.Count == 0)
                    return;
                    
                entries = new List<LogEntry>(_logQueue);
                _logQueue.Clear();
            }

            try
            {
                // Check if we need a new log file (daily rotation)
                if (ShouldRotateLog())
                {
                    UpdateLogFile();
                    CleanupOldLogs();
                }

                // Write all entries to file
                using (var writer = new StreamWriter(_currentLogFile, true))
                {
                    foreach (var entry in entries)
                    {
                        writer.WriteLine(entry.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
                
                // Put entries back in queue for retry
                lock (_queueLock)
                {
                    foreach (var entry in entries)
                    {
                        _logQueue.Enqueue(entry);
                    }
                }
            }
        }

        /// <summary>
        /// Check if log should be rotated (daily)
        /// </summary>
        private bool ShouldRotateLog()
        {
            if (string.IsNullOrEmpty(_currentLogFile) || !File.Exists(_currentLogFile))
                return true;

            var fileInfo = new FileInfo(_currentLogFile);
            // Rotate nếu khác ngày HOẶC khác giờ
            return fileInfo.CreationTime.Date < DateTime.Now.Date || fileInfo.CreationTime.Hour < DateTime.Now.Hour;
        }

        /// <summary>
        /// Update the current log file path
        /// </summary>
        private void UpdateLogFile()
        {
            // Tạo thư mục con theo ngày (yyyyMMdd) trong thư mục log chính
            string dailyFolderPath = Path.Combine(_logDirectory, DateTime.Now.ToString("yyyyMMdd"));
            
            if (!Directory.Exists(dailyFolderPath))
            {
                Directory.CreateDirectory(dailyFolderPath);
            }

            // Tách log theo giờ: AOI_HH.log (ví dụ AOI_08.log)
            string fileName = $"AOI_{DateTime.Now:HH}.log";
            _currentLogFile = Path.Combine(dailyFolderPath, fileName);
        }

        /// <summary>
        /// Clean up old log files
        /// </summary>
        private void CleanupOldLogs()
        {
            try
            {
                if (!Directory.Exists(_logDirectory)) return;

                // Lấy tất cả các thư mục con (định dạng yyyyMMdd)
                var directories = Directory.GetDirectories(_logDirectory);
                
                if (directories.Length <= _maxLogFiles)
                    return;

                // Sắp xếp theo tên (yyyyMMdd)
                Array.Sort(directories);
                
                int foldersToDelete = directories.Length - _maxLogFiles;
                for (int i = 0; i < foldersToDelete; i++)
                {
                    try
                    {
                        // Xóa cả thư mục và file bên trong
                        Directory.Delete(directories[i], true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete old log directory {directories[i]}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cleanup old logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Immediately flush all pending logs
        /// </summary>
        public void Flush()
        {
            FlushLogs(null);
        }

        /// <summary>
        /// Dispose the log manager
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            
            try
            {
                // Flush any remaining logs
                Flush();
                
                // Dispose timer
                _flushTimer?.Dispose();
                
                LogInfo("LogManager", "Logging system disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing LogManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Get recent log entries from the current log file
        /// </summary>
        /// <param name="maxLines">Maximum number of lines to return</param>
        /// <returns>Recent log entries</returns>
        public List<string> GetRecentLogs(int maxLines = 100)
        {
            var logs = new List<string>();
            
            if (!File.Exists(_currentLogFile))
                return logs;

            try
            {
                var allLines = File.ReadAllLines(_currentLogFile);
                int startIndex = Math.Max(0, allLines.Length - maxLines);
                
                for (int i = startIndex; i < allLines.Length; i++)
                {
                    logs.Add(allLines[i]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read recent logs: {ex.Message}");
            }

            return logs;
        }

        /// <summary>
        /// Get log file information
        /// </summary>
        /// <returns>Dictionary with log file info</returns>
        public Dictionary<string, object> GetLogInfo()
        {
            var info = new Dictionary<string, object>
            {
                ["LogDirectory"] = _logDirectory,
                ["CurrentLogFile"] = _currentLogFile,
                ["MinimumLogLevel"] = _minimumLogLevel,
                ["ConsoleLoggingEnabled"] = _consoleLoggingEnabled,
                ["FileLoggingEnabled"] = _fileLoggingEnabled,
                ["MaxLogFiles"] = _maxLogFiles,
                ["QueuedEntries"] = 0
            };

            lock (_queueLock)
            {
                info["QueuedEntries"] = _logQueue.Count;
            }

            if (File.Exists(_currentLogFile))
            {
                var fileInfo = new FileInfo(_currentLogFile);
                info["CurrentLogFileSize"] = fileInfo.Length;
                info["CurrentLogFileCreated"] = fileInfo.CreationTime;
            }

            return info;
        }
    }

    /// <summary>
    /// Static helper class for easier logging access
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Log a trace message
        /// </summary>
        public static void Trace(string source, string message, Exception exception = null)
        {
            LogManager.Instance.LogTrace(source, message, exception);
        }
        
        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void Debug(string source, string message, Exception exception = null)
        {
            LogManager.Instance.LogDebug(source, message, exception);
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        public static void Info(string source, string message, Exception exception = null)
        {
            LogManager.Instance.LogInfo(source, message, exception);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warning(string source, string message, Exception exception = null)
        {
            LogManager.Instance.LogWarning(source, message, exception);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string source, string message, Exception exception = null)
        {
            LogManager.Instance.LogError(source, message, exception);
        }

        /// <summary>
        /// Log a fatal error message
        /// </summary>
        public static void Fatal(string source, string message, Exception exception = null)
        {
            LogManager.Instance.LogFatal(source, message, exception);
        }

        /// <summary>
        /// Log a critical error message
        /// </summary>
        public static void Critical(string source, string message, Exception exception = null)
        {
            LogManager.Instance.LogCritical(source, message, exception);
        }

    }
}


