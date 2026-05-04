using System;
using System.Collections.Generic;
using HaengSungAOI_WPF.Core.PLC;

namespace HaengSungAOI_WPF.Services.Machine
{
    public interface IServoMonitorService : IDisposable
    {
        event EventHandler<ServoStatusChangedEventArgs> StatusChanged;
        event EventHandler<ServoErrorEventArgs> ErrorDetected;
        event EventHandler<ServoErrorEventArgs> ErrorCleared;
        event EventHandler<ServoAxis> MoveCompleted;

        IReadOnlyDictionary<ServoAxis, ServoAxisStatus> AxisStatuses { get; }
        bool IsMonitoring { get; }

        void StartMonitoring();
        void StopMonitoring();
        void ForceUpdate();
        
        ServoAxisStatus GetAxisStatus(ServoAxis axis);
        double GetCurrentPosition(ServoAxis axis);
        double GetCurrentSpeed(ServoAxis axis);
        double GetErrorCode(ServoAxis axis);
        bool HasError(ServoAxis axis);
        bool HasAnyError();
        List<ServoAxis> GetAxesWithErrors();
        bool IsAxisMoving(ServoAxis axis);
        bool IsAxisHomed(ServoAxis axis);
    }
}
