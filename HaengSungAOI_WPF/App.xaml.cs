using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HaengSungAOI_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static int? ActualMachineId { get; private set; }
        public static string Machine_Name { get; private set; }
        public static System.Collections.Generic.Dictionary<string, int> ErrorDict { get; private set; }

        /// <summary>
        /// true = máy đang ở Auto mode (sau khi nhấn HMI_Start, trước khi nhấn HMI_Stop).
        /// Được set bởi MainWindow khi nhấn HMI_Auto_PB / HMI_Manual_PB.
        /// </summary>
        public static bool IsAutoMode { get; set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Lấy Machine ID và Error Dictionary từ CSDL ngay khi khởi động
            try
            {
                Machine_Name = ConfigurationManager.AppSettings["Machine_Name"];
                Task.Run(async () =>
                {
                    var dbService = new HaengSungAOI_WPF.Services.Database.AutoVisionDbService();
                    if (!string.IsNullOrEmpty(Machine_Name))
                    {
                        ActualMachineId = await dbService.GetActualMachineIdAsync(Machine_Name);
                    }
                    ErrorDict = await dbService.LoadErrorDictionaryAsync();
                }).Wait();
            }
            catch
            {
                // Bỏ qua lỗi kết nối DB khi khởi động
            }

            int num = 0;
            try
            {
                Process[] processes = Process.GetProcesses();
                for (int i = 0; i < processes.Length; i++)
                {
                    try
                    {
                        if (processes[i].ProcessName.IndexOf("HaengSungAOI", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            num++;
                        }
                    }
                    catch
                    {
                        // ignore processes we can't inspect
                    }
                }
            }
            catch
            {
                // if process enumeration fails, allow startup to continue or handle as needed
            }

            if (num > 1)
            {
                MessageBox.Show("Chương trình đã chạy...", "Thông báo");
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            // Bắt lỗi crash toàn cục
            Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            RecordMachineEndTimeOnCrash();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            RecordMachineEndTimeOnCrash();
        }

        private void RecordMachineEndTimeOnCrash()
        {
            try
            {
                if (ActualMachineId.HasValue)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var dbService = new HaengSungAOI_WPF.Services.Database.AutoVisionDbService();
                            await dbService.UpdateVisionOperatingEndAsync(ActualMachineId.Value);
                        }
                        catch
                        {
                            // Bỏ qua lỗi trong quá trình crash
                        }
                    }).Wait(2000); // Đợi tối đa 2s để ghi nhận kịp trước khi crash
                }
            }
            catch
            {
                // Bỏ qua lỗi trong quá trình crash
            }
        }
    }
}
