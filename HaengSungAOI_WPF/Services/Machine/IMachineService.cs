using System;
using System.Threading.Tasks;
using HaengSungAOI_WPF.Machine;
using HaengSungAOI_WPF.Models;

namespace HaengSungAOI_WPF.Services.Machine
{
    public interface IMachineService : IDisposable
    {
        bool IsRunning { get; }
        bool IsInitialized { get; }
        MachineMode Mode { get; set; }
        PCBModel CurrentModel { get; }
        object FrontendControl { get; set; }

        void Initialize();
        void Start();
        void Stop();
        void EmergencyStop();
        
        void ClearQueues();
        void UpdateModel(PCBModel model);
        
        event Action<bool> OnRunningStateChanged;
        event Action<string> OnStatusMessageChanged;
    }
}
