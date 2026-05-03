using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HaengSungAOI_WPF.Utils;

namespace HaengSungAOI_WPF.Machine.PLC
{
    public static class PLCConfiguration
    {
        public static int ConfigureFromConstants(PLCController plc)
        {
            if (plc == null)
                throw new ArgumentNullException(nameof(plc));

            int configuredCount = 0;

            try
            {
                Logger.Info("PLCConfiguration", "Configuring PLC from constant dictionaries...");

                // MainWindow HMI Push Buttons - MainWindowHMIButtons group
                foreach (var kvp in PLCAddresses.HMI_PushButtons)
                {
                    if (true)
                        plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"HMI Push Button: {kvp.Key}", 
                            PLCMonitoringGroup.MainWindowHMIButtons);
                    else
                        plc.AddCoil(kvp.Key, kvp.Value, $"HMI Push Button: {kvp.Key}", 
                            PLCMonitoringGroup.MainWindowHMIButtons);
                    configuredCount++;
                }

                // MainWindow HMI Lamps - MainWindowHMILamps group
                foreach (var kvp in PLCAddresses.HMI_Lamps)
                {
                    if (kvp.Value >= 200 && kvp.Value <= 210)
                        plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"HMI Lamp: {kvp.Key}", 
                            PLCMonitoringGroup.MainWindowHMILamps);
                    else
                        plc.AddCoil(kvp.Key, kvp.Value, $"HMI Lamp: {kvp.Key}", 
                            PLCMonitoringGroup.MainWindowHMILamps);
                    configuredCount++;
                }

                // HMI Select buttons - HMISelect group
                foreach (var kvp in PLCAddresses.HMI_Select)
                {
                    plc.AddCoil(kvp.Key, kvp.Value, $"HMI Select: {kvp.Key}", 
                        PLCMonitoringGroup.HMISelect);
                    configuredCount++;
                }

                // HMI Select lamps - HMISelect group
                foreach (var kvp in PLCAddresses.HMI_Select_Lamps)
                {
                    plc.AddCoil(kvp.Key, kvp.Value, $"HMI Select Lamp: {kvp.Key}", 
                        PLCMonitoringGroup.HMISelect);
                    configuredCount++;
                }

                // HMI Select registers - HMISelect group
                foreach (var kvp in PLCAddresses.HMI_Select_Registers)
                {
                    plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"HMI Select Register: {kvp.Key}", 
                        PLCMonitoringGroup.HMISelect);
                    configuredCount++;
                }

                // HMI Select lamp registers - HMISelect group
                foreach (var kvp in PLCAddresses.HMI_Select_Lamp_Registers)
                {
                    plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"HMI Select Lamp Register: {kvp.Key}", 
                        PLCMonitoringGroup.HMISelect);
                    configuredCount++;
                }

                // Tray Quantity Registers - MainWindowHMILamps group (always monitored like other main window data)
                foreach (var kvp in PLCAddresses.TrayQuantity_Registers)
                {
                    plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"Tray Quantity: {kvp.Key}", 
                        PLCMonitoringGroup.MainWindowHMILamps);
                    configuredCount++;
                }

                // Product Logging Registers - MainWindowHMILamps group (for real-time product tracking)
                foreach (var kvp in PLCAddresses.ProductLog_Registers)
                {
                    plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"Product Log: {kvp.Key}", 
                        PLCMonitoringGroup.MainWindowHMILamps);
                    configuredCount++;
                }

                // OK Barcode Registers - MainWindowHMILamps group (for OK product barcode reading)
                foreach (var kvp in PLCAddresses.OKBarcode_Registers)
                {
                    plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"OK Barcode: {kvp.Key}", 
                        PLCMonitoringGroup.MainWindowHMILamps);
                    configuredCount++;
                }

                // NG Barcode Registers - MainWindowHMILamps group (for NG product barcode reading)
                foreach (var kvp in PLCAddresses.NGBarcode_Registers)
                {
                    plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"NG Barcode: {kvp.Key}", 
                        PLCMonitoringGroup.MainWindowHMILamps);
                    configuredCount++;
                }

                // Alarm Registers - MachineAlarms group (always monitored for safety)
                foreach (var kvp in PLCAddresses.Alarm_Registers)
                {
                    plc.AddHoldingRegister(kvp.Key, kvp.Value, 1, $"Alarm: {kvp.Key}", 
                        PLCMonitoringGroup.MachineAlarms);
                    configuredCount++;
                }

                // Servo status coils - ServoErrors group (for safety monitoring)
                foreach (var kvp in PLCAddresses.Servo_Status_Coils)
                {
                    plc.AddCoil(kvp.Key, kvp.Value, $"Servo Status: {kvp.Key}", 
                        PLCMonitoringGroup.ServoErrors);
                    configuredCount++;
                }

                // ===== CONFIGURE ROBOT POSITION DATA (LREAL - 4 registers each) =====
                // These are used by ModelConfig for reading/writing robot positions
                // ServoTargets group since they're written during model configuration
                foreach (var kvp in PLCAddresses.ServoPositionData)
                {
                    plc.AddHoldingRegister(kvp.Key, kvp.Value, 4, $"Robot Position/Speed: {kvp.Key}", 
                        PLCMonitoringGroup.ServoTargets);
                    configuredCount++;
                }

                // Configure servo monitoring registers using ServoAddressCalculator
                configuredCount += ConfigureServoMonitoring(plc);

                Logger.Info("PLCConfiguration", $"Successfully configured {configuredCount} PLC data points from constants");
            }
            catch (Exception ex)
            {
                Logger.Error("PLCConfiguration", $"Error configuring PLC from constants: {ex.Message}", ex);
            }

            return configuredCount;
        }

        /// <summary>
        /// Configure servo monitoring registers for all axes (Current Position, Speed, Error Code, etc.)
        /// NOW WITH MONITORING GROUPS for dynamic activation/deactivation
        /// </summary>
        private static int ConfigureServoMonitoring(PLCController plc)
        {
            int count = 0;
            
            foreach (ServoAxis axis in Enum.GetValues(typeof(ServoAxis)))
            {
                string axisName = ServoAddressCalculator.GetAxisDisplayName(axis);
                
                // Current Position (4 registers for LREAL) - ServoPositions group
                ushort posAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.CurrentPosition);
                plc.AddHoldingRegister($"{axisName}_CurrentPosition", posAddr, 4, 
                    $"{axisName} Current Position", PLCMonitoringGroup.ServoPositions);
                count++;
                
                // Error Code (4 registers) - ServoErrors group (always monitored for safety)
                ushort errorAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.ErrorCode);
                plc.AddHoldingRegister($"{axisName}_ErrorCode", errorAddr, 4, 
                    $"{axisName} Error Code", PLCMonitoringGroup.ServoErrors);
                count++;

                // Current Speed (4 registers for LREAL) - ServoStatus group
                ushort speedAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.CurrentSpeed);
                plc.AddHoldingRegister($"{axisName}_CurrentSpeed", speedAddr, 4, 
                    $"{axisName} Current Speed", PLCMonitoringGroup.ServoStatus);
                count++;

                // Operation Status (4 registers for LREAL) - ServoStatus group
                ushort statusAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.OperationStatus);
                plc.AddHoldingRegister($"{axisName}_OperationStatus", statusAddr, 4, 
                    $"{axisName} Operation Status", PLCMonitoringGroup.ServoStatus);
                count++;

                // ORG Found (1 register for BOOL) - ServoStatus group
                ushort orgAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.ORGFound);
                plc.AddHoldingRegister($"{axisName}_ORGFound", orgAddr, 1, 
                    $"{axisName} ORG Found", PLCMonitoringGroup.ServoStatus);
                count++;

                // Move Completed (1 register for BOOL) - ServoStatus group
                ushort moveAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.MoveCompleted);
                plc.AddHoldingRegister($"{axisName}_MoveCompleted", moveAddr, 1, 
                    $"{axisName} Move Completed", PLCMonitoringGroup.ServoStatus);
                count++;

                // Target Position (4 registers for LREAL) - ServoTargets group
                ushort targetPosAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.TargetPosition);
                plc.AddHoldingRegister($"{axisName}_TargetPosition", targetPosAddr, 4, 
                    $"{axisName} Target Position", PLCMonitoringGroup.ServoTargets);
                count++;

                // Target Speed (4 registers for LREAL) - ServoTargets group
                ushort targetSpeedAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.TargetSpeed);
                plc.AddHoldingRegister($"{axisName}_TargetSpeed", targetSpeedAddr, 4, 
                    $"{axisName} Target Speed", PLCMonitoringGroup.ServoTargets);
                count++;

                // Current Point (2 registers for INT) - ServoTargets group
                ushort pointAddr = ServoAddressCalculator.GetParameterAddress(axis, ServoParameter.CurrentPoint);
                plc.AddHoldingRegister($"{axisName}_CurrentPoint", pointAddr, 2, 
                    $"{axisName} Current Point", PLCMonitoringGroup.ServoTargets);
                count++;
            }

            // Configure HMI buttons and lamps for all axes
            count += ConfigureServoHMIControls(plc);
            
            Logger.Info("PLCConfiguration", $"Configured {count} servo monitoring data points with dynamic groups");
            return count;
        }

        /// <summary>
        /// Configure servo HMI button and lamp data points for all axes
        /// Based on Servo para.csv: MW6100+ for buttons, MW6150+ for lamps
        /// NOW WITH MONITORING GROUPS for dynamic activation/deactivation
        /// </summary>
        private static int ConfigureServoHMIControls(PLCController plc)
        {
            int count = 0;

            foreach (ServoAxis axis in Enum.GetValues(typeof(ServoAxis)))
            {
                string axisName = ServoAddressCalculator.GetAxisDisplayName(axis);

                // Configure essential HMI buttons (1 register each for BOOL) - ServoJogButtons group
                // Servo ON
                ushort servoOnAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.ServoON);
                plc.AddHoldingRegister($"HMI_{axisName}_Servo_ON_PB", servoOnAddr, 1, 
                    $"{axisName} Servo ON Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // ORG (Homing)
                ushort orgAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.ORG);
                plc.AddHoldingRegister($"HMI_{axisName}_ORG_PB", orgAddr, 1, 
                    $"{axisName} ORG Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Jog Plus
                ushort jogPlusAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.JogPlus);
                plc.AddHoldingRegister($"HMI_{axisName}_Jog_Plus_PB", jogPlusAddr, 1, 
                    $"{axisName} Jog Plus Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Jog Minus
                ushort jogMinusAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.JogMinus);
                plc.AddHoldingRegister($"HMI_{axisName}_Jog_Minus_PB", jogMinusAddr, 1, 
                    $"{axisName} Jog Minus Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Jog Plus High Speed
                ushort jogPlusHiAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.JogPlusHispeed);
                plc.AddHoldingRegister($"HMI_{axisName}_Jog_Plus_Hispeed_PB", jogPlusHiAddr, 1, 
                    $"{axisName} Jog Plus Hispeed Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Jog Minus High Speed
                ushort jogMinusHiAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.JogMinusHispeed);
                plc.AddHoldingRegister($"HMI_{axisName}_Jog_Minus_Hispeed_PB", jogMinusHiAddr, 1, 
                    $"{axisName} Jog Minus Hispeed Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Inching Plus
                ushort inchPlusAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.InchingPlus);
                plc.AddHoldingRegister($"HMI_{axisName}_Inching_Plus_PB", inchPlusAddr, 1, 
                    $"{axisName} Inching Plus Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Inching Minus
                ushort inchMinusAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.InchingMinus);
                plc.AddHoldingRegister($"HMI_{axisName}_Inching_Minus_PB", inchMinusAddr, 1, 
                    $"{axisName} Inching Minus Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Step Plus
                ushort stepPlusAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.StepPlus);
                plc.AddHoldingRegister($"HMI_{axisName}_Step_Plus_PB", stepPlusAddr, 1, 
                    $"{axisName} Step Plus Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Step Minus
                ushort stepMinusAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.StepMinus);
                plc.AddHoldingRegister($"HMI_{axisName}_Step_Minus_PB", stepMinusAddr, 1, 
                    $"{axisName} Step Minus Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Move
                ushort moveAddr = ServoAddressCalculator.GetHMIButtonAddress(axis, ServoHMIButton.Move);
                plc.AddHoldingRegister($"HMI_{axisName}_Move_PB", moveAddr, 1, 
                    $"{axisName} Move Button", PLCMonitoringGroup.ServoJogButtons);
                count++;

                // Configure Position buttons (Pos1-Pos11) - ServoPositionButtons group
                for (int pos = 1; pos <= 11; pos++)
                {
                    ushort posAddr = ServoAddressCalculator.GetPositionButtonAddress(axis, pos);
                    plc.AddHoldingRegister($"HMI_{axisName}_Pos{pos}_PB", posAddr, 1, 
                        $"{axisName} Position {pos} Button", PLCMonitoringGroup.ServoPositionButtons);
                    count++;
                }

                // Configure HMI Lamps (corresponding to buttons) - ServoJogLamps group
                // Servo ON Lamp
                ushort servoOnLampAddr = ServoAddressCalculator.GetHMILampAddress(axis, ServoHMIButton.ServoON);
                plc.AddHoldingRegister($"HMI_Lamp_{axisName}_Servo_ON_PB", servoOnLampAddr, 1, 
                    $"{axisName} Servo ON Lamp", PLCMonitoringGroup.ServoJogLamps);
                count++;

                // ORG Lamp
                ushort orgLampAddr = ServoAddressCalculator.GetHMILampAddress(axis, ServoHMIButton.ORG);
                plc.AddHoldingRegister($"HMI_Lamp_{axisName}_ORG_PB", orgLampAddr, 1, 
                    $"{axisName} ORG Lamp", PLCMonitoringGroup.ServoJogLamps);
                count++;

                // Jog Plus Lamp
                ushort jogPlusLampAddr = ServoAddressCalculator.GetHMILampAddress(axis, ServoHMIButton.JogPlus);
                plc.AddHoldingRegister($"HMI_Lamp_{axisName}_Jog_Plus_PB", jogPlusLampAddr, 1, 
                    $"{axisName} Jog Plus Lamp", PLCMonitoringGroup.ServoJogLamps);
                count++;

                // Jog Minus Lamp
                ushort jogMinusLampAddr = ServoAddressCalculator.GetHMILampAddress(axis, ServoHMIButton.JogMinus);
                plc.AddHoldingRegister($"HMI_Lamp_{axisName}_Jog_Minus_PB", jogMinusLampAddr, 1, 
                    $"{axisName} Jog Minus Lamp", PLCMonitoringGroup.ServoJogLamps);
                count++;

                // Configure Position lamps (Pos1-Pos11) - ServoPositionButtons group
                for (int pos = 1; pos <= 11; pos++)
                {
                    ushort posLampAddr = ServoAddressCalculator.GetPositionLampAddress(axis, pos);
                    plc.AddHoldingRegister($"HMI_Lamp_{axisName}_Pos{pos}_PB", posLampAddr, 1, 
                        $"{axisName} Position {pos} Lamp", PLCMonitoringGroup.ServoPositionButtons);
                    count++;
                }
            }

            Logger.Info("PLCConfiguration", $"Configured {count} servo HMI control data points with dynamic groups");
            return count;
        }

        public static int ConfigureFromCSV(PLCController plc, string csvFilePath)
        {
            if (plc == null)
                throw new ArgumentNullException(nameof(plc));

            if (!File.Exists(csvFilePath))
            {
                Logger.Error("PLCConfiguration", $"CSV file not found: {csvFilePath}");
                return 0;
            }

            int configuredCount = 0;

            try
            {
                Logger.Info("PLCConfiguration", $"Loading PLC configuration from: {csvFilePath}");
                var lines = File.ReadAllLines(csvFilePath);

                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var dataPoints = ParseCSVLine(line);

                    foreach (var dataPoint in dataPoints)
                    {
                        try
                        {
                            if (dataPoint.IsMX || dataPoint.IsB)
                                plc.AddCoil(dataPoint.Name, dataPoint.Address, dataPoint.Description);
                            else if (dataPoint.IsMR)
                                plc.AddHoldingRegister(dataPoint.Name, dataPoint.Address, 1, dataPoint.Description);

                            configuredCount++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning("PLCConfiguration", $"Failed to add data point '{dataPoint.Name}': {ex.Message}");
                        }
                    }
                }

                Logger.Info("PLCConfiguration", $"Successfully configured {configuredCount} PLC data points from CSV");
            }
            catch (Exception ex)
            {
                Logger.Error("PLCConfiguration", $"Error loading PLC configuration: {ex.Message}", ex);
            }

            return configuredCount;
        }

        private static List<PLCDataPointInfo> ParseCSVLine(string line)
        {
            var dataPoints = new List<PLCDataPointInfo>();
            var fields = SplitCSVLine(line);

            for (int i = 0; i < fields.Count - 1; i += 2)
            {
                var addressField = fields[i].Trim();
                var commentField = fields[i + 1].Trim();

                if (string.IsNullOrWhiteSpace(addressField) || string.IsNullOrWhiteSpace(commentField))
                    continue;

                var dataPoint = ParseDataPoint(addressField, commentField);
                if (dataPoint != null)
                    dataPoints.Add(dataPoint);
            }

            return dataPoints;
        }

        private static PLCDataPointInfo ParseDataPoint(string addressField, string commentField)
        {
            if (addressField.Trim() == "" || commentField.Contains("HMI. PB"))
                return null;

            var mxMatch = Regex.Match(addressField, @"MX(\d+)\.(\d+)", RegexOptions.IgnoreCase);
            if (mxMatch.Success)
            {
                int byteNum = int.Parse(mxMatch.Groups[1].Value);
                int bitNum = int.Parse(mxMatch.Groups[2].Value);
                ushort address = (ushort)(byteNum * 8 + bitNum);

                return new PLCDataPointInfo
                {
                    Name = CleanName(commentField),
                    Address = address,
                    Description = commentField,
                    IsMX = true
                };
            }

            var mrMatch = Regex.Match(addressField, @"MR(\d+)", RegexOptions.IgnoreCase);
            if (mrMatch.Success)
            {
                ushort address = ushort.Parse(mrMatch.Groups[1].Value);

                return new PLCDataPointInfo
                {
                    Name = CleanName(commentField),
                    Address = address,
                    Description = commentField,
                    IsMR = true
                };
            }

            var bMatch = Regex.Match(addressField, @"B([0-9A-F]{2})", RegexOptions.IgnoreCase);
            if (bMatch.Success)
            {
                ushort address = Convert.ToUInt16(bMatch.Groups[1].Value, 16);

                return new PLCDataPointInfo
                {
                    Name = CleanName(commentField),
                    Address = address,
                    Description = commentField,
                    IsB = true
                };
            }

            return null;
        }

        private static string CleanName(string text)
        {
            text = text.Replace("HMI.", "HMI_");
            text = Regex.Replace(text, @"[^\w]", "_");
            text = Regex.Replace(text, @"_+", "_");
            text = text.Trim('_');
            return text;
        }

        private static List<string> SplitCSVLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                    inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField);
                    currentField = "";
                }
                else
                    currentField += c;
            }

            fields.Add(currentField);
            return fields;
        }

        public static Dictionary<string, List<string>> GetConfiguredDataPointsSummary(PLCController plc)
        {
            var summary = new Dictionary<string, List<string>>
            {
                { "Coils", new List<string>() },
                { "HoldingRegisters", new List<string>() }
            };

            var dataPointNames = plc.GetDataPointNames();

            foreach (var name in dataPointNames)
            {
                var dataPoint = plc.GetDataPoint(name);
                if (dataPoint != null)
                {
                    if (dataPoint.DataType == PLCDataType.Coil)
                        summary["Coils"].Add($"{name} @ {dataPoint.Address} - {dataPoint.Description}");
                    else if (dataPoint.DataType == PLCDataType.HoldingRegister)
                        summary["HoldingRegisters"].Add($"{name} @ {dataPoint.Address} - {dataPoint.Description}");
                }
            }

            return summary;
        }

        public static void PrintConfigurationSummary(PLCController plc)
        {
            var summary = GetConfiguredDataPointsSummary(plc);
            Logger.Info("PLCConfiguration", "=== PLC Configuration Summary ===");
            Logger.Info("PLCConfiguration", $"Total Data Points: {plc.GetDataPointNames().Count}");
            Logger.Info("PLCConfiguration", $"Coils: {summary["Coils"].Count}");
            Logger.Info("PLCConfiguration", $"Holding Registers: {summary["HoldingRegisters"].Count}");
        }

        public static PLCAxis CreatePLCAxis(string name, PLCController plc, string plcAxisPrefix, ushort homingMode = 0)
        {
            if (plc == null)
                throw new ArgumentNullException(nameof(plc), "PLCController cannot be null");

            if (string.IsNullOrEmpty(plcAxisPrefix))
                throw new ArgumentException("PLC axis prefix cannot be null or empty", nameof(plcAxisPrefix));

            string servoButton = $"HMI_{plcAxisPrefix}_Servo_ON_PB";
            string orgButton = $"HMI_{plcAxisPrefix}_ORG_PB";
            string jogPlusButton = $"HMI_{plcAxisPrefix}_Jog_Plus_PB";
            string jogMinusButton = $"HMI_{plcAxisPrefix}_Jog_Minus_PB";

            bool buttonsConfigured =
              plc.GetDataPoint(servoButton) != null &&
                plc.GetDataPoint(orgButton) != null &&
              plc.GetDataPoint(jogPlusButton) != null &&
              plc.GetDataPoint(jogMinusButton) != null;

            if (!buttonsConfigured)
            {
                Logger.Warning("PLCConfiguration",
      $"Not all PLC buttons found for axis {plcAxisPrefix}. Ensure PLC is configured with ConfigureFromConstants()");
 }

            var plcAxis = new PLCAxis(name, plc, plcAxisPrefix, 0, 0, homingMode);
            Logger.Info("PLCConfiguration", $"Created PLC axis: {name} with prefix {plcAxisPrefix}");

            return plcAxis;
        }

        public static Dictionary<string, PLCAxis> CreateInfeedAxes(PLCController plc)
        {
            var axes = new Dictionary<string, PLCAxis>();

 try
    {
    axes["X1"] = CreatePLCAxis("InfeedAxisX1", plc, "X1", 0);
     axes["Y1"] = CreatePLCAxis("InfeedAxisY1", plc, "Z1", 0);
   axes["C1"] = CreatePLCAxis("InfeedAxisC1", plc, "C1", 0);

          Logger.Info("PLCConfiguration", $"Created {axes.Count} Infeed axes for PLC control");
        }
   catch (Exception ex)
{
                Logger.Error("PLCConfiguration", $"Error creating Infeed axes: {ex.Message}", ex);
 }

            return axes;
        }
    }
}
