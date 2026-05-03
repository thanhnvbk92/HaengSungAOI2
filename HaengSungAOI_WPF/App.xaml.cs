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

namespace HaengSungAOI_WPF
{
    public partial class App : Application
    {
        private readonly IHost _host;
        public IServiceProvider ServiceProvider => _host.Services;
        
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

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddDebug();
                    logging.AddConsole();
                })
                .Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core Services
            services.AddSingleton<IGlobalStateService, GlobalStateService>();
            services.AddSingleton<AutoVisionDbService>();
            services.AddSingleton<IErrorService, ErrorService>();

            // Machine & Hardware Services
            services.AddSingleton<IPlcService, PlcService>();
            services.AddSingleton<IVisionService, VisionService>();
            services.AddSingleton<IScanOutService, ScanOutService>();
            services.AddSingleton<IImageDisplayService, ImageDisplayService>();
            services.AddSingleton<IHmiService, HmiService>();
            services.AddSingleton<IMachineService, MachineService>();

            // ViewModels
            services.AddSingleton<HmiViewModel>();
            services.AddSingleton<MainViewModel>();

            // Windows
            services.AddSingleton<MainWindow>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            if (_host == null) return;

            await _host.StartAsync();

            // Khởi tạo Global State từ DB (giữ nguyên logic cũ nhưng bóc tách)
            await InitializeGlobalStateAsync();

            // Thiết lập xử lý lỗi toàn cục
            SetupExceptionHandling();

            // Hiển thị Main Window
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private async Task InitializeGlobalStateAsync()
        {
            try
            {
                var globalState = _host.Services.GetRequiredService<IGlobalStateService>();
                var dbService = _host.Services.GetRequiredService<AutoVisionDbService>();
                
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
                var globalState = _host.Services.GetRequiredService<IGlobalStateService>();
                if (globalState.ActualMachineId.HasValue)
                {
                    var dbService = _host.Services.GetRequiredService<AutoVisionDbService>();
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

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}
