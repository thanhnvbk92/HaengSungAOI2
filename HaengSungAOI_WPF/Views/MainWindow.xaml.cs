using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using HaengSungAOI_WPF.ViewModels;
using HaengSungAOI_WPF.Services.Machine;

namespace HaengSungAOI_WPF.Views
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

        /// <summary>
        /// Cập nhật giá trị EBR từ backend (được gọi từ logic Machine.PLC cũ)
        /// </summary>
        public void SetEbrFromBackend(string ebr)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.CurrentEbrValue = ebr;
            }
        }

        #region Language Flag Event Handlers

        private void FlagVN_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Chuyển ngôn ngữ sang Tiếng Việt
            System.Diagnostics.Debug.WriteLine("[MainWindow] Language changed to Vietnamese");
        }

        private void FlagKR_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Chuyển ngôn ngữ sang Tiếng Hàn
            System.Diagnostics.Debug.WriteLine("[MainWindow] Language changed to Korean");
        }

        private void FlagEN_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Chuyển ngôn ngữ sang Tiếng Anh
            System.Diagnostics.Debug.WriteLine("[MainWindow] Language changed to English");
        }

        #endregion

        #region Image Event Handlers

        private void ViewImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string imagePath)
            {
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(imagePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Cannot open image: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Image file not found.", "Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        #endregion
    }
}



