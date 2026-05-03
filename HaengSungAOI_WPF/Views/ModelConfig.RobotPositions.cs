using System;
using System.Windows;
using HaengSungAOI_WPF.Machine;
using HaengSungAOI_WPF.ViewModels;
using HaengSungAOI_WPF.Models;
using System.Linq;
using HaengSungAOI_WPF.Machine.PLC;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Controls;

namespace HaengSungAOI_WPF
{
    /// <summary>
    /// Partial class for ModelConfig - Robot Position Loading and Monitoring
    /// </summary>
    public partial class ModelConfig
    {
        private RobotPositionManager _robotPositionManager;

        // Servo monitoring fields
        private ServoMonitor _servoMonitor;
        private DispatcherTimer _positionUpdateTimer;
        private bool _isMonitoringPositions = false;

        private void InitializeRobotPositionCollections()
        {
            _robotPositionManager = new RobotPositionManager();

            // Bind collections to DataGrids
            InfeedRobotGrid.ItemsSource = _robotPositionManager.InfeedRobotPositions;
            TransferRobotGrid.ItemsSource = _robotPositionManager.TransferRobotPositions;
            OutfeedRobotGrid.ItemsSource = _robotPositionManager.OutfeedRobotPositions;
            Inspect1RobotGrid.ItemsSource = _robotPositionManager.Inspect1RobotPositions;
            Inspect2RobotGrid.ItemsSource = _robotPositionManager.Inspect2RobotPositions;

            // Subscribe to collection changes for tracking
            _robotPositionManager.InfeedRobotPositions.CollectionChanged += (s, e) => OnModelDataChanged(s, null);
            _robotPositionManager.TransferRobotPositions.CollectionChanged += (s, e) => OnModelDataChanged(s, null);
            _robotPositionManager.OutfeedRobotPositions.CollectionChanged += (s, e) => OnModelDataChanged(s, null);
            _robotPositionManager.Inspect1RobotPositions.CollectionChanged += (s, e) => OnModelDataChanged(s, null);
            _robotPositionManager.Inspect2RobotPositions.CollectionChanged += (s, e) => OnModelDataChanged(s, null);

            // Initialize position monitoring
            InitializePositionMonitoring();
        }

