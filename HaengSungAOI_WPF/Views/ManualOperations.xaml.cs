using System;
using System.Collections.Generic;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Machine;
using HaengSungAOI_WPF.Utils;
using HaengSungAOI_WPF.Machine.PLC.PLC;

namespace HaengSungAOI_WPF
{
    /// <summary>
    /// Interaction logic for ManualOperations.xaml
    /// Manual operations window - simplified to PLC-only control
    /// All robot and axis references have been removed
    /// </summary>
    public partial class ManualOperations : Window
    {
        private MainWindow _mainWindow;
        private Machine.Machine _machine;

        // Track axis control button states for PLC-based control
        private Dictionary<string, bool> _axisJogStates = new Dictionary<string, bool>();

        public ManualOperations()
        {
            InitializeComponent();
            UpdateStatusDisplay();
        }

        public ManualOperations(MainWindow mainWindow) : this()
        {
            _mainWindow = mainWindow;
            _machine = mainWindow?.GetMachine();

            // Subscribe to PLC data changes for button feedback
            if (_machine?.PLC != null)
            {
                _machine.PLC.DataChanged += OnPLCDataChanged;
                Logger.Info("ManualOps", "Subscribed to PLC data changes for axis control feedback");
            }

            Closed += ManualOperations_Closed;
        }

        private void ManualOperations_Closed(object sender, EventArgs e)
        {
            try
            {
                // Unsubscribe from PLC events
                if (_machine?.PLC != null)
                {
                    _machine.PLC.DataChanged -= OnPLCDataChanged;
                }

                // Release all jog buttons for safety
                ReleaseAllPLCJogButtons();

                Logger.Info("ManualOps", "Manual Operations window closed and cleanup completed");
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error during ManualOperations cleanup: {ex.Message}", ex);
            }
        }

        #region PLC Event Handlers

        /// <summary>
        /// Handle PLC data changes for lamp feedback on axis control buttons
        /// </summary>
        private void OnPLCDataChanged(object sender, PLCDataChangedEventArgs e)
        {
            try
            {
                // Update button appearances based on PLC lamp states
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // PLC data change handling for UI updates
                }));
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error handling PLC data change: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Release all PLC jog buttons for safety
        /// </summary>
        private void ReleaseAllPLCJogButtons()
        {
            try
            {
                if (_machine?.PLC == null || !_machine.PLC.IsConnected)
                {
                    Logger.Warning("ManualOps", "Cannot release PLC jog buttons - PLC not connected");
                    return;
                }

                // Release all axis jog buttons
                string[] jogButtons = new[]
                {
                    "HMI_X1_Jog_Plus_PB", "HMI_X1_Jog_Minus_PB",
                    "HMI_Z1_Jog_Plus_PB", "HMI_Z1_Jog_Minus_PB",
                    "HMI_C1_Jog_Plus_PB", "HMI_C1_Jog_Minus_PB"
                };

                foreach (var button in jogButtons)
                {
                    try
                    {
                        _machine.PLC.WriteCoil(button, false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ManualOps", $"Error releasing button {button}: {ex.Message}", ex);
                    }
                }

                Logger.Info("ManualOps", "Released all PLC jog buttons");
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in ReleaseAllPLCJogButtons: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handle mouse down events for PLC control buttons - Set register to 1
        /// </summary>
        private void PLCButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string addressKey)
                {
                    if (_machine?.PLC != null && _machine.PLC.IsConnected)
                    {
                        // Write 1 to the PLC register on mouse down
                        Task.Run(async () =>
                        {
                            try
                            {
                                await _machine.PLC.WriteHoldingRegisterAsync(addressKey, 1);
                                Logger.Debug("ManualOps", $"Button {addressKey} pressed - Set to 1");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("ManualOps", $"Error writing 1 to PLC address {addressKey}: {ex.Message}", ex);
                            }
                        });
                    }
                    else
                    {
                        Logger.Warning("ManualOps", $"Cannot write to {addressKey}: PLC not connected");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in PLCButton_MouseDown: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handle mouse up events for PLC control buttons - Set register to 0
        /// </summary>
        private void PLCButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string addressKey)
                {
                    if (_machine?.PLC != null && _machine.PLC.IsConnected)
                    {
                        // Write 0 to the PLC register on mouse up
                        Task.Run(async () =>
                        {
                            try
                            {
                                await _machine.PLC.WriteHoldingRegisterAsync(addressKey, 0);
                                Logger.Debug("ManualOps", $"Button {addressKey} released - Set to 0");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("ManualOps", $"Error writing 0 to PLC address {addressKey}: {ex.Message}", ex);
                            }
                        });
                    }
                    else
                    {
                        Logger.Warning("ManualOps", $"Cannot write to {addressKey}: PLC not connected");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in PLCButton_MouseUp: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Handle click events for general PLC control buttons (kept for backward compatibility)
        /// The button's Tag property contains the PLC address key
        /// </summary>
        private void PLCButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string addressKey)
                {
                    // Write to PLC - standard push button behavior (momentary or toggle handled by PLC logic usually, 
                    // but HMI often pulses. Here we toggle or pulse based on requirement. 
                    // Assuming pulse (write 1 then 0) or simple set 1 for command buttons.)
                    // For simply triggering an action:
                    
                    if (_machine?.PLC != null && _machine.PLC.IsConnected)
                    {
                        // Pulse the bit
                        Task.Run(async () =>
                        {
                            try
                            {
                                await _machine.PLC.WriteHoldingRegisterAsync(addressKey, 1);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("ManualOps", $"Error writing to PLC address {addressKey}: {ex.Message}", ex);
                            }
                        });
                    }
                    else
                    {
                        Logger.Warning("ManualOps", $"Cannot write to {addressKey}: PLC not connected");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in PLCButton_Click: {ex.Message}", ex);
            }
        }

        #endregion

        #region Emergency and Safety Controls

        private void EmergencyStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _machine?.EmergencyStop();
                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in EmergencyStop_Click: {ex.Message}", ex);
            }
        }

        private void ResetEmergency_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _machine?.ResetEmergency();
                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error in ResetEmergency_Click: {ex.Message}", ex);
            }
        }

