using System;

namespace HaengSungAOI_WPF.Models
{
    public class TbAutoVisionScanout
    {
        public int Id { get; set; }
        public string Pid { get; set; }
        public string ScanoutStatus { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? ScanoutTime { get; set; }
        public string ebr {  get; set; }
        public string wo { get; set; }
    }
}
