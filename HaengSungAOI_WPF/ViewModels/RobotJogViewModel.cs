using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services.Machine;
using HaengSungAOI_WPF.Core.PLC;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class RobotJogViewModel : ObservableObject, IDisposable
    {
        private readonly IMachineService _machineService;
        private readonly IPlcService _plcService;
        private readonly IMachineHmiService _hmiService;
        private readonly IServoMonitorService _servoMonitor;
        private readonly Dictionary<string, bool> _buttonPressStates = new Dictionary<string, bool>();

        private readonly Dictionary<string, string> _tagMap = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> _lampStates = new Dictionary<string, bool>();

        private string _title = "Robot Jog Controls";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private RobotType _robotType;
        public RobotType RobotType
        {
            get => _robotType;
            set => SetProperty(ref _robotType, value);
        }

        private Visibility _infeedAxesVisibility = Visibility.Collapsed;
        public Visibility InfeedAxesVisibility
        {
            get => _infeedAxesVisibility;
            set => SetProperty(ref _infeedAxesVisibility, value);
        }

        private Visibility _transferAxesVisibility = Visibility.Collapsed;
        public Visibility TransferAxesVisibility
        {
            get => _transferAxesVisibility;
            set => SetProperty(ref _transferAxesVisibility, value);
        }

        private Visibility _outfeedAxesVisibility = Visibility.Collapsed;
        public Visibility OutfeedAxesVisibility
        {
            get => _outfeedAxesVisibility;
            set => SetProperty(ref _outfeedAxesVisibility, value);
        }

        private Visibility _inspectAxesVisibility = Visibility.Collapsed;
        public Visibility InspectAxesVisibility
        {
            get => _inspectAxesVisibility;
            set => SetProperty(ref _inspectAxesVisibility, value);
        }

        // Speed Properties
        private double _speedAxis1;
        public double SpeedAxis1
        {
            get => _speedAxis1;
            set => SetProperty(ref _speedAxis1, value);
        }

        private double _speedAxis2;
        public double SpeedAxis2
        {
            get => _speedAxis2;
            set => SetProperty(ref _speedAxis2, value);
        }

        private double _speedAxis3;
        public double SpeedAxis3
        {
            get => _speedAxis3;
            set => SetProperty(ref _speedAxis3, value);
        }

        private double _speedAxis4;
        public double SpeedAxis4
        {
            get => _speedAxis4;
            set => SetProperty(ref _speedAxis4, value);
        }

        private string _labelAxis1 = "Axis 1:";
        public string LabelAxis1
        {
            get => _labelAxis1;
            set => SetProperty(ref _labelAxis1, value);
        }

        private string _labelAxis2 = "Axis 2:";
        public string LabelAxis2
        {
            get => _labelAxis2;
            set => SetProperty(ref _labelAxis2, value);
        }

        private string _labelAxis3 = "Axis 3:";
        public string LabelAxis3
        {
            get => _labelAxis3;
            set => SetProperty(ref _labelAxis3, value);
        }

        private string _labelAxis4 = "Axis 4:";
        public string LabelAxis4
        {
            get => _labelAxis4;
            set => SetProperty(ref _labelAxis4, value);
        }

        private Visibility _visibilityAxis3 = Visibility.Collapsed;
        public Visibility VisibilityAxis3
        {
            get => _visibilityAxis3;
            set => SetProperty(ref _visibilityAxis3, value);
        }

        private Visibility _visibilityAxis4 = Visibility.Collapsed;
        public Visibility VisibilityAxis4
        {
            get => _visibilityAxis4;
            set => SetProperty(ref _visibilityAxis4, value);
        }

        // Position Properties
        private double _posX;
        public double PosX
        {
            get => _posX;
            set => SetProperty(ref _posX, value);
        }

        private double _posY;
        public double PosY
        {
            get => _posY;
            set => SetProperty(ref _posY, value);
        }

        private double _posZ;
        public double PosZ
        {
            get => _posZ;
            set => SetProperty(ref _posZ, value);
        }

        private double _posR;
        public double PosR
        {
            get => _posR;
            set => SetProperty(ref _posR, value);
        }

        // Visibility for special Jog Pad buttons
        private Visibility _visibilityJogY = Visibility.Visible;
        public Visibility VisibilityJogY
        {
            get => _visibilityJogY;
            set => SetProperty(ref _visibilityJogY, value);
        }

        private Visibility _visibilityJogZ = Visibility.Visible;
        public Visibility VisibilityJogZ
        {
            get => _visibilityJogZ;
            set => SetProperty(ref _visibilityJogZ, value);
        }

        private Visibility _visibilityJogR = Visibility.Visible;
        public Visibility VisibilityJogR
        {
            get => _visibilityJogR;
            set => SetProperty(ref _visibilityJogR, value);
        }

        private Visibility _visibilityJogZ61 = Visibility.Collapsed;
        public Visibility VisibilityJogZ61
        {
            get => _visibilityJogZ61;
            set => SetProperty(ref _visibilityJogZ61, value);
        }

        private Visibility _visibilityJogZ62 = Visibility.Collapsed;
        public Visibility VisibilityJogZ62
        {
            get => _visibilityJogZ62;
            set => SetProperty(ref _visibilityJogZ62, value);
        }

        private Visibility _visibilityCylinderTray = Visibility.Collapsed;
        public Visibility VisibilityCylinderTray
        {
            get => _visibilityCylinderTray;
            set => SetProperty(ref _visibilityCylinderTray, value);
        }

        private Visibility _visibilityVacuumTray = Visibility.Collapsed;
        public Visibility VisibilityVacuumTray
        {
            get => _visibilityVacuumTray;
            set => SetProperty(ref _visibilityVacuumTray, value);
        }

        private Visibility _visibilityCylinder = Visibility.Visible;
        public Visibility VisibilityCylinder
        {
            get => _visibilityCylinder;
            set => SetProperty(ref _visibilityCylinder, value);
        }

        private Visibility _visibilityJogX = Visibility.Visible;
        public Visibility VisibilityJogX
        {
            get => _visibilityJogX;
            set => SetProperty(ref _visibilityJogX, value);
        }


        public IAsyncRelayCommand<string> ButtonDownCommand { get; }
        public IAsyncRelayCommand<string> ButtonUpCommand { get; }
        public IRelayCommand<string> WriteSpeedCommand { get; }
        public IRelayCommand<string> ReadSpeedCommand { get; }
        public IRelayCommand ReadAllSpeedsCommand { get; }
        public IRelayCommand WriteAllSpeedsCommand { get; }
        public IRelayCommand EmergencyStopCommand { get; }

        public RobotJogViewModel(IMachineService machineService, IServoMonitorService servoMonitor, RobotType robotType)
        {
            _machineService = machineService;
            _plcService = machineService.PLC;
            _hmiService = machineService.HMI;
            _servoMonitor = servoMonitor;
            RobotType = robotType;

            ButtonDownCommand = new AsyncRelayCommand<string>(ButtonDown);
            ButtonUpCommand = new AsyncRelayCommand<string>(ButtonUp);
            WriteSpeedCommand = new RelayCommand<string>(WriteSpeed);
            ReadSpeedCommand = new RelayCommand<string>(ReadSpeed);
            ReadAllSpeedsCommand = new RelayCommand(ReadAllSpeeds);
            WriteAllSpeedsCommand = new RelayCommand(WriteAllSpeeds);
            EmergencyStopCommand = new RelayCommand(EmergencyStop);

            InitializeLayout();

            // Subscribe to position monitoring via IServoMonitorService
            if (_servoMonitor != null)
            {
                _servoMonitor.StartMonitoring();
                _servoMonitor.StatusChanged += ServoMonitor_StatusChanged;
            }

            // Subscribe to lamp changes from HMI service
            if (_hmiService != null)
            {
                _hmiService.LampStateChanged += OnLampStateChanged;
            }

            ReadAllSpeeds();
        }

        private void OnLampStateChanged(object sender, HmiLampStateChangedEventArgs e)
        {
            string buttonTag = e.LampName.Replace("_LP", "_PB");
            _lampStates[buttonTag] = e.IsOn;
            
            foreach (var entry in _tagMap)
            {
                if (entry.Value == buttonTag)
                {
                    _lampStates[entry.Key] = e.IsOn;
                }
            }
            
            OnPropertyChanged(nameof(LampStates));
        }

        public Dictionary<string, bool> LampStates => _lampStates;

        private void InitializeLayout()
        {
            Title = $"{RobotType} Jog Controls";

            InfeedAxesVisibility = RobotType == RobotType.Infeed ? Visibility.Visible : Visibility.Collapsed;
            TransferAxesVisibility = RobotType == RobotType.Transfer ? Visibility.Visible : Visibility.Collapsed;
            OutfeedAxesVisibility = RobotType == RobotType.Outfeed ? Visibility.Visible : Visibility.Collapsed;
            InspectAxesVisibility = RobotType == RobotType.Inspect1 || RobotType == RobotType.Inspect2 ? Visibility.Visible : Visibility.Collapsed;

            _tagMap.Clear();

            switch (RobotType)
            {
                case RobotType.Infeed:
                    LabelAxis1 = "X1 Speed:";
                    LabelAxis2 = "Y1 Speed:";
                    LabelAxis3 = "C1 Speed:";
                    VisibilityAxis3 = Visibility.Visible;
                    VisibilityJogZ = Visibility.Collapsed;
                    VisibilityCylinderTray = Visibility.Collapsed;
                    VisibilityVacuumTray = Visibility.Collapsed;

                    _tagMap["JogXPlus"] = "HMI_AX1_JogPlus_PB";
                    _tagMap["JogXMinus"] = "HMI_AX1_JogMinus_PB";
                    _tagMap["JogYPlus"] = "HMI_AY1_JogPlus_PB";
                    _tagMap["JogYMinus"] = "HMI_AY1_JogMinus_PB";
                    _tagMap["JogRPlus"] = "HMI_AC1_JogPlus_PB";
                    _tagMap["JogRMinus"] = "HMI_AC1_JogMinus_PB";
                    _tagMap["CylinderUp"] = "HMI_Cyl_Infeed_Up_PB";
                    _tagMap["CylinderDown"] = "HMI_Cyl_Infeed_Down_PB";
                    _tagMap["VacuumON"] = "HMI_Vacuum_Infeed_ON_PB";
                    _tagMap["VacuumOFF"] = "HMI_Vacuum_Infeed_OFF_PB";
                    break;

                case RobotType.Transfer:
                    LabelAxis1 = "X2 Speed:";
                    LabelAxis2 = "Z2 Speed:";
                    VisibilityJogY = Visibility.Collapsed;
                    VisibilityJogR = Visibility.Collapsed;
                    VisibilityCylinderTray = Visibility.Collapsed;
                    VisibilityVacuumTray = Visibility.Collapsed;

                    _tagMap["JogXPlus"] = "HMI_AX2_JogPlus_PB";
                    _tagMap["JogXMinus"] = "HMI_AX2_JogMinus_PB";
                    _tagMap["JogZPlus"] = "HMI_AZ2_JogPlus_PB";
                    _tagMap["JogZMinus"] = "HMI_AZ2_JogMinus_PB";
                    _tagMap["CylinderUp"] = "HMI_Cyl_NG_Up_PB";
                    _tagMap["CylinderDown"] = "HMI_Cyl_NG_Down_PB";
                    _tagMap["VacuumON"] = "HMI_Vacuum_Transfer_ON_PB";
                    _tagMap["VacuumOFF"] = "HMI_Vacuum_Transfer_OFF_PB";
                    break;

                case RobotType.Outfeed:
                    LabelAxis1 = "X3 Speed:";
                    LabelAxis2 = "Y3 Speed:";
                    LabelAxis3 = "Z61 Speed:";
                    LabelAxis4 = "Z62 Speed:";
                    VisibilityAxis3 = Visibility.Visible;
                    VisibilityAxis4 = Visibility.Visible;
                    VisibilityJogZ = Visibility.Collapsed;
                    VisibilityJogR = Visibility.Collapsed;
                    VisibilityJogZ61 = Visibility.Visible;
                    VisibilityJogZ62 = Visibility.Visible;
                    VisibilityCylinderTray = Visibility.Visible;
                    VisibilityVacuumTray = Visibility.Visible;

                    _tagMap["JogXPlus"] = "HMI_AX3_JogPlus_PB";
                    _tagMap["JogXMinus"] = "HMI_AX3_JogMinus_PB";
                    _tagMap["JogYPlus"] = "HMI_AY3_JogPlus_PB";
                    _tagMap["JogYMinus"] = "HMI_AY3_JogMinus_PB";
                    _tagMap["JogZ61Plus"] = "HMI_AZ61_JogPlus_PB";
                    _tagMap["JogZ61Minus"] = "HMI_AZ61_JogMinus_PB";
                    _tagMap["JogZ62Plus"] = "HMI_AZ62_JogPlus_PB";
                    _tagMap["JogZ62Minus"] = "HMI_AZ62_JogMinus_PB";
                    _tagMap["CylinderUp"] = "HMI_Cyl_Outfeed_Up_PB";
                    _tagMap["CylinderDown"] = "HMI_Cyl_Outfeed_Down_PB";
                    _tagMap["VacuumON"] = "HMI_Vacuum_Outfeed_ON_PB";
                    _tagMap["VacuumOFF"] = "HMI_Vacuum_Outfeed_OFF_PB";
                    _tagMap["CylinderTrayUp"] = "HMI_Cyl_Pickup_Tray_Up_PB";
                    _tagMap["CylinderTrayDown"] = "HMI_Cyl_Pickup_Tray_Down_PB";
                    _tagMap["VacuumTrayON"] = "HMI_Vacuum_Pickup_Tray_ON_PB";
                    _tagMap["VacuumTrayOFF"] = "HMI_Vacuum_Pickup_Tray_OFF_PB";
                    break;

                case RobotType.Inspect1:
                    LabelAxis1 = "Z4 Speed:";
                    LabelAxis2 = "C4 Speed:";
                    VisibilityJogX = Visibility.Collapsed;
                    VisibilityJogY = Visibility.Collapsed;
                    VisibilityCylinder = Visibility.Collapsed;

                    _tagMap["JogZPlus"] = "HMI_AZ4_JogPlus_PB";
                    _tagMap["JogZMinus"] = "HMI_AZ4_JogMinus_PB";
                    _tagMap["JogRPlus"] = "HMI_AC4_JogPlus_PB";
                    _tagMap["JogRMinus"] = "HMI_AC4_JogMinus_PB";
                    _tagMap["VacuumON"] = "HMI_Vacuum_Inspect1_ON_PB";
                    _tagMap["VacuumOFF"] = "HMI_Vacuum_Inspect1_OFF_PB";
                    break;

                case RobotType.Inspect2:
                    LabelAxis1 = "Z5 Speed:";
                    LabelAxis2 = "C5 Speed:";
                    VisibilityJogX = Visibility.Collapsed;
                    VisibilityJogY = Visibility.Collapsed;
                    VisibilityCylinder = Visibility.Collapsed;

                    _tagMap["JogZPlus"] = "HMI_AZ5_JogPlus_PB";
                    _tagMap["JogZMinus"] = "HMI_AZ5_JogMinus_PB";
                    _tagMap["JogRPlus"] = "HMI_AC5_JogPlus_PB";
                    _tagMap["JogRMinus"] = "HMI_AC5_JogMinus_PB";
                    _tagMap["VacuumON"] = "HMI_Vacuum_Inspect2_ON_PB";
                    _tagMap["VacuumOFF"] = "HMI_Vacuum_Inspect2_OFF_PB";
                    break;
            }
        }

        private void ServoMonitor_StatusChanged(object sender, HaengSungAOI_WPF.Core.PLC.ServoStatusChangedEventArgs e)
        {
            // Update UI on UI Thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateCurrentPositions();
            });
        }

        private void UpdateCurrentPositions()
        {
            if (_servoMonitor == null) return;

            try
            {
                switch (RobotType)
                {
                    case RobotType.Infeed:
                        PosX = _servoMonitor.GetCurrentPosition(ServoAxis.X1);
                        PosY = _servoMonitor.GetCurrentPosition(ServoAxis.Y1);
                        PosR = _servoMonitor.GetCurrentPosition(ServoAxis.C1);
                        break;
                    case RobotType.Transfer:
                        PosX = _servoMonitor.GetCurrentPosition(ServoAxis.X2);
                        PosZ = _servoMonitor.GetCurrentPosition(ServoAxis.Z2);
                        break;
                    case RobotType.Outfeed:
                        PosX = _servoMonitor.GetCurrentPosition(ServoAxis.X3);
                        PosY = _servoMonitor.GetCurrentPosition(ServoAxis.Y3);
                        PosZ = _servoMonitor.GetCurrentPosition(ServoAxis.Z61);
                        PosR = _servoMonitor.GetCurrentPosition(ServoAxis.Z62);
                        break;
                    case RobotType.Inspect1:
                        PosZ = _servoMonitor.GetCurrentPosition(ServoAxis.Z4);
                        PosR = _servoMonitor.GetCurrentPosition(ServoAxis.C4);
                        break;
                    case RobotType.Inspect2:
                        PosZ = _servoMonitor.GetCurrentPosition(ServoAxis.Z5);
                        PosR = _servoMonitor.GetCurrentPosition(ServoAxis.C5);
                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        private string ResolveTag(string inputTag)
        {
            if (_tagMap.TryGetValue(inputTag, out string resolvedTag))
            {
                return resolvedTag;
            }
            return inputTag;
        }

        private async Task ButtonDown(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            try
            {
                string resolvedTag = ResolveTag(tag);
                _buttonPressStates[resolvedTag] = true;
                await _hmiService.HandleButtonPressAsync(resolvedTag, true);
            }
            catch (Exception)
            {
            }
        }

        private async Task ButtonUp(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            try
            {
                string resolvedTag = ResolveTag(tag);
                _buttonPressStates[resolvedTag] = false;
                await _hmiService.HandleButtonPressAsync(resolvedTag, false);
            }
            catch (Exception)
            {
            }
        }

        private void WriteSpeed(string axisKey)
        {
            double value = 0;
            string tagName = "";

            switch (axisKey)
            {
                case "Axis1": value = SpeedAxis1; tagName = GetJogSpeedTag(1); break;
                case "Axis2": value = SpeedAxis2; tagName = GetJogSpeedTag(2); break;
                case "Axis3": value = SpeedAxis3; tagName = GetJogSpeedTag(3); break;
                case "Axis4": value = SpeedAxis4; tagName = GetJogSpeedTag(4); break;
            }

            if (!string.IsNullOrEmpty(tagName))
            {
                _plcService.WriteRegister(tagName, (ushort)value);
            }
        }

        private void ReadSpeed(string axisKey)
        {
            string tagName = "";
            switch (axisKey)
            {
                case "Axis1": tagName = GetJogSpeedTag(1); break;
                case "Axis2": tagName = GetJogSpeedTag(2); break;
                case "Axis3": tagName = GetJogSpeedTag(3); break;
                case "Axis4": tagName = GetJogSpeedTag(4); break;
            }

            if (!string.IsNullOrEmpty(tagName))
            {
                double value = _plcService.GetDoubleValue(tagName);
                switch (axisKey)
                {
                    case "Axis1": SpeedAxis1 = value; break;
                    case "Axis2": SpeedAxis2 = value; break;
                    case "Axis3": SpeedAxis3 = value; break;
                    case "Axis4": SpeedAxis4 = value; break;
                }
            }
        }

        private void ReadAllSpeeds()
        {
            ReadSpeed("Axis1");
            ReadSpeed("Axis2");
            if (VisibilityAxis3 == Visibility.Visible) ReadSpeed("Axis3");
            if (VisibilityAxis4 == Visibility.Visible) ReadSpeed("Axis4");
        }

        private void WriteAllSpeeds()
        {
            WriteSpeed("Axis1");
            WriteSpeed("Axis2");
            if (VisibilityAxis3 == Visibility.Visible) WriteSpeed("Axis3");
            if (VisibilityAxis4 == Visibility.Visible) WriteSpeed("Axis4");
        }

        private void EmergencyStop()
        {
            _plcService.WriteRegister("HMI_EmergencyStop_PB", 1);
            ReleaseAllButtons();
        }

        private string GetJogSpeedTag(int axisIndex)
        {
            switch (RobotType)
            {
                case RobotType.Infeed:
                    if (axisIndex == 1) return "X1_JogSpeed";
                    if (axisIndex == 2) return "Y1_JogSpeed";
                    if (axisIndex == 3) return "C1_JogSpeed";
                    break;
                case RobotType.Transfer:
                    if (axisIndex == 1) return "X2_JogSpeed";
                    if (axisIndex == 2) return "Z2_JogSpeed";
                    break;
                case RobotType.Outfeed:
                    if (axisIndex == 1) return "X3_JogSpeed";
                    if (axisIndex == 2) return "Y3_JogSpeed";
                    if (axisIndex == 3) return "Z61_JogSpeed";
                    if (axisIndex == 4) return "Z62_JogSpeed";
                    break;
                case RobotType.Inspect1:
                case RobotType.Inspect2:
                    if (axisIndex == 1) return "Z5_JogSpeed";
                    if (axisIndex == 2) return "C5_JogSpeed";
                    break;
            }
            return "";
        }

        public void ReleaseAllButtons()
        {
            foreach (var tag in _buttonPressStates.Keys.ToList())
            {
                if (_buttonPressStates[tag])
                {
                    _plcService.WriteRegister(tag, 0);
                    _buttonPressStates[tag] = false;
                }
            }
        }

        public void Dispose()
        {
            if (_hmiService != null)
            {
                _hmiService.LampStateChanged -= OnLampStateChanged;
            }

            if (_servoMonitor != null)
            {
                _servoMonitor.StatusChanged -= ServoMonitor_StatusChanged;
            }
            
            ReleaseAllButtons();
        }
    }
}



