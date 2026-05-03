using System;
using System.Threading.Tasks;
using HaengSungAOI_WPF.Machine;

namespace HaengSungAOI_WPF.Services.Machine
{
    public class ScanOutReceivedEventArgs : EventArgs
    {
        public string RawResponse { get; }
        public string PID { get; }
        public string Status { get; }
        public string ErrorMessage { get; }

        public ScanOutReceivedEventArgs(string rawResponse)
        {
            RawResponse = rawResponse;
            var parts = rawResponse.Split('|');
            if (parts.Length >= 2)
            {
                Status = parts[0];
                PID = parts[1];
                if (parts.Length >= 5) ErrorMessage = parts[4];
            }
        }
    }

    public interface IScanOutService : IDisposable
    {
        bool IsOpen { get; }
        void Open(string portName, int baudRate = 115200);
        void Close();
        
        Task<ScanOutResult> PerformScanOutAsync(string pid, int slot);
        
        event EventHandler<ScanOutReceivedEventArgs> DataReceived;
    }
}
