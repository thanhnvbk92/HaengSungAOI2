using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentModbus;
using HaengSungAOI_WPF.Utils;
using HaengSungAOI_WPF.Machine.PLC;

namespace HaengSungAOI_WPF.Machine.PLC.PLC
{
    /// <summary>
    /// PLC data type for different Modbus functions
    /// </summary>
    public enum PLCDataType
    {
        Coil,              // Read/Write single bit (Function 01/05)
        DiscreteInput,     // Read-only single bit (Function 02)
        HoldingRegister,   // Read/Write 16-bit register (Function 03/06/16)
        InputRegister      // Read-only 16-bit register (Function 04)
    }

    /// <summary>
    /// Configuration for a PLC data point
    /// </summary>
    public class PLCDataPoint
    {
        public string Name { get; set; }
        public PLCDataType DataType { get; set; }
        public ushort Address { get; set; }
        public ushort Length { get; set; } = 1; // For registers: number of registers to read
        public string Description { get; set; }
        public object Value { get; set; }
        public object PreviousValue { get; set; }
        public bool HasChanged { get; set; }
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Monitoring group(s) this data point belongs to
        /// Can be combined using bitwise OR for multiple groups
        /// </summary>
        public PLCMonitoringGroup MonitoringGroup { get; set; } = PLCMonitoringGroup.All;

        /// <summary>
        /// Gets whether this data point should be monitored based on current active groups
        /// </summary>
        /// <param name="activeGroups">Currently active monitoring groups</param>
        /// <returns>True if this data point should be read</returns>
        public bool ShouldMonitor(PLCMonitoringGroup activeGroups)
        {
            // If data point belongs to "All", always monitor
            if (MonitoringGroup == PLCMonitoringGroup.All)
                return true;

            // Check if any of the data point's groups are active
            return (MonitoringGroup & activeGroups) != 0;
        }
    }

