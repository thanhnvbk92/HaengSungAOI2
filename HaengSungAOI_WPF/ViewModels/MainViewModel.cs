using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services;
using HaengSungAOI_WPF.Services.Machine;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IGlobalStateService _globalState;
        private readonly IMachineService _machineService;
        private readonly IErrorService _errorService;
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
                    HaengSungAOI_WPF.Machine.Machine.CurrentEbr = (value == "Not Set") ? "" : value;
                }
            }
        }

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private bool _isInitialized;

        [ObservableProperty]
        private string _currentModelName = "No Model Selected";

        [ObservableProperty]
        private string _errorStatusText = "System OK";

        [ObservableProperty]
        private string _errorStatusColor = "#00FF00"; // Lime

        [ObservableProperty]
        private string _errorListButtonContent = "Error List";

        [ObservableProperty]
        private string _errorListButtonBackground = "#232336";

        public ObservableCollection<InspectionResult> InspectionHistory { get; } = new ObservableCollection<InspectionResult>();

        public MainViewModel(IGlobalStateService globalState, IMachineService machineService, HmiViewModel hmi, IErrorService errorService)
        {
            _globalState = globalState;
            _machineService = machineService;
            _errorService = errorService;
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
        }

        [RelayCommand]
        private void ChangeLanguage(string cultureCode)
        {
            // Logic chuyển đổi ngôn ngữ
            StatusMessage = $"Language changed to: {cultureCode}";
            // Ở đây có thể gọi một ILocalizationService
        }

        private void UpdateErrorStatus()
        {
            int critical = _errorService.CriticalErrorCount;
            int unacknowledged = _errorService.UnacknowledgedErrorCount;
            int total = _errorService.TotalErrorCount;

            if (critical > 0)
            {
                ErrorStatusText = $"CRITICAL: {critical} critical error(s)";
                ErrorStatusColor = "#FF0000"; // Red
                ErrorListButtonBackground = "#FF0000";
            }
            else if (unacknowledged > 0)
            {
                ErrorStatusText = $"ERRORS: {unacknowledged} unacknowledged error(s)";
                ErrorStatusColor = "#FFA500"; // Orange
                ErrorListButtonBackground = "#FFA500";
            }
            else
            {
                ErrorStatusText = "System OK";
                ErrorStatusColor = "#00FF00"; // Lime
                ErrorListButtonBackground = "#232336";
            }

            ErrorListButtonContent = total > 0 ? $"Error List ({total})" : "Error List";
        }

        [RelayCommand]
        private void StartMachine()
        {
            _machineService.Start();
        }

        [RelayCommand]
        private void StopMachine()
        {
            _machineService.Stop();
        }

        [RelayCommand]
        private void ShowModelJob()
        {
            _statusMessage = "Opening Model/Job Selection...";
            // Logic mở cửa sổ Model/Job
        }

        [RelayCommand]
        private void ShowHistory()
        {
            _statusMessage = "Opening History...";
        }

        [RelayCommand]
        private void ShowSettings()
        {
            _statusMessage = "Opening Settings...";
        }

        [RelayCommand]
        private void ShowErrorList()
        {
            _statusMessage = "Opening Error List...";
        }

        [RelayCommand]
        private void ShowLogs()
        {
            _statusMessage = "Opening Logs...";
        }

        [RelayCommand]
        private void ShowDatabase()
        {
            _statusMessage = "Opening Database...";
        }

        [RelayCommand]
        private void ShowCamera()
        {
            _statusMessage = "Opening Camera...";
        }

        [RelayCommand]
        private void ShowHelp()
        {
            _statusMessage = "Opening Help...";
        }

        [RelayCommand]
        private void ExitApplication()
        {
            System.Windows.Application.Current.Shutdown();
        }

        [RelayCommand]
        private void EditTrayQuantity(string type)
        {
            // Logic ShowTrayQuantityEditDialog sẽ được gọi ở đây hoặc qua Service
            // Để đơn giản và tuân thủ MVVM, ta có thể dùng IDialogService nếu có, 
            // hoặc phát ra một Message/Event để View hiển thị Dialog.
            StatusMessage = $"Editing {type} Quantity...";
        }
    }
}
