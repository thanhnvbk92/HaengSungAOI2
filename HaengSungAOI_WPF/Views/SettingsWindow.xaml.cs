using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using HaengSungAOI_WPF.Machine.PLC;
using HaengSungAOI_WPF.Utils;

namespace HaengSungAOI_WPF
{
    /// <summary>
    /// Settings Window with Servo Parameter Table (Axes as Columns)
    /// Provides read/write access to servo parameters from PLC based on Servo para.csv
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly Machine.Machine _machine;
        private PLCController _plc;
        private ObservableCollection<ServoParameterRowData> _parameterRows;
        private bool _isInitializing = false;

        public SettingsWindow(Machine.Machine machine)
        {
            InitializeComponent();
            _machine = machine;
            _plc = machine?.PLC;

            // Initialize collections
            _parameterRows = new ObservableCollection<ServoParameterRowData>();
            ParameterDataGrid.ItemsSource = _parameterRows;

            //// Initialize EnableScanOut checkbox from machine state
            //// Use _isInitializing flag to prevent Checked/Unchecked events from
            //// firing back and writing to _machine during initialization.

            //_isInitializing = true;
            //if (_machine != null)
            //{
            //    EnableScanOutCheckBox.IsChecked = _machine.EnableScanOut;
            //}
            //_isInitializing = false;

            // Load parameters
            LoadParameters();

            // Update connection status
            UpdateConnectionStatus();
        }

