using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HaengSungAOI_WPF.Services;
using HaengSungAOI_WPF.Services.Machine;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Services.UI;
using HaengSungAOI_WPF.ViewModels;
using HaengSungAOI_WPF.Services.Database;
using System.Configuration;
using LeadshineHmi.Services;
using HaengSungAOI_WPF.Views;

namespace HaengSungAOI_WPF
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;
        public IServiceProvider ServiceProvider => _serviceProvider;
        
        public static IMachineService MachineService => ((App)Current).ServiceProvider.GetService<IMachineService>();
        public static IPlcService PlcService => ((App)Current).ServiceProvider.GetService<IPlcService>();
        public static AutoVisionDbService DatabaseService => ((App)Current).ServiceProvider.GetService<AutoVisionDbService>();
        public static IGlobalStateService GlobalState => ((App)Current).ServiceProvider.GetService<IGlobalStateService>();

        // Legacy static properties for Machine/Services access
        public static int? ActualMachineId { get; set; }
        public static Dictionary<string, int> ErrorDict { get; set; } = new Dictionary<string, int>();

        public App()
        {
            // Kiểm tra Single Instance sớm nhất có thể
            if (!IsSingleInstance())
            {
                MessageBox.Show("Chương trình đã chạy...", "Thông báo");
                Application.Current.Shutdown();
                return;
            }

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(configure => 
            {
                // configure.AddConsole(); // Nếu cần thiết
            });

            // Core Services
            services.AddSingleton<IGlobalStateService, GlobalStateService>();
            services.AddSingleton<AutoVisionDbService>();
            services.AddSingleton<InspectionHistoryManager>();
            services.AddSingleton<IModelDatabaseManager, ModelDatabaseManager>();
            services.AddSingleton<IErrorService, ErrorService>();
            services.AddSingleton<MainWindowDialogService>();

            // Machine & Hardware Services
            services.AddSingleton<IPlcService, HmiPlcService>();
            services.AddSingleton<IVisionService, VisionService>();
            services.AddSingleton<IScanOutService, ScanOutService>();
            services.AddSingleton<IImageDisplayService, ImageDisplayService>();
            services.AddSingleton<IIoConfigService, IoConfigService>();
            services.AddSingleton<IHmiSimulatorService, HmiSimulatorService>();
            services.AddSingleton<IHmiService, HmiService>();
            services.AddSingleton<IMachineHmiService, MachineHmiService>();
            services.AddSingleton<IMachineService, MachineService>();

            // ViewModels
            services.AddSingleton<HmiViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<ModelConfigViewModel>();
            services.AddTransient<HistoryViewModel>();
            services.AddTransient<ManualOperationsViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Windows
            services.AddSingleton<MainWindow>();
            services.AddTransient<ManualOperations>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<ErrorListWindow>(sp => 
            {
                var machineService = sp.GetRequiredService<IMachineService>();
                return new ErrorListWindow(machineService.Machine);
            });
            services.AddTransient<ModelConfig>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            if (_serviceProvider == null) return;

            // Khởi tạo Global State từ DB (giữ nguyên logic cũ nhưng bóc tách)
            await InitializeGlobalStateAsync();

            // Thiết lập xử lý lỗi toàn cục
            SetupExceptionHandling();

            // Hiển thị Main Window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private async Task InitializeGlobalStateAsync()
        {
            try
            {
                var globalState = _serviceProvider.GetRequiredService<IGlobalStateService>();
                var dbService = _serviceProvider.GetRequiredService<AutoVisionDbService>();
                
                globalState.MachineName = ConfigurationManager.AppSettings["Machine_Name"];
                
                if (!string.IsNullOrEmpty(globalState.MachineName))
                {
                    globalState.ActualMachineId = await dbService.GetActualMachineIdAsync(globalState.MachineName);
                    App.ActualMachineId = globalState.ActualMachineId; // Sync to legacy static
                }
                globalState.ErrorDict = await dbService.LoadErrorDictionaryAsync();
                App.ErrorDict = globalState.ErrorDict; // Sync to legacy static
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing global state: {ex.Message}");
                // Bỏ qua lỗi kết nối DB khi khởi động như logic cũ
            }
        }

        private void SetupExceptionHandling()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                RecordMachineEndTimeOnCrash();
                // e.Handled = true; // Có thể set true nếu muốn ngăn app crash
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                RecordMachineEndTimeOnCrash();
            };
        }

        private void RecordMachineEndTimeOnCrash()
        {
            try
            {
                var globalState = _serviceProvider.GetRequiredService<IGlobalStateService>();
                if (globalState.ActualMachineId.HasValue)
                {
                    var dbService = _serviceProvider.GetRequiredService<AutoVisionDbService>();
                    // Chạy đồng bộ vì app đang crash
                    Task.Run(async () => await dbService.UpdateVisionOperatingEndAsync(globalState.ActualMachineId.Value)).Wait(2000);
                }
            }
            catch { }
        }

        private bool IsSingleInstance()
        {
            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            int count = Process.GetProcessesByName(currentProcessName).Length;
            return count <= 1;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider != null)
            {
                _serviceProvider.Dispose();
            }
            base.OnExit(e);
        }
    }
}
