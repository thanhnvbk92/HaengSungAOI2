using System.Collections.Generic;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.Services.Machine
{
    public interface IHmiSimulatorService
    {
        bool IsSimulationMode { get; set; }
        Task SimulateOperationAsync(string operationName);
        void SetInput(string tagName, bool value);
        bool GetOutput(string tagName);
        float GetAxisPosition(string axisName);
    }
}
