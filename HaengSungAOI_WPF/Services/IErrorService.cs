using System;

namespace HaengSungAOI_WPF.Services
{
    public interface IErrorService
    {
        int TotalErrorCount { get; }
        int UnacknowledgedErrorCount { get; }
        int CriticalErrorCount { get; }
        bool HasAlarms { get; }
        
        event Action ErrorsChanged;
        
        void AcknowledgeAll();
        void ReportError(string source, string message, Exception ex = null);
        void ReportError(ErrorType type, string source, string message, Exception ex = null);
    }
}



