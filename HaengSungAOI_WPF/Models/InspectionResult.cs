using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.Models
{
    /// <summary>
    /// Represents an AOI inspection result record
    /// </summary>
    public class InspectionResult : INotifyPropertyChanged
    {
        private int _id;
        private int _stt;
        private string _pcbCode;
        private string _modelName;
        private DateTime _inspectionDateTime;
        private string _result;
        private int _totalDefects;
        private int _totalOK;
        private int _totalNG;
        private string _operatorName;
        private string _station;
        private string _note;
        private double _inspectionTime;
        private string _imagePath;
        private string _reportPath;
        private List<DefectResult> _defects;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public int STT
        {
            get => _stt;
            set { _stt = value; OnPropertyChanged(nameof(STT)); }
        }

        public string PCBCode
        {
            get => _pcbCode;
            set { _pcbCode = value; OnPropertyChanged(nameof(PCBCode)); }
        }

        public string ModelName
        {
            get => _modelName;
            set { _modelName = value; OnPropertyChanged(nameof(ModelName)); }
        }

        public DateTime InspectionDateTime
        {
            get => _inspectionDateTime;
            set { _inspectionDateTime = value; OnPropertyChanged(nameof(InspectionDateTime)); }
        }

        public string InspectionDateTimeString => InspectionDateTime.ToString("yyyy-MM-dd HH:mm:ss");

        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(nameof(Result)); }
        }

        public int TotalDefects
        {
            get => _totalDefects;
            set { _totalDefects = value; OnPropertyChanged(nameof(TotalDefects)); }
        }

        public int TotalOK
        {
            get => _totalOK;
            set { _totalOK = value; OnPropertyChanged(nameof(TotalOK)); }
        }

        public int TotalNG
        {
            get => _totalNG;
            set { _totalNG = value; OnPropertyChanged(nameof(TotalNG)); }
        }

        public string OperatorName
        {
            get => _operatorName;
            set { _operatorName = value; OnPropertyChanged(nameof(OperatorName)); }
        }

        public string Station
        {
            get => _station;
            set { _station = value; OnPropertyChanged(nameof(Station)); }
        }

        public string Note
        {
            get => _note;
            set { _note = value; OnPropertyChanged(nameof(Note)); }
        }

        public double InspectionTime
        {
            get => _inspectionTime;
            set { _inspectionTime = value; OnPropertyChanged(nameof(InspectionTime)); }
        }

        public string InspectionTimeString => $"{InspectionTime:F2}s";

        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
        }

        public string ReportPath
        {
            get => _reportPath;
            set { _reportPath = value; OnPropertyChanged(nameof(ReportPath)); }
        }

        public List<DefectResult> Defects
        {
            get => _defects ?? (_defects = new List<DefectResult>());
            set { _defects = value; OnPropertyChanged(nameof(Defects)); }
        }

        public InspectionResult()
        {
            InspectionDateTime = DateTime.Now;
            OperatorName = "admin";
            Result = "PASS";
            Defects = new List<DefectResult>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a single defect found during inspection
    /// </summary>
    public class DefectResult : INotifyPropertyChanged
    {
        private int _id;
        private int _inspectionResultId;
        private string _camera;
        private string _errorType;
        private string _coordinates;
        private string _imagePath;
        private string _status;
        private double _confidence;
        private string _description;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public int InspectionResultId
        {
            get => _inspectionResultId;
            set { _inspectionResultId = value; OnPropertyChanged(nameof(InspectionResultId)); }
        }

        public string Camera
        {
            get => _camera;
            set { _camera = value; OnPropertyChanged(nameof(Camera)); }
        }

        public string ErrorType
        {
            get => _errorType;
            set { _errorType = value; OnPropertyChanged(nameof(ErrorType)); }
        }

        public string Coordinates
        {
            get => _coordinates;
            set { _coordinates = value; OnPropertyChanged(nameof(Coordinates)); }
        }

        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public double Confidence
        {
            get => _confidence;
            set { _confidence = value; OnPropertyChanged(nameof(Confidence)); }
        }

        public string ConfidenceString => $"{Confidence:F1}%";

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public DefectResult()
        {
            Status = "NEW";
            Confidence = 95.0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}