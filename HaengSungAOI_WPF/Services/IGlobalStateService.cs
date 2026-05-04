using System.Collections.Generic;

namespace HaengSungAOI_WPF.Services
{
    public interface IGlobalStateService
    {
        int? ActualMachineId { get; set; }
        string MachineName { get; set; }
        Dictionary<string, int> ErrorDict { get; set; }
        bool IsAutoMode { get; set; }
    }

    public class GlobalStateService : IGlobalStateService
    {
        public int? ActualMachineId { get; set; }
        public string MachineName { get; set; }
        public Dictionary<string, int> ErrorDict { get; set; } = new Dictionary<string, int>();
        public bool IsAutoMode { get; set; } = false;
    }
}



