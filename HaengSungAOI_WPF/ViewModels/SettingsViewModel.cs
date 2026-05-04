using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HaengSungAOI_WPF.Machine.PLC;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services.Machine;
using HaengSungAOI_WPF.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IMachineService _machineService;
        private readonly IPlcService _plcService;

        private const int DELAY_BETWEEN_READS_MS = 5;
        private const int DELAY_BETWEEN_PARAMS_MS = 20;

        private ObservableCollection<ServoParameterRowData> _parameterRows;
        public ObservableCollection<ServoParameterRowData> ParameterRows
        {
            get => _parameterRows;
            set => SetProperty(ref _parameterRows, value);
        }

        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _connectionStatusText = "PLC: Disconnected";
        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => SetProperty(ref _connectionStatusText, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ICommand ReadAllCommand { get; }
        public ICommand WriteAllCommand { get; }
        public ICommand RefreshCommand { get; }

        public SettingsViewModel(IMachineService machineService)
        {
            _machineService = machineService;
            _plcService = machineService?.PLC;
            _parameterRows = new ObservableCollection<ServoParameterRowData>();

            ReadAllCommand = new AsyncRelayCommand(ReadAll);
            WriteAllCommand = new AsyncRelayCommand(WriteAll);
            RefreshCommand = new RelayCommand(Refresh);

            InitializeParameters();
            UpdateConnectionStatus();

            if (_plcService != null)
            {
                _plcService.ConnectionStatusChanged += (s, e) => UpdateConnectionStatus();
            }
        }

        private void InitializeParameters()
        {
            _parameterRows.Clear();
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Current Position", DataType = "LREAL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Current Speed", DataType = "LREAL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Error Code", DataType = "LREAL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Operation Status", DataType = "LREAL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "ORG Found", DataType = "BOOL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Move Completed", DataType = "BOOL", IsReadOnly = true });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Acceleration", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Deceleration", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "ORG Speed (Fast)", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Jog Speed", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Inching Distance", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Inching Speed", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Target Position", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Target Speed", DataType = "LREAL", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Target Point", DataType = "INT", IsReadOnly = false });
            _parameterRows.Add(new ServoParameterRowData { ParameterName = "Current Point", DataType = "INT", IsReadOnly = true });

            StatusText = $"Loaded {_parameterRows.Count} parameters";
        }

        private void UpdateConnectionStatus()
        {
            if (_plcService != null && _plcService.IsConnected)
            {
                ConnectionStatusText = "PLC: Connected";
            }
            else
            {
                ConnectionStatusText = "PLC: Disconnected";
            }
        }

        private async Task ReadAll()
        {
            if (_plcService == null || !_plcService.IsConnected)
            {
                MessageBox.Show("PLC is not connected.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsBusy = true;
            StatusText = "Reading all parameters...";

            int readCount = 0;
            int errorCount = 0;

            try
            {
                await Task.Run(() =>
                {
                    foreach (var row in ParameterRows)
                    {
                        foreach (ServoAxis axis in Enum.GetValues(typeof(ServoAxis)))
                        {
                            try
                            {
                                string axisName = ServoAddressCalculator.GetAxisDisplayName(axis);
                                string paramName = row.ParameterName.Replace(" ", "").Replace("(", "").Replace(")", "");
                                string tagName = $"{axisName}_{paramName}";

                                object value = null;
                                if (row.DataType == "LREAL")
                                {
                                    value = _plcService.GetDoubleValue(tagName);
                                }
                                else if (row.DataType == "INT" || row.DataType == "BOOL")
                                {
                                    value = _plcService.GetUInt16Value(tagName);
                                }

                                if (value != null)
                                {
                                    string formattedValue = FormatValue(value, row.DataType);
                                    row.SetAxisValue(axisName, formattedValue);
                                    readCount++;
                                }

                                Thread.Sleep(DELAY_BETWEEN_READS_MS);
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                Logger.Error("SettingsVM", $"Error reading {row.ParameterName} for axis {axis}: {ex.Message}");
                            }
                        }
                        Thread.Sleep(DELAY_BETWEEN_PARAMS_MS);
                    }
                });

                StatusText = $"Read {readCount} values successfully ({errorCount} errors)";
                MessageBox.Show($"Successfully read {readCount} values from PLC.", "Read Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error("SettingsVM", $"Bulk read failed: {ex.Message}", ex);
                StatusText = "Read failed";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task WriteAll()
        {
            if (_plcService == null || !_plcService.IsConnected)
            {
                MessageBox.Show("PLC is not connected.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to write ALL parameters to the PLC?", 
                "Confirm Bulk Write", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            StatusText = "Writing all parameters...";

            int writeCount = 0;
            int errorCount = 0;

            try
            {
                await Task.Run(() =>
                {
                    foreach (var row in ParameterRows)
                    {
                        if (row.IsReadOnly) continue;

                        foreach (ServoAxis axis in Enum.GetValues(typeof(ServoAxis)))
                        {
                            try
                            {
                                string axisName = ServoAddressCalculator.GetAxisDisplayName(axis);
                                string paramName = row.ParameterName.Replace(" ", "").Replace("(", "").Replace(")", "");
                                string tagName = $"{axisName}_{paramName}";
                                string stringValue = row.GetAxisValue(axisName);

                                if (string.IsNullOrEmpty(stringValue)) continue;

                                bool success = false;
                                if (row.DataType == "LREAL")
                                {
                                    if (double.TryParse(stringValue, out double val))
                                    {
                                        _plcService.WriteDouble(tagName, val);
                                        success = true;
                                    }
                                }
                                else if (row.DataType == "INT" || row.DataType == "BOOL")
                                {
                                    if (ushort.TryParse(stringValue, out ushort val))
                                    {
                                        _plcService.WriteRegister(tagName, val);
                                        success = true;
                                    }
                                }

                                if (success) writeCount++;
                                Thread.Sleep(DELAY_BETWEEN_READS_MS);
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                Logger.Error("SettingsVM", $"Error writing {row.ParameterName} for axis {axis}: {ex.Message}");
                            }
                        }
                        Thread.Sleep(DELAY_BETWEEN_PARAMS_MS);
                    }
                });

                StatusText = $"Wrote {writeCount} values successfully ({errorCount} errors)";
                MessageBox.Show($"Successfully wrote {writeCount} values to PLC.", "Write Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error("SettingsVM", $"Bulk write failed: {ex.Message}", ex);
                StatusText = "Write failed";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Refresh()
        {
            UpdateConnectionStatus();
            StatusText = "Status refreshed";
        }

        private string FormatValue(object value, string dataType)
        {
            if (value == null) return "0";

            if (dataType == "LREAL")
            {
                if (value is double d) return d.ToString("F3");
                if (value is float f) return f.ToString("F3");
            }

            return value.ToString();
        }
    }
}
