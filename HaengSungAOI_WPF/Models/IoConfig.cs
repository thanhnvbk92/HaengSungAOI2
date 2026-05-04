using System.Collections.Generic;

namespace HaengSungAOI_WPF.Models
{
    public class IoItem
    {
        public string Name { get; set; }
        public ushort Address { get; set; }
        public string Type { get; set; } // Coil, HoldingRegister, etc.
        public string Description { get; set; }
    }

    public class IoConfig
    {
        public List<IoItem> PushButtons { get; set; } = new List<IoItem>();
        public List<IoItem> Lamps { get; set; } = new List<IoItem>();
        public List<IoItem> Registers { get; set; } = new List<IoItem>();
        public Dictionary<string, ushort> RobotPositions { get; set; } = new Dictionary<string, ushort>();
    }
}