    /// <summary>
    /// Event arguments for PLC data changes
    /// </summary>
    public class PLCDataChangedEventArgs : EventArgs
    {
        public string DataPointName { get; set; }
        public PLCDataType DataType { get; set; }
        public ushort Address { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Event arguments for PLC connection status
    /// </summary>
    public class PLCConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// PLC Controller for Modbus TCP communication with sequential operations
    /// Reads periodically, writes on-demand (trigger-based) to avoid overwriting
    /// Uses dictionaries for easy configuration of data points
    /// Implements inter-command delay to prevent PLC poll cycle conflicts
    /// </summary>
    public class PLCController : IDisposable
    {
        #region Private Fields

        private ModbusTcpClient _tcpClient;
        private string _ipAddress;
        private int _port;
        private byte _unitIdentifier;

        private Timer _readTimer;
        private Timer _connectionCheckTimer;

        private int _readInterval = PLCConstants.PLC_READ_INTERVAL_MS; // milliseconds
        private int _connectionCheckInterval = PLCConstants.PLC_CONNECTION_CHECK_INTERVAL_MS; // milliseconds
        private int _interCommandDelay = PLCConstants.PLC_INTER_COMMAND_DELAY_MS; // milliseconds delay between write commands
        private int _writePauseDelay = PLCConstants.PLC_WRITE_PAUSE_DELAY_MS; // milliseconds delay to wait before/after writes

        private bool _isConnected = false;
        private bool _isRunning = false;
        private bool _disposed = false;
        private bool _isWriting = false; // Flag to pause reading during writes
        private bool _isReading = false; // Flag to prevent re-entry during reads

        private readonly object _lockObject = new object();
        private readonly object _connectionLock = new object();
        private readonly object _writeLock = new object(); // Separate lock for write operations
        private readonly object _readLock = new object(); // Separate lock for read operations
        private readonly SemaphoreSlim _ioSemaphore = new SemaphoreSlim(1, 1); // For async IO synchronization

        // Dictionary to store data point configurations
        private Dictionary<string, PLCDataPoint> _dataPoints;

        // Active monitoring groups
        private PLCMonitoringGroup _activeMonitoringGroups = PLCConstants.DEFAULT_MONITORING_GROUPS;

        // Timestamp tracking for inter-command delay
        private DateTime _lastWriteCommandTime = DateTime.MinValue;
        private readonly object _writeTimingLock = new object();

        // Statistics
        private int _successfulReads = 0;
        private int _failedReads = 0;
        private int _successfulWrites = 0;
        private int _failedWrites = 0;
        private DateTime _lastSuccessfulRead;
        private DateTime _lastSuccessfulWrite;

        // Cycle time statistics for performance monitoring
        private List<double> _recentCycleTimes = new List<double>();
        private const int MAX_CYCLE_TIMES_TRACKED = 20;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a data point value changes
        /// </summary>
        public event EventHandler<PLCDataChangedEventArgs> DataChanged;

        /// <summary>
        /// Raised when connection status changes
        /// </summary>
        public event EventHandler<PLCConnectionEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Raised when an error occurs
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the PLC is currently connected
        /// </summary>
        public bool IsConnected
        {
            get { lock (_connectionLock) { return _isConnected; } }
            private set
            {
                lock (_connectionLock)
                {
                    if (_isConnected != value)
                    {
                        _isConnected = value;
                        OnConnectionStatusChanged(value, value ? "Connected" : "Disconnected");
                    }
                }
            }
        }

        /// <summary>
        /// Gets whether the controller is running (polling)
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Gets or sets the read interval in milliseconds
        /// </summary>
        public int ReadInterval
        {
            get => _readInterval;
            set
            {
                _readInterval = value;
                if (_isRunning)
                {
                    RestartReadTimer();
                }
            }
        }

        /// <summary>
        /// Gets or sets the inter-command delay in milliseconds (default: 30ms)
        /// This delay is enforced between consecutive write commands to prevent PLC poll cycle conflicts.
        /// Recommended range: 20-50ms depending on PLC scan time.
        /// </summary>
        public int InterCommandDelay
        {
            get => _interCommandDelay;
            set
            {
                if (value < 0)
                {
                    Logger.Warning("PLCController", $"Invalid inter-command delay: {value}ms. Using 0ms.");
                    _interCommandDelay = 0;
                }
                else if (value > 500)
                {
                    Logger.Warning("PLCController", $"Large inter-command delay: {value}ms. This may cause slow response.");
                    _interCommandDelay = value;
                }
                else
                {
                    _interCommandDelay = value;
                }

                Logger.Info("PLCController", $"Inter-command delay set to {_interCommandDelay}ms");
            }
        }

        /// <summary>
        /// Gets the number of successful reads
        /// </summary>
        public int SuccessfulReads => _successfulReads;

        /// <summary>
        /// Gets the number of failed reads
        /// </summary>
        public int FailedReads => _failedReads;

        /// <summary>
        /// Gets the number of successful writes
        /// </summary>
        public int SuccessfulWrites => _successfulWrites;

        /// <summary>
        /// Gets the number of failed writes
        /// </summary>
        public int FailedWrites => _failedWrites;

        /// <summary>
        /// Gets the last successful read timestamp
        /// </summary>
        public DateTime LastSuccessfulRead => _lastSuccessfulRead;

        /// <summary>
        /// Gets the last successful write timestamp
        /// </summary>
        public DateTime LastSuccessfulWrite => _lastSuccessfulWrite;

        /// <summary>
        /// Gets the currently active monitoring groups
        /// </summary>
        public PLCMonitoringGroup ActiveMonitoringGroups => _activeMonitoringGroups;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for Modbus TCP connection
        /// </summary>
        /// <param name="ipAddress">PLC IP address</param>
        /// <param name="port">Modbus TCP port (default: 502)</param>
        /// <param name="unitIdentifier">Unit identifier (default: 1)</param>
        public PLCController(string ipAddress, int port = 502, byte unitIdentifier = 1)
        {
            _ipAddress = ipAddress;
            _port = port;
            _unitIdentifier = unitIdentifier;

            _dataPoints = new Dictionary<string, PLCDataPoint>();

            Logger.Info("PLCController", $"Initialized for Modbus TCP: {ipAddress}:{port}, Unit ID: {unitIdentifier}");
        }

        #endregion

        #region Monitoring Group Management

        /// <summary>
        /// Set which monitoring groups are currently active
        /// This immediately affects which data points are read in the next cycle
        /// </summary>
        /// <param name="groups">The monitoring groups to activate</param>
        public void SetActiveMonitoringGroups(PLCMonitoringGroup groups)
        {
            lock (_lockObject)
            {
                var previousGroups = _activeMonitoringGroups;
                _activeMonitoringGroups = groups;

                // Count how many data points will be monitored
                int activeCount = _dataPoints.Values.Count(dp => dp.ShouldMonitor(groups));
                int totalCount = _dataPoints.Count;

                Logger.Info("PLCController",
                    $"Active monitoring groups changed: {previousGroups} -> {groups}");
                Logger.Info("PLCController",
                    $"Monitoring {activeCount}/{totalCount} data points ({(activeCount * 100.0 / Math.Max(totalCount, 1)):F1}%)");
            }
        }

        /// <summary>
        /// Add monitoring group(s) to currently active groups
        /// </summary>
        /// <param name="groups">The monitoring group(s) to enable</param>
        public void EnableMonitoringGroups(PLCMonitoringGroup groups)
        {
            lock (_lockObject)
            {
                _activeMonitoringGroups |= groups;
                Logger.Info("PLCController", $"Enabled monitoring groups: {groups}. Active: {_activeMonitoringGroups}");
            }
        }

        /// <summary>
        /// Remove monitoring group(s) from currently active groups
        /// </summary>
        /// <param name="groups">The monitoring group(s) to disable</param>
        public void DisableMonitoringGroups(PLCMonitoringGroup groups)
        {
            lock (_lockObject)
            {
                _activeMonitoringGroups &= ~groups;
                Logger.Info("PLCController", $"Disabled monitoring groups: {groups}. Active: {_activeMonitoringGroups}");
            }
        }

        /// <summary>
        /// Get count of data points that would be monitored for given groups
        /// </summary>
        /// <param name="groups">The monitoring groups to check</param>
        /// <returns>Number of data points that would be monitored</returns>
        public int GetMonitoredDataPointCount(PLCMonitoringGroup groups)
        {
            lock (_lockObject)
            {
                return _dataPoints.Values.Count(dp => dp.ShouldMonitor(groups));
            }
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Add a coil (bit) data point
        /// </summary>
        public void AddCoil(string name, ushort address, string description = "")
        {
            AddDataPoint(name, PLCDataType.Coil, address, 1, description);
        }

        /// <summary>
        /// Add a coil data point with monitoring group
        /// </summary>
        public void AddCoil(string name, ushort address, string description, PLCMonitoringGroup monitoringGroup)
        {
            AddDataPoint(name, PLCDataType.Coil, address, 1, description, monitoringGroup);
        }

        /// <summary>
        /// Add a discrete input (read-only bit) data point
        /// </summary>
        public void AddDiscreteInput(string name, ushort address, string description = "")
        {
            AddDataPoint(name, PLCDataType.DiscreteInput, address, 1, description);
        }

        /// <summary>
        /// Add a holding register data point
        /// </summary>
        public void AddHoldingRegister(string name, ushort address, ushort length = 1, string description = "")
        {
            AddDataPoint(name, PLCDataType.HoldingRegister, address, length, description, PLCMonitoringGroup.All);
        }

        /// <summary>
        /// Add a holding register data point with monitoring group
        /// </summary>
        public void AddHoldingRegister(string name, ushort address, ushort length, string description, PLCMonitoringGroup monitoringGroup)
        {
            AddDataPoint(name, PLCDataType.HoldingRegister, address, length, description, monitoringGroup);
        }

        /// <summary>
        /// Add an input register (read-only) data point
        /// </summary>
        public void AddInputRegister(string name, ushort address, ushort length = 1, string description = "")
        {
            AddDataPoint(name, PLCDataType.InputRegister, address, length, description, PLCMonitoringGroup.All);
        }

        /// <summary>
        /// Add an input register data point with monitoring group
        /// </summary>
        public void AddInputRegister(string name, ushort address, ushort length, string description, PLCMonitoringGroup monitoringGroup)
        {
            AddDataPoint(name, PLCDataType.InputRegister, address, length, description, monitoringGroup);
        }

        /// <summary>
        /// Add a data point to the configuration
        /// </summary>
        private void AddDataPoint(string name, PLCDataType dataType, ushort address, ushort length, string description)
        {
            AddDataPoint(name, dataType, address, length, description, PLCMonitoringGroup.All);
        }

        /// <summary>
        /// Add a data point to the configuration with monitoring group
        /// </summary>
        private void AddDataPoint(string name, PLCDataType dataType, ushort address, ushort length, string description, PLCMonitoringGroup monitoringGroup)
        {
            lock (_lockObject)
            {
                if (_dataPoints.ContainsKey(name))
                {
                    Logger.Warning("PLCController", $"Data point '{name}' already exists. Updating configuration.");
                    _dataPoints.Remove(name);
                }

                var dataPoint = new PLCDataPoint
                {
                    Name = name,
                    DataType = dataType,
                    Address = address,
                    Length = length,
                    Description = description,
                    Value = null,
                    PreviousValue = null,
                    HasChanged = false,
                    LastUpdated = DateTime.MinValue,
                    MonitoringGroup = monitoringGroup
                };

                _dataPoints.Add(name, dataPoint);
                Logger.Info("PLCController", $"Added data point: {name} ({dataType}) @ {address}, Group: {monitoringGroup}");
            }
        }

        /// <summary>
        /// Remove a data point from the configuration
        /// </summary>
        public bool RemoveDataPoint(string name)
        {
            lock (_lockObject)
            {
                bool removed = _dataPoints.Remove(name);
                if (removed)
                {
                    Logger.Info("PLCController", $"Removed data point: {name}");
                }
                return removed;
            }
        }

        /// <summary>
        /// Clear all data points
        /// </summary>
        public void ClearDataPoints()
        {
            lock (_lockObject)
            {
                _dataPoints.Clear();
                Logger.Info("PLCController", "Cleared all data points");
            }
        }

        /// <summary>
        /// Get all data point names
        /// </summary>
        public List<string> GetDataPointNames()
        {
            lock (_lockObject)
            {
                return _dataPoints.Keys.ToList();
            }
        }

        /// <summary>
        /// Get data point configuration (returns a copy to avoid threading issues)
        /// </summary>
        public PLCDataPoint GetDataPoint(string name)
        {
            lock (_lockObject)
            {
                if (_dataPoints.TryGetValue(name, out var dataPoint))
                {
                    // Return a shallow copy to avoid threading issues
                    return new PLCDataPoint
                    {
                        Name = dataPoint.Name,
                        DataType = dataPoint.DataType,
                        Address = dataPoint.Address,
                        Length = dataPoint.Length,
                        Description = dataPoint.Description,
                        Value = dataPoint.Value,
                        PreviousValue = dataPoint.PreviousValue,
                        HasChanged = dataPoint.HasChanged,
                        LastUpdated = dataPoint.LastUpdated
                    };
                }
                return null;
            }
        }

        #endregion

        #region Connection Methods

        /// <summary>
        /// Connect to the PLC
        /// </summary>
        public bool Connect()
        {
            try
            {
                Logger.Info("PLCController", "Attempting to connect to PLC...");

                lock (_connectionLock)
                {
                    if (_isConnected)
                    {
                        Logger.Warning("PLCController", "Already connected to PLC");
                        return true;
                    }

                    _tcpClient = new ModbusTcpClient();
                    _tcpClient.Connect(System.Net.IPAddress.Parse(_ipAddress), FluentModbus.ModbusEndianness.BigEndian);
                    IsConnected = true;
                    Logger.Info("PLCController", $"Connected to PLC via Modbus TCP: {_ipAddress}:{_port}");

                    return true;
                }
            }
            catch (SocketException ex)
            {
                Logger.Error("PLCController", $"Socket error connecting to PLC: {ex.Message}", ex);
                OnErrorOccurred($"Connection failed: {ex.Message}");
                IsConnected = false;
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error connecting to PLC: {ex.Message}", ex);
                OnErrorOccurred($"Connection failed: {ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the PLC
        /// </summary>
        public void Disconnect()
        {
            try
            {
                Logger.Info("PLCController", "Disconnecting from PLC...");

                Stop(); // Stop polling first

                lock (_connectionLock)
                {
                    _tcpClient?.Disconnect();
                    _tcpClient?.Dispose();
                    _tcpClient = null;

                    IsConnected = false;
                }

                Logger.Info("PLCController", "Disconnected from PLC");
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error disconnecting from PLC: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check if the connection is still alive
        /// </summary>
        private void CheckConnection(object state)
        {
            if (!_isRunning) return;

            try
            {
                if (_tcpClient == null || !_tcpClient.IsConnected)
                {
                    Logger.Warning("PLCController", "Connection lost, attempting to reconnect...");
                    IsConnected = false;
                    Connect();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Connection check failed: {ex.Message}", ex);
                IsConnected = false;
            }
        }

        #endregion

        #region Start/Stop Methods

        /// <summary>
        /// Start periodic reading
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                Logger.Warning("PLCController", "Already running");
                return;
            }

            if (!IsConnected)
            {
                Logger.Warning("PLCController", "Not connected. Attempting to connect...");
                if (!Connect())
                {
                    Logger.Error("PLCController", "Cannot start: Connection failed");
                    return;
                }
            }

            _isRunning = true;

            // Start read timer for periodic polling
            _readTimer = new Timer(ReadAllDataPoints, null, 0, _readInterval);

            // Start connection check timer
            _connectionCheckTimer = new Timer(CheckConnection, null, _connectionCheckInterval, _connectionCheckInterval);

            Logger.Info("PLCController", "Started periodic reading");
        }

        /// <summary>
        /// Stop periodic reading
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;

            // Stop timers
            _readTimer?.Dispose();
            _readTimer = null;

            _connectionCheckTimer?.Dispose();
            _connectionCheckTimer = null;

            Logger.Info("PLCController", "Stopped periodic reading");
        }

        private void RestartReadTimer()
        {
            _readTimer?.Dispose();
            if (_isRunning)
            {
                _readTimer = new Timer(ReadAllDataPoints, null, 0, _readInterval);
            }
        }

        #endregion

        #region Read Methods

        /// <summary>
        /// Read all configured data points using fixed-size block reads (64 bits per block)
        /// This is more reliable than variable-size batching
        /// </summary>
        private void ReadAllDataPoints(object state)
        {
            // Prevent re-entry
            if (!Monitor.TryEnter(_readLock))
            {
                //Logger.Warning("PLCController", "Previous read cycle still in progress - skipping this cycle");
                return;
            }

            try
            {
                if (!IsConnected) return;

                if (_isWriting)
                {
                    //Logger.Debug("PLCController", "Skipping read cycle - write in progress");
                    return;
                }

                var cycleStart = DateTime.Now;

                // Create a snapshot of data points to avoid holding lock during I/O
                // FILTER by active monitoring groups for dynamic performance optimization
                List<PLCDataPoint> dataPointsSnapshot;
                lock (_lockObject)
                {
                    dataPointsSnapshot = _dataPoints.Values
                        .Where(dp => dp.ShouldMonitor(_activeMonitoringGroups))
                        .ToList();
                }

                // Group by data type
                var coilPoints = dataPointsSnapshot.Where(dp => dp.DataType == PLCDataType.Coil).ToList();
                var discretePoints = dataPointsSnapshot.Where(dp => dp.DataType == PLCDataType.DiscreteInput).ToList();
                var holdingPoints = dataPointsSnapshot.Where(dp => dp.DataType == PLCDataType.HoldingRegister).ToList();
                var inputPoints = dataPointsSnapshot.Where(dp => dp.DataType == PLCDataType.InputRegister).ToList();

                //// ===== CYCLE START LOGGING =====
                //Logger.Debug("PLCController",
                //    $"??? Read Cycle #{_successfulReads + 1} Start ??? " +
                //    $"Active Groups: {_activeMonitoringGroups}, " +
                //    $"Monitoring: {dataPointsSnapshot.Count}/{_dataPoints.Count} points " +
                //    $"(Coils: {coilPoints.Count}, Discrete: {discretePoints.Count}, " +
                //    $"Holding: {holdingPoints.Count}, Input: {inputPoints.Count})");

                // Track timing for each section
                var sectionStart = DateTime.Now;

                // Read coils in 64-bit blocks
                if (coilPoints.Count > 0 && !_isWriting)
                {
                    ReadCoilsInBlocks(coilPoints);
                    var coilDuration = (DateTime.Now - sectionStart).TotalMilliseconds;
                    //Logger.Debug("PLCController", $"  Coils read in {coilDuration:F1}ms ({coilPoints.Count} points)");
                    sectionStart = DateTime.Now;
                }

                // Read discrete inputs in 64-bit blocks
                if (discretePoints.Count > 0 && !_isWriting)
                {
                    ReadDiscreteInputsInBlocks(discretePoints);
                    var discreteDuration = (DateTime.Now - sectionStart).TotalMilliseconds;
                    //Logger.Debug("PLCController", $"  Discrete Inputs read in {discreteDuration:F1}ms ({discretePoints.Count} points)");
                    sectionStart = DateTime.Now;
                }

                // Read holding registers in blocks
                if (holdingPoints.Count > 0 && !_isWriting)
                {
                    ReadHoldingRegistersInBlocks(holdingPoints);
                    var holdingDuration = (DateTime.Now - sectionStart).TotalMilliseconds;
                    //Logger.Debug("PLCController", $"  Holding Registers read in {holdingDuration:F1}ms ({holdingPoints.Count} points)");
                    sectionStart = DateTime.Now;
                }

                // Read input registers in blocks
                if (inputPoints.Count > 0 && !_isWriting)
                {
                    ReadInputRegistersInBlocks(inputPoints);
                    var inputDuration = (DateTime.Now - sectionStart).TotalMilliseconds;
                    //Logger.Debug("PLCController", $"  Input Registers read in {inputDuration:F1}ms ({inputPoints.Count} points)");
                }

                _successfulReads++;
                _lastSuccessfulRead = DateTime.Now;

                var cycleDuration = (DateTime.Now - cycleStart).TotalMilliseconds;

                // Track cycle time for statistics
                _recentCycleTimes.Add(cycleDuration);
                if (_recentCycleTimes.Count > MAX_CYCLE_TIMES_TRACKED)
                {
                    _recentCycleTimes.RemoveAt(0); // Remove oldest
                }

                // Calculate average cycle time
                double avgCycleTime = _recentCycleTimes.Average();
                double minCycleTime = _recentCycleTimes.Min();
                double maxCycleTime = _recentCycleTimes.Max();


            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error in ReadAllDataPoints: {ex.Message}", ex);
                _failedReads++;
                OnErrorOccurred($"Read error: {ex.Message}");
            }
            finally
            {
                Monitor.Exit(_readLock);
            }
        }

        /// <summary>
        /// Read coils in fixed 64-bit blocks for reliability
        /// Address range 0-166 requires: blocks 0-63, 64-127, 128-191 (3 blocks)
        /// Address range 400-574 requires: blocks 384-447, 448-511, 512-575 (3 blocks)
        /// 
        /// IMPORTANT: Modbus coils are packed into bytes (8 bits per byte)
        /// </summary>
        private void ReadCoilsInBlocks(List<PLCDataPoint> dataPoints)
        {
            if (dataPoints.Count == 0) return;

            try
            {
                var methodStart = DateTime.Now;

                // Determine which 64-bit blocks we need to read
                var blocksNeeded = new HashSet<ushort>();
                foreach (var dp in dataPoints)
                {
                    ushort blockStart = (ushort)((dp.Address / 64) * 64);
                    blocksNeeded.Add(blockStart);
                }

                //Logger.Debug("PLCController", $"  Coils: {dataPoints.Count} points require {blocksNeeded.Count} block reads " +
                //$"(addresses: {string.Join(", ", blocksNeeded.OrderBy(b => b).Select(b => $"{b}-{b + 63}"))})");

                // Cache for storing block data
                var blockCache = new Dictionary<ushort, bool[]>();
                int successfulBlocks = 0;
                int failedBlocks = 0;

                // Read each 64-bit block
                foreach (var blockStart in blocksNeeded.OrderBy(b => b))
                {
                    if (_isWriting) return;

                    try
                    {
                        var blockReadStart = DateTime.Now;

                        // Read 64 coils (will return 8 bytes since coils are packed 8 per byte)
                        var rawData = _tcpClient.ReadCoils(_unitIdentifier, blockStart, 64);
                        var blockData = new bool[64];

                        var blockReadDuration = (DateTime.Now - blockReadStart).TotalMilliseconds;

                        // Unpack bits from bytes - Modbus packs 8 coils per byte
                        // Byte 0 contains coils 0-7, Byte 1 contains coils 8-15, etc.
                        for (int byteIndex = 0; byteIndex < rawData.Length && byteIndex < 8; byteIndex++)
                        {
                            byte currentByte = rawData[byteIndex];

                            // Extract each bit from the byte
                            for (int bitIndex = 0; bitIndex < 8; bitIndex++)
                            {
                                int coilIndex = byteIndex * 8 + bitIndex;
                                if (coilIndex < 64)
                                {
                                    // Check if bit is set: (byte >> bitIndex) & 1
                                    blockData[coilIndex] = ((currentByte >> bitIndex) & 1) == 1;
                                }
                            }
                        }

                        blockCache[blockStart] = blockData;
                        successfulBlocks++;

                        //Logger.Debug("PLCController", $"      Block {blockStart}-{blockStart + 63}: {blockReadDuration:F1}ms, " +
                        //$"64 bits from {rawData.Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PLCController", $"Error reading coil block @ {blockStart}: {ex.Message}", ex);
                        _failedReads++;
                        failedBlocks++;
                    }
                }

                var methodDuration = (DateTime.Now - methodStart).TotalMilliseconds;
                //Logger.Debug("PLCController", $"    Coils Summary: {methodDuration:F1}ms total, " +
                //$"{successfulBlocks} blocks OK, {failedBlocks} blocks failed, " +
                //$"avg {(successfulBlocks > 0 ? methodDuration / successfulBlocks : 0):F1}ms per block");

                // Extract individual data points from cached blocks
                foreach (var dataPoint in dataPoints)
                {
                    try
                    {
                        ushort blockStart = (ushort)((dataPoint.Address / 64) * 64);

                        if (!blockCache.TryGetValue(blockStart, out bool[] blockData))
                        {
                            Logger.Warning("PLCController", $"Block data not available for '{dataPoint.Name}' @ {dataPoint.Address}");
                            continue;
                        }

                        int offsetInBlock = dataPoint.Address - blockStart;

                        // Bounds check
                        if (offsetInBlock < 0 || offsetInBlock >= blockData.Length)
                        {
                            Logger.Warning("PLCController", $"Offset {offsetInBlock} out of bounds for '{dataPoint.Name}'");
                            continue;
                        }

                        // Extract value
                        object newValue;
                        if (dataPoint.Length == 1)
                        {
                            newValue = blockData[offsetInBlock];
                        }
                        else
                        {
                            // Multi-bit value
                            int availableLength = Math.Min((int)dataPoint.Length, blockData.Length - offsetInBlock);
                            if (availableLength <= 0) continue;
                            newValue = blockData.Skip(offsetInBlock).Take(availableLength).ToArray();
                        }

                        UpdateDataPointValue(dataPoint, newValue);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PLCController", $"Error extracting coil '{dataPoint.Name}': {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error in ReadCoilsInBlocks: {ex.Message}", ex);
                _failedReads++;
            }
        }

        /// <summary>
        /// Read discrete inputs in fixed 64-bit blocks
        /// IMPORTANT: Modbus discrete inputs are packed into bytes (8 bits per byte)
        /// </summary>
        private void ReadDiscreteInputsInBlocks(List<PLCDataPoint> dataPoints)
        {
            if (dataPoints.Count == 0) return;

            try
            {
                var blocksNeeded = new HashSet<ushort>();
                foreach (var dp in dataPoints)
                {
                    ushort blockStart = (ushort)((dp.Address / 64) * 64);
                    blocksNeeded.Add(blockStart);
                }

                var blockCache = new Dictionary<ushort, bool[]>();

                foreach (var blockStart in blocksNeeded.OrderBy(b => b))
                {
                    if (_isWriting) return;

                    try
                    {
                        // Read 64 discrete inputs (will return 8 bytes since bits are packed 8 per byte)
                        var rawData = _tcpClient.ReadDiscreteInputs(_unitIdentifier, blockStart, 64);
                        var blockData = new bool[64];

                        // Unpack bits from bytes
                        for (int byteIndex = 0; byteIndex < rawData.Length && byteIndex < 8; byteIndex++)
                        {
                            byte currentByte = rawData[byteIndex];


                            for (int bitIndex = 0; bitIndex < 8; bitIndex++)
                            {
                                int coilIndex = byteIndex * 8 + bitIndex;
                                if (coilIndex < 64)
                                {
                                    blockData[coilIndex] = ((currentByte >> bitIndex) & 1) == 1;
                                }
                            }
                        }

                        blockCache[blockStart] = blockData;
                        //Logger.Debug("PLCController", $"Read discrete input block @ {blockStart}-{blockStart + 63} (64 bits from {rawData.Length} bytes)");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PLCController", $"Error reading discrete input block @ {blockStart}: {ex.Message}", ex);
                    }
                }

                foreach (var dataPoint in dataPoints)
                {
                    try
                    {
                        ushort blockStart = (ushort)((dataPoint.Address / 64) * 64);
                        if (!blockCache.TryGetValue(blockStart, out bool[] blockData)) continue;

                        int offsetInBlock = dataPoint.Address - blockStart;
                        if (offsetInBlock < 0 || offsetInBlock >= blockData.Length) continue;

                        object newValue = dataPoint.Length == 1
                            ? (object)blockData[offsetInBlock]
                            : blockData.Skip(offsetInBlock).Take(Math.Min((int)dataPoint.Length, blockData.Length - offsetInBlock)).ToArray();

                        UpdateDataPointValue(dataPoint, newValue);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PLCController", $"Error extracting discrete input '{dataPoint.Name}': {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error in ReadDiscreteInputsInBlocks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Read holding registers in blocks (32 registers per block for efficiency)
        /// </summary>
        private void ReadHoldingRegistersInBlocks(List<PLCDataPoint> dataPoints)
        {
            if (dataPoints.Count == 0) return;

            try
            {
                var methodStart = DateTime.Now;

                // Group registers into 32-register blocks
                var blocksNeeded = new HashSet<ushort>();
                foreach (var dp in dataPoints)
                {
                    ushort blockStart = (ushort)((dp.Address / 32) * 32);
                    blocksNeeded.Add(blockStart);
                }

                //Logger.Debug("PLCController", $"    Holding Registers: {dataPoints.Count} points require {blocksNeeded.Count} block reads " +
                //$"(addresses: {string.Join(", ", blocksNeeded.OrderBy(b => b).Select(b => $"{b}-{b + 31}"))})");

                var blockCache = new Dictionary<ushort, ushort[]>();
                int successfulBlocks = 0;
                int failedBlocks = 0;

                foreach (var blockStart in blocksNeeded.OrderBy(b => b))
                {
                    if (_isWriting) return;

                    try
                    {
                        var blockReadStart = DateTime.Now;
                        var blockData = _tcpClient.ReadHoldingRegisters<ushort>(_unitIdentifier, blockStart, 32).ToArray();
                        var blockReadDuration = (DateTime.Now - blockReadStart).TotalMilliseconds;

                        blockCache[blockStart] = blockData;
                        successfulBlocks++;

                        //Logger.Debug("PLCController", $"      Block {blockStart}-{blockStart + 31}: {blockReadDuration:F1}ms, " +
                        //$"{blockData.Length} registers read");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PLCController", $"Error reading holding register block @ {blockStart}: {ex.Message}", ex);
                        failedBlocks++;
                    }
                }

                var methodDuration = (DateTime.Now - methodStart).TotalMilliseconds;
                //Logger.Debug("PLCController", $"    Holding Registers Summary: {methodDuration:F1}ms total, " +
                //$"{successfulBlocks} blocks OK, {failedBlocks} blocks failed, " +
                //$"avg {(successfulBlocks > 0 ? methodDuration / successfulBlocks : 0):F1}ms per block");

                foreach (var dataPoint in dataPoints)
                {
                    try
                    {
                        ushort blockStart = (ushort)((dataPoint.Address / 32) * 32);
                        if (!blockCache.TryGetValue(blockStart, out ushort[] blockData)) continue;

                        int offsetInBlock = dataPoint.Address - blockStart;
                        if (offsetInBlock < 0 || offsetInBlock >= blockData.Length) continue;

                        object newValue = dataPoint.Length == 1
                            ? (object)blockData[offsetInBlock]
                            : blockData.Skip(offsetInBlock).Take(Math.Min((int)dataPoint.Length, blockData.Length - offsetInBlock)).ToArray();

                        UpdateDataPointValue(dataPoint, newValue);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PLCController", $"Error extracting holding register '{dataPoint.Name}': {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error in ReadHoldingRegistersInBlocks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Read input registers in blocks (32 registers per block)
        /// </summary>
        private void ReadInputRegistersInBlocks(List<PLCDataPoint> dataPoints)
        {
            if (dataPoints.Count == 0) return;

            try
            {
                var blocksNeeded = new HashSet<ushort>();
                foreach (var dp in dataPoints)
                {
                    ushort blockStart = (ushort)((dp.Address / 32) * 32);
                    blocksNeeded.Add(blockStart);
                }

                var blockCache = new Dictionary<ushort, ushort[]>();

                foreach (var blockStart in blocksNeeded.OrderBy(b => b))
                {
                    if (_isWriting) return;

                    try
                    {
                        var blockData = _tcpClient.ReadInputRegisters<ushort>(_unitIdentifier, blockStart, 32).ToArray();
                        blockCache[blockStart] = blockData;
                        //Logger.Debug("PLCController", $"Read input register block @ {blockStart}-{blockStart + 31}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PLCController", $"Error reading input register block @ {blockStart}: {ex.Message}", ex);
                    }
                }

                foreach (var dataPoint in dataPoints)
                {
                    try
                    {
                        ushort blockStart = (ushort)((dataPoint.Address / 32) * 32);
                        if (!blockCache.TryGetValue(blockStart, out ushort[] blockData)) continue;

                        int offsetInBlock = dataPoint.Address - blockStart;
                        if (offsetInBlock < 0 || offsetInBlock >= blockData.Length) continue;

                        object newValue = dataPoint.Length == 1
                            ? (object)blockData[offsetInBlock]
                            : blockData.Skip(offsetInBlock).Take(Math.Min((int)dataPoint.Length, blockData.Length - offsetInBlock)).ToArray();

                        UpdateDataPointValue(dataPoint, newValue);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PLCController", $"Error extracting input register '{dataPoint.Name}': {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error in ReadInputRegistersInBlocks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update data point value and fire change events if needed
        /// </summary>
        private void UpdateDataPointValue(PLCDataPoint dataPoint, object newValue)
        {
            dataPoint.PreviousValue = dataPoint.Value;
            dataPoint.Value = newValue;
            dataPoint.LastUpdated = DateTime.Now;

            if (!AreValuesEqual(dataPoint.PreviousValue, newValue))
            {
                dataPoint.HasChanged = true;
                OnDataChanged(dataPoint);
            }
            else
            {
                dataPoint.HasChanged = false;
            }
        }

        /// <summary>
        /// Read a single data point (fallback for individual reads)
        /// </summary>
        private void ReadDataPoint(PLCDataPoint dataPoint)
        {
            if (!IsConnected) return;

            object newValue = null;

            try
            {
                switch (dataPoint.DataType)
                {
                    case PLCDataType.Coil:
                        newValue = ReadCoils(dataPoint.Address, dataPoint.Length);
                        break;

                    case PLCDataType.DiscreteInput:
                        newValue = ReadDiscreteInputs(dataPoint.Address, dataPoint.Length);
                        break;

                    case PLCDataType.HoldingRegister:
                        newValue = ReadHoldingRegisters(dataPoint.Address, dataPoint.Length);
                        break;

                    case PLCDataType.InputRegister:
                        newValue = ReadInputRegisters(dataPoint.Address, dataPoint.Length);
                        break;
                }

                // Check if value changed
                dataPoint.PreviousValue = dataPoint.Value;
                dataPoint.Value = newValue;
                dataPoint.LastUpdated = DateTime.Now;

                if (!AreValuesEqual(dataPoint.PreviousValue, newValue))
                {
                    dataPoint.HasChanged = true;
                    OnDataChanged(dataPoint);
                }
                else
                {
                    dataPoint.HasChanged = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error reading '{dataPoint.Name}' at address {dataPoint.Address}: {ex.Message}", ex);
                throw;
            }
        }

        private object ReadCoils(ushort address, ushort length)
        {
            var result = _tcpClient.ReadCoils(_unitIdentifier, address, length);
            return length == 1 ? (object)result.ToArray()[0] : result.ToArray();
        }

        private object ReadDiscreteInputs(ushort address, ushort length)
        {
            var result = _tcpClient.ReadDiscreteInputs(_unitIdentifier, address, length);
            return length == 1 ? (object)result.ToArray()[0] : result.ToArray();
        }

        private object ReadHoldingRegisters(ushort address, ushort length)
        {
            var result = _tcpClient.ReadHoldingRegisters<ushort>(_unitIdentifier, address, length);
            return length == 1 ? (object)result.ToArray()[0] : result.ToArray();
        }

        private object ReadInputRegisters(ushort address, ushort length)
        {
            var result = _tcpClient.ReadInputRegisters<ushort>(_unitIdentifier, address, length);
            return length == 1 ? (object)result.ToArray()[0] : result.ToArray();
        }

        /// <summary>
        /// Get the current value of a data point by name
        /// </summary>
        public object GetValue(string name)
        {
            lock (_lockObject)
            {
                if (_dataPoints.TryGetValue(name, out var dataPoint))
                {
                    return dataPoint.Value;
                }
                return null;
            }
        }

        /// <summary>
        /// Get the current value of a data point as a boolean (for coils/discrete inputs)
        /// </summary>
        public bool? GetBoolValue(string name)
        {
            var value = GetValue(name);
            if (value is bool b)
                return b;
            return null;
        }

        /// <summary>
        /// Get the current value of a data point as a 16-bit register value
        /// </summary>
        public ushort? GetRegisterValue(string name)
        {
            var value = GetValue(name);
            if (value is ushort us)
                return us;
            return null;
        }

        /// <summary>
        /// Get the current value of a data point as an array of 16-bit register values
        /// </summary>
        public ushort[] GetRegisterArrayValue(string name)
        {
            var value = GetValue(name);
            if (value is ushort[] arr)
                return arr;
            return null;
        }

        /// <summary>
        /// Read holding registers directly from PLC (bypassing cache) - for time-critical reads
        /// Use this when you need the absolute latest values, not cached polling data
        /// </summary>
        /// <param name="startAddress">Starting register address</param>
        /// <param name="count">Number of registers to read</param>
        /// <returns>Array of register values, or null on failure</returns>
        public ushort[] ReadHoldingRegistersDirect(ushort startAddress, ushort count)
        {
            lock (_readLock)
            {
                try
                {
                    if (!IsConnected || _tcpClient == null)
                    {
                        Logger.Warning("PLCController", $"Cannot perform direct read: not connected");
                        return null;
                    }

                    var result = _tcpClient.ReadHoldingRegisters<ushort>(_unitIdentifier, startAddress, count).ToArray();
                    //Logger.Debug("PLCController", $"Direct read {count} registers from address {startAddress}");
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.Error("PLCController", $"Error in direct register read at address {startAddress}: {ex.Message}", ex);
                    _failedReads++;
                    return null;
                }
            }
        }

        #endregion

        #region Write Methods (On-Demand / Trigger-Based)

        /// <summary>
        /// Ensures minimum delay between write commands to prevent PLC poll cycle conflicts
        /// </summary>
        private async Task EnsureInterCommandDelayAsync()
        {
            int remainingDelay = 0;
            lock (_writeTimingLock)
            {
                if (_lastWriteCommandTime != DateTime.MinValue)
                {
                    var timeSinceLastWrite = (DateTime.Now - _lastWriteCommandTime).TotalMilliseconds;
                    remainingDelay = (int)Math.Ceiling(_interCommandDelay - timeSinceLastWrite);
                }

                _lastWriteCommandTime = DateTime.Now;
            }

            if (remainingDelay > 0)
            {
                await Task.Delay(remainingDelay);
            }
        }

        /// <summary>
        /// Write a coil (bit) immediately - trigger-based, not queued
        /// Enforces inter-command delay to prevent PLC poll cycle conflicts
        /// </summary>
        public async Task WriteCoilAsync(string name, bool value)
        {
            await _ioSemaphore.WaitAsync();
            try
            {
                _isWriting = true;
                await EnsureInterCommandDelayAsync();
                await Task.Delay(_writePauseDelay);

                PLCDataPoint dataPoint;
                lock (_lockObject)
                {
                    if (!_dataPoints.TryGetValue(name, out dataPoint)) return;
                    if (dataPoint.DataType != PLCDataType.Coil) return;
                }

                await _tcpClient.WriteSingleCoilAsync(_unitIdentifier, dataPoint.Address, value);
                Logger.Info("PLCController", $"Wrote coil '{name}' @ {dataPoint.Address}: {value}");
                _successfulWrites++;
                _lastSuccessfulWrite = DateTime.Now;

                await Task.Delay(_writePauseDelay);
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error writing coil '{name}': {ex.Message}", ex);
                _failedWrites++;
                OnErrorOccurred($"Write error for '{name}': {ex.Message}");
            }
            finally
            {
                _isWriting = false;
                _ioSemaphore.Release();
            }
        }

        public void WriteCoil(string name, bool value)
        {
            WriteCoilAsync(name, value).GetAwaiter().GetResult();
        }

        public async Task WriteHoldingRegisterAsync(string name, ushort value)
        {
            await _ioSemaphore.WaitAsync();
            try
            {
                _isWriting = true;
                await EnsureInterCommandDelayAsync();
                await Task.Delay(_writePauseDelay);

                PLCDataPoint dataPoint;
                lock (_lockObject)
                {
                    if (!_dataPoints.TryGetValue(name, out dataPoint))
                    {
                        Logger.Warning("PLCController", $"Data point '{name}' not found");
                        return;
                    }

                    if (dataPoint.DataType != PLCDataType.HoldingRegister)
                    {
                        Logger.Warning("PLCController", $"Data point '{name}' is not a holding register");
                        return;
                    }
                }

                // Perform write asynchronously using FluentModbus Async method
                await _tcpClient.WriteSingleRegisterAsync(_unitIdentifier, dataPoint.Address, value);
                Logger.Info("PLCController", $"Wrote register '{name}' @ {dataPoint.Address}: {value}");
                
                _successfulWrites++;
                _lastSuccessfulWrite = DateTime.Now;

                // Give PLC time to process before resuming reads
                await Task.Delay(_writePauseDelay);
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error writing register '{name}': {ex.Message}", ex);
                _failedWrites++;
                OnErrorOccurred($"Write error for '{name}': {ex.Message}");
            }
            finally
            {
                _isWriting = false; 
                _ioSemaphore.Release();
            }
        }

        public void WriteHoldingRegister(string name, ushort value)
        {
            WriteHoldingRegisterAsync(name, value).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Write multiple holding registers immediately - trigger-based, not queued
        /// Enforces inter-command delay to prevent PLC poll cycle conflicts
        /// </summary>
        public async Task WriteHoldingRegistersAsync(string name, ushort[] values)
        {
            await _ioSemaphore.WaitAsync();
            try
            {
                _isWriting = true;
                await EnsureInterCommandDelayAsync();
                await Task.Delay(_writePauseDelay);

                PLCDataPoint dataPoint;
                lock (_lockObject)
                {
                    if (!_dataPoints.TryGetValue(name, out dataPoint)) return;
                    if (dataPoint.DataType != PLCDataType.HoldingRegister) return;
                }

                // FluentModbus does not have WriteMultipleRegistersAsync, so we use Task.Run 
                // protected by our _ioSemaphore.
                await Task.Run(() => _tcpClient.WriteMultipleRegisters(_unitIdentifier, dataPoint.Address, values));
                Logger.Info("PLCController", $"Wrote registers '{name}' @ {dataPoint.Address}: [{string.Join(", ", values)}]");
                _successfulWrites++;
                await Task.Delay(_writePauseDelay);
            }
            finally
            {
                _isWriting = false;
                _ioSemaphore.Release();
            }
        }

        public void WriteHoldingRegisters(string name, ushort[] values)
        {
            WriteHoldingRegistersAsync(name, values).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Write multiple holding registers directly to an address - trigger-based, not queued
        /// Enforces inter-command delay to prevent PLC poll cycle conflicts
        /// </summary>
        public async Task WriteHoldingRegistersDirectAsync(ushort address, ushort[] values)
        {
            await _ioSemaphore.WaitAsync();
            try
            {
                _isWriting = true;
                await EnsureInterCommandDelayAsync();
                await Task.Delay(_writePauseDelay);

                if (!IsConnected || _tcpClient == null) return;

                // FluentModbus does not have WriteMultipleRegistersAsync, so we use Task.Run
                await Task.Run(() => _tcpClient.WriteMultipleRegisters(_unitIdentifier, address, values));
                _successfulWrites++;

                await Task.Delay(_writePauseDelay);
            }
            finally
            {
                _isWriting = false;
                _ioSemaphore.Release();
            }
        }

        public void WriteHoldingRegistersDirect(ushort address, ushort[] values)
        {
            WriteHoldingRegistersDirectAsync(address, values).GetAwaiter().GetResult();
        }

        private void WriteSingleCoil(ushort address, bool value)
        {
            _tcpClient.WriteSingleCoil(_unitIdentifier, address, value);
        }

        private void WriteSingleHoldingRegister(ushort address, ushort value)
        {
            _tcpClient.WriteSingleRegister(_unitIdentifier, address, value);
        }

        private void WriteMultipleHoldingRegisters(ushort address, ushort[] values)
        {
            _tcpClient.WriteMultipleRegisters(_unitIdentifier, address, values);
        }
        #endregion

        #region Helper Methods

        /// <summary>
        /// Compare two values for equality
        /// </summary>
        private bool AreValuesEqual(object value1, object value2)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;

            if (value1 is bool[] boolArr1 && value2 is bool[] boolArr2)
            {
                return boolArr1.SequenceEqual(boolArr2);
            }

            if (value1 is ushort[] ushortArr1 && value2 is ushort[] ushortArr2)
            {
                return ushortArr1.SequenceEqual(ushortArr2);
            }

            return value1.Equals(value2);
        }

        /// <summary>
        /// Get statistics report
        /// </summary>
        public string GetStatisticsReport()
        {
            return $@"PLC Controller Statistics:
Connected: {IsConnected}
Running: {IsRunning}
Successful Reads: {_successfulReads}
Failed Reads: {_failedReads}
Successful Writes: {_successfulWrites}
Failed Writes: {_failedWrites}
Last Successful Read: {(_lastSuccessfulRead == DateTime.MinValue ? "Never" : _lastSuccessfulRead.ToString("yyyy-MM-dd HH:mm:ss"))}
Last Successful Write: {(_lastSuccessfulWrite == DateTime.MinValue ? "Never" : _lastSuccessfulWrite.ToString("yyyy-MM-dd HH:mm:ss"))}
Data Points Configured: {_dataPoints.Count}";
        }

        /// <summary>
        /// Get all data points report
        /// </summary>
        public string GetDataPointsReport()
        {
            lock (_lockObject)
            {
                var report = "PLC Data Points:\n";
                report += new string('-', 80) + "\n";
                report += $"{"Name",-20} {"Type",-15} {"Address",-10} {"Value",-20} {"Updated",-25}\n";
                report += new string('-', 80) + "\n";

                foreach (var kvp in _dataPoints)
                {
                    var dp = kvp.Value;
                    string valueStr = dp.Value?.ToString() ?? "null";
                    if (dp.Value is bool[] boolArr)
                        valueStr = $"[{string.Join(",", boolArr)}]";
                    else if (dp.Value is ushort[] ushortArr)
                        valueStr = $"[{string.Join(",", ushortArr)}]";

                    string updatedStr = dp.LastUpdated == DateTime.MinValue ? "Never" : dp.LastUpdated.ToString("HH:mm:ss.fff");

                    report += $"{dp.Name,-20} {dp.DataType,-15} {dp.Address,-10} {valueStr,-20} {updatedStr,-25}\n";
                }

                return report;
            }
        }

        /// <summary>
        /// Get detailed read performance analysis report
        /// </summary>
        public string GetPerformanceReport()
        {
            lock (_lockObject)
            {
                var report = new System.Text.StringBuilder();
                report.AppendLine("=== PLC Read Performance Analysis ===");
                report.AppendLine($"Read Interval: {_readInterval}ms");
                report.AppendLine($"Inter-Command Delay: {_interCommandDelay}ms");
                report.AppendLine($"Write Pause Delay: {_writePauseDelay}ms");
                report.AppendLine();

                // Group data points by type
                var coils = _dataPoints.Values.Where(dp => dp.DataType == PLCDataType.Coil).ToList();
                var discrete = _dataPoints.Values.Where(dp => dp.DataType == PLCDataType.DiscreteInput).ToList();
                var holding = _dataPoints.Values.Where(dp => dp.DataType == PLCDataType.HoldingRegister).ToList();
                var input = _dataPoints.Values.Where(dp => dp.DataType == PLCDataType.InputRegister).ToList();

                report.AppendLine($"Total Data Points: {_dataPoints.Count}");
                report.AppendLine($"  Coils: {coils.Count}");
                report.AppendLine($"  Discrete Inputs: {discrete.Count}");
                report.AppendLine($"  Holding Registers: {holding.Count}");
                report.AppendLine($"  Input Registers: {input.Count}");
                report.AppendLine();

                // Calculate expected block reads
                var coilBlocks = new HashSet<ushort>();
                foreach (var dp in coils)
                {
                    coilBlocks.Add((ushort)((dp.Address / 64) * 64));
                }

                var discreteBlocks = new HashSet<ushort>();
                foreach (var dp in discrete)
                {
                    discreteBlocks.Add((ushort)((dp.Address / 64) * 64));
                }

                var holdingBlocks = new HashSet<ushort>();
                foreach (var dp in holding)
                {
                    holdingBlocks.Add((ushort)((dp.Address / 32) * 32));
                }

                var inputBlocks = new HashSet<ushort>();
                foreach (var dp in input)
                {
                    inputBlocks.Add((ushort)((dp.Address / 32) * 32));
                }

                int totalBlocks = coilBlocks.Count + discreteBlocks.Count + holdingBlocks.Count + inputBlocks.Count;

                report.AppendLine($"Estimated Block Reads per Cycle: {totalBlocks}");
                report.AppendLine($"  Coil blocks (64-bit): {coilBlocks.Count}");
                report.AppendLine($"  Discrete blocks (64-bit): {discreteBlocks.Count}");
                report.AppendLine($"  Holding blocks (32-reg): {holdingBlocks.Count}");
                report.AppendLine($"  Input blocks (32-reg): {inputBlocks.Count}");
                report.AppendLine();

                // Estimate timing
                int estimatedTimeMs = totalBlocks * 30; // Assume ~30ms per Modbus request
                report.AppendLine($"Estimated Cycle Time: ~{estimatedTimeMs}ms (@ 30ms/request)");
                report.AppendLine($"Compared to Interval: {_readInterval}ms");

                if (estimatedTimeMs > _readInterval * 0.8)
                {
                    report.AppendLine();
                    report.AppendLine("?? WARNING: Estimated cycle time exceeds 80% of read interval!");
                    report.AppendLine("Recommendations:");
                    report.AppendLine($"  1. Increase ReadInterval to at least {(int)(estimatedTimeMs * 1.5)}ms");
                    report.AppendLine("  2. Reduce block size from 32 to 16 or 8 registers");
                    report.AppendLine("  3. Remove non-essential data points from configuration");
                    report.AppendLine($"  4. Current blocks could be reduced by {holdingBlocks.Count / 2} if using 16-reg blocks");
                }

                report.AppendLine();
                report.AppendLine($"Statistics:");
                report.AppendLine($"  Successful Reads: {_successfulReads}");
                report.AppendLine($"  Failed Reads: {_failedReads}");
                report.AppendLine($"  Successful Writes: {_successfulWrites}");
                report.AppendLine($"  Failed Writes: {_failedWrites}");
                report.AppendLine($"  Last Successful Read: {(_lastSuccessfulRead == DateTime.MinValue ? "Never" : _lastSuccessfulRead.ToString("yyyy-MM-dd HH:mm:ss.fff"))}");

                return report.ToString();
            }
        }

        /// <summary>
        /// Log performance report to logger
        /// </summary>
        public void LogPerformanceReport()
        {
            var report = GetPerformanceReport();
            foreach (var line in report.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (line.Contains("WARNING"))
                        Logger.Warning("PLCController", line);
                    else
                        Logger.Info("PLCController", line);
                }
            }
        }
        #endregion

        #region Event Handlers

        private void OnDataChanged(PLCDataPoint dataPoint)
        {
            try
            {
                DataChanged?.Invoke(this, new PLCDataChangedEventArgs
                {
                    DataPointName = dataPoint.Name,
                    DataType = dataPoint.DataType,
                    Address = dataPoint.Address,
                    OldValue = dataPoint.PreviousValue,
                    NewValue = dataPoint.Value,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error in DataChanged event handler: {ex.Message}", ex);
            }
        }

        private void OnConnectionStatusChanged(bool isConnected, string message)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(this, new PLCConnectionEventArgs
                {
                    IsConnected = isConnected,
                    Message = message,
                    Timestamp = DateTime.Now
                });

                //if (isConnected)
                //{
                //    Logger.Info("PLCController", $"Connection status changed: {message}");
                //}
                //else
                //{
                //    Logger.Warning("PLCController", $"Connection status changed: {message}");
                //}
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error in ConnectionStatusChanged event handler: {ex.Message}", ex);
            }
        }

        private void OnErrorOccurred(string errorMessage)
        {
            try
            {
                ErrorOccurred?.Invoke(this, errorMessage);
            }
            catch (Exception ex)
            {
                Logger.Error("PLCController", $"Error in ErrorOccurred event handler: {ex.Message}", ex);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Stop();
                Disconnect();
            }

            _disposed = true;
        }

        #endregion
    }
}