        /// <summary>
        /// Initialize servo position monitoring from PLC
        /// </summary>
        private void InitializePositionMonitoring()
        {
            try
            {
                // Initialize update timer (200ms = 5Hz update rate)
                _positionUpdateTimer = new DispatcherTimer();
                _positionUpdateTimer.Interval = TimeSpan.FromMilliseconds(200);
                _positionUpdateTimer.Tick += PositionUpdateTimer_Tick;

                Debug.WriteLine("[ModelConfig] Position monitoring initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error initializing position monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Update current position displays from PLC
        /// </summary>
        private void PositionUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!_isMonitoringPositions || _servoMonitor == null)
                return;

            try
            {
                // DEBUG: Log first axis position to verify data is being read
                try
                {
                    double testX1 = _servoMonitor.GetCurrentPosition(ServoAxis.X1);
                    //Debug.WriteLine($"[ModelConfig] X1 Position: {testX1:F3}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ModelConfig] Error reading X1: {ex.Message}");
                }

                // Update Infeed Robot positions (X1, Y1, C1)
                UpdateInfeedCurrentPositions();

                // Update Transfer Robot positions (X2, Z2)
                UpdateTransferCurrentPositions();

                // Update Outfeed Robot positions (X3, Y3)
                UpdateOutfeedCurrentPositions();

                // Update Inspect 1 positions (Z4, C4)
                UpdateInspect1CurrentPositions();

                // Update Inspect 2 positions (Z5, C5)
                UpdateInspect2CurrentPositions();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error updating positions: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Infeed robot current positions from servo monitor
        /// </summary>
        private void UpdateInfeedCurrentPositions()
        {
            if (_servoMonitor == null) return;

            try
            {
                double currentX = _servoMonitor.GetCurrentPosition(ServoAxis.X1);
                double currentY = _servoMonitor.GetCurrentPosition(ServoAxis.Y1);
                double currentC = _servoMonitor.GetCurrentPosition(ServoAxis.C1);

                // Update TextBlocks if they exist in XAML (using correct names from XAML)
                var xTextBlock = this.FindName("InfeedCurrentX") as TextBlock;
                if (xTextBlock != null)
                    xTextBlock.Text = $"{currentX:F3}";

                var yTextBlock = this.FindName("InfeedCurrentY") as TextBlock;
                if (yTextBlock != null)
                    yTextBlock.Text = $"{currentY:F3}";

                var rTextBlock = this.FindName("InfeedCurrentR") as TextBlock;
                if (rTextBlock != null)
                    rTextBlock.Text = $"{currentC:F3}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error updating Infeed positions: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Transfer robot current positions from servo monitor
        /// </summary>
        private void UpdateTransferCurrentPositions()
        {
            if (_servoMonitor == null) return;

            try
            {
                double currentX = _servoMonitor.GetCurrentPosition(ServoAxis.X2);
                double currentZ = _servoMonitor.GetCurrentPosition(ServoAxis.Z2);

                // Update TextBlocks if they exist in XAML
                var xTextBlock = this.FindName("TransferCurrentX") as TextBlock;
                if (xTextBlock != null)
                    xTextBlock.Text = $"{currentX:F3}";

                var zTextBlock = this.FindName("TransferCurrentZ") as TextBlock;
                if (zTextBlock != null)
                    zTextBlock.Text = $"{currentZ:F3}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error updating Transfer positions: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Outfeed robot current positions from servo monitor
        /// </summary>
        private void UpdateOutfeedCurrentPositions()
        {
            if (_servoMonitor == null) return;

            try
            {
                double currentX = _servoMonitor.GetCurrentPosition(ServoAxis.X3);
                double currentY = _servoMonitor.GetCurrentPosition(ServoAxis.Y3);

                // Update TextBlocks if they exist in XAML
                var xTextBlock = this.FindName("OutfeedCurrentX") as TextBlock;
                if (xTextBlock != null)
                    xTextBlock.Text = $"{currentX:F3}";

                var yTextBlock = this.FindName("OutfeedCurrentY") as TextBlock;
                if (yTextBlock != null)
                    yTextBlock.Text = $"{currentY:F3}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error updating Outfeed positions: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Inspect 1 current positions from servo monitor
        /// </summary>
        private void UpdateInspect1CurrentPositions()
        {
            if (_servoMonitor == null) return;

            try
            {
                double currentZ = _servoMonitor.GetCurrentPosition(ServoAxis.Z4);
                double currentC = _servoMonitor.GetCurrentPosition(ServoAxis.C4);

                // Update TextBlocks if they exist in XAML
                var zTextBlock = this.FindName("Inspect1CurrentZ") as TextBlock;
                if (zTextBlock != null)
                    zTextBlock.Text = $"{currentZ:F3}";

                var cTextBlock = this.FindName("Inspect1CurrentC") as TextBlock;
                if (cTextBlock != null)
                    cTextBlock.Text = $"{currentC:F3}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error updating Inspect1 positions: {ex.Message}");
            }
        }

        /// <summary>
        /// Update Inspect 2 current positions from servo monitor
        /// </summary>
        private void UpdateInspect2CurrentPositions()
        {
            if (_servoMonitor == null) return;

            try
            {
                double currentZ = _servoMonitor.GetCurrentPosition(ServoAxis.Z5);
                double currentC = _servoMonitor.GetCurrentPosition(ServoAxis.C5);

                // Update TextBlocks if they exist in XAML
                var zTextBlock = this.FindName("Inspect2CurrentZ") as TextBlock;
                if (zTextBlock != null)
                    zTextBlock.Text = $"{currentZ:F3}";

                var cTextBlock = this.FindName("Inspect2CurrentC") as TextBlock;
                if (cTextBlock != null)
                    cTextBlock.Text = $"{currentC:F3}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error updating Inspect2 positions: {ex.Message}");
            }
        }

        /// <summary>
        /// Start monitoring servo positions (called when Robot Config tab is activated)
        /// ALSO SWITCHES PLC MONITORING GROUPS FOR PERFORMANCE
        /// </summary>
        private void StartPositionMonitoring()
        {
            if (_isMonitoringPositions) return;

            try
            {
                var plc = GetPLCController();
                if (plc == null || !plc.IsConnected)
                {
                    Debug.WriteLine("[ModelConfig] Cannot start monitoring: PLC not connected");
                    return;
                }

                // SWITCH TO MODELCONFIG MONITORING GROUPS for position monitoring
                plc.SetActiveMonitoringGroups(PLCConstants.MODELCONFIG_MONITORING_GROUPS);
                Debug.WriteLine("[ModelConfig] Switched PLC to MODELCONFIG_MONITORING_GROUPS");

                // Create servo monitor if it doesn't exist
                if (_servoMonitor == null)
                {
                    _servoMonitor = new ServoMonitor(plc, 200); // 200ms update interval
                    _servoMonitor.StartMonitoring();
                    Debug.WriteLine("[ModelConfig] ServoMonitor created and started");
                }

                // Start UI update timer
                _positionUpdateTimer?.Start();
                _isMonitoringPositions = true;

                Debug.WriteLine("[ModelConfig] Position monitoring started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error starting position monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop monitoring servo positions (called when Robot Config tab is deactivated)
        /// ALSO REVERTS PLC MONITORING GROUPS TO DEFAULT
        /// </summary>
        private void StopPositionMonitoring()
        {
            if (!_isMonitoringPositions) return;

            try
            {
                _positionUpdateTimer?.Stop();
                _isMonitoringPositions = false;

                // REVERT TO DEFAULT MONITORING GROUPS when leaving ModelConfig
                var plc = GetPLCController();
                if (plc != null && plc.IsConnected)
                {
                    plc.SetActiveMonitoringGroups(PLCConstants.DEFAULT_MONITORING_GROUPS);
                    Debug.WriteLine("[ModelConfig] Reverted PLC to DEFAULT_MONITORING_GROUPS");
                }

                Debug.WriteLine("[ModelConfig] Position monitoring stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error stopping position monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle tab selection change to start/stop monitoring
        /// </summary>
        private void OnRobotConfigTabActivated(bool isActive)
        {
            if (isActive)
            {
                StartPositionMonitoring();
            }
            else
            {
                StopPositionMonitoring();
            }
        }

        private void LoadInfeedRobotPositions()
        {
            _robotPositionManager.LoadInfeedPositions(_currentModel);
        }

        private void LoadTransferRobotPositions()
        {
            _robotPositionManager.LoadTransferPositions(_currentModel);
        }

        private void LoadOutfeedRobotPositions()
        {
            _robotPositionManager.LoadOutfeedPositions(_currentModel);
        }

        private void LoadInspect1RobotPositions()
        {
            _robotPositionManager.LoadInspect1Positions(_currentModel);
        }

        private void LoadInspect2RobotPositions()
        {
            _robotPositionManager.LoadInspect2Positions(_currentModel);
        }

        private void SaveInfeedRobotPositions()
        {
            _robotPositionManager.SaveInfeedPositions(_currentModel);
        }

        private void SaveTransferRobotPositions()
        {
            _robotPositionManager.SaveTransferPositions(_currentModel);
        }

        private void SaveOutfeedRobotPositions()
        {
            _robotPositionManager.SaveOutfeedPositions(_currentModel);
        }

        private void SaveInspect1RobotPositions()
        {
            _robotPositionManager.SaveInspect1Positions(_currentModel);
        }

        private void SaveInspect2RobotPositions()
        {
            _robotPositionManager.SaveInspect2Positions(_currentModel);
        }

        #region Button Event Handlers

        // Infeed Robot Controls
        private void SaveInfeedCurrentPosition_Click(object sender, RoutedEventArgs e)
        {
            // Get current selected row in DataGrid
            if (InfeedRobotGrid.SelectedItem is RobotPositionEntry selectedPosition && _servoMonitor != null)
            {
                try
                {
                    // Read current positions from servo monitor
                    selectedPosition.X = (float)_servoMonitor.GetCurrentPosition(ServoAxis.X1);
                    selectedPosition.Y = (float)_servoMonitor.GetCurrentPosition(ServoAxis.Y1);
                    selectedPosition.R = (float)_servoMonitor.GetCurrentPosition(ServoAxis.C1);

                    InfeedRobotGrid.Items.Refresh();
                    _isModelDataChanged = true;

                    MessageBox.Show($"Current position saved to '{selectedPosition.Position}':\n" +
                  $"X={selectedPosition.X:F3}, Y={selectedPosition.Y:F3}, R={selectedPosition.R:F3}",
                 "Position Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading current position: {ex.Message}",
       "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MoveInfeedToSelected_Click(object sender, RoutedEventArgs e)
        {
            if (InfeedRobotGrid.SelectedItem is RobotPositionEntry selectedPosition)
            {
                try
                {
                    var plc = GetPLCController();
                    if (plc == null || !plc.IsConnected)
                    {
                        MessageBox.Show("PLC is not connected. Please connect to PLC first.",
                    "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Write target positions for X1, Y1, C1 axes
                    WriteServoTargetPosition(plc, ServoAxis.X1, selectedPosition.X, selectedPosition.SpeedX,
                     selectedPosition.Accel, selectedPosition.Decel);
                    WriteServoTargetPosition(plc, ServoAxis.Y1, selectedPosition.Y, selectedPosition.SpeedY,
                  selectedPosition.Accel, selectedPosition.Decel);
                    WriteServoTargetPosition(plc, ServoAxis.C1, selectedPosition.R, selectedPosition.SpeedR,
                    selectedPosition.Accel, selectedPosition.Decel);

                    // Trigger move command for all axes
                    TriggerServoMove(plc, ServoAxis.X1);
                    System.Threading.Thread.Sleep(50); // Small delay between commands
                    TriggerServoMove(plc, ServoAxis.Y1);
                    System.Threading.Thread.Sleep(50);
                    TriggerServoMove(plc, ServoAxis.C1);

                    MessageBox.Show($"Moving Infeed Robot to '{selectedPosition.Position}':\n" +
                   $"X={selectedPosition.X:F3}, Y={selectedPosition.Y:F3}, R={selectedPosition.R:F3}",
             "Move Command Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving Infeed robot: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
        MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void InfeedTestVacuum_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Infeed Test Vacuum functionality not yet implemented.",
                  "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowInfeedJogControls_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var machine = _mainWindow?.GetMachine();
                if (machine == null)
                {
                    MessageBox.Show("Machine not initialized. Please check system status.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var jogWindow = new RobotJogWindow(machine, RobotType.Infeed);
                jogWindow.Owner = this;
                jogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Infeed jog controls: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Transfer Robot Controls
        private void SaveTransferCurrentPosition_Click(object sender, RoutedEventArgs e)
        {
            if (TransferRobotGrid.SelectedItem is RobotPositionEntry selectedPosition && _servoMonitor != null)
            {
                try
                {
                    selectedPosition.X = (float)_servoMonitor.GetCurrentPosition(ServoAxis.X2);
                    selectedPosition.Z = (float)_servoMonitor.GetCurrentPosition(ServoAxis.Z2);

                    TransferRobotGrid.Items.Refresh();
                    _isModelDataChanged = true;

                    MessageBox.Show($"Current position saved to '{selectedPosition.Position}':\n" +
                $"X={selectedPosition.X:F3}, Z={selectedPosition.Z:F3}",
                  "Position Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading current position: {ex.Message}",
                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
                      MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MoveTransferToSelected_Click(object sender, RoutedEventArgs e)
        {
            if (TransferRobotGrid.SelectedItem is RobotPositionEntry selectedPosition)
            {
                try
                {
                    var plc = GetPLCController();
                    if (plc == null || !plc.IsConnected)
                    {
                        MessageBox.Show("PLC is not connected. Please connect to PLC first.",
                            "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Write target positions for X2, Z2 axes
                    WriteServoTargetPosition(plc, ServoAxis.X2, selectedPosition.X, selectedPosition.SpeedX,
                        selectedPosition.Accel, selectedPosition.Decel);
                    WriteServoTargetPosition(plc, ServoAxis.Z2, selectedPosition.Z, selectedPosition.SpeedZ,
                 selectedPosition.Accel, selectedPosition.Decel);

                    // Trigger move command for both axes
                    TriggerServoMove(plc, ServoAxis.X2);
                    System.Threading.Thread.Sleep(50);
                    TriggerServoMove(plc, ServoAxis.Z2);

                    MessageBox.Show($"Moving Transfer Robot to '{selectedPosition.Position}':\n" +
                      $"X={selectedPosition.X:F3}, Z={selectedPosition.Z:F3}",
                   "Move Command Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving Transfer robot: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TransferTestVacuum_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Transfer Test Vacuum functionality not yet implemented.",
                "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowTransferJogControls_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var machine = _mainWindow?.GetMachine();
                if (machine == null)
                {
                    MessageBox.Show("Machine not initialized. Please check system status.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var jogWindow = new RobotJogWindow(machine, RobotType.Transfer);
                jogWindow.Owner = this;
                jogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Transfer jog controls: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Outfeed Robot Controls
        private void SaveOutfeedCurrentPosition_Click(object sender, RoutedEventArgs e)
        {
            if (OutfeedRobotGrid.SelectedItem is RobotPositionEntry selectedPosition && _servoMonitor != null)
            {
                try
                {
                    selectedPosition.X = (float)_servoMonitor.GetCurrentPosition(ServoAxis.X3);
                    selectedPosition.Y = (float)_servoMonitor.GetCurrentPosition(ServoAxis.Y3);

                    OutfeedRobotGrid.Items.Refresh();
                    _isModelDataChanged = true;

                    MessageBox.Show($"Current position saved to '{selectedPosition.Position}':\n" +
                         $"X={selectedPosition.X:F3}, Y={selectedPosition.Y:F3}",
                          "Position Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading current position: {ex.Message}",
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MoveOutfeedToSelected_Click(object sender, RoutedEventArgs e)
        {
            if (OutfeedRobotGrid.SelectedItem is RobotPositionEntry selectedPosition)
            {
                try
                {
                    var plc = GetPLCController();
                    if (plc == null || !plc.IsConnected)
                    {
                        MessageBox.Show("PLC is not connected. Please connect to PLC first.",
                        "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Write target positions for X3, Y3 axes
                    WriteServoTargetPosition(plc, ServoAxis.X3, selectedPosition.X, selectedPosition.SpeedX,
                     selectedPosition.Accel, selectedPosition.Decel);
                    WriteServoTargetPosition(plc, ServoAxis.Y3, selectedPosition.Y, selectedPosition.SpeedY,
                           selectedPosition.Accel, selectedPosition.Decel);

                    // Trigger move command for both axes
                    TriggerServoMove(plc, ServoAxis.X3);
                    System.Threading.Thread.Sleep(50);
                    TriggerServoMove(plc, ServoAxis.Y3);

                    MessageBox.Show($"Moving Outfeed Robot to '{selectedPosition.Position}':\n" +
                  $"X={selectedPosition.X:F3}, Y={selectedPosition.Y:F3}",
               "Move Command Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving Outfeed robot: {ex.Message}",
              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
                     MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OutfeedTestVacuum_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Outfeed Test Vacuum functionality not yet implemented.",
                "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowOutfeedJogControls_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var machine = _mainWindow?.GetMachine();
                if (machine == null)
                {
                    MessageBox.Show("Machine not initialized. Please check system status.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var jogWindow = new RobotJogWindow(machine, RobotType.Outfeed);
                jogWindow.Owner = this;
                jogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Outfeed jog controls: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Inspect 1 Robot Controls
        private void SaveInspect1CurrentPosition_Click(object sender, RoutedEventArgs e)
        {
            if (Inspect1RobotGrid.SelectedItem is RobotPositionEntry selectedPosition && _servoMonitor != null)
            {
                try
                {
                    selectedPosition.Z = (float)_servoMonitor.GetCurrentPosition(ServoAxis.Z4);
                    selectedPosition.C = (float)_servoMonitor.GetCurrentPosition(ServoAxis.C4);

                    Inspect1RobotGrid.Items.Refresh();
                    _isModelDataChanged = true;

                    MessageBox.Show($"Current position saved to '{selectedPosition.Position}':\n" +
               $"Z={selectedPosition.Z:F3}, C={selectedPosition.C:F3}",
                      "Position Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading current position: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MoveInspect1ToSelected_Click(object sender, RoutedEventArgs e)
        {
            if (Inspect1RobotGrid.SelectedItem is RobotPositionEntry selectedPosition)
            {
                try
                {
                    var plc = GetPLCController();
                    if (plc == null || !plc.IsConnected)
                    {
                        MessageBox.Show("PLC is not connected. Please connect to PLC first.",
                   "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Write target positions for Z4, C4 axes
                    WriteServoTargetPosition(plc, ServoAxis.Z4, selectedPosition.Z, selectedPosition.SpeedZ,
                     selectedPosition.Accel, selectedPosition.Decel);
                    WriteServoTargetPosition(plc, ServoAxis.C4, selectedPosition.C, selectedPosition.Speed,
                      selectedPosition.Accel, selectedPosition.Decel);

                    // Trigger move command for both axes
                    TriggerServoMove(plc, ServoAxis.Z4);
                    System.Threading.Thread.Sleep(50);
                    TriggerServoMove(plc, ServoAxis.C4);

                    MessageBox.Show($"Moving Inspect 1 Robot to '{selectedPosition.Position}':\n" +
                     $"Z={selectedPosition.Z:F3}, C={selectedPosition.C:F3}",
                        "Move Command Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving Inspect 1 robot: {ex.Message}",
                       "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
           MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowInspect1JogControls_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var machine = _mainWindow?.GetMachine();
                if (machine == null)
                {
                    MessageBox.Show("Machine not initialized. Please check system status.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var jogWindow = new RobotJogWindow(machine, RobotType.Inspect1);
                jogWindow.Owner = this;
                jogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Inspect 1 jog controls: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Inspect 2 Robot Controls
        private void SaveInspect2CurrentPosition_Click(object sender, RoutedEventArgs e)
        {
            if (Inspect2RobotGrid.SelectedItem is RobotPositionEntry selectedPosition && _servoMonitor != null)
            {
                try
                {
                    selectedPosition.Z = (float)_servoMonitor.GetCurrentPosition(ServoAxis.Z5);
                    selectedPosition.C = (float)_servoMonitor.GetCurrentPosition(ServoAxis.C5);

                    Inspect2RobotGrid.Items.Refresh();
                    _isModelDataChanged = true;

                    MessageBox.Show($"Current position saved to '{selectedPosition.Position}':\n" +
                  $"Z={selectedPosition.Z:F3}, C={selectedPosition.C:F3}",
                   "Position Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading current position: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
                      MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MoveInspect2ToSelected_Click(object sender, RoutedEventArgs e)
        {
            if (Inspect2RobotGrid.SelectedItem is RobotPositionEntry selectedPosition)
            {
                try
                {
                    var plc = GetPLCController();
                    if (plc == null || !plc.IsConnected)
                    {
                        MessageBox.Show("PLC is not connected. Please connect to PLC first.",
                   "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Write target positions for Z5, C5 axes
                    WriteServoTargetPosition(plc, ServoAxis.Z5, selectedPosition.Z, selectedPosition.SpeedZ,
                      selectedPosition.Accel, selectedPosition.Decel);
                    WriteServoTargetPosition(plc, ServoAxis.C5, selectedPosition.C, selectedPosition.Speed,
                    selectedPosition.Accel, selectedPosition.Decel);

                    // Trigger move command for both axes
                    TriggerServoMove(plc, ServoAxis.Z5);
                    System.Threading.Thread.Sleep(50);
                    TriggerServoMove(plc, ServoAxis.C5);

                    MessageBox.Show($"Moving Inspect 2 Robot to '{selectedPosition.Position}':\n" +
                          $"Z={selectedPosition.Z:F3}, C={selectedPosition.C:F3}",
                    "Move Command Sent", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving Inspect 2 robot: {ex.Message}",
                 "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a position row first.", "No Selection",
         MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void ShowInspect2JogControls_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var machine = _mainWindow?.GetMachine();
                if (machine == null)
                {
                    MessageBox.Show("Machine not initialized. Please check system status.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var jogWindow = new RobotJogWindow(machine, RobotType.Inspect2);
                jogWindow.Owner = this;
                jogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Inspect 2 jog controls: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReadPositionsFromPLC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var plc = GetPLCController();

                if (plc == null || !plc.IsConnected)
                {
                    MessageBox.Show("PLC is not connected. Please connect to PLC first.", "PLC Not Connected",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _isLoadingModel = true;

                // Read robot positions from PLC
                ReadInfeedPositionsFromPLC(plc);
                ReadTransferPositionsFromPLC(plc);
                ReadOutfeedPositionsFromPLC(plc);
                ReadInspect1PositionsFromPLC(plc);
                ReadInspect2PositionsFromPLC(plc);

                _isLoadingModel = false;
                _isModelDataChanged = true;

                // Refresh the DataGrids
                InfeedRobotGrid.Items.Refresh();
                TransferRobotGrid.Items.Refresh();
                OutfeedRobotGrid.Items.Refresh();
                Inspect1RobotGrid.Items.Refresh();
                Inspect2RobotGrid.Items.Refresh();

                MessageBox.Show("Position data read from PLC successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _isLoadingModel = false;
                MessageBox.Show($"Error reading positions from PLC: {ex.Message}", "Error",
                  MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void WritePositionsToPLC_Click(object sender, RoutedEventArgs e)
        {
            var plc = GetPLCController();
            if (plc == null || !plc.IsConnected)
            {
                MessageBox.Show("PLC is not connected. Please connect to PLC first.", "PLC Not Connected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Are you sure you want to write all position data to PLC?\n\nThis will overwrite existing PLC position data.",
                "Confirm Write to PLC", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // Dùng chung spinner flow với SetActiveModel
            await WritePositionsToPLCWithSpinnerAsync();
        }

        private PLCController GetPLCController()
        {
            return _mainWindow?.GetMachine()?.PLC;
        }

        #endregion

        #region Helper Methods for Servo Control

        /// <summary>
        /// Write target position, speed, acceleration, and deceleration to PLC for a servo axis
        /// </summary>
        private void WriteServoTargetPosition(PLCController plc, ServoAxis axis, float position, float speed, float accel, float decel)
        {
            string axisName = ServoAddressCalculator.GetAxisDisplayName(axis);

            try
            {
                // Convert float values to LREAL (double) format for PLC
                double positionDouble = (double)position;
                double speedDouble = (double)speed;
                double accelDouble = (double)accel;
                double decelDouble = (double)decel;

                // Convert double to byte array (8 bytes for LREAL)
                byte[] posBytes = BitConverter.GetBytes(positionDouble);
                byte[] speedBytes = BitConverter.GetBytes(speedDouble);
                byte[] accelBytes = BitConverter.GetBytes(accelDouble);
                byte[] decelBytes = BitConverter.GetBytes(decelDouble);

                // Convert bytes to ushort array (4 registers for each LREAL)
                ushort[] posRegisters = new ushort[4];
                ushort[] speedRegisters = new ushort[4];
                ushort[] accelRegisters = new ushort[4];
                ushort[] decelRegisters = new ushort[4];

                for (int i = 0; i < 4; i++)
                {
                    posRegisters[i] = BitConverter.ToUInt16(posBytes, i * 2);
                    speedRegisters[i] = BitConverter.ToUInt16(speedBytes, i * 2);
                    accelRegisters[i] = BitConverter.ToUInt16(accelBytes, i * 2);
                    decelRegisters[i] = BitConverter.ToUInt16(decelBytes, i * 2);
                }

                // Get PLC addresses for target parameters
                ushort targetPosAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.TargetPosition);
                ushort targetSpeedAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.TargetSpeed);
                ushort accelAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.Acceleration);
                ushort decelAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.Deceleration);

                // Write directly to PLC holding register addresses (not using data point names)
                plc.WriteHoldingRegistersDirect(targetPosAddr, posRegisters);
                System.Threading.Thread.Sleep(30); // Delay between writes

                plc.WriteHoldingRegistersDirect(targetSpeedAddr, speedRegisters);
                System.Threading.Thread.Sleep(30);

                plc.WriteHoldingRegistersDirect(accelAddr, accelRegisters);
                System.Threading.Thread.Sleep(30);

                plc.WriteHoldingRegistersDirect(decelAddr, decelRegisters);

                Debug.WriteLine($"[ModelConfig] Written target position to {axisName}: Pos={position:F3}, Speed={speed:F1}, Accel={accel:F3}, Decel={decel:F3}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error writing target position for {axisName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Trigger the move command for a servo axis by pulsing the Move button
        /// </summary>
        private void TriggerServoMove(PLCController plc, ServoAxis axis)
        {
            string axisName = ServoAddressCalculator.GetAxisDisplayName(axis);
            string moveButtonName = $"HMI_{axisName}_Move_PB";

            try
            {
                // Pulse the move button (set true, wait, set false)
                plc.WriteCoil(moveButtonName, true);
                System.Threading.Thread.Sleep(100); // Hold button for 100ms
                plc.WriteCoil(moveButtonName, false);

                Debug.WriteLine($"[ModelConfig] Triggered move command for {axisName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error triggering move for {axisName}: {ex.Message}");
                throw;
            }
        }

        #endregion

        /// <summary>
        /// Cleanup when window is closing
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Stop monitoring and cleanup
            StopPositionMonitoring();
            _servoMonitor?.StopMonitoring();
            _servoMonitor?.Dispose();
            _positionUpdateTimer?.Stop();
        }
    }
}
