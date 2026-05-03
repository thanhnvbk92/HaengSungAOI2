using System;
using System.Windows;
using HaengSungAOI_WPF.ViewModels;
using HaengSungAOI_WPF.Services.Machine;

namespace HaengSungAOI_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IMachineService _machineService;

        public MainWindow(MainViewModel viewModel, IMachineService machineService)
        {
            InitializeComponent();
            
            _machineService = machineService;
            this.DataContext = viewModel;

            // Đăng ký sự kiện Loaded để khởi tạo các thành phần UI-bound (VisionMaster)
            this.Loaded += MainWindow_Loaded;
            
            // Đăng ký sự kiện Closing để dọn dẹp tài nguyên
            this.Closing += (s, e) => _machineService.Dispose();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Kết nối control FrontEnd (VisionMaster) vào service
            // Đây là yêu cầu cấp UI vì control này được định nghĩa trong XAML
            if (FrontEnd != null)
            {
                _machineService.FrontendControl = FrontEnd;
            }

            // Trigger khởi tạo máy từ ViewModel nếu cần, hoặc Service đã tự chạy
            // Lưu ý: MainViewModel hiện đang tự chạy Initialize() trong constructor
            // Nhưng tốt nhất nên đảm bảo FrontendControl đã được set trước.
            if (!_machineService.IsInitialized)
            {
                _machineService.Initialize();
            }
        }
    }
}