        /// <summary>
        /// Load all parameters as rows
        /// </summary>
        private void LoadParameters()
        {
            _parameterRows.Clear();

            // Add each parameter as a row
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Current Position", DataType = "LREAL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Current Speed", DataType = "LREAL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Error Code", DataType = "LREAL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Operation Status", DataType = "LREAL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "ORG Found", DataType = "BOOL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Move Completed", DataType = "BOOL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Acceleration", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Deceleration", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "ORG Speed (Fast)", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Jog Speed", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Inching Distance", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Inching Speed", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Target Position", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Target Speed", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Target Point", DataType = "INT", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Current Point", DataType = "INT", IsReadOnly = true });

            UpdateStatus($"Loaded {_parameterRows.Count} parameters");
        }

        /// <summary>
        /// Update PLC connection status display
        /// </summary>
        private void UpdateConnectionStatus()
        {
            if (_plc != null && _plc.IsConnected)
            {
                ConnectionStatusText.Text = "PLC: Connected";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                ReadAllButton.IsEnabled = true;
                WriteAllButton.IsEnabled = true;
            }
            else
            {
                ConnectionStatusText.Text = "PLC: Disconnected";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                ReadAllButton.IsEnabled = false;
                WriteAllButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Read all parameters from PLC for all axes
        /// </summary>
        private async void ReadAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_plc == null || !_plc.IsConnected)
            {
                MessageBox.Show("PLC is not connected.", "Connection Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Warn user about long operation
            var result = MessageBox.Show(
                "Reading all parameters will take approximately 30-60 seconds.\n\n" +
                "This operation will read 224 values (16 parameters � 14 axes) from the PLC.\n\n" +
                "The PLC's periodic monitoring will be paused during this operation.\n\n" +
                "Do you want to continue?",
                "Read All Parameters",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
                return;

            // Disable buttons during read to prevent multiple clicks
            ReadAllButton.IsEnabled = false;
            WriteAllButton.IsEnabled = false;
            RefreshButton.IsEnabled = false;

            // Store whether PLC was running
            bool wasPlcRunning = _plc.IsRunning;

            try
            {
                // Pause PLC periodic reading to prevent conflicts
                if (wasPlcRunning)
                {
                    UpdateStatus("Pausing PLC periodic monitoring...");
                    _plc.Stop();

                    // Wait a bit to ensure all pending operations complete
                    await System.Threading.Tasks.Task.Delay(500);

                    Logger.Info("SettingsWindow", "Paused PLC periodic monitoring for bulk parameter read");
                }

                UpdateStatus("Reading all parameters from PLC...");
                Logger.Info("SettingsWindow", "=== BULK READ OPERATION STARTED ===");

                // Run on background thread to not freeze UI
                await System.Threading.Tasks.Task.Run(() =>
                {
                    int readCount = 0;
                    int errorCount = 0;

                    // Read with delays to avoid overwhelming PLC
                    const int DELAY_BETWEEN_READS_MS = 50; // 50ms delay between each read
                    const int DELAY_BETWEEN_PARAMS_MS = 200; // 200ms delay between parameters

                    foreach (var row in _parameterRows)
                    {
                        int axisIndex = 0;

                        // Read for each axis
                        foreach (ServoAxis axis in Enum.GetValues(typeof(ServoAxis)))
                        {
                            try
                            {
                                string axisName = ServoAddressCalculator.GetAxisDisplayName(axis);
                                ServoParameter param = GetServoParameter(row.ParameterName);

                                // Get the PLC address for this parameter
                                ushort address = ServoAddressCalculator.GetParameterAddress(axis, param);

                                // Read directly from PLC address based on data type
                                object value = null;

                                if (row.DataType == "LREAL")
                                {
                                    // LREAL takes 4 registers (8 bytes) - read as ushort array
                                    ushort[] registers = ReadHoldingRegistersDirectly(address, 4);
                                    if (registers != null && registers.Length == 4)
                                    {
                                        // Convert to double
                                        byte[] bytes = new byte[8];
                                        Buffer.BlockCopy(registers, 0, bytes, 0, 8);
                                        value = BitConverter.ToDouble(bytes, 0);

                                        // Info log the read
                                        Logger.Info("SettingsWindow",
                                            $"READ: {axisName}.{row.ParameterName} @ MW{address} (LREAL, 4 regs) " +
                                            $"= {value:F3} [Regs: {string.Join(",", registers)}]");
                                    }
                                }
                                else if (row.DataType == "INT")
                                {
                                    // INT takes 2 registers - but we'll treat as single ushort for simplicity
                                    ushort[] registers = ReadHoldingRegistersDirectly(address, 1);
                                    if (registers != null && registers.Length > 0)
                                    {
                                        value = registers[0];

                                        // Info log the read
                                        Logger.Info("SettingsWindow",
                                            $"READ: {axisName}.{row.ParameterName} @ MW{address} (INT, 1 reg) = {value}");
                                    }
                                }
                                else if (row.DataType == "BOOL")
                                {
                                    // BOOL takes 1 register
                                    ushort[] registers = ReadHoldingRegistersDirectly(address, 1);
                                    if (registers != null && registers.Length > 0)
                                    {
                                        value = registers[0];

                                        // Info log the read
                                        Logger.Info("SettingsWindow",
                                            $"READ: {axisName}.{row.ParameterName} @ MW{address} (BOOL, 1 reg) = {value}");
                                    }
                                }

                                if (value != null)
                                {
                                    string formattedValue = FormatValue(value, row.DataType);

                                    // Update UI on UI thread
                                    Dispatcher.Invoke(() =>
                                    {
                                        row.SetAxisValue(axisName, formattedValue);
                                    });

                                    readCount++;
                                }

                                // Add delay between reads to prevent overwhelming PLC
                                Thread.Sleep(DELAY_BETWEEN_READS_MS);
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                Logger.Error("SettingsWindow", $"Error reading {row.ParameterName} for axis {axis}: {ex.Message}", ex);
                            }

                            axisIndex++;
                        }

                        // Add longer delay between parameters
                        Thread.Sleep(DELAY_BETWEEN_PARAMS_MS);

                        // Update UI periodically to show progress
                        Dispatcher.Invoke(() =>
                        {
                            UpdateStatus($"Reading... {readCount} values read, {errorCount} errors");
                        });
                    }

                    Logger.Info("SettingsWindow",
                        $"=== BULK READ OPERATION COMPLETED === " +
                        $"Success: {readCount}, Errors: {errorCount}");

                    Dispatcher.Invoke(() =>
                    {
                        UpdateStatus($"Read {readCount} values from PLC successfully ({errorCount} errors)");

                        if (errorCount > 0)
                        {
                            MessageBox.Show($"Read completed with {errorCount} errors.\nCheck logs for details.",
                                "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            MessageBox.Show($"Successfully read {readCount} values from PLC.",
                                "Read Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Logger.Error("SettingsWindow", $"Error reading parameters: {ex.Message}", ex);
                MessageBox.Show($"Error reading parameters:\n{ex.Message}", "Read Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Read failed");
            }
            finally
            {
                // Resume PLC periodic reading if it was running
                if (wasPlcRunning)
                {
                    UpdateStatus("Resuming PLC periodic monitoring...");
                    _plc.Start();
                    Logger.Info("SettingsWindow", "Resumed PLC periodic monitoring after bulk parameter read");
                }

                // Re-enable buttons
                ReadAllButton.IsEnabled = true;
                WriteAllButton.IsEnabled = true;
                RefreshButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Write all parameters to PLC for all axes
        /// </summary>
        private async void WriteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_plc == null || !_plc.IsConnected)
            {
                MessageBox.Show("PLC is not connected.", "Connection Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Confirm write operation
            var result = MessageBox.Show(
                "Are you sure you want to write all parameters to the PLC?\n\n" +
                "This will overwrite the current PLC values.\n\n" +
                "The PLC's periodic monitoring will be paused during this operation.",
                "Confirm Write",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            // Disable buttons during write
            ReadAllButton.IsEnabled = false;
            WriteAllButton.IsEnabled = false;
            RefreshButton.IsEnabled = false;

            // Store whether PLC was running
            bool wasPlcRunning = _plc.IsRunning;

            try
            {
                // Pause PLC periodic reading to prevent conflicts
                if (wasPlcRunning)
                {
                    UpdateStatus("Pausing PLC periodic monitoring...");
                    _plc.Stop();

                    // Wait a bit to ensure all pending operations complete
                    await System.Threading.Tasks.Task.Delay(500);

                    Logger.Info("SettingsWindow", "Paused PLC periodic monitoring for bulk parameter write");
                }

                UpdateStatus("Writing all parameters to PLC...");
                Logger.Info("SettingsWindow", "=== BULK WRITE OPERATION STARTED ===");

                await System.Threading.Tasks.Task.Run(() =>
                {
                    int writeCount = 0;
                    int errorCount = 0;

                    // Write with delays to avoid overwhelming PLC
                    const int DELAY_BETWEEN_WRITES_MS = 50; // 50ms delay between each write
                    const int DELAY_BETWEEN_PARAMS_MS = 200; // 200ms delay between parameters

                    foreach (var row in _parameterRows)
                    {
                        // Skip read-only parameters
                        if (row.IsReadOnly)
                            continue;

                        // Write for each axis
                        foreach (ServoAxis axis in Enum.GetValues(typeof(ServoAxis)))
                        {
                            try
                            {
                                string axisName = ServoAddressCalculator.GetAxisDisplayName(axis);
                                string value = row.GetAxisValue(axisName);

                                // Skip if empty
                                if (string.IsNullOrWhiteSpace(value))
                                    continue;

                                ServoParameter param = GetServoParameter(row.ParameterName);
                                ushort address = ServoAddressCalculator.GetParameterAddress(axis, param);

                                object valueToWrite = ConvertValue(value, row.DataType);

                                if (valueToWrite != null)
                                {
                                    WriteValueToPLCDirect(axisName, row.ParameterName, address, valueToWrite, row.DataType);
                                    writeCount++;
                                }

                                // Add delay between writes
                                Thread.Sleep(DELAY_BETWEEN_WRITES_MS);
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                Logger.Error("SettingsWindow", $"Error writing {row.ParameterName} for axis {axis}: {ex.Message}", ex);
                            }
                        }

                        // Add longer delay between parameters
                        Thread.Sleep(DELAY_BETWEEN_PARAMS_MS);

                        // Update UI periodically
                        Dispatcher.Invoke(() =>
                        {
                            UpdateStatus($"Writing... {writeCount} values written, {errorCount} errors");
                        });
                    }

                    Logger.Info("SettingsWindow",
                        $"=== BULK WRITE OPERATION COMPLETED === " +
                        $"Success: {writeCount}, Errors: {errorCount}");

                    Dispatcher.Invoke(() =>
                    {
                        UpdateStatus($"Wrote {writeCount} values to PLC successfully ({errorCount} errors)");

                        if (errorCount > 0)
                        {
                            MessageBox.Show($"Write completed with {errorCount} errors.\nCheck logs for details.",
                                "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            MessageBox.Show($"Successfully wrote {writeCount} values to PLC.",
                                "Write Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    });
                });

                // Re-read to verify
                await System.Threading.Tasks.Task.Delay(1000); // Wait a bit before reading back
                ReadAllButton_Click(sender, e);
            }
            catch (Exception ex)
            {
                Logger.Error("SettingsWindow", $"Error writing parameters: {ex.Message}", ex);
                MessageBox.Show($"Error writing parameters:\n{ex.Message}", "Write Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Write failed");
            }
            finally
            {
                // Resume PLC periodic reading if it was running
                if (wasPlcRunning)
                {
                    UpdateStatus("Resuming PLC periodic monitoring...");
                    _plc.Start();
                    Logger.Info("SettingsWindow", "Resumed PLC periodic monitoring after bulk parameter write");
                }

                // Re-enable buttons
                ReadAllButton.IsEnabled = true;
                WriteAllButton.IsEnabled = true;
                RefreshButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Write a value directly to PLC address (not using registered data points)
        /// Includes connection validation and automatic reconnection
        /// </summary>
        private void WriteValueToPLCDirect(string axisName, string paramName, ushort address, object value, string dataType)
        {
            const int MAX_RETRY_ATTEMPTS = 3;
            int retryCount = 0;

            while (retryCount < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    // Use reflection to access the private _tcpClient
                    var tcpClientField = _plc.GetType().GetField("_tcpClient",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (tcpClientField == null)
                    {
                        throw new Exception("Could not access PLC TCP client via reflection");
                    }

                    var tcpClient = tcpClientField.GetValue(_plc) as FluentModbus.ModbusTcpClient;

                    // Validate connection before writing
                    if (tcpClient == null || !tcpClient.IsConnected)
                    {
                        Logger.Warning("SettingsWindow",
                            $"PLC connection lost before writing {axisName}.{paramName}. Attempting reconnect...");

                        // Attempt to reconnect
                        if (!ReconnectPLC())
                        {
                            throw new Exception("Failed to reconnect to PLC");
                        }

                        // Get the tcpClient again after reconnection
                        tcpClient = tcpClientField.GetValue(_plc) as FluentModbus.ModbusTcpClient;

                        if (tcpClient == null || !tcpClient.IsConnected)
                        {
                            throw new Exception("PLC reconnection successful but connection is not active");
                        }
                    }

                    // Get unit identifier
                    var unitIdField = _plc.GetType().GetField("_unitIdentifier",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    byte unitId = unitIdField != null ? (byte)unitIdField.GetValue(_plc) : (byte)1;

                    // Perform the write operation based on data type
                    if (dataType == "LREAL" && value is double doubleValue)
                    {
                        // Convert double to 4 ushort registers
                        byte[] bytes = BitConverter.GetBytes(doubleValue);
                        ushort[] registers = new ushort[4];
                        Buffer.BlockCopy(bytes, 0, registers, 0, 8);

                        // Write 4 registers at once using WriteMultipleRegisters
                        tcpClient.WriteMultipleRegisters(unitId, address, registers);

                        // Info log the write
                        Logger.Info("SettingsWindow",
                            $"WRITE: {axisName}.{paramName} @ MW{address} (LREAL, 4 regs) " +
                            $"= {doubleValue:F3} [Regs: {string.Join(",", registers)}]");
                    }
                    else if (dataType == "INT" && value is int intValue)
                    {
                        tcpClient.WriteSingleRegister(unitId, address, (ushort)intValue);

                        // Info log the write
                        Logger.Info("SettingsWindow",
                            $"WRITE: {axisName}.{paramName} @ MW{address} (INT, 1 reg) = {intValue}");
                    }
                    else if (dataType == "BOOL" && value is bool boolValue)
                    {
                        ushort registerValue = boolValue ? (ushort)1 : (ushort)0;
                        tcpClient.WriteSingleRegister(unitId, address, registerValue);

                        // Info log the write
                        Logger.Info("SettingsWindow",
                            $"WRITE: {axisName}.{paramName} @ MW{address} (BOOL, 1 reg) = {boolValue} ({registerValue})");
                    }

                    // Success - exit retry loop
                    return;
                }
                catch (System.IO.IOException ioEx)
                {
                    retryCount++;
                    Logger.Warning("SettingsWindow",
                        $"I/O error writing {axisName}.{paramName} @ MW{address} (Attempt {retryCount}/{MAX_RETRY_ATTEMPTS}): {ioEx.Message}");

                    if (retryCount >= MAX_RETRY_ATTEMPTS)
                    {
                        Logger.Error("SettingsWindow",
                            $"Failed to write {axisName}.{paramName} @ MW{address} after {MAX_RETRY_ATTEMPTS} attempts", ioEx);
                        throw;
                    }

                    // Wait before retry
                    Thread.Sleep(500);

                    // Try to reconnect before next attempt
                    ReconnectPLC();
                }
                catch (System.Net.Sockets.SocketException sockEx)
                {
                    retryCount++;
                    Logger.Warning("SettingsWindow",
                        $"Socket error writing {axisName}.{paramName} @ MW{address} (Attempt {retryCount}/{MAX_RETRY_ATTEMPTS}): {sockEx.Message}");

                    if (retryCount >= MAX_RETRY_ATTEMPTS)
                    {
                        Logger.Error("SettingsWindow",
                            $"Failed to write {axisName}.{paramName} @ MW{address} after {MAX_RETRY_ATTEMPTS} attempts", sockEx);
                        throw;
                    }

                    // Wait before retry
                    Thread.Sleep(500);

                    // Try to reconnect before next attempt
                    ReconnectPLC();
                }
                catch (Exception ex)
                {
                    Logger.Error("SettingsWindow",
                        $"Error writing {axisName}.{paramName} @ MW{address}: {ex.Message}", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Attempt to reconnect to PLC
        /// </summary>
        private bool ReconnectPLC()
        {
            try
            {
                Logger.Info("SettingsWindow", "Attempting to reconnect to PLC...");

                if (_plc == null)
                {
                    Logger.Error("SettingsWindow", "PLC controller is null, cannot reconnect");
                    return false;
                }

                // Check if already connected
                if (_plc.IsConnected)
                {
                    Logger.Info("SettingsWindow", "PLC is already connected");
                    return true;
                }

                // Try to connect
                bool connected = _plc.Connect();

                if (connected)
                {
                    Logger.Info("SettingsWindow", "Successfully reconnected to PLC");

                    // Update UI status on UI thread
                    Dispatcher.Invoke(() =>
                    {
                        UpdateConnectionStatus();
                        UpdateStatus("Reconnected to PLC");
                    });
                }
                else
                {
                    Logger.Error("SettingsWindow", "Failed to reconnect to PLC");
                }

                return connected;
            }
            catch (Exception ex)
            {
                Logger.Error("SettingsWindow", $"Error reconnecting to PLC: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Write a value to PLC with appropriate data type handling (OLD METHOD - DEPRECATED)
        /// </summary>
        private void WriteValueToPLC(string dataPointName, object value, string dataType)
        {
            if (dataType == "LREAL" && value is double doubleValue)
            {
                // Convert double to 4 ushort registers
                byte[] bytes = BitConverter.GetBytes(doubleValue);
                ushort[] registers = new ushort[4];
                Buffer.BlockCopy(bytes, 0, registers, 0, 8);

                // Write each register individually
                for (int i = 0; i < registers.Length; i++)
                {
                    _plc.WriteHoldingRegister($"{dataPointName}_Reg{i}", registers[i]);
                }
            }
            else if (dataType == "INT" && value is int intValue)
            {
                _plc.WriteHoldingRegister(dataPointName, (ushort)intValue);
            }
            else if (dataType == "BOOL" && value is bool boolValue)
            {
                _plc.WriteHoldingRegister(dataPointName, boolValue ? (ushort)1 : (ushort)0);
            }
        }

        /// <summary>
        /// Get ServoParameter enum from parameter name
        /// </summary>
        private ServoParameter GetServoParameter(string parameterName)
        {
            switch (parameterName)
            {
                case "Current Position": return ServoParameter.CurrentPosition;
                case "Current Speed": return ServoParameter.CurrentSpeed;
                case "Error Code": return ServoParameter.ErrorCode;
                case "Operation Status": return ServoParameter.OperationStatus;
                case "ORG Found": return ServoParameter.ORGFound;
                case "Move Completed": return ServoParameter.MoveCompleted;
                case "Acceleration": return ServoParameter.Acceleration;
                case "Deceleration": return ServoParameter.Deceleration;
                case "ORG Speed (Fast)": return ServoParameter.ORGSpeedFast;
                case "Jog Speed": return ServoParameter.JogSpeed;
                case "Inching Distance": return ServoParameter.InchingDistance;
                case "Inching Speed": return ServoParameter.InchingSpeed;
                case "Target Position": return ServoParameter.TargetPosition;
                case "Target Speed": return ServoParameter.TargetSpeed;
                case "Target Point": return ServoParameter.TargetPoint;
                case "Current Point": return ServoParameter.CurrentPoint;
                default: return ServoParameter.CurrentPosition;
            }
        }

        /// <summary>
        /// Refresh the parameter grid
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateConnectionStatus();
            LoadParameters();
            UpdateStatus("Refreshed");
        }

        /// <summary>
        /// Format a value for display based on data type
        /// </summary>
        private string FormatValue(object value, string dataType)
        {
            if (value == null)
                return "N/A";

            try
            {
                switch (dataType)
                {
                    case "LREAL":
                        if (value is double d)
                            return d.ToString("F3");
                        if (value is ushort[] regs && regs.Length >= 4)
                        {
                            byte[] bytes = new byte[8];
                            Buffer.BlockCopy(regs, 0, bytes, 0, 8);
                            double doubleVal = BitConverter.ToDouble(bytes, 0);
                            return doubleVal.ToString("F3");
                        }
                        break;

                    case "INT":
                        if (value is int i)
                            return i.ToString();
                        if (value is ushort us)
                            return us.ToString();
                        break;

                    case "BOOL":
                        if (value is bool b)
                            return b ? "1" : "0";
                        if (value is ushort usb)
                            return usb != 0 ? "1" : "0";
                        break;
                }

                return value.ToString();
            }
            catch
            {
                return "Error";
            }
        }

        /// <summary>
        /// Convert a string value to the appropriate type for PLC writing
        /// </summary>
        private object ConvertValue(string value, string dataType)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            try
            {
                switch (dataType)
                {
                    case "LREAL":
                        if (double.TryParse(value, out double d))
                            return d;
                        break;

                    case "INT":
                        if (int.TryParse(value, out int i))
                            return i;
                        break;

                    case "BOOL":
                        value = value.ToLower();
                        if (value == "true" || value == "1")
                            return true;
                        if (value == "false" || value == "0")
                            return false;
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("SettingsWindow", $"Error converting value '{value}' to {dataType}: {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Update status text
        /// </summary>
        private void UpdateStatus(string message)
        {
            StatusText.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        private void OverrideInspectionToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_machine != null)
            {
                _machine.overideInspection = true;
                UpdateStatus("Override Inspection: ON");
            }
        }

        private void OverrideInspectionToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_machine != null)
            {
                _machine.overideInspection = false;
                UpdateStatus("Override Inspection: OFF");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //private void EnableScanOutCheckBox_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (_isInitializing) return;
        //    if (_machine != null)
        //    {
        //        _machine.EnableScanOut = true;
        //        UpdateStatus("ScanOut: ENABLED");
        //        Logger.Info("SettingsWindow", "ScanOut feature enabled");
        //    }
        //}

        //private void EnableScanOutCheckBox_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (_isInitializing) return;
        //    if (_machine != null)
        //    {
        //        _machine.EnableScanOut = false;
        //        UpdateStatus("ScanOut: DISABLED");
        //        Logger.Info("SettingsWindow", "ScanOut feature disabled");
        //    }
        //}

        /// <summary>
        /// Read holding registers directly from PLC without using registered data points
        /// Includes connection validation and automatic reconnection
        /// </summary>
        private ushort[] ReadHoldingRegistersDirectly(ushort address, ushort length)
        {
            const int MAX_RETRY_ATTEMPTS = 3;
            int retryCount = 0;

            while (retryCount < MAX_RETRY_ATTEMPTS)
            {
                try
                {
                    // Use reflection to access the private _tcpClient
                    var tcpClientField = _plc.GetType().GetField("_tcpClient",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (tcpClientField == null)
                    {
                        throw new Exception("Could not access PLC TCP client via reflection");
                    }

                    var tcpClient = tcpClientField.GetValue(_plc) as FluentModbus.ModbusTcpClient;

                    // Validate connection before reading
                    if (tcpClient == null || !tcpClient.IsConnected)
                    {
                        Logger.Warning("SettingsWindow",
                            $"PLC connection lost before reading @ MW{address}. Attempting reconnect...");

                        // Attempt to reconnect
                        if (!ReconnectPLC())
                        {
                            throw new Exception("Failed to reconnect to PLC");
                        }

                        // Get the tcpClient again after reconnection
                        tcpClient = tcpClientField.GetValue(_plc) as FluentModbus.ModbusTcpClient;

                        if (tcpClient == null || !tcpClient.IsConnected)
                        {
                            throw new Exception("PLC reconnection successful but connection is not active");
                        }
                    }

                    // Get unit identifier
                    var unitIdField = _plc.GetType().GetField("_unitIdentifier",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    byte unitId = unitIdField != null ? (byte)unitIdField.GetValue(_plc) : (byte)1;

                    // Call ReadHoldingRegisters<ushort> directly
                    var result = tcpClient.ReadHoldingRegisters<ushort>(unitId, address, length);

                    if (result != null)
                    {
                        return result.ToArray();
                    }

                    // Success - exit retry loop
                    return null;
                }
                catch (System.IO.IOException ioEx)
                {
                    retryCount++;
                    Logger.Warning("SettingsWindow",
                        $"I/O error reading @ MW{address} (Attempt {retryCount}/{MAX_RETRY_ATTEMPTS}): {ioEx.Message}");

                    if (retryCount >= MAX_RETRY_ATTEMPTS)
                    {
                        Logger.Error("SettingsWindow",
                            $"Failed to read @ MW{address} after {MAX_RETRY_ATTEMPTS} attempts", ioEx);
                        throw;
                    }

                    // Wait before retry
                    Thread.Sleep(500);

                    // Try to reconnect before next attempt
                    ReconnectPLC();
                }
                catch (System.Net.Sockets.SocketException sockEx)
                {
                    retryCount++;
                    Logger.Warning("SettingsWindow",
                        $"Socket error reading @ MW{address} (Attempt {retryCount}/{MAX_RETRY_ATTEMPTS}): {sockEx.Message}");

                    if (retryCount >= MAX_RETRY_ATTEMPTS)
                    {
                        Logger.Error("SettingsWindow",
                            $"Failed to read @ MW{address} after {MAX_RETRY_ATTEMPTS} attempts", sockEx);
                        throw;
                    }

                    // Wait before retry
                    Thread.Sleep(500);

                    // Try to reconnect before next attempt
                    ReconnectPLC();
                }
                catch (Exception ex)
                {
                    Logger.Error("SettingsWindow", $"Error reading registers directly @ {address}: {ex.Message}", ex);
                    return null;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Represents a row in the servo parameter table (one parameter across all axes)
    /// </summary>
    public class ServoParameterRowData : INotifyPropertyChanged
    {
        private string _ax1, _ay1, _ac1, _ax2, _az2, _ax3, _ay3, _az4, _ac4, _az5, _ac5, _az61, _az62, _cv7;

        public string ParameterName { get; set; }
        public string DataType { get; set; }
        public bool IsReadOnly { get; set; }

        // Properties for each axis column
        public string AX1 { get => _ax1; set { _ax1 = value; OnPropertyChanged(); } }
        public string AY1 { get => _ay1; set { _ay1 = value; OnPropertyChanged(); } }
        public string AC1 { get => _ac1; set { _ac1 = value; OnPropertyChanged(); } }
        public string AX2 { get => _ax2; set { _ax2 = value; OnPropertyChanged(); } }
        public string AZ2 { get => _az2; set { _az2 = value; OnPropertyChanged(); } }
        public string AX3 { get => _ax3; set { _ax3 = value; OnPropertyChanged(); } }
        public string AY3 { get => _ay3; set { _ay3 = value; OnPropertyChanged(); } }
        public string AZ4 { get => _az4; set { _az4 = value; OnPropertyChanged(); } }
        public string AC4 { get => _ac4; set { _ac4 = value; OnPropertyChanged(); } }
        public string AZ5 { get => _az5; set { _az5 = value; OnPropertyChanged(); } }
        public string AC5 { get => _ac5; set { _ac5 = value; OnPropertyChanged(); } }
        public string AZ61 { get => _az61; set { _az61 = value; OnPropertyChanged(); } }
        public string AZ62 { get => _az62; set { _az62 = value; OnPropertyChanged(); } }
        public string CV7 { get => _cv7; set { _cv7 = value; OnPropertyChanged(); } }

        /// <summary>
        /// Set value for a specific axis
        /// </summary>
        public void SetAxisValue(string axisName, string value)
        {
            switch (axisName)
            {
                case "AX1": AX1 = value; break;
                case "AY1": AY1 = value; break;
                case "AC1": AC1 = value; break;
                case "AX2": AX2 = value; break;
                case "AZ2": AZ2 = value; break;
                case "AX3": AX3 = value; break;
                case "AY3": AY3 = value; break;
                case "AZ4": AZ4 = value; break;
                case "AC4": AC4 = value; break;
                case "AZ5": AZ5 = value; break;
                case "AC5": AC5 = value; break;
                case "AZ61": AZ61 = value; break;
                case "AZ62": AZ62 = value; break;
                case "CV7": CV7 = value; break;
            }
        }

        /// <summary>
        /// Get value for a specific axis
        /// </summary>
        public string GetAxisValue(string axisName)
        {
            switch (axisName)
            {
                case "AX1": return AX1;
                case "AY1": return AY1;
                case "AC1": return AC1;
                case "AX2": return AX2;
                case "AZ2": return AZ2;
                case "AX3": return AX3;
                case "AY3": return AY3;
                case "AZ4": return AZ4;
                case "AC4": return AC4;
                case "AZ5": return AZ5;
                case "AC5": return AC5;
                case "AZ61": return AZ61;
                case "AZ62": return AZ62;
                case "CV7": return CV7;
                default: return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}