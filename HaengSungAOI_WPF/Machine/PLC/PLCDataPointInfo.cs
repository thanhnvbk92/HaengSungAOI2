namespace HaengSungAOI_WPF.Machine.PLC
{
    internal class PLCDataPointInfo
    {
        public string Name { get; set; }
        public ushort Address { get; set; }
        public string Description { get; set; }
        public bool IsMX { get; set; }
        public bool IsMR { get; set; }
        public bool IsB { get; set; }
    }
}
