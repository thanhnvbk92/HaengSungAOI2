using System;

namespace HaengSungAOI_WPF.Models
{
    public class TbAutoVisionResult
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public string Pid { get; set; }
        public string WorkOrder { get; set; }
        public string Station { get; set; }
        public string Result { get; set; }
        public string Ebr { get; set; }
        public string ImagePath { get; set; }
        public DateTime? InspectionTime { get; set; }
        public double? TackTime { get; set; }
        public string Note {  get; set; }
    }
}



