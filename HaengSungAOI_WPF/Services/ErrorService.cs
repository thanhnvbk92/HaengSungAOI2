using System;
using HaengSungAOI_WPF.Machine;

namespace HaengSungAOI_WPF.Services
{
    public class ErrorService : IErrorService
    {
        private readonly MachineErrorList _errorList;
        
        public int TotalErrorCount => _errorList.ErrorCount;
        public int UnacknowledgedErrorCount => _errorList.UnacknowledgedErrorCount;
        public int CriticalErrorCount => _errorList.UnacknowledgedCriticalErrorCount;
        
        // Alarms logic moved from MainWindow.xaml.cs if needed, 
        // for now we can simplify or expose if MachineErrorList handles it.
        public bool HasAlarms { get; set; } 

        public event Action ErrorsChanged;

        public ErrorService()
        {
            _errorList = MachineErrorList.Instance;
            _errorList.ErrorAdded += (s, e) => ErrorsChanged?.Invoke();
            _errorList.CriticalErrorAdded += (s, e) => ErrorsChanged?.Invoke();
            // In a real scenario, we would also subscribe to events that decrease count
        }

        public void AcknowledgeAll()
        {
            // Implementation of acknowledging errors if needed
        }
    }
}
