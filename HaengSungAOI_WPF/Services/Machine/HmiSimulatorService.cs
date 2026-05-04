using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HaengSungAOI_WPF.Services.Machine
{
    public class HmiSimulatorService : IHmiSimulatorService
    {
        private readonly ILogger<HmiSimulatorService> _logger;
        private readonly Dictionary<string, bool> _inputs = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _outputs = new Dictionary<string, bool>();
        private readonly Dictionary<string, float> _axisPositions = new Dictionary<string, float>();

        public bool IsSimulationMode { get; set; } = true;

        public HmiSimulatorService(ILogger<HmiSimulatorService> logger)
        {
            _logger = logger;
            InitializeDefaultState();
        }

        private void InitializeDefaultState()
        {
            // Initial positions
            _axisPositions["X1"] = 0;
            _axisPositions["Y1"] = 0;
            _axisPositions["X2"] = 0;
            _axisPositions["Z2"] = 135.0f; // NG position height
        }

        public async Task SimulateOperationAsync(string operationName)
        {
            if (!IsSimulationMode) return;

            _logger.LogInformation($"Simulating operation: {operationName}");
            
            switch (operationName)
            {
                case "MoveToPickup":
                    await SimulateAxisMove("X1", 100.0f);
                    await SimulateAxisMove("Y1", 100.0f);
                    break;
                case "InfeedPickup":
                    SetInput("Infeed_Vacuum_Sensor", true);
                    break;
                // Add more simulation scenarios
            }
        }

        private async Task SimulateAxisMove(string axis, float targetPos)
        {
            float current = _axisPositions.ContainsKey(axis) ? _axisPositions[axis] : 0;
            // Simple linear interpolation simulation
            _axisPositions[axis] = targetPos;
            await Task.Delay(500); // Simulate move time
            _logger.LogDebug($"Axis {axis} moved to {targetPos}");
        }

        public void SetInput(string tagName, bool value)
        {
            _inputs[tagName] = value;
            _logger.LogDebug($"Simulator Input {tagName} set to {value}");
        }

        public bool GetOutput(string tagName)
        {
            return _outputs.TryGetValue(tagName, out bool val) && val;
        }

        public float GetAxisPosition(string axisName)
        {
            return _axisPositions.TryGetValue(axisName, out float pos) ? pos : 0;
        }
    }
}
