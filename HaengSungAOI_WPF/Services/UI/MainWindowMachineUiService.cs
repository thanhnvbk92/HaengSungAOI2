using HaengSungAOI_WPF.Machine;
using HaengSungAOI_WPF.Machine.PLC;
using HaengSungAOI_WPF.Machine.PLC.PLC;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Utils;
using HaengSungAOI_WPF.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace HaengSungAOI_WPF.Services.UI
{
    public class MainWindowMachineUiService
    {
        private static readonly SolidColorBrush BrushErrorCritical = new SolidColorBrush(Colors.Red);
        private static readonly SolidColorBrush BrushErrorWarning = new SolidColorBrush(Colors.Orange);
        private static readonly SolidColorBrush BrushTrayGood = new SolidColorBrush(Color.FromRgb(0x87, 0xCE, 0xEB));
        private static readonly SolidColorBrush BrushNa = new SolidColorBrush(Colors.Gray);

        public void UpdateTrayQuantities(MainWindowViewModel vm, Machine.Machine machine)
        {
            if (vm == null) return;

            if (machine == null)
            {
                vm.PcbSlotText = "N/A";
                vm.PcbTrayQuantityText = "N/A";
                vm.BlankTrayQuantityText = "N/A";
                vm.PcbSlotForeground = BrushNa;
                vm.PcbTrayQuantityForeground = BrushNa;
                vm.BlankTrayQuantityForeground = BrushNa;
                return;
            }

            var pcbSlot = machine.PCB_Quantity;
            var pcbTray = machine.PCBTrayQuantity;
            var blankTray = machine.BlankTrayQuantity;

            vm.PcbSlotText = $"{pcbSlot}/48";
            vm.PcbTrayQuantityText = pcbTray.ToString();
            vm.BlankTrayQuantityText = blankTray.ToString();

            vm.PcbSlotForeground = GetSlotColorBrush(pcbSlot);
            vm.PcbTrayQuantityForeground = GetTrayColorBrush(pcbTray, 2, 4);
            vm.BlankTrayQuantityForeground = GetTrayColorBrush(blankTray, 2, 4);
        }

        public async Task WritePcbTrayQuantityToPlcAsync(Window owner, Machine.Machine machine, MachineErrorList errorList, ushort value)
        {
            try
            {
                var plc = machine?.PLC;
                if (plc == null || !plc.IsConnected)
                {
                    MessageBox.Show(owner, "PLC chưa kết nối. Không thể cập nhật tray quantity.", "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ushort address;
                if (!PLCAddresses.TrayQuantity_Registers.TryGetValue("PCB_Slot", out address))
                {
                    MessageBox.Show(owner, "Lỗi cấu hình địa chỉ PLC. Không thể cập nhật tray quantity.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await Task.Run(() => plc.WriteHoldingRegistersDirect(address, new ushort[] { value }));
                machine.PCBTrayQuantity = value;
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", $"Error writing PCB Tray Quantity to PLC: {ex.Message}", ex);
                errorList?.AddException("PLC", "Failed to write PCB Tray Quantity to PLC", ex);
                MessageBox.Show(owner, $"Lỗi ghi PLC: {ex.Message}", "PLC Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task HandleHmiButtonDownAsync(
            string buttonTag,
            PLCController plc,
            MachineErrorList errorList,
            Func<bool> hasUnacknowledgedErrors,
            Func<string, string, bool> showCriticalConfirmation,
            Action setAutoModeOn,
            Action setAutoModeOff,
            Func<Task> initSession,
            Func<Task> updateEnd)
        {
            if (string.IsNullOrWhiteSpace(buttonTag) || plc == null || !plc.IsConnected) return;

            try
            {
                if (buttonTag == "HMI_Counter_Reset_PB")
                {
                    bool confirmed = showCriticalConfirmation("CẢNH BÁO",
                        "BẠN CÓ CHẮC CHẮN MUỐN RESET KHÔNG?\n\nLưu ý: Chỉ thực hiện khi đã LẤY HẾT TRAY OUT ra khỏi máy!");
                    if (!confirmed) return;
                }

                if ((buttonTag == "HMI_Start" || buttonTag == "Start") && hasUnacknowledgedErrors())
                {
                    MessageBox.Show("Vẫn còn mã lỗi chưa được xác nhận trên hệ thống.\nVui lòng mở 'Danh sách lỗi' và clear lỗi trước khi Start.",
                        "Cảnh báo Lỗi Máy", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await plc.WriteHoldingRegisterAsync(buttonTag, 1);

                if (buttonTag == "HMI_Auto_PB")
                {
                    await Task.Delay(100);
                    await plc.WriteHoldingRegisterAsync(buttonTag, 0);
                    setAutoModeOn();
                }
                else if (buttonTag == "HMI_Manual_PB")
                {
                    await Task.Delay(100);
                    await plc.WriteHoldingRegisterAsync(buttonTag, 0);
                    setAutoModeOff();
                }
                else if (buttonTag == "HMI_Counter_Reset_PB")
                {
                    await Task.Delay(100);
                    await plc.WriteHoldingRegisterAsync(buttonTag, 0);
                }

                if (buttonTag == "HMI_Start")
                {
                    if (initSession != null) await initSession();
                }
                else if (buttonTag == "HMI_Stop")
                {
                    if (updateEnd != null) await updateEnd();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", $"Error handling HMI button '{buttonTag}' mouse down", ex);
                errorList?.AddException("HMI", $"Failed to handle HMI button '{buttonTag}' mouse down", ex);
            }
        }

        public async Task HandleHmiButtonUpAsync(string buttonTag, PLCController plc, MachineErrorList errorList)
        {
            if (string.IsNullOrWhiteSpace(buttonTag)) return;

            try
            {
                if (plc == null || !plc.IsConnected) return;
                await plc.WriteHoldingRegisterAsync(buttonTag, 0);
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", $"Error handling HMI button '{buttonTag}' mouse up", ex);
                errorList?.AddException("HMI", $"Failed to handle HMI button '{buttonTag}' mouse up", ex);
            }
        }

        private static Brush GetSlotColorBrush(int value)
        {
            if (value >= 48) return BrushErrorCritical;
            if (value >= 40) return BrushErrorWarning;
            return BrushTrayGood;
        }

        private static Brush GetTrayColorBrush(int value, int redThreshold, int warnThreshold)
        {
            if (value <= redThreshold) return BrushErrorCritical;
            if (value <= warnThreshold) return BrushErrorWarning;
            return BrushTrayGood;
        }
    }
}
