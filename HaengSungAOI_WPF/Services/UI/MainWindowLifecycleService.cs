using HaengSungAOI_WPF.Machine;
using HaengSungAOI_WPF.Machine.PLC;
using HaengSungAOI_WPF.Services.Machine;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Utils;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VMControls.WPF.Release;

namespace HaengSungAOI_WPF.Services.UI
{
    public class MainWindowLifecycleService
    {
        public async Task InitializeMainWindowAsync(
            HaengSungAOI_WPF.Machine.Machine machine,
            VmFrontendControl frontendControl,
            Action populateHmiLampMapping,
            Action updateMachineControlButtons,
            Action<string> setLoadingStatus,
            Func<Task> hideLoadingOverlayAsync)
        {
            try
            {
                setLoadingStatus("Đang tải giao diện HMI...");
                await Task.Delay(50);
                populateHmiLampMapping();

                setLoadingStatus("Đang kết nối PLC và Vision...");
                await Task.Delay(80);
                machine.frontendControl = frontendControl;
                machine.Initialize();

                setLoadingStatus("Đang cấu hình hệ thống...");
                await Task.Delay(50);
                updateMachineControlButtons();

                setLoadingStatus("Đang kích hoạt monitoring PLC...");
                machine.PLC?.SetActiveMonitoringGroups(PLCConstants.DEFAULT_MONITORING_GROUPS);
                Logger.Info("MainWindow", "Set PLC monitoring to DEFAULT_MONITORING_GROUPS");

                setLoadingStatus("Sẵn sàng!");
                await Task.Delay(400);
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error during MainWindow initialization", ex);
                setLoadingStatus($"Lỗi khởi tạo: {ex.Message}");
                await Task.Delay(2000);
            }
            finally
            {
                await hideLoadingOverlayAsync();
            }
        }

        public void HandleWindowActivated(HaengSungAOI_WPF.Machine.Machine machine)
        {
            try
            {
                machine?.PLC?.SetActiveMonitoringGroups(PLCConstants.DEFAULT_MONITORING_GROUPS);
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error switching monitoring groups on MainWindow activation", ex);
            }
        }

        public void HandleWindowKeyDown(Window window, KeyEventArgs e, HaengSungAOI_WPF.Machine.Machine machine, MachineErrorList errorList, Action updateMachineControlButtons)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullscreen(window);
                return;
            }

            if (e.Key == Key.F12)
            {
                try
                {
                    Logger.Warning("MainWindow", "Emergency stop hotkey pressed (F12)");
                    machine?.EmergencyStop();
                    machine?.StopMachine();
                    MessageBox.Show("EMERGENCY STOP ACTIVATED!", "Emergency", MessageBoxButton.OK, MessageBoxImage.Warning);
                    updateMachineControlButtons();
                }
                catch (Exception ex)
                {
                    Logger.Fatal("MainWindow", "Error during emergency stop", ex);
                    errorList?.AddException("Emergency", "Emergency stop failed", ex);
                    MessageBox.Show($"Error during emergency stop: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void CleanupOnClosing(
            DispatcherTimer errorStatusTimer,
            EventHandler updateErrorStatusDisplay,
            DispatcherTimer trayQuantityTimer,
            EventHandler updateTrayQuantities,
            DispatcherTimer hmiLampUpdateTimer,
            EventHandler updateHmiLamps,
            MachineErrorList errorList,
            EventHandler<MachineErrorEventArgs> onCriticalErrorAdded,
            EventHandler<MachineErrorEventArgs> onErrorAdded,
            HaengSungAOI_WPF.Machine.Machine machine,
            Action<bool> onMachineEnabledStateChanged,
            int? actualMachineId)
        {
            try
            {
                Logger.Info("MainWindow", "MainWindow closing - cleaning up resources");

                if (errorStatusTimer != null) errorStatusTimer.Tick -= updateErrorStatusDisplay;
                if (trayQuantityTimer != null) trayQuantityTimer.Tick -= updateTrayQuantities;
                if (hmiLampUpdateTimer != null) hmiLampUpdateTimer.Tick -= updateHmiLamps;

                errorStatusTimer?.Stop();
                trayQuantityTimer?.Stop();
                hmiLampUpdateTimer?.Stop();

                if (errorList != null)
                {
                    errorList.CriticalErrorAdded -= onCriticalErrorAdded;
                    errorList.ErrorAdded -= onErrorAdded;
                }

                if (machine != null)
                {
                    machine.OnMachineEnabledStateChanged -= onMachineEnabledStateChanged;
                    machine.Dispose();
                }

                TryUpdateOperatingEnd(actualMachineId);
                Logger.Info("MainWindow", "MainWindow cleanup completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error during MainWindow cleanup", ex);
            }
        }

        private static void TryUpdateOperatingEnd(int? actualMachineId)
        {
            try
            {
                if (!actualMachineId.HasValue) return;
                var dbService = new AutoVisionDbService();
                Task.Run(async () => await dbService.UpdateVisionOperatingEndAsync(actualMachineId.Value)).Wait();
                Logger.Info("MainWindow", "Updated Vision Operating End Time during cleanup");
            }
            catch (Exception dbEx)
            {
                Logger.Error("MainWindow", $"Error updating Vision Operating End Time during cleanup: {dbEx.Message}");
            }
        }

        private static void ToggleFullscreen(Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowStyle = WindowStyle.SingleBorderWindow;
                window.WindowState = WindowState.Normal;
                window.Topmost = false;
                window.ResizeMode = ResizeMode.CanResize;
                return;
            }

            window.WindowStyle = WindowStyle.None;
            window.WindowState = WindowState.Maximized;
            window.Topmost = true;
            window.ResizeMode = ResizeMode.NoResize;
        }
    }
}
