using HaengSungAOI_WPF.Machine.PLC.PLC;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HaengSungAOI_WPF.Services.UI
{
    public class MainWindowHmiService
    {
        private readonly Dictionary<string, Ellipse> _hmiLamps = new Dictionary<string, Ellipse>();
        private readonly Dictionary<string, bool> _lastLampStates = new Dictionary<string, bool>();

        private static readonly SolidColorBrush LampOnBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x00));
        private static readonly SolidColorBrush LampOffBrush = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));

        public bool AutoMode { get; private set; }

        static MainWindowHmiService()
        {
            LampOnBrush.Freeze();
            LampOffBrush.Freeze();
        }

        public void PopulateLampMapping(
            Button btnHMIAuto,
            Button btnHMIManual,
            Button btnHMIReset,
            Button btnHMIOrigin,
            Button btnHMIStart,
            Button btnHMIStop,
            Button btnHMIBuzzerOff,
            Button btnHMIEndCycle,
            Button btnHMICounterReset)
        {
            _hmiLamps.Clear();
            _hmiLamps["HMI_Auto_PB"] = FindLampInButton(btnHMIAuto);
            _hmiLamps["HMI_Manual_PB"] = FindLampInButton(btnHMIManual);
            _hmiLamps["HMI_Reset_PB"] = FindLampInButton(btnHMIReset);
            _hmiLamps["HMI_Origin"] = FindLampInButton(btnHMIOrigin);
            _hmiLamps["HMI_Start"] = FindLampInButton(btnHMIStart);
            _hmiLamps["HMI_Stop"] = FindLampInButton(btnHMIStop);
            _hmiLamps["HMI_Buzzer_Off"] = FindLampInButton(btnHMIBuzzerOff);
            _hmiLamps["HMI_End_Cycle"] = FindLampInButton(btnHMIEndCycle);
            _hmiLamps["HMI_Counter_Reset_PB"] = FindLampInButton(btnHMICounterReset);
        }

        public void UpdateLamps(PLCController plc)
        {
            if (plc == null || !plc.IsConnected || _hmiLamps.Count == 0) return;

            foreach (var kvp in _hmiLamps)
            {
                var lamp = kvp.Value;
                if (lamp == null) continue;

                string lampTag = kvp.Key.Replace("HMI_", "HMI_Lamp_");
                var dataPoint = plc.GetDataPoint(lampTag);
                if (dataPoint == null) continue;

                bool isOn = false;
                if (dataPoint.Value is ushort regValue) isOn = regValue != 0;
                else if (dataPoint.Value is bool boolValue) isOn = boolValue;

                if (!_lastLampStates.TryGetValue(lampTag, out bool lastState) || lastState != isOn)
                {
                    _lastLampStates[lampTag] = isOn;
                    lamp.Fill = isOn ? LampOnBrush : LampOffBrush;
                }
            }
        }

        public void SetAutoMode(bool isAutoMode)
        {
            AutoMode = isAutoMode;
        }

        public static void ApplyButtonVisibility(bool isAutoMode, Button btnHMIAuto, Button btnHMIManual, Button btnHMIReset,
            Button btnHMIOrigin, Button btnHMIStart, Button btnHMIStop, Button btnHMIBuzzerOff, Button btnHMIEndCycle, Button btnHMICounterReset)
        {
            if (isAutoMode)
            {
                if (btnHMIOrigin != null) btnHMIOrigin.Visibility = Visibility.Collapsed;
                if (btnHMIManual != null) btnHMIManual.Visibility = Visibility.Visible;
                return;
            }

            if (btnHMIAuto != null) btnHMIAuto.Visibility = Visibility.Visible;
            if (btnHMIManual != null) btnHMIManual.Visibility = Visibility.Visible;
            if (btnHMIReset != null) btnHMIReset.Visibility = Visibility.Visible;
            if (btnHMIOrigin != null) btnHMIOrigin.Visibility = Visibility.Visible;
            if (btnHMIStart != null) btnHMIStart.Visibility = Visibility.Visible;
            if (btnHMIStop != null) btnHMIStop.Visibility = Visibility.Visible;
            if (btnHMIBuzzerOff != null) btnHMIBuzzerOff.Visibility = Visibility.Visible;
            if (btnHMIEndCycle != null) btnHMIEndCycle.Visibility = Visibility.Visible;
            if (btnHMICounterReset != null) btnHMICounterReset.Visibility = Visibility.Visible;
        }

        private static Ellipse FindLampInButton(Button button)
        {
            if (button?.Content is StackPanel stackPanel)
            {
                foreach (var child in stackPanel.Children)
                {
                    if (child is Ellipse ellipse) return ellipse;
                }
            }
            return null;
        }
    }
}
