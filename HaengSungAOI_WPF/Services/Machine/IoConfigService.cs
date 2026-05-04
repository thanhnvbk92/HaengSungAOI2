using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HaengSungAOI_WPF.Core.PLC;
using HaengSungAOI_WPF.Models;
using System.Text.Json;

namespace HaengSungAOI_WPF.Services.Machine
{
    public class IoConfigService : IIoConfigService
    {
        public IoConfig CurrentConfig { get; private set; } = new IoConfig();
        private readonly Dictionary<string, ushort> _addressMap = new Dictionary<string, ushort>();

        public IoConfigService()
        {
            // Initialize with default values from PLCAddresses
            InitializeFromStatic();
        }

        private void InitializeFromStatic()
        {
            foreach (var kvp in PLCAddresses.HMI_PushButtons)
            {
                CurrentConfig.PushButtons.Add(new IoItem { Name = kvp.Key, Address = kvp.Value, Type = "Coil" });
                _addressMap[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in PLCAddresses.HMI_Lamps)
            {
                CurrentConfig.Lamps.Add(new IoItem { Name = kvp.Key, Address = kvp.Value, Type = "Coil" });
                _addressMap[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in PLCAddresses.ServoPositionData)
            {
                CurrentConfig.RobotPositions[kvp.Key] = kvp.Value;
                _addressMap[kvp.Key] = kvp.Value;
            }
            
            // Add other registers if needed
        }

        public void LoadConfig(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    CurrentConfig = JsonSerializer.Deserialize<IoConfig>(json);
                    
                    // Rebuild map
                    _addressMap.Clear();
                    foreach (var item in CurrentConfig.PushButtons) _addressMap[item.Name] = item.Address;
                    foreach (var item in CurrentConfig.Lamps) _addressMap[item.Name] = item.Address;
                    foreach (var item in CurrentConfig.Registers) _addressMap[item.Name] = item.Address;
                    foreach (var kvp in CurrentConfig.RobotPositions) _addressMap[kvp.Key] = kvp.Value;
                }
                catch (Exception)
                {
                    // Fallback to static if load fails
                    InitializeFromStatic();
                }
            }
        }

        public ushort GetAddress(string tagName)
        {
            if (_addressMap.TryGetValue(tagName, out ushort address))
                return address;
            throw new KeyNotFoundException($"Tag '{tagName}' not found in IO configuration.");
        }

        public bool TryGetAddress(string tagName, out ushort address)
        {
            return _addressMap.TryGetValue(tagName, out address);
        }
    }
}