        private void EnableMotors_Click(object sender, RoutedEventArgs e)
        {
        }

        private void DisableMotors_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region PCB Infeed Robot Controls

        private void InfeedMoveToIdle_Click(object sender, RoutedEventArgs e)
        {
        }

        private void InfeedMoveToPickup_Click(object sender, RoutedEventArgs e)
        {
        }

        private void InfeedMoveToPlace_Click(object sender, RoutedEventArgs e)
        {
        }

        private void InfeedJog_X_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_X_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_X_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_X_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_Y_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_Y_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_Y_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_Y_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_C_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_C_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_C_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedJog_C_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void InfeedStopAll_Click(object sender, RoutedEventArgs e)
        {
        }

        private void InfeedEnableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        private void InfeedDisableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnInfeedCylinderDown_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnInfeedCylinderUp_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region PCB Transfer Robot Controls

        private void TransferMoveToIdle_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TransferMoveToPickup_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TransferMoveToPreparePickup_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TransferMoveToPlace_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TransferMoveToPreparPlace_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnMoveToNG_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TransferJog_X_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void TransferJog_X_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void TransferJog_X_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void TransferJog_X_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void TransferJog_Z_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void TransferJog_Z_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void TransferJog_Z_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void TransferJog_Z_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void TransferJogButton_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void TransferEnableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TransferDisableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnTestNG1_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnTestNG2_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_TransferCylDown_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_TransferCylUp_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_JogNGConveyor_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void btn_JogNGConveyor_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        #endregion

        #region PCB Outfeed Robot Controls

        private void OutfeedMoveToIdle_Click(object sender, RoutedEventArgs e)
        {
        }

        private void OutfeedMoveToPickup_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnOutfeedMoveToOK1_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnOutfeedMoveToOK2_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnOutfeedMoveToOK3_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnOutfeedMoveToOK4_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnOutfeedMoveToOK5_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnOutfeedMoveToOK6_Click(object sender, RoutedEventArgs e)
        {
        }

        private void OutfeedMoveToNGPlace_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnOutfeedMoveToPickTray_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnOutfeedMoveToPlaceTray_Click(object sender, RoutedEventArgs e)
        {
        }

