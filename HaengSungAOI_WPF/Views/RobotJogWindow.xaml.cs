using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HaengSungAOI_WPF.Machine;
using HaengSungAOI_WPF.Machine.PLC;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Utils;

namespace HaengSungAOI_WPF
{
    public partial class RobotJogWindow : Window
    {
        private Machine.Machine _machine;
        private RobotType _robotType;
        private PLCController _plc;
        private ServoMonitor _servoMonitor;
        private DispatcherTimer _positionUpdateTimer;
        private DispatcherTimer _lampUpdateTimer;
        
        // Track which buttons are currently pressed
        private Dictionary<string, bool> _buttonPressStates = new Dictionary<string, bool>();
        
        // Cache of buttons for lamp updates
        private Dictionary<string, Button> _buttonCache = new Dictionary<string, Button>();
        
        // Jog speed axis mapping: Axis1, Axis2, Axis3, Axis4 (nullable for robots with fewer axes)
        private ServoAxis? _jogSpeedAxis1;
        private ServoAxis? _jogSpeedAxis2;
        private ServoAxis? _jogSpeedAxis3;
        private ServoAxis? _jogSpeedAxis4;


        // Lamp state colors
        private static readonly SolidColorBrush LampOnBrush = new SolidColorBrush(Color.FromRgb(0, 200, 0)); // Bright green
        private static readonly SolidColorBrush LampOffBrush = new SolidColorBrush(Color.FromRgb(35, 35, 54)); // #232336
        private static readonly SolidColorBrush ServoOnLampBrush = new SolidColorBrush(Color.FromRgb(0, 255, 100)); // Bright green for servo
        private static readonly SolidColorBrush JogActiveBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Orange for jog active

        public RobotJogWindow(Machine.Machine machine, RobotType robotType)
        {
            InitializeComponent();
            _machine = machine;
            _robotType = robotType;
            _plc = machine?.PLC;
            
            SetupJogControls();
            InitializePositionMonitoring();
            InitializeLampMonitoring();
            
            // Set title based on robot type
            TitleTextBlock.Text = $"{robotType} Robot Jog Controls";
            this.Title = $"{robotType} Robot Jog Controls";
            
            // Read initial jog speeds from PLC
            ReadAllJogSpeedsFromPLC();
            
            // SWITCH TO ROBOTJOG MONITORING GROUPS when window opens
            if (_plc != null && _plc.IsConnected)
            {
                _plc.SetActiveMonitoringGroups(PLCConstants.ROBOTJOG_MONITORING_GROUPS);
                Logger.Info("RobotJogWindow", $"Switched PLC to ROBOTJOG_MONITORING_GROUPS for {robotType}");
            }
            
            // Cleanup on window closing
            this.Closing += RobotJogWindow_Closing;
        }

        #region Lamp Monitoring

        /// <summary>
        /// Initialize lamp monitoring to update button backgrounds based on PLC lamp states
        /// </summary>
        private void InitializeLampMonitoring()
        {
            try
            {
                // Cache all buttons for faster lookup during lamp updates
                CacheButtons();
                
                // Setup lamp update timer
                _lampUpdateTimer = new DispatcherTimer();
                _lampUpdateTimer.Interval = TimeSpan.FromMilliseconds(200);
                _lampUpdateTimer.Tick += UpdateLampStates;
                _lampUpdateTimer.Start();
                
                // Subscribe to PLC data changes for immediate updates
                if (_plc != null)
                {
                    _plc.DataChanged += OnPLCDataChanged;
                }
                
                Logger.Info("RobotJogWindow", $"Lamp monitoring initialized for {_robotType}");
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error initializing lamp monitoring: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cache all buttons in the visible robot group for faster lookup
        /// </summary>
        private void CacheButtons()
        {
            _buttonCache.Clear();
            
            // Get the visible axis group based on robot type
            FrameworkElement visibleGroup = null;
            switch (_robotType)
            {
                case RobotType.Infeed:
                    visibleGroup = InfeedAxesGroup;
                    break;
                case RobotType.Transfer:
                    visibleGroup = TransferAxesGroup;
                    break;
                case RobotType.Outfeed:
                    visibleGroup = OutfeedAxesGroup;
                    break;
                case RobotType.Inspect1:
                    visibleGroup = Inspect1AxesGroup;
                    break;
                case RobotType.Inspect2:
                    visibleGroup = Inspect2AxesGroup;
                    break;
            }
            
            // Cache buttons from the left axis group
            if (visibleGroup != null)
            {
                CacheButtonsRecursive(visibleGroup);
            }
            
            // Also cache jog pad buttons and cylinder/vacuum buttons from right side
            CacheButtonsRecursive(JogControlsGrid);
            
            Logger.Debug("RobotJogWindow", $"Cached {_buttonCache.Count} buttons for lamp monitoring");
        }

        /// <summary>
        /// Recursively find and cache all buttons with tags
        /// </summary>
        private void CacheButtonsRecursive(DependencyObject parent)
        {
            if (parent == null) return;
            
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Button button && button.Tag is string tag && !string.IsNullOrEmpty(tag))
                {
                    _buttonCache[tag] = button;
                }
                
                CacheButtonsRecursive(child);
            }
        }

