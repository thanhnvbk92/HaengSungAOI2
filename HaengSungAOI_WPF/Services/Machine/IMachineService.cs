using System;
using System.Threading.Tasks;
using HaengSungAOI_WPF.Core;
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

        IPlcService PLC { get; }
        IMachineHmiService HMI { get; }
        HaengSungAOI_WPF.Core.Machine Machine { get; }
        
        void Initialize();
        void Start();
        void Stop();
        void EmergencyStop();
        void ResetEmergency();
        
        void ClearQueues();
        void UpdateModel(PCBModel model);
        
        bool EnableScanOut { get; set; }
        bool OverrideInspection { get; set; }
        
        event Action<bool> OnRunningStateChanged;
        event Action<string> OnStatusMessageChanged;
    }
}