        private void OutfeedHomeXAxes_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnPickNewTray_Click(object sender, RoutedEventArgs e)
        {
        }

        private void OutfeedJog_X_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void OutfeedJog_X_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void OutfeedJog_X_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void OutfeedJog_X_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void OutfeedJog_Y_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void OutfeedJog_Y_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void OutfeedJog_Y_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void OutfeedJog_Y_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void OutfeedStopAll_Click(object sender, RoutedEventArgs e)
        {
        }

        private void OutfeedEnableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        private void OutfeedDisableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        private void OutfeedCylinderDown_Click(object sender, RoutedEventArgs e)
        {
        }

        private void OutfeedCylinderUp_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_OutfeedCyl2Down_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_OutfeedCyl2Up_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_OutVacuum2_on_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btn_OutVacuum2_off_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region Inspection Robot 1 Controls

        private void Inspect1MoveToIdle_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect1MoveToFocus1_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect1MoveToFocus2_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect1MoveToFocus3_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect1StartSequence_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect1StopSequence_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect1Jog_Z_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect1Jog_Z_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect1Jog_Z_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect1Jog_Z_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect1Jog_C_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect1Jog_C_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect1Jog_C_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect1Jog_C_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect1JogButton_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void Inspect1StopAll_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect1EnableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect1DisableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region Inspection Robot 2 Controls

        private void Inspect2MoveToIdle_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect2MoveToFocus1_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect2MoveToFocus2_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect2MoveToFocus3_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect2StartSequence_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect2StopSequence_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect2Jog_Z_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect2Jog_Z_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect2Jog_Z_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect2Jog_Z_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect2Jog_C_Neg_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect2Jog_C_Neg_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect2Jog_C_Pos_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect2Jog_C_Pos_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void Inspect2JogButton_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void Inspect2StopAll_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect2EnableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Inspect2DisableVacuum_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region System Controls

        private void HomeAllAxes_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ResetAlarms_Click(object sender, RoutedEventArgs e)
        {
        }

        private void VisionTrigger_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ConveyorStart_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ConveyorStop_Click(object sender, RoutedEventArgs e)
        {
        }

        private void LightTowerTest_Click(object sender, RoutedEventArgs e)
        {
        }

        private void BuzzerTest_Click(object sender, RoutedEventArgs e)
        {
        }

        private void IOStatus_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region Axis Control Tab

        private void AxisListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void RefreshAxisInfo_Click(object sender, RoutedEventArgs e)
        {
        }

        private void JogNegative_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void JogNegative_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void JogPositive_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void JogPositive_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void JogButton_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void JogNegative_Click(object sender, RoutedEventArgs e)
        {
        }

        private void JogPositive_Click(object sender, RoutedEventArgs e)
        {
        }

        private void HomeAxis_Click(object sender, RoutedEventArgs e)
        {
        }

        private void StopAxis_Click(object sender, RoutedEventArgs e)
        {
        }

        private void EnableAxis_Click(object sender, RoutedEventArgs e)
        {
        }

        private void AbsoluteMove_Click(object sender, RoutedEventArgs e)
        {
        }

        private void StopAllAxes_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region Helper Methods

        private void UpdateStatusDisplay()
        {
            try
            {
                // Simplified status display for PLC-based system
                // Check if UI elements exist before updating
                var emergencyStatus = this.FindName("EmergencyStatus") as TextBlock;
                if (emergencyStatus != null)
                {
                    emergencyStatus.Text = "Emergency: Normal";
                    emergencyStatus.Foreground = System.Windows.Media.Brushes.Green;
                }

                var connectionStatus = this.FindName("ConnectionStatus") as TextBlock;
                if (connectionStatus != null)
                {
                    bool plcConnected = _machine?.PLC?.IsConnected ?? false;
                    connectionStatus.Text = plcConnected ? "PLC: Connected" : "PLC: Disconnected";
                    connectionStatus.Foreground = plcConnected ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ManualOps", $"Error updating status display: {ex.Message}", ex);
            }
        }

        #endregion

        #region Window Controls

        private void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatusDisplay();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}