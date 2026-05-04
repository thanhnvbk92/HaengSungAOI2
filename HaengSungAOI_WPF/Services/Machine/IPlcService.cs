using System;
using System.Collections.Generic;



namespace HaengSungAOI_WPF.Services.Machine
{
    public class VisionTriggerEventArgs : EventArgs
    {
        public string TagName { get; }
        public string ProcedureName { get; }
        public ushort TriggerValue { get; }
        public DateTime Timestamp { get; }

        public VisionTriggerEventArgs(string tagName, string procedureName, ushort triggerValue)
        {
            TagName = tagName;
            ProcedureName = procedureName;
            TriggerValue = triggerValue;
            Timestamp = DateTime.Now;
        }
    }

    public class AlarmEventArgs : EventArgs
    {
        public string AlarmName { get; }
        public string Message { get; }
        public bool IsActive { get; }
        public ushort Address { get; }

        public AlarmEventArgs(string alarmName, string message, bool isActive, ushort address)
        {
            AlarmName = alarmName;
            Message = message;
            IsActive = isActive;
            Address = address;
        }
    }

    public class TrayUpdateEventArgs : EventArgs
    {
        public string TagName { get; }
        public ushort NewValue { get; }

        public TrayUpdateEventArgs(string tagName, ushort newValue)
        {
            TagName = tagName;
            NewValue = newValue;
        }
    }

    public interface IPlcService : IDisposable
    {
        bool IsConnected { get; }
        void Start();
        void Stop();
        bool Connect();
        void Disconnect();

        event EventHandler<VisionTriggerEventArgs> VisionTriggered;
        event EventHandler<AlarmEventArgs> AlarmChanged;
        event EventHandler<TrayUpdateEventArgs> TrayUpdated;
        event EventHandler<bool> ConnectionStatusChanged;
        event EventHandler<Dictionary<string, bool>> HmiLampStateChanged;

        void WriteVisionResult(string procedureName, bool isOK);
        void WriteAlignPosition(double x, double y, double angle);
        void WriteRegister(string tagName, ushort value);
        void WriteDouble(string tagName, double value);
        System.Threading.Tasks.Task WriteRegisterAsync(string tagName, ushort value);
        System.Threading.Tasks.Task WriteDoubleAsync(string tagName, double value);
        System.Threading.Tasks.Task WriteRobotPositionAsync(string posName, double value);
        System.Threading.Tasks.Task DownloadModelParametersAsync(IDictionary<string, object> parameters);
        System.Threading.Tasks.Task SetHmiButtonAsync(string tagName, bool value);
        ushort[] GetRegisterArrayValue(string dataPointName);
        double GetDoubleValue(string tagName);
        ushort GetUInt16Value(string tagName);
        short GetInt16Value(string tagName);

        /// <summary>
        /// Reads a range of holding registers from the PLC starting at the specified address.
        /// </summary>
        ushort[] GetRegisterArrayValue(ushort startAddress, ushort count);

        /// <summary>
        /// Writes an array of values to holding registers starting at the specified address.
        /// </summary>
        void WriteHoldingRegisters(ushort address, ushort[] values);

        /// <summary>
        /// Writes multiple holding registers directly to an address.
        /// </summary>
        void WriteHoldingRegistersDirect(ushort address, ushort[] values);

        /// <summary>
        /// Writes a value to a coil by name.
        /// </summary>
        void WriteCoil(string name, bool value);

        /// <summary>
        /// Sets which groups of PLC tags should be actively monitored/polled.
        /// </summary>
        /// <param name="groups">Flags representing the groups to monitor.</param>
        void SetActiveMonitoringGroups(HaengSungAOI_WPF.Machine.PLC.PLCMonitoringGroup groups);
    }
}
