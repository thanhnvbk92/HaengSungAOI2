using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HaengSungAOI_WPF.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace HaengSungAOI_WPF.ViewModels
{
    public  class MainWindowViewModel : ObservableObject
    {
        private const int MaxHistoryItems = 50;

        private string _currentModelName = "No Model Selected";
        private string _currentEbrValue = "Not Set";
        private bool _enableScanOut;
        private bool _isByPass;
        private string _errorStatusText = "System OK";
        private string _machineStatusText = "Machine: Stopped";
        private string _pcbSlotText = "0 / 48";
        private string _pcbTrayQuantityText = "0";
        private string _blankTrayQuantityText = "0";
        private string _loadingStatusText = "Đang khởi tạo hệ thống...";
        private Brush _pcbSlotForeground = new SolidColorBrush(Color.FromRgb(0x87, 0xCE, 0xEB));
        private Brush _pcbTrayQuantityForeground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x7F));
        private Brush _blankTrayQuantityForeground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));

        public MainWindowViewModel()
        {
            ChangeToVietnameseCommand = new RelayCommand(() => RequestChangeToVietnamese?.Invoke());
            ChangeToKoreanCommand = new RelayCommand(() => RequestChangeToKorean?.Invoke());
            ChangeToEnglishCommand = new RelayCommand(() => RequestChangeToEnglish?.Invoke());
            OpenModelConfigCommand = new RelayCommand(() => RequestOpenModelConfig?.Invoke());
            OpenSettingsCommand = new RelayCommand(() => RequestOpenSettings?.Invoke());
            OpenErrorListCommand = new RelayCommand(() => RequestOpenErrorList?.Invoke());
            OpenTrayQuantityDialogCommand = new RelayCommand(() => RequestOpenTrayQuantityDialog?.Invoke());
            OpenEbrDialogCommand = new RelayCommand(() => RequestOpenEbrDialog?.Invoke());
            HmiButtonDownCommand = new RelayCommand<string>(OnHmiButtonDown);
            HmiButtonUpCommand = new RelayCommand<string>(OnHmiButtonUp);
        }

        public ObservableCollection<InspectionResult> RecentInspectionResults { get; } = new ObservableCollection<InspectionResult>();

        public ICommand ChangeToVietnameseCommand { get; }
        public ICommand ChangeToKoreanCommand { get; }
        public ICommand ChangeToEnglishCommand { get; }
        public ICommand OpenModelConfigCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenErrorListCommand { get; }
        public ICommand OpenTrayQuantityDialogCommand { get; }
        public ICommand OpenEbrDialogCommand { get; }
        public ICommand HmiButtonDownCommand { get; }
        public ICommand HmiButtonUpCommand { get; }

        public event Action RequestChangeToVietnamese;
        public event Action RequestChangeToKorean;
        public event Action RequestChangeToEnglish;
        public event Action RequestOpenModelConfig;
        public event Action RequestOpenSettings;
        public event Action RequestOpenErrorList;
        public event Action RequestOpenTrayQuantityDialog;
        public event Action RequestOpenEbrDialog;
        public event Action<string> EbrValueChanged;
        public event Action<bool> EnableScanOutChanged;
        public event Action<bool> IsByPassChanged;
        public event Action<string> RequestHmiButtonDown;
        public event Action<string> RequestHmiButtonUp;

        public string CurrentModelName
        {
            get => _currentModelName;
            set => SetProperty(ref _currentModelName, value);
        }

        public string CurrentEbrValue
        {
            get => _currentEbrValue;
            set
            {
                if (SetProperty(ref _currentEbrValue, value))
                {
                    EbrValueChanged?.Invoke(value);
                }
            }
        }

        public bool EnableScanOut
        {
            get => _enableScanOut;
            set
            {
                if (SetProperty(ref _enableScanOut, value))
                {
                    EnableScanOutChanged?.Invoke(value);
                }
            }
        }

        public bool IsByPass
        {
            get => _isByPass;
            set
            {
                if (SetProperty(ref _isByPass, value))
                {
                    IsByPassChanged?.Invoke(value);
                }
            }
        }

        public string ErrorStatusText
        {
            get => _errorStatusText;
            set => SetProperty(ref _errorStatusText, value);
        }

        public string MachineStatusText
        {
            get => _machineStatusText;
            set => SetProperty(ref _machineStatusText, value);
        }

        public string PcbSlotText
        {
            get => _pcbSlotText;
            set => SetProperty(ref _pcbSlotText, value);
        }

        public string PcbTrayQuantityText
        {
            get => _pcbTrayQuantityText;
            set => SetProperty(ref _pcbTrayQuantityText, value);
        }

        public string BlankTrayQuantityText
        {
            get => _blankTrayQuantityText;
            set => SetProperty(ref _blankTrayQuantityText, value);
        }

        public string LoadingStatusText
        {
            get => _loadingStatusText;
            set => SetProperty(ref _loadingStatusText, value);
        }

        public Brush PcbSlotForeground
        {
            get => _pcbSlotForeground;
            set => SetProperty(ref _pcbSlotForeground, value);
        }

        public Brush PcbTrayQuantityForeground
        {
            get => _pcbTrayQuantityForeground;
            set => SetProperty(ref _pcbTrayQuantityForeground, value);
        }

        public Brush BlankTrayQuantityForeground
        {
            get => _blankTrayQuantityForeground;
            set => SetProperty(ref _blankTrayQuantityForeground, value);
        }

        public void AddHistoryResult(InspectionResult displayResult)
        {
            RecentInspectionResults.Insert(0, displayResult);
            if (RecentInspectionResults.Count > MaxHistoryItems)
            {
                RecentInspectionResults.RemoveAt(RecentInspectionResults.Count - 1);
            }

            for (int i = 0; i < RecentInspectionResults.Count; i++)
            {
                RecentInspectionResults[i].STT = i + 1;
            }
        }

        private void OnHmiButtonDown(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            RequestHmiButtonDown?.Invoke(tag);
        }

        private void OnHmiButtonUp(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            RequestHmiButtonUp?.Invoke(tag);
        }
    }
}
