using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services;
using HaengSungAOI_WPF.Services.Machine;
using HaengSungAOI_WPF.Services.UI;
using System.Windows.Input;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IGlobalStateService _globalState;
        private readonly IMachineService _machineService;
        private readonly IErrorService _errorService;
        private readonly MainWindowDialogService _dialogService;
        public HmiViewModel Hmi { get; }

        private string _currentEbrValue = "Not Set";
        public string CurrentEbrValue
        {
            get => _currentEbrValue;
            set
            {
                if (SetProperty(ref _currentEbrValue, value))
                {
                    // Đồng bộ xuống Machine static variable
                    HaengSungAOI_WPF.Core.Machine.CurrentEbr = (value == "Not Set") ? "" : value;
                }
            }
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        private bool _isInitialized=true;
        public bool IsInitialized
        {
            get => _isInitialized;
            set => SetProperty(ref _isInitialized, value);
        }

        private string _currentModelName = "No Model Selected";
        public string CurrentModelName
        {
            get => _currentModelName;
            set => SetProperty(ref _currentModelName, value);
        }

        private string _errorStatusText = "System OK";
        public string ErrorStatusText
        {
            get => _errorStatusText;
            set => SetProperty(ref _errorStatusText, value);
        }

        private string _errorStatusColor = "#00FF00"; // Lime
        public string ErrorStatusColor
        {
            get => _errorStatusColor;
            set => SetProperty(ref _errorStatusColor, value);
        }

        private string _errorListButtonContent = "Error List";
        public string ErrorListButtonContent
        {
            get => _errorListButtonContent;
            set => SetProperty(ref _errorListButtonContent, value);
        }

        private string _errorListButtonBackground = "#232336";
        public string ErrorListButtonBackground
        {
            get => _errorListButtonBackground;
            set => SetProperty(ref _errorListButtonBackground, value);
        }

        private string _pcbSlotText = "0/48";
        public string PcbSlotText
        {
            get => _pcbSlotText;
            set => SetProperty(ref _pcbSlotText, value);
        }

        private string _pcbTrayQuantityText = "0";
        public string PcbTrayQuantityText
        {
            get => _pcbTrayQuantityText;
            set => SetProperty(ref _pcbTrayQuantityText, value);
        }

        private string _blankTrayQuantityText = "0";
        public string BlankTrayQuantityText
        {
            get => _blankTrayQuantityText;
            set => SetProperty(ref _blankTrayQuantityText, value);
        }

        private System.Windows.Media.Brush _pcbSlotForeground = System.Windows.Media.Brushes.White;
        public System.Windows.Media.Brush PcbSlotForeground
        {
            get => _pcbSlotForeground;
            set => SetProperty(ref _pcbSlotForeground, value);
        }

        private System.Windows.Media.Brush _pcbTrayQuantityForeground = System.Windows.Media.Brushes.White;
        public System.Windows.Media.Brush PcbTrayQuantityForeground
        {
            get => _pcbTrayQuantityForeground;
            set => SetProperty(ref _pcbTrayQuantityForeground, value);
        }

        private System.Windows.Media.Brush _blankTrayQuantityForeground = System.Windows.Media.Brushes.White;
        public System.Windows.Media.Brush BlankTrayQuantityForeground
        {
            get => _blankTrayQuantityForeground;
            set => SetProperty(ref _blankTrayQuantityForeground, value);
        }

        public ObservableCollection<InspectionResult> InspectionHistory { get; } = new ObservableCollection<InspectionResult>();

        public MainViewModel(IGlobalStateService globalState, IMachineService machineService, HmiViewModel hmi, IErrorService errorService, MainWindowDialogService dialogService)
        {
            _globalState = globalState;
            _machineService = machineService;
            _errorService = errorService;
            _dialogService = dialogService;
            Hmi = hmi;

            _machineService.OnRunningStateChanged += (running) => IsRunning = running;
            _machineService.OnStatusMessageChanged += (msg) => StatusMessage = msg;
            
            _errorService.ErrorsChanged += UpdateErrorStatus;
            
            // Đăng ký nhận kết quả Vision
            WeakReferenceMessenger.Default.Register<InspectionResult>(this, (r, m) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Thêm vào đầu danh sách để hiển thị mới nhất lên trên
                    InspectionHistory.Insert(0, m);
                    // Giới hạn 100 bản ghi để tránh lag
                    if (InspectionHistory.Count > 100) InspectionHistory.RemoveAt(100);
                });
            });

            // Khởi tạo trạng thái ban đầu
            CurrentModelName = _machineService.CurrentModel?.ModelName ?? "No Model Selected";
            UpdateErrorStatus();
            IsInitialized = true;
        }

        private void UpdateErrorStatus()
        {
            int total = _errorService.TotalErrorCount;
            ErrorStatusText = total > 0 ? $"{total} Active Errors" : "System OK";
            ErrorStatusColor = total > 0 ? "#FF0000" : "#00FF00";
            ErrorListButtonBackground = total > 0 ? "#FF4444" : "#232336";
            ErrorListButtonContent = total > 0 ? $"Error List ({total})" : "Error List";
        }

        // Removed HMI buttons from MainViewModel

        private void ChangeLanguage(string cultureCode)
        {
            StatusMessage = $"Language changed to: {cultureCode}";
        }

        private void StartMachine()
        {
            _machineService.Start();
        }

        private void StopMachine()
        {
            _machineService.Stop();
        }

        private void ShowManualOperations()
        {
            StatusMessage = "Opening Manual Operations...";
            _dialogService.ShowManualOperationsWindow(System.Windows.Application.Current.MainWindow);
        }

        private void ShowModelJob()
        {
            StatusMessage = "Opening Model Selection...";
            _dialogService.ShowModelConfigWindow(System.Windows.Application.Current.MainWindow);
        }

        private void ShowHistory()
        {
            StatusMessage = "Opening History...";
            _dialogService.ShowHistoryWindow(System.Windows.Application.Current.MainWindow);
        }

        private void ShowSettings()
        {
            StatusMessage = "Opening Settings...";
            _dialogService.ShowSettingsWindow(System.Windows.Application.Current.MainWindow);
        }

        private void ShowErrorList()
        {
            StatusMessage = "Opening Error List...";
            _dialogService.ShowErrorListWindow(System.Windows.Application.Current.MainWindow);
        }

        private void ShowLogs()
        {
            StatusMessage = "Opening Logs...";
        }

        private void ShowDatabase()
        {
            StatusMessage = "Opening Database...";
        }

        private void ShowCamera()
        {
            StatusMessage = "Opening Camera...";
        }

        private void ShowHelp()
        {
            StatusMessage = "Opening Help...";
        }

        private void ExitApplication()
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void EditTrayQuantity(string type)
        {
            StatusMessage = $"Editing {type} Quantity...";
        }

        public System.Windows.Input.ICommand ChangeLanguageCommand => new RelayCommand<string>(ChangeLanguage);
        public System.Windows.Input.ICommand StartMachineCommand => new RelayCommand(StartMachine);
        public System.Windows.Input.ICommand StopMachineCommand => new RelayCommand(StopMachine);
        public System.Windows.Input.ICommand ShowManualOperationsCommand => new RelayCommand(ShowManualOperations);
        public System.Windows.Input.ICommand ShowModelJobCommand => new RelayCommand(ShowModelJob);
        public System.Windows.Input.ICommand ShowHistoryCommand => new RelayCommand(ShowHistory);
        public System.Windows.Input.ICommand ShowSettingsCommand => new RelayCommand(ShowSettings);
        public System.Windows.Input.ICommand ShowErrorListCommand => new RelayCommand(ShowErrorList);
        public System.Windows.Input.ICommand ShowLogsCommand => new RelayCommand(ShowLogs);
        public System.Windows.Input.ICommand ShowDatabaseCommand => new RelayCommand(ShowDatabase);
        public System.Windows.Input.ICommand ShowCameraCommand => new RelayCommand(ShowCamera);
        public System.Windows.Input.ICommand ShowHelpCommand => new RelayCommand(ShowHelp);
        public System.Windows.Input.ICommand ExitApplicationCommand => new RelayCommand(ExitApplication);
        public System.Windows.Input.ICommand EditTrayQuantityCommand => new RelayCommand<string>(EditTrayQuantity);

    }
}



