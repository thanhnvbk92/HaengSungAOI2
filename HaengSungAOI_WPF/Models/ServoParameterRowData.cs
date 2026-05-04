using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HaengSungAOI_WPF.Models
{
    /// <summary>
    /// Represents a row in the servo parameter table (one parameter across all axes)
    /// </summary>
    public partial class ServoParameterRowData : ObservableObject
    {
        private string _ax1;
        public string AX1 { get => _ax1; set => SetProperty(ref _ax1, value); }

        private string _ay1;
        public string AY1 { get => _ay1; set => SetProperty(ref _ay1, value); }

        private string _ac1;
        public string AC1 { get => _ac1; set => SetProperty(ref _ac1, value); }

        private string _ax2;
        public string AX2 { get => _ax2; set => SetProperty(ref _ax2, value); }

        private string _az2;
        public string AZ2 { get => _az2; set => SetProperty(ref _az2, value); }

        private string _ax3;
        public string AX3 { get => _ax3; set => SetProperty(ref _ax3, value); }

        private string _ay3;
        public string AY3 { get => _ay3; set => SetProperty(ref _ay3, value); }

        private string _az4;
        public string AZ4 { get => _az4; set => SetProperty(ref _az4, value); }

        private string _ac4;
        public string AC4 { get => _ac4; set => SetProperty(ref _ac4, value); }

        private string _az5;
        public string AZ5 { get => _az5; set => SetProperty(ref _az5, value); }

        private string _ac5;
        public string AC5 { get => _ac5; set => SetProperty(ref _ac5, value); }

        private string _az61;
        public string AZ61 { get => _az61; set => SetProperty(ref _az61, value); }

        private string _az62;
        public string AZ62 { get => _az62; set => SetProperty(ref _az62, value); }

        private string _cv7;
        public string CV7 { get => _cv7; set => SetProperty(ref _cv7, value); }

        public string ParameterName { get; set; }
        public string DataType { get; set; }
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Set value for a specific axis
        /// </summary>
        public void SetAxisValue(string axisName, string value)
        {
            switch (axisName)
            {
                case "AX1": AX1 = value; break;
                case "AY1": AY1 = value; break;
                case "AC1": AC1 = value; break;
                case "AX2": AX2 = value; break;
                case "AZ2": AZ2 = value; break;
                case "AX3": AX3 = value; break;
                case "AY3": AY3 = value; break;
                case "AZ4": AZ4 = value; break;
                case "AC4": AC4 = value; break;
                case "AZ5": AZ5 = value; break;
                case "AC5": AC5 = value; break;
                case "AZ61": AZ61 = value; break;
                case "AZ62": AZ62 = value; break;
                case "CV7": CV7 = value; break;
            }
        }

        /// <summary>
        /// Get value for a specific axis
        /// </summary>
        public string GetAxisValue(string axisName)
        {
            switch (axisName)
            {
                case "AX1": return AX1;
                case "AY1": return AY1;
                case "AC1": return AC1;
                case "AX2": return AX2;
                case "AZ2": return AZ2;
                case "AX3": return AX3;
                case "AY3": return AY3;
                case "AZ4": return AZ4;
                case "AC4": return AC4;
                case "AZ5": return AZ5;
                case "AC5": return AC5;
                case "AZ61": return AZ61;
                case "AZ62": return AZ62;
                case "CV7": return CV7;
                default: return null;
            }
        }
    }
}