        /// <summary>
        /// Handle PLC data changes for immediate lamp updates
        /// </summary>
        private void OnPLCDataChanged(object sender, PLCDataChangedEventArgs e)
        {
            try
            {
                // Check if this is a lamp data point
                if (e.DataPointName.Contains("Lamp") || e.DataPointName.EndsWith("_LP"))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateLampForDataPoint(e.DataPointName, e.NewValue);
                    }));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error handling PLC data change: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update lamp state for a specific data point
        /// </summary>
        private void UpdateLampForDataPoint(string dataPointName, object value)
        {
            try
            {
                // Convert lamp data point name to button tag
                // Lamp format: HMI_AX1_ServoON_LP -> Button tag: HMI_AX1_ServoON_PB
                string buttonTag = dataPointName.Replace("_LP", "_PB");
                
                if (_buttonCache.TryGetValue(buttonTag, out Button button))
                {
                    bool isOn = ConvertToLampState(value);
                    UpdateButtonLamp(button, isOn);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("RobotJogWindow", $"Error updating lamp for {dataPointName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Periodically update all lamp states from PLC
        /// </summary>
        private void UpdateLampStates(object sender, EventArgs e)
        {
            if (_plc == null || !_plc.IsConnected) return;
            
            try
            {
                foreach (var kvp in _buttonCache)
                {
                    string buttonTag = kvp.Key;
                    Button button = kvp.Value;
                    
                    // Try to get lamp state from PLC
                    bool lampState = GetLampStateForButton(buttonTag);
                    UpdateButtonLamp(button, lampState);
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("RobotJogWindow", $"Error updating lamp states: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the lamp state for a button from the PLC
        /// </summary>
        private bool GetLampStateForButton(string buttonTag)
        {
            try
            {
                ushort? lampAddress = GetLampAddressForButton(buttonTag);
                if (!lampAddress.HasValue)
                    return false;
                
                // Read lamp state directly from PLC register
                var registers = _plc.ReadHoldingRegistersDirect(lampAddress.Value, 1);
                if (registers != null && registers.Length > 0)
                {
                    return registers[0] != 0;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.Debug("RobotJogWindow", $"Error getting lamp state for {buttonTag}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the lamp address for a button tag
        /// </summary>
        private ushort? GetLampAddressForButton(string buttonTag)
        {
            try
            {
                // First check if it's a cylinder/vacuum button in PLCAddresses.HMI_Lamps
                string lampKey = "HMI_Lamp_" + buttonTag.Substring(4); // Remove "HMI_" and add "HMI_Lamp_"
                if (PLCAddresses.HMI_Lamps.TryGetValue(lampKey, out ushort lampAddr))
                {
                    return lampAddr;
                }
                
                // Check for servo axis buttons (format: HMI_AX1_ServoON_PB)
                if (buttonTag.StartsWith("HMI_A"))
                {
                    var parts = buttonTag.Split('_');
                    if (parts.Length >= 4)
                    {
                        string axisName = parts[1]; // e.g., "AX1", "AY1", "AC1"
                        string buttonType = parts[2]; // e.g., "ServoON", "JogPlus", "Pos1"
                        
                        // Map axis name to ServoAxis enum
                        ServoAxis? axis = MapAxisNameToServoAxis(axisName);
                        if (!axis.HasValue) return null;
                        
                        // Map button type to ServoHMIButton enum
                        ServoHMIButton? hmiButton = MapButtonTypeToServoHMIButton(buttonType);
                        if (!hmiButton.HasValue) return null;
                        
                        // Get the lamp address from ServoAddressCalculator
                        return ServoAddressCalculator.GetHMILampAddress(axis.Value, hmiButton.Value);
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Map axis name string to ServoAxis enum
        /// </summary>
        private ServoAxis? MapAxisNameToServoAxis(string axisName)
        {
            switch (axisName.ToUpper())
            {
                case "AX1": return ServoAxis.X1;
                case "AY1": return ServoAxis.Y1;
                case "AC1": return ServoAxis.C1;
                case "AX2": return ServoAxis.X2;
                case "AZ2": return ServoAxis.Z2;
                case "AX3": return ServoAxis.X3;
                case "AY3": return ServoAxis.Y3;
                case "AZ4": return ServoAxis.Z4;
                case "AC4": return ServoAxis.C4;
                case "AZ5": return ServoAxis.Z5;
                case "AC5": return ServoAxis.C5;
                case "AZ61": return ServoAxis.Z61;
                case "AZ62": return ServoAxis.Z62;
                case "CV7": return ServoAxis.CV7;
                default: return null;
            }
        }

        /// <summary>
        /// Map button type string to ServoHMIButton enum
        /// </summary>
        private ServoHMIButton? MapButtonTypeToServoHMIButton(string buttonType)
        {
            switch (buttonType)
            {
                case "ServoON": return ServoHMIButton.ServoON;
                case "ORG": return ServoHMIButton.ORG;
                case "JogPlus": return ServoHMIButton.JogPlus;
                case "JogMinus": return ServoHMIButton.JogMinus;
                case "JogPlusHispeed": return ServoHMIButton.JogPlusHispeed;
                case "JogMinusHispeed": return ServoHMIButton.JogMinusHispeed;
                case "InchingPlus": return ServoHMIButton.InchingPlus;
                case "InchingMinus": return ServoHMIButton.InchingMinus;
                case "StepPlus": return ServoHMIButton.StepPlus;
                case "StepMinus": return ServoHMIButton.StepMinus;
                case "Move": return ServoHMIButton.Move;
                case "Pos1": return ServoHMIButton.Pos1;
                case "Pos2": return ServoHMIButton.Pos2;
                case "Pos3": return ServoHMIButton.Pos3;
                case "Pos4": return ServoHMIButton.Pos4;
                case "Pos5": return ServoHMIButton.Pos5;
                case "Pos6": return ServoHMIButton.Pos6;
                case "Pos7": return ServoHMIButton.Pos7;
                case "Pos8": return ServoHMIButton.Pos8;
                case "Pos9": return ServoHMIButton.Pos9;
                case "Pos10": return ServoHMIButton.Pos10;
                case "Pos11": return ServoHMIButton.Pos11;
                default: return null;
            }
        }

        /// <summary>
        /// Convert PLC value to lamp state boolean
        /// </summary>
        private bool ConvertToLampState(object value)
        {
            if (value == null) return false;
            
            if (value is bool b) return b;
            if (value is ushort u) return u != 0;
            if (value is int i) return i != 0;
            if (value is ushort[] arr && arr.Length > 0) return arr[0] != 0;
            
            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Update a button's appearance based on lamp state
        /// </summary>
        private void UpdateButtonLamp(Button button, bool isOn)
        {
            try
            {
                if (button == null) return;
                
                string content = button.Content?.ToString() ?? "";
                
                if (isOn)
                {
                    // Different colors for different button types
                    if (content.Contains("Servo"))
                    {
                        button.Background = ServoOnLampBrush;
                    }
                    else if (content.Contains("Jog"))
                    {
                        button.Background = JogActiveBrush;
                    }
                    else
                    {
                        button.Background = LampOnBrush;
                    }
                    button.Foreground = Brushes.Black;
                }
                else
                {
                    button.Background = LampOffBrush;
                    button.Foreground = Brushes.White;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("RobotJogWindow", $"Error updating button lamp: {ex.Message}");
            }
        }

        #endregion

        #region PLC Button Event Handlers

        /// <summary>
        /// Get the PLC address for a button tag
        /// </summary>
        private ushort? GetButtonAddressForTag(string buttonTag)
        {
            try
            {
                // First check if it's a cylinder/vacuum button in PLCAddresses.HMI_PushButtons
                if (PLCAddresses.HMI_PushButtons.TryGetValue(buttonTag, out ushort pbAddr))
                {
                    return pbAddr;
                }
                
                // Check for servo axis buttons (format: HMI_AX1_ServoON_PB)
                if (buttonTag.StartsWith("HMI_A"))
                {
                    var parts = buttonTag.Split('_');
                    if (parts.Length >= 4)
                    {
                        string axisName = parts[1]; // e.g., "AX1", "AY1", "AC1"
                        string buttonType = parts[2]; // e.g., "ServoON", "JogPlus", "Pos1"
                        
                        // Map axis name to ServoAxis enum
                        ServoAxis? axis = MapAxisNameToServoAxis(axisName);
                        if (!axis.HasValue) return null;
                        
                        // Map button type to ServoHMIButton enum
                        ServoHMIButton? hmiButton = MapButtonTypeToServoHMIButton(buttonType);
                        if (!hmiButton.HasValue) return null;
                        
                        // Get the button address from ServoAddressCalculator
                        return ServoAddressCalculator.GetHMIButtonAddress(axis.Value, hmiButton.Value);
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Handle mouse down events for PLC control buttons - Set register to 1
        /// </summary>
        private void PLCButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string buttonTag)
                {
                    if (_plc != null && _plc.IsConnected)
                    {
                        // Track button state
                        _buttonPressStates[buttonTag] = true;
                        
                        // Get the PLC address for this button
                        ushort? address = GetButtonAddressForTag(buttonTag);
                        if (!address.HasValue)
                        {
                            Logger.Warning("RobotJogWindow", $"Unknown button tag: {buttonTag}");
                            return;
                        }
                        
                        // Write 1 to the PLC register on mouse down
                        Task.Run(() =>
                        {
                            try
                            {
                                _plc.WriteHoldingRegistersDirect(address.Value, new ushort[] { 1 });
                                Logger.Debug("RobotJogWindow", $"Button {buttonTag} pressed - Set MW{address.Value} to 1");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("RobotJogWindow", $"Error writing 1 to PLC address MW{address.Value}: {ex.Message}", ex);
                            }
                        });
                    }
                    else
                    {
                        Logger.Warning("RobotJogWindow", $"Cannot write to {buttonTag}: PLC not connected");
                        MessageBox.Show("PLC is not connected. Please check PLC connection in settings.", 
                            "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error in PLCButton_MouseDown: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handle mouse up events for PLC control buttons - Set register to 0
        /// </summary>
        private void PLCButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string buttonTag)
                {
                    // Track button state
                    _buttonPressStates[buttonTag] = false;
                    
                    if (_plc != null && _plc.IsConnected)
                    {
                        // Get the PLC address for this button
                        ushort? address = GetButtonAddressForTag(buttonTag);
                        if (!address.HasValue)
                        {
                            Logger.Warning("RobotJogWindow", $"Unknown button tag: {buttonTag}");
                            return;
                        }
                        
                        // Write 0 to the PLC register on mouse up
                        Task.Run(() =>
                        {
                            try
                            {
                                _plc.WriteHoldingRegistersDirect(address.Value, new ushort[] { 0 });
                                Logger.Debug("RobotJogWindow", $"Button {buttonTag} released - Set MW{address.Value} to 0");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("RobotJogWindow", $"Error writing 0 to PLC address MW{address.Value}: {ex.Message}", ex);
                            }
                        });
                    }
                    else
                    {
                        Logger.Warning("RobotJogWindow", $"Cannot write to {buttonTag}: PLC not connected");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error in PLCButton_MouseUp: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Release all PLC buttons that are currently pressed (for safety)
        /// </summary>
        private void ReleaseAllPLCButtons()
        {
            try
            {
                if (_plc == null || !_plc.IsConnected)
                {
                    Logger.Warning("RobotJogWindow", "Cannot release PLC buttons - PLC not connected");
                    return;
                }

                foreach (var kvp in _buttonPressStates)
                {
                    if (kvp.Value) // If button is pressed
                    {
                        try
                        {
                            ushort? address = GetButtonAddressForTag(kvp.Key);
                            if (address.HasValue)
                            {
                                _plc.WriteHoldingRegistersDirect(address.Value, new ushort[] { 0 });
                                Logger.Debug("RobotJogWindow", $"Released button {kvp.Key} (MW{address.Value})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("RobotJogWindow", $"Error releasing button {kvp.Key}: {ex.Message}", ex);
                        }
                    }
                }

                _buttonPressStates.Clear();
                Logger.Info("RobotJogWindow", "Released all PLC buttons");
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error in ReleaseAllPLCButtons: {ex.Message}", ex);
            }
        }

        #endregion

        private void InitializePositionMonitoring()
        {
            try
            {
                if (_plc != null && _plc.IsConnected)
                {
                    // Create servo monitor for position updates
                    _servoMonitor = new ServoMonitor(_plc, 200);
                    _servoMonitor.StartMonitoring();
                    
                    // Setup position update timer
                    _positionUpdateTimer = new DispatcherTimer();
                    _positionUpdateTimer.Interval = TimeSpan.FromMilliseconds(200);
                    _positionUpdateTimer.Tick += UpdatePositionDisplays;
                    _positionUpdateTimer.Start();
                    
                    Logger.Info("RobotJogWindow", $"Position monitoring started for {_robotType}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error initializing position monitoring: {ex.Message}", ex);
            }
        }

        private void UpdatePositionDisplays(object sender, EventArgs e)
        {
            if (_servoMonitor == null) return;

            try
            {
                // Update position displays based on robot type
                switch (_robotType)
                {
                    case RobotType.Infeed:
                        UpdateAxisPosition("InfeedJogXPosition", ServoAxis.X1);
                        UpdateAxisPosition("InfeedJogYPosition", ServoAxis.Y1);
                        UpdateAxisPosition("InfeedJogRPosition", ServoAxis.C1);
                        break;
                    case RobotType.Transfer:
                        UpdateAxisPosition("TransferJogXPosition", ServoAxis.X2);
                        UpdateAxisPosition("TransferJogZPosition", ServoAxis.Z2);
                        break;
                    case RobotType.Outfeed:
                        UpdateAxisPosition("OutfeedJogXPosition", ServoAxis.X3);
                        UpdateAxisPosition("OutfeedJogYPosition", ServoAxis.Y3);
                        UpdateAxisPosition("OutfeedJogZ61Position", ServoAxis.Z61);
                        UpdateAxisPosition("OutfeedJogZ62Position", ServoAxis.Z62);
                        break;
                    case RobotType.Inspect1:
                        UpdateAxisPosition("InspectJogZPosition", ServoAxis.Z4);
                        UpdateAxisPosition("InspectJogCPosition", ServoAxis.C4);
                        break;
                    case RobotType.Inspect2:
                        UpdateAxisPosition("InspectJogZPosition", ServoAxis.Z5);
                        UpdateAxisPosition("InspectJogCPosition", ServoAxis.C5);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error updating positions: {ex.Message}", ex);
            }
        }

        private void UpdateAxisPosition(string controlTag, ServoAxis axis)
        {
            try
            {
                double position = _servoMonitor.GetCurrentPosition(axis);
                
                // Find the position TextBox by tag
                var positionTextBox = FindPositionTextBox(controlTag);
                if (positionTextBox != null)
                {
                    positionTextBox.Text = $"{position:F3}";
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("RobotJogWindow", $"Error updating axis {axis} position: {ex.Message}");
            }
        }

        private TextBox FindPositionTextBox(string tag)
        {
            return FindControlByTag<TextBox>(JogControlsGrid, tag);
        }

        private T FindControlByTag<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T element && element.Tag?.ToString() == tag)
                {
                    return element;
                }

                var result = FindControlByTag<T>(child, tag);
                if (result != null)
                    return result;
            }
            
            return null;
        }

        private void SetupJogControls()
        {
            // Hide all robot axis groups first
            InfeedAxesGroup.Visibility = Visibility.Collapsed;
            TransferAxesGroup.Visibility = Visibility.Collapsed;
            OutfeedAxesGroup.Visibility = Visibility.Collapsed;
            Inspect1AxesGroup.Visibility = Visibility.Collapsed;
            Inspect2AxesGroup.Visibility = Visibility.Collapsed;
            
            // Show the appropriate group and configure jog pad based on robot type
            switch (_robotType)
            {
                case RobotType.Infeed:
                    InfeedAxesGroup.Visibility = Visibility.Visible;
                    SetupJogPadForInfeed();
                    break;
                case RobotType.Transfer:
                    TransferAxesGroup.Visibility = Visibility.Visible;
                    SetupJogPadForTransfer();
                    break;
                case RobotType.Outfeed:
                    OutfeedAxesGroup.Visibility = Visibility.Visible;
                    SetupJogPadForOutfeed();
                    break;
                case RobotType.Inspect1:
                    Inspect1AxesGroup.Visibility = Visibility.Visible;
                    SetupJogPadForInspect1();
                    break;
                case RobotType.Inspect2:
                    Inspect2AxesGroup.Visibility = Visibility.Visible;
                    SetupJogPadForInspect2();
                    break;
            }
            
            Logger.Info("RobotJogWindow", $"Setup jog controls for {_robotType} robot");
        }

        #region Jog Pad Setup Methods

        private void SetupJogPadForInfeed()
        {
            // X/Y Jog buttons for Infeed (X1, Y1)
            JogXPlusBtn.Tag = "HMI_AX1_JogPlus_PB";
            JogXMinusBtn.Tag = "HMI_AX1_JogMinus_PB";
            JogYPlusBtn.Tag = "HMI_AY1_JogPlus_PB";
            JogYMinusBtn.Tag = "HMI_AY1_JogMinus_PB";
            
            // Hide Z jog (Infeed has no Z)
            JogZPlusBtn.Visibility = Visibility.Collapsed;
            JogZMinusBtn.Visibility = Visibility.Collapsed;

            // Z Tray In/Out (Infeed has no Z61,Z62)
            JogZ61MinusBtn.Visibility = Visibility.Collapsed;
            JogZ61PlusBtn.Visibility = Visibility.Collapsed;
            JogZ62MinusBtn.Visibility = Visibility.Collapsed;
            JogZ62PlusBtn.Visibility = Visibility.Collapsed;

            // R jog for C1 axis
            JogRPlusBtn.Tag = "HMI_AC1_JogPlus_PB";
            JogRMinusBtn.Tag = "HMI_AC1_JogMinus_PB";

            // Cylinder & Vacuum
            CylinderTrayUpBtn.Visibility = Visibility.Collapsed;
            CylinderTrayDownBtn.Visibility = Visibility.Collapsed;
            VacuumTrayOnBtn.Visibility = Visibility.Collapsed;
            VacuumTrayOffBtn.Visibility = Visibility.Collapsed;
            CylinderUpBtn.Tag = "HMI_Cyl_Infeed_Up_PB";
            CylinderDownBtn.Tag = "HMI_Cyl_Infeed_Down_PB";
            VacuumOnBtn.Tag = "HMI_Vacuum_Infeed_ON_PB";
            VacuumOffBtn.Tag = "HMI_Vacuum_Infeed_OFF_PB";
            
            // Jog speed axes
            _jogSpeedAxis1 = ServoAxis.X1;
            _jogSpeedAxis2 = ServoAxis.Y1;
            _jogSpeedAxis3 = ServoAxis.C1;
            JogSpeedAxis1Label.Text = "X1 Jog Speed:";
            JogSpeedAxis2Label.Text = "Y1 Jog Speed:";
            JogSpeedAxis3Label.Text = "C1 Jog Speed:";
            JogSpeedAxis3Panel.Visibility = Visibility.Visible;
        }

        private void SetupJogPadForTransfer()
        {
            // X jog for Transfer (X2)
            JogXPlusBtn.Tag = "HMI_AX2_JogPlus_PB";
            JogXMinusBtn.Tag = "HMI_AX2_JogMinus_PB";
            
            // Hide Y jog (Transfer has no Y)
            JogYPlusBtn.Visibility = Visibility.Collapsed;
            JogYMinusBtn.Visibility = Visibility.Collapsed;
            
            // Z jog for Z2 axis
            JogZPlusBtn.Tag = "HMI_AZ2_JogPlus_PB";
            JogZMinusBtn.Tag = "HMI_AZ2_JogMinus_PB";

            // Z Tray In/Out (Transfer has no Z61,Z62)
            JogZ61MinusBtn.Visibility = Visibility.Collapsed;
            JogZ61PlusBtn.Visibility = Visibility.Collapsed;
            JogZ62MinusBtn.Visibility = Visibility.Collapsed;
            JogZ62PlusBtn.Visibility = Visibility.Collapsed;

            // Hide R jog (Transfer has no rotation)
            JogRPlusBtn.Visibility = Visibility.Collapsed;
            JogRMinusBtn.Visibility = Visibility.Collapsed;

            // Cylinder & Vacuum           
            CylinderTrayUpBtn.Visibility = Visibility.Collapsed;
            CylinderTrayDownBtn.Visibility = Visibility.Collapsed;
            VacuumTrayOnBtn.Visibility = Visibility.Collapsed;
            VacuumTrayOffBtn.Visibility = Visibility.Collapsed;
            CylinderUpBtn.Tag = "HMI_Cyl_NG_Up_PB";
            CylinderDownBtn.Tag = "HMI_Cyl_NG_Down_PB";
            VacuumOnBtn.Tag = "HMI_Vacuum_Transfer_ON_PB";
            VacuumOffBtn.Tag = "HMI_Vacuum_Transfer_OFF_PB";
            
            // Jog speed axes
            _jogSpeedAxis1 = ServoAxis.X2;
            _jogSpeedAxis2 = ServoAxis.Z2;
            _jogSpeedAxis3 = null;
            JogSpeedAxis1Label.Text = "X2 Jog Speed:";
            JogSpeedAxis2Label.Text = "Z2 Jog Speed:";
            JogSpeedAxis3Panel.Visibility = Visibility.Collapsed;
        }

        private void SetupJogPadForOutfeed()
        {
            // X/Y Jog buttons for Outfeed (X3, Y3)
            JogXPlusBtn.Tag = "HMI_AX3_JogPlus_PB";
            JogXMinusBtn.Tag = "HMI_AX3_JogMinus_PB";
            JogYPlusBtn.Tag = "HMI_AY3_JogPlus_PB";
            JogYMinusBtn.Tag = "HMI_AY3_JogMinus_PB";            

            // Hide Z jog (Outfeed has no Z)
            JogZPlusBtn.Visibility = Visibility.Collapsed;
            JogZMinusBtn.Visibility = Visibility.Collapsed;

            // Z Tray In/Out (Z61,Z62)
            JogZ61MinusBtn.Tag = "HMI_AZ61_JogMinus_PB";
            JogZ61PlusBtn.Tag = "HMI_AZ61_JogPlus_PB";
            JogZ62MinusBtn.Tag = "HMI_AZ62_JogMinus_PB";
            JogZ62PlusBtn.Tag = "HMI_AZ62_JogPlus_PB";

            // Hide R jog (Outfeed has no rotation)
            JogRPlusBtn.Visibility = Visibility.Collapsed;
            JogRMinusBtn.Visibility = Visibility.Collapsed;
            
            // Cylinder & Vacuum
            CylinderUpBtn.Tag = "HMI_Cyl_Outfeed_Up_PB";
            CylinderDownBtn.Tag = "HMI_Cyl_Outfeed_Down_PB";
            VacuumOnBtn.Tag = "HMI_Vacuum_Outfeed_ON_PB";
            VacuumOffBtn.Tag = "HMI_Vacuum_Outfeed_OFF_PB";
            CylinderTrayUpBtn.Tag = "HMI_Cyl_Pickup_Tray_Up_PB";
            CylinderTrayDownBtn.Tag = "HMI_Cyl_Pickup_Tray_Down_PB";
            VacuumTrayOnBtn.Tag = "HMI_Vacuum_Pickup_Tray_ON_PB";
            VacuumTrayOffBtn.Tag = "HMI_Vacuum_Pickup_Tray_OFF_PB";

            // Jog speed axes
            _jogSpeedAxis1 = ServoAxis.X3;
            _jogSpeedAxis2 = ServoAxis.Y3;
            _jogSpeedAxis3 = ServoAxis.Z61;
            _jogSpeedAxis4 = ServoAxis.Z62;
            JogSpeedAxis1Label.Text = "X3 Jog Speed:";
            JogSpeedAxis2Label.Text = "Y3 Jog Speed:";
            JogSpeedAxis3Label.Text = "Z61 Jog Speed:";
            JogSpeedAxis4Label.Text = "Z62 Jog Speed:";
            JogSpeedAxis4Panel.Visibility = Visibility.Collapsed;
        }

        private void SetupJogPadForInspect1()
        {
            // Hide X/Y jog (Inspect has no X/Y)
            JogXPlusBtn.Visibility = Visibility.Collapsed;
            JogXMinusBtn.Visibility = Visibility.Collapsed;
            JogYPlusBtn.Visibility = Visibility.Collapsed;
            JogYMinusBtn.Visibility = Visibility.Collapsed;
            
            // Z jog for Z4 axis (Focus)
            JogZPlusBtn.Tag = "HMI_AZ4_JogPlus_PB";
            JogZMinusBtn.Tag = "HMI_AZ4_JogMinus_PB";

            // Z Tray In/Out (Transfer has no Z61,Z62)
            JogZ61MinusBtn.Visibility = Visibility.Collapsed;
            JogZ61PlusBtn.Visibility = Visibility.Collapsed;
            JogZ62MinusBtn.Visibility = Visibility.Collapsed;
            JogZ62PlusBtn.Visibility = Visibility.Collapsed;

            // R jog for C4 axis (Rotation)
            JogRPlusBtn.Tag = "HMI_AC4_JogPlus_PB";
            JogRMinusBtn.Tag = "HMI_AC4_JogMinus_PB";

            // Vacuum (no cylinder for inspect)
            CylinderUpBtn.Visibility = Visibility.Collapsed;
            CylinderDownBtn.Visibility = Visibility.Collapsed;
            CylinderTrayUpBtn.Visibility = Visibility.Collapsed;
            CylinderTrayDownBtn.Visibility = Visibility.Collapsed;
            VacuumTrayOnBtn.Visibility = Visibility.Collapsed;
            VacuumTrayOffBtn.Visibility = Visibility.Collapsed;
            VacuumOnBtn.Tag = "HMI_Vacuum_Inspect1_ON_PB";
            VacuumOffBtn.Tag = "HMI_Vacuum_Inspect1_OFF_PB";
            
            // Jog speed axes
            _jogSpeedAxis1 = ServoAxis.Z4;
            _jogSpeedAxis2 = ServoAxis.C4;
            _jogSpeedAxis3 = null;
            JogSpeedAxis1Label.Text = "Z4 Jog Speed:";
            JogSpeedAxis2Label.Text = "C4 Jog Speed:";
            JogSpeedAxis3Panel.Visibility = Visibility.Collapsed;
        }

        private void SetupJogPadForInspect2()
        {
            // Hide X/Y jog (Inspect has no X/Y)
            JogXPlusBtn.Visibility = Visibility.Collapsed;
            JogXMinusBtn.Visibility = Visibility.Collapsed;
            JogYPlusBtn.Visibility = Visibility.Collapsed;
            JogYMinusBtn.Visibility = Visibility.Collapsed;
            
            // Z jog for Z5 axis (Focus)
            JogZPlusBtn.Tag = "HMI_AZ5_JogPlus_PB";
            JogZMinusBtn.Tag = "HMI_AZ5_JogMinus_PB";

            // Z Tray In/Out (Transfer has no Z61,Z62)
            JogZ61MinusBtn.Visibility = Visibility.Collapsed;
            JogZ61PlusBtn.Visibility = Visibility.Collapsed;
            JogZ62MinusBtn.Visibility = Visibility.Collapsed;
            JogZ62PlusBtn.Visibility = Visibility.Collapsed;

            // R jog for C5 axis (Rotation)
            JogRPlusBtn.Tag = "HMI_AC5_JogPlus_PB";
            JogRMinusBtn.Tag = "HMI_AC5_JogMinus_PB";

            // Vacuum (no cylinder for inspect)
            CylinderUpBtn.Visibility = Visibility.Collapsed;
            CylinderDownBtn.Visibility = Visibility.Collapsed;
            CylinderTrayUpBtn.Visibility = Visibility.Collapsed;
            CylinderTrayDownBtn.Visibility = Visibility.Collapsed;
            VacuumTrayOnBtn.Visibility = Visibility.Collapsed;
            VacuumTrayOffBtn.Visibility = Visibility.Collapsed;
            VacuumOnBtn.Tag = "HMI_Vacuum_Inspect2_ON_PB";
            VacuumOffBtn.Tag = "HMI_Vacuum_Inspect2_OFF_PB";
            
            // Jog speed axes
            _jogSpeedAxis1 = ServoAxis.Z5;
            _jogSpeedAxis2 = ServoAxis.C5;
            _jogSpeedAxis3 = null;
            JogSpeedAxis1Label.Text = "Z5 Jog Speed:";
            JogSpeedAxis2Label.Text = "C5 Jog Speed:";
            JogSpeedAxis3Panel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Jog Speed Read/Write

        private ServoAxis? GetAxisForTag(string tag)
        {
            switch (tag)
            {
                case "Axis1": return _jogSpeedAxis1;
                case "Axis2": return _jogSpeedAxis2;
                case "Axis3": return _jogSpeedAxis3;
                default: return null;
            }
        }

        private TextBox GetSpeedTextBoxForTag(string tag)
        {
            switch (tag)
            {
                case "Axis1": return JogSpeedAxis1TextBox;
                case "Axis2": return JogSpeedAxis2TextBox;
                case "Axis3": return JogSpeedAxis3TextBox;
                default: return null;
            }
        }

        private void ReadJogSpeedFromPLC(ServoAxis axis, TextBox textBox)
        {
            if (_plc == null || !_plc.IsConnected || textBox == null) return;

            try
            {
                ushort address = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.JogSpeed);
                var registers = _plc.ReadHoldingRegistersDirect(address, 4);
                if (registers != null && registers.Length >= 4)
                {
                    byte[] bytes = new byte[8];
                    Buffer.BlockCopy(registers, 0, bytes, 0, 8);
                    double speed = BitConverter.ToDouble(bytes, 0);
                    textBox.Text = speed.ToString("F1");
                    Logger.Debug("RobotJogWindow", $"Read JogSpeed for {axis}: {speed:F1} from MW{address}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error reading jog speed for {axis}: {ex.Message}", ex);
            }
        }

        private void WriteJogSpeedToPLC(ServoAxis axis, TextBox textBox)
        {
            if (_plc == null || !_plc.IsConnected || textBox == null) return;

            try
            {
                if (!double.TryParse(textBox.Text, out double speed))
                {
                    MessageBox.Show("Please enter a valid numeric value.", "Invalid Input",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ushort address = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.JogSpeed);
                byte[] bytes = BitConverter.GetBytes(speed);
                ushort[] registers = new ushort[4];
                Buffer.BlockCopy(bytes, 0, registers, 0, 8);
                _plc.WriteHoldingRegistersDirect(address, registers);
                Logger.Info("RobotJogWindow", $"Wrote JogSpeed for {axis}: {speed:F1} to MW{address}");
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error writing jog speed for {axis}: {ex.Message}", ex);
                MessageBox.Show($"Error writing jog speed: {ex.Message}", "PLC Write Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReadAllJogSpeedsFromPLC()
        {
            if (_jogSpeedAxis1.HasValue)
                ReadJogSpeedFromPLC(_jogSpeedAxis1.Value, JogSpeedAxis1TextBox);
            if (_jogSpeedAxis2.HasValue)
                ReadJogSpeedFromPLC(_jogSpeedAxis2.Value, JogSpeedAxis2TextBox);
            if (_jogSpeedAxis3.HasValue)
                ReadJogSpeedFromPLC(_jogSpeedAxis3.Value, JogSpeedAxis3TextBox);
        }

        private void WriteAllJogSpeedsToPLC()
        {
            if (_jogSpeedAxis1.HasValue)
                WriteJogSpeedToPLC(_jogSpeedAxis1.Value, JogSpeedAxis1TextBox);
            if (_jogSpeedAxis2.HasValue)
                WriteJogSpeedToPLC(_jogSpeedAxis2.Value, JogSpeedAxis2TextBox);
            if (_jogSpeedAxis3.HasValue)
                WriteJogSpeedToPLC(_jogSpeedAxis3.Value, JogSpeedAxis3TextBox);
        }

        private void ReadJogSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                var axis = GetAxisForTag(tag);
                var textBox = GetSpeedTextBoxForTag(tag);
                if (axis.HasValue && textBox != null)
                    ReadJogSpeedFromPLC(axis.Value, textBox);
            }
        }

        private void WriteJogSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                var axis = GetAxisForTag(tag);
                var textBox = GetSpeedTextBoxForTag(tag);
                if (axis.HasValue && textBox != null)
                    WriteJogSpeedToPLC(axis.Value, textBox);
            }
        }

        private void ReadAllJogSpeeds_Click(object sender, RoutedEventArgs e)
        {
            ReadAllJogSpeedsFromPLC();
        }

        private void WriteAllJogSpeeds_Click(object sender, RoutedEventArgs e)
        {
            WriteAllJogSpeedsToPLC();
        }

        private void JogSpeedTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                // Find which axis this textbox belongs to and write
                if (textBox == JogSpeedAxis1TextBox && _jogSpeedAxis1.HasValue)
                    WriteJogSpeedToPLC(_jogSpeedAxis1.Value, textBox);
                else if (textBox == JogSpeedAxis2TextBox && _jogSpeedAxis2.HasValue)
                    WriteJogSpeedToPLC(_jogSpeedAxis2.Value, textBox);
                else if (textBox == JogSpeedAxis3TextBox && _jogSpeedAxis3.HasValue)
                    WriteJogSpeedToPLC(_jogSpeedAxis3.Value, textBox);
            }
        }

        #endregion

        private void EmergencyStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Emergency Stop - This will stop all robot movements immediately!\n\nContinue?", 
                    "Emergency Stop", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Release all PLC buttons
                    ReleaseAllPLCButtons();
                    
                    // Trigger machine emergency stop
                    _machine?.EmergencyStop();
                    
                    Logger.Warning("RobotJogWindow", $"{_robotType} Robot emergency stop executed");
                    MessageBox.Show($"{_robotType} Robot emergency stop executed!", "Emergency Stop", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error during emergency stop: {ex.Message}", ex);
                MessageBox.Show($"Error during emergency stop: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RobotJogWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Release all PLC buttons before closing
                ReleaseAllPLCButtons();
                
                // Stop lamp monitoring
                _lampUpdateTimer?.Stop();
                
                // Unsubscribe from PLC events
                if (_plc != null)
                {
                    _plc.DataChanged -= OnPLCDataChanged;
                }
                
                // REVERT TO DEFAULT MONITORING GROUPS when closing jog window
                if (_plc != null && _plc.IsConnected)
                {
                    _plc.SetActiveMonitoringGroups(PLCConstants.DEFAULT_MONITORING_GROUPS);
                    Logger.Info("RobotJogWindow", "Reverted PLC to DEFAULT_MONITORING_GROUPS on window close");
                }
                
                // Stop position monitoring
                _servoMonitor?.StopMonitoring();
                _servoMonitor?.Dispose();
                _positionUpdateTimer?.Stop();
                
                // Clear button cache
                _buttonCache.Clear();
                
                Logger.Info("RobotJogWindow", $"{_robotType} Robot Jog Window closing and cleanup completed");
            }
            catch (Exception ex)
            {
                Logger.Error("RobotJogWindow", $"Error during RobotJogWindow cleanup: {ex.Message}", ex);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void JogRPlusBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void JogZMinusBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}