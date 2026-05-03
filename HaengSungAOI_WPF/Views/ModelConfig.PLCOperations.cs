using System;
using System.Windows;
using HaengSungAOI_WPF.Machine;
using System.Linq;
using HaengSungAOI_WPF.Machine.PLC;

namespace HaengSungAOI_WPF
{
    /// <summary>
    /// Partial class for ModelConfig - PLC Read/Write Operations
    /// </summary>
    public partial class ModelConfig
    {
        #region Read Positions from PLC

        private void ReadInfeedPositionsFromPLC(PLCController plc)
        {
            var idle = _robotPositionManager.InfeedRobotPositions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.X = (float)ReadLRealFromPLC(plc, "X1_Pos1_Idle");
                idle.Y = (float)ReadLRealFromPLC(plc, "Y1_Pos1_Idle");
                idle.R = (float)ReadLRealFromPLC(plc, "R1_Pos1_Idle");
                idle.SpeedX = (float)ReadLRealFromPLC(plc, "X1_Speed_Pos1");
                idle.SpeedY = (float)ReadLRealFromPLC(plc, "Y1_Speed_Pos1");
                idle.SpeedR = (float)ReadLRealFromPLC(plc, "R1_Speed_Pos1");
            }

            var pickup = _robotPositionManager.InfeedRobotPositions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                pickup.X = (float)ReadLRealFromPLC(plc, "X1_Pos2_Pickup");
                pickup.Y = (float)ReadLRealFromPLC(plc, "Y1_Pos2_Pickup");
                pickup.R = (float)ReadLRealFromPLC(plc, "R1_Pos2_Pickup");
                pickup.SpeedX = (float)ReadLRealFromPLC(plc, "X1_Speed_Pos2");
                pickup.SpeedY = (float)ReadLRealFromPLC(plc, "Y1_Speed_Pos2");
                pickup.SpeedR = (float)ReadLRealFromPLC(plc, "R1_Speed_Pos2");
            }

            var place = _robotPositionManager.InfeedRobotPositions.FirstOrDefault(p => p.Position == "Place");
            if (place != null)
            {
                place.X = (float)ReadLRealFromPLC(plc, "X1_Pos3_Place");
                place.Y = (float)ReadLRealFromPLC(plc, "Y1_Pos3_Place");
                place.R = (float)ReadLRealFromPLC(plc, "R1_Pos3_Place");
                place.SpeedX = (float)ReadLRealFromPLC(plc, "X1_Speed_Pos3");
                place.SpeedY = (float)ReadLRealFromPLC(plc, "Y1_Speed_Pos3");
                place.SpeedR = (float)ReadLRealFromPLC(plc, "R1_Speed_Pos3");
            }
        }

        private void ReadTransferPositionsFromPLC(PLCController plc)
        {
            var positions = _robotPositionManager.TransferRobotPositions;

            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.X = (float)ReadLRealFromPLC(plc, "X2_Pos1_Idle");
                idle.Z = (float)ReadLRealFromPLC(plc, "Z2_Pos1_Idle");
                idle.SpeedX = (float)ReadLRealFromPLC(plc, "X2_Speed_Pos1");
                idle.SpeedZ = (float)ReadLRealFromPLC(plc, "Z2_Speed_Pos1");
            }

            var preparePickup = positions.FirstOrDefault(p => p.Position == "Prepare Pickup");
            if (preparePickup != null)
            {
                preparePickup.X = (float)ReadLRealFromPLC(plc, "X2_Pos2_PreparePickup");
                preparePickup.Z = (float)ReadLRealFromPLC(plc, "Z2_Pos2_PreparePickup");
                preparePickup.SpeedX = (float)ReadLRealFromPLC(plc, "X2_Speed_Pos2");
                preparePickup.SpeedZ = (float)ReadLRealFromPLC(plc, "Z2_Speed_Pos2");
            }

            var pickup = positions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                pickup.X = (float)ReadLRealFromPLC(plc, "X2_Pos3_Pickup");
                pickup.Z = (float)ReadLRealFromPLC(plc, "Z2_Pos3_Pickup");
                pickup.SpeedX = (float)ReadLRealFromPLC(plc, "X2_Speed_Pos3");
                pickup.SpeedZ = (float)ReadLRealFromPLC(plc, "Z2_Speed_Pos3");
            }

            var preparePlace = positions.FirstOrDefault(p => p.Position == "Prepare Place");
            if (preparePlace != null)
            {
                preparePlace.X = (float)ReadLRealFromPLC(plc, "X2_Pos4_PreparePlace");
                preparePlace.Z = (float)ReadLRealFromPLC(plc, "Z2_Pos4_PreparePlace");
                preparePlace.SpeedX = (float)ReadLRealFromPLC(plc, "X2_Speed_Pos4");
                preparePlace.SpeedZ = (float)ReadLRealFromPLC(plc, "Z2_Speed_Pos4");
            }

            var place = positions.FirstOrDefault(p => p.Position == "Place");
            if (place != null)
            {
                place.X = (float)ReadLRealFromPLC(plc, "X2_Pos5_Place");
                place.Z = (float)ReadLRealFromPLC(plc, "Z2_Pos5_Place");
                place.SpeedX = (float)ReadLRealFromPLC(plc, "X2_Speed_Pos5");
                place.SpeedZ = (float)ReadLRealFromPLC(plc, "Z2_Speed_Pos5");
            }

            var ng = positions.FirstOrDefault(p => p.Position == "NG Position");
            if (ng != null)
            {
                ng.X = (float)ReadLRealFromPLC(plc, "X2_Pos6_NGPosition");
                ng.Z = (float)ReadLRealFromPLC(plc, "Z2_Pos6_NGPosition");
                ng.SpeedX = (float)ReadLRealFromPLC(plc, "X2_Speed_Pos6");
                ng.SpeedZ = (float)ReadLRealFromPLC(plc, "Z2_Speed_Pos6");
            }
        }

        private void ReadOutfeedPositionsFromPLC(PLCController plc)
        {
            var positions = _robotPositionManager.OutfeedRobotPositions;

            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.X = (float)ReadLRealFromPLC(plc, "X3_Pos1_Idle");
                idle.Y = (float)ReadLRealFromPLC(plc, "Y3_Pos1_Idle");
                idle.SpeedX = (float)ReadLRealFromPLC(plc, "X3_Speed_Pos1");
                idle.SpeedY = (float)ReadLRealFromPLC(plc, "Y3_Speed_Pos1");
            }

            var pickup = positions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                pickup.X = (float)ReadLRealFromPLC(plc, "X3_Pos2_Pickup");
                pickup.Y = (float)ReadLRealFromPLC(plc, "Y3_Pos2_Pickup");
                pickup.SpeedX = (float)ReadLRealFromPLC(plc, "X3_Speed_Pos2");
                pickup.SpeedY = (float)ReadLRealFromPLC(plc, "Y3_Speed_Pos2");
            }

            // OK Place positions 1-6
            ReadOutfeedOKPlace(plc, positions, "OK Place 1", 3);
            ReadOutfeedOKPlace(plc, positions, "OK Place 2", 4);
            ReadOutfeedOKPlace(plc, positions, "OK Place 3", 5);
            ReadOutfeedOKPlace(plc, positions, "OK Place 4", 6);
            ReadOutfeedOKPlace(plc, positions, "OK Place 5", 7);
            ReadOutfeedOKPlace(plc, positions, "OK Place 6", 8);

            var ngPlace = positions.FirstOrDefault(p => p.Position == "NG Place");
            if (ngPlace != null)
            {
                ngPlace.X = (float)ReadLRealFromPLC(plc, "X3_Pos9_NGPlace");
                ngPlace.Y = (float)ReadLRealFromPLC(plc, "Y3_Pos9_NGPlace");
                ngPlace.SpeedX = (float)ReadLRealFromPLC(plc, "X3_Speed_Pos9");
                ngPlace.SpeedY = (float)ReadLRealFromPLC(plc, "Y3_Speed_Pos9");
            }

            var pickupTray = positions.FirstOrDefault(p => p.Position == "Pickup Tray");
            if (pickupTray != null)
            {
                pickupTray.X = (float)ReadLRealFromPLC(plc, "X3_Pos10_PickupTray");
                pickupTray.Y = (float)ReadLRealFromPLC(plc, "Y3_Pos10_PickupTray");
                pickupTray.SpeedX = (float)ReadLRealFromPLC(plc, "X3_Speed_Pos10");
                pickupTray.SpeedY = (float)ReadLRealFromPLC(plc, "Y3_Speed_Pos10");
            }

            var placeTray = positions.FirstOrDefault(p => p.Position == "Place Tray");
            if (placeTray != null)
            {
                placeTray.X = (float)ReadLRealFromPLC(plc, "X3_Pos11_PlaceTray");
                placeTray.Y = (float)ReadLRealFromPLC(plc, "Y3_Pos11_PlaceTray");
                placeTray.SpeedX = (float)ReadLRealFromPLC(plc, "X3_Speed_Pos11");
                placeTray.SpeedY = (float)ReadLRealFromPLC(plc, "Y3_Speed_Pos11");
            }
        }

        private void ReadOutfeedOKPlace(PLCController plc, System.Collections.ObjectModel.ObservableCollection<Models.RobotPositionEntry> positions,
            string positionName, int posNumber)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                position.X = (float)ReadLRealFromPLC(plc, $"X3_Pos{posNumber}_OKPlace{posNumber - 2}");
                position.Y = (float)ReadLRealFromPLC(plc, $"Y3_Pos{posNumber}_OKPlace{posNumber - 2}");
                position.SpeedX = (float)ReadLRealFromPLC(plc, $"X3_Speed_Pos{posNumber}");
                position.SpeedY = (float)ReadLRealFromPLC(plc, $"Y3_Speed_Pos{posNumber}");
            }
        }

        private void ReadInspect1PositionsFromPLC(PLCController plc)
        {
            var positions = _robotPositionManager.Inspect1RobotPositions;

            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.Z = (float)ReadLRealFromPLC(plc, "Z4_Pos1_Idle");
                idle.C = (float)ReadLRealFromPLC(plc, "C4_Pos1_Idle");
                idle.SpeedZ = (float)ReadLRealFromPLC(plc, "Z4_Speed_Pos1");
                idle.Speed = (float)ReadLRealFromPLC(plc, "C4_Speed_Pos1");
            }

            ReadInspectFocusPosition(plc, positions, "Focus 1", "Z4_Pos2_Focus1", "C4_Pos2_Focus1", "Z4_Speed_Pos2", "C4_Speed_Pos2");
            ReadInspectFocusPosition(plc, positions, "Focus 2", "Z4_Pos3_Focus2", "C4_Pos3_Focus2", "Z4_Speed_Pos3", "C4_Speed_Pos3");
            ReadInspectFocusPosition(plc, positions, "Focus 3", "Z4_Pos4_Focus3", "C4_Pos4_Focus3", "Z4_Speed_Pos4", "C4_Speed_Pos4");
        }

        private void ReadInspect2PositionsFromPLC(PLCController plc)
        {
            var positions = _robotPositionManager.Inspect2RobotPositions;

            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.Z = (float)ReadLRealFromPLC(plc, "Z5_Pos1_Idle");
                idle.C = (float)ReadLRealFromPLC(plc, "C5_Pos1_Idle");
                idle.SpeedZ = (float)ReadLRealFromPLC(plc, "Z5_Speed_Pos1");
                idle.Speed = (float)ReadLRealFromPLC(plc, "C5_Speed_Pos1");
            }

            ReadInspectFocusPosition(plc, positions, "Focus 1", "Z5_Pos2_Focus1", "C5_Pos2_Focus1", "Z5_Speed_Pos2", "C5_Speed_Pos2");
            ReadInspectFocusPosition(plc, positions, "Focus 2", "Z5_Pos3_Focus2", "C5_Pos3_Focus2", "Z5_Speed_Pos3", "C5_Speed_Pos3");
            ReadInspectFocusPosition(plc, positions, "Focus 3", "Z5_Pos4_Focus3", "C5_Pos4_Focus3", "Z5_Speed_Pos4", "C5_Speed_Pos4");
            ReadInspectFocusPosition(plc, positions, "Unload", "Z5_Pos5_Unload", "C5_Pos5_Unload", "Z5_Speed_Pos5", "C5_Speed_Pos5");
        }

        private void ReadInspectFocusPosition(PLCController plc, System.Collections.ObjectModel.ObservableCollection<Models.RobotPositionEntry> positions,
       string positionName, string zTag, string cTag, string speedZTag, string speedCTag)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                position.Z = (float)ReadLRealFromPLC(plc, zTag);
                position.C = (float)ReadLRealFromPLC(plc, cTag);
                position.SpeedZ = (float)ReadLRealFromPLC(plc, speedZTag);
                position.Speed = (float)ReadLRealFromPLC(plc, speedCTag);
            }
        }

        #endregion

        #region Write Positions to PLC

        private void WriteInfeedPositionsToPLC(PLCController plc)
        {
            var positions = _robotPositionManager.InfeedRobotPositions;

            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                WriteLRealToPLC(plc, "X1_Pos1_Idle", idle.X);
                WriteLRealToPLC(plc, "Y1_Pos1_Idle", idle.Y);
                WriteLRealToPLC(plc, "R1_Pos1_Idle", idle.R);
                WriteLRealToPLC(plc, "X1_Speed_Pos1", idle.SpeedX);
                WriteLRealToPLC(plc, "Y1_Speed_Pos1", idle.SpeedY);
                WriteLRealToPLC(plc, "R1_Speed_Pos1", idle.SpeedR);
            }

            var pickup = positions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                WriteLRealToPLC(plc, "X1_Pos2_Pickup", pickup.X);
                WriteLRealToPLC(plc, "Y1_Pos2_Pickup", pickup.Y);
                WriteLRealToPLC(plc, "R1_Pos2_Pickup", pickup.R);
                WriteLRealToPLC(plc, "X1_Speed_Pos2", pickup.SpeedX);
                WriteLRealToPLC(plc, "Y1_Speed_Pos2", pickup.SpeedY);
                WriteLRealToPLC(plc, "R1_Speed_Pos2", pickup.SpeedR);
            }

            var place = positions.FirstOrDefault(p => p.Position == "Place");
            if (place != null)
            {
                WriteLRealToPLC(plc, "X1_Pos3_Place", place.X);
                WriteLRealToPLC(plc, "Y1_Pos3_Place", place.Y);
                WriteLRealToPLC(plc, "R1_Pos3_Place", place.R);
                WriteLRealToPLC(plc, "X1_Speed_Pos3", place.SpeedX);
                WriteLRealToPLC(plc, "Y1_Speed_Pos3", place.SpeedY);
                WriteLRealToPLC(plc, "R1_Speed_Pos3", place.SpeedR);
            }
        }

        private void WriteTransferPositionsToPLC(PLCController plc)
        {
            var positions = _robotPositionManager.TransferRobotPositions;

            WriteTransferPosition(plc, positions, "Idle", 1);
            WriteTransferPosition(plc, positions, "Prepare Pickup", 2);
            WriteTransferPosition(plc, positions, "Pickup", 3);
            WriteTransferPosition(plc, positions, "Prepare Place", 4);
            WriteTransferPosition(plc, positions, "Place", 5);
            WriteTransferPosition(plc, positions, "NG Position", 6);
        }

        private void WriteTransferPosition(PLCController plc, System.Collections.ObjectModel.ObservableCollection<Models.RobotPositionEntry> positions,
            string positionName, int posNumber)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                string posLabel = positionName.Replace(" ", "");
                WriteLRealToPLC(plc, $"X2_Pos{posNumber}_{posLabel}", position.X);
                WriteLRealToPLC(plc, $"Z2_Pos{posNumber}_{posLabel}", position.Z);
                WriteLRealToPLC(plc, $"X2_Speed_Pos{posNumber}", position.SpeedX);
                WriteLRealToPLC(plc, $"Z2_Speed_Pos{posNumber}", position.SpeedZ);
            }
        }

        private void WriteOutfeedPositionsToPLC(PLCController plc)
        {
            var positions = _robotPositionManager.OutfeedRobotPositions;

            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                WriteLRealToPLC(plc, "X3_Pos1_Idle", idle.X);
                WriteLRealToPLC(plc, "Y3_Pos1_Idle", idle.Y);
                WriteLRealToPLC(plc, "X3_Speed_Pos1", idle.SpeedX);
                WriteLRealToPLC(plc, "Y3_Speed_Pos1", idle.SpeedY);
            }

            var pickup = positions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                WriteLRealToPLC(plc, "X3_Pos2_Pickup", pickup.X);
                WriteLRealToPLC(plc, "Y3_Pos2_Pickup", pickup.Y);
                WriteLRealToPLC(plc, "X3_Speed_Pos2", pickup.SpeedX);
                WriteLRealToPLC(plc, "Y3_Speed_Pos2", pickup.SpeedY);
            }

            // Write OK Place positions
            for (int i = 1; i <= 6; i++)
            {
                WriteOutfeedOKPlace(plc, positions, $"OK Place {i}", i + 2);
            }

            var ngPlace = positions.FirstOrDefault(p => p.Position == "NG Place");
            if (ngPlace != null)
            {
                WriteLRealToPLC(plc, "X3_Pos9_NGPlace", ngPlace.X);
                WriteLRealToPLC(plc, "Y3_Pos9_NGPlace", ngPlace.Y);
                WriteLRealToPLC(plc, "X3_Speed_Pos9", ngPlace.SpeedX);
                WriteLRealToPLC(plc, "Y3_Speed_Pos9", ngPlace.SpeedY);
            }

            var pickupTray = positions.FirstOrDefault(p => p.Position == "Pickup Tray");
            if (pickupTray != null)
            {
                WriteLRealToPLC(plc, "X3_Pos10_PickupTray", pickupTray.X);
                WriteLRealToPLC(plc, "Y3_Pos10_PickupTray", pickupTray.Y);
                WriteLRealToPLC(plc, "X3_Speed_Pos10", pickupTray.SpeedX);
                WriteLRealToPLC(plc, "Y3_Speed_Pos10", pickupTray.SpeedY);
            }

            var placeTray = positions.FirstOrDefault(p => p.Position == "Place Tray");
            if (placeTray != null)
            {
                WriteLRealToPLC(plc, "X3_Pos11_PlaceTray", placeTray.X);
                WriteLRealToPLC(plc, "Y3_Pos11_PlaceTray", placeTray.Y);
                WriteLRealToPLC(plc, "X3_Speed_Pos11", placeTray.SpeedX);
                WriteLRealToPLC(plc, "Y3_Speed_Pos11", placeTray.SpeedY);
            }
        }

        private void WriteOutfeedOKPlace(PLCController plc, System.Collections.ObjectModel.ObservableCollection<Models.RobotPositionEntry> positions,
           string positionName, int posNumber)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                WriteLRealToPLC(plc, $"X3_Pos{posNumber}_OKPlace{posNumber - 2}", position.X);
                WriteLRealToPLC(plc, $"Y3_Pos{posNumber}_OKPlace{posNumber - 2}", position.Y);
                WriteLRealToPLC(plc, $"X3_Speed_Pos{posNumber}", position.SpeedX);
                WriteLRealToPLC(plc, $"Y3_Speed_Pos{posNumber}", position.SpeedY);
            }
        }

        private void WriteInspect1PositionsToPLC(PLCController plc)
        {
            var positions = _robotPositionManager.Inspect1RobotPositions;

            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                WriteLRealToPLC(plc, "Z4_Pos1_Idle", idle.Z);
                WriteLRealToPLC(plc, "C4_Pos1_Idle", idle.C);
                WriteLRealToPLC(plc, "Z4_Speed_Pos1", idle.SpeedZ);
                WriteLRealToPLC(plc, "C4_Speed_Pos1", idle.Speed);
            }

            WriteInspectFocusPosition(plc, positions, "Focus 1", "Z4_Pos2_Focus1", "C4_Pos2_Focus1", "Z4_Speed_Pos2", "C4_Speed_Pos2");
            WriteInspectFocusPosition(plc, positions, "Focus 2", "Z4_Pos3_Focus2", "C4_Pos3_Focus2", "Z4_Speed_Pos3", "C4_Speed_Pos3");
            WriteInspectFocusPosition(plc, positions, "Focus 3", "Z4_Pos4_Focus3", "C4_Pos4_Focus3", "Z4_Speed_Pos4", "C4_Speed_Pos4");
        }

        private void WriteInspect2PositionsToPLC(PLCController plc)
        {
            var positions = _robotPositionManager.Inspect2RobotPositions;

            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                WriteLRealToPLC(plc, "Z5_Pos1_Idle", idle.Z);
                WriteLRealToPLC(plc, "C5_Pos1_Idle", idle.C);
                WriteLRealToPLC(plc, "Z5_Speed_Pos1", idle.SpeedZ);
                WriteLRealToPLC(plc, "C5_Speed_Pos1", idle.Speed);
            }

            WriteInspectFocusPosition(plc, positions, "Focus 1", "Z5_Pos2_Focus1", "C5_Pos2_Focus1", "Z5_Speed_Pos2", "C5_Speed_Pos2");
            WriteInspectFocusPosition(plc, positions, "Focus 2", "Z5_Pos3_Focus2", "C5_Pos3_Focus2", "Z5_Speed_Pos3", "C5_Speed_Pos3");
            WriteInspectFocusPosition(plc, positions, "Focus 3", "Z5_Pos4_Focus3", "C5_Pos4_Focus3", "Z5_Speed_Pos4", "C5_Speed_Pos4");
            WriteInspectFocusPosition(plc, positions, "Unload", "Z5_Pos5_Unload", "C5_Pos5_Unload", "Z5_Speed_Pos5", "C5_Speed_Pos5");
        }

        private void WriteInspectFocusPosition(PLCController plc, System.Collections.ObjectModel.ObservableCollection<Models.RobotPositionEntry> positions,
      string positionName, string zTag, string cTag, string speedZTag, string speedCTag)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                WriteLRealToPLC(plc, zTag, position.Z);
                WriteLRealToPLC(plc, cTag, position.C);
                WriteLRealToPLC(plc, speedZTag, position.SpeedZ);
                WriteLRealToPLC(plc, speedCTag, position.Speed);
            }
        }

        #endregion

        #region PLC Helper Methods

        /// <summary>
        /// Read LREAL (64-bit double) from PLC using data point name
        /// LREAL uses 4 consecutive 16-bit registers
        /// </summary>
        private double ReadLRealFromPLC(PLCController plc, string dataPointName)
        {
            try
            {
                // Get the register array value by name (4 registers for LREAL)
                ushort[] registers = plc.GetRegisterArrayValue(dataPointName);
                if (registers == null || registers.Length < 4)
                    return 0.0;

                // Convert 4 x 16-bit registers to 64-bit double (LREAL)
                // Little-endian byte order (LSB first)
                byte[] bytes = new byte[8];
                bytes[0] = (byte)(registers[0] & 0xFF);
                bytes[1] = (byte)((registers[0] >> 8) & 0xFF);
                bytes[2] = (byte)(registers[1] & 0xFF);
                bytes[3] = (byte)((registers[1] >> 8) & 0xFF);
                bytes[4] = (byte)(registers[2] & 0xFF);
                bytes[5] = (byte)((registers[2] >> 8) & 0xFF);
                bytes[6] = (byte)(registers[3] & 0xFF);
                bytes[7] = (byte)((registers[3] >> 8) & 0xFF);

                return BitConverter.ToDouble(bytes, 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading LREAL from PLC for '{dataPointName}': {ex.Message}");
                return 0.0;
            }
        }

        private void WriteLRealToPLC(PLCController plc, string dataPointName, double value)
        {
            try
            {
                // Convert 64-bit double to 4 x 16-bit registers
                byte[] bytes = BitConverter.GetBytes(value);

                ushort[] registers = new ushort[4];
                registers[0] = (ushort)(bytes[0] | (bytes[1] << 8));
                registers[1] = (ushort)(bytes[2] | (bytes[3] << 8));
                registers[2] = (ushort)(bytes[4] | (bytes[5] << 8));
                registers[3] = (ushort)(bytes[6] | (bytes[7] << 8));

                // Write 4 consecutive 16-bit registers using data point name
                plc.WriteHoldingRegisters(dataPointName, registers);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing LREAL to PLC for '{dataPointName}': {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}
