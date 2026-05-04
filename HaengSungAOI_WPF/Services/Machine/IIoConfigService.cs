using System.Collections.Generic;
using HaengSungAOI_WPF.Models;

namespace HaengSungAOI_WPF.Services.Machine
{
    public interface IIoConfigService
    {
        IoConfig CurrentConfig { get; }
        void LoadConfig(string filePath);
        ushort GetAddress(string tagName);
        bool TryGetAddress(string tagName, out ushort address);
    }
}



