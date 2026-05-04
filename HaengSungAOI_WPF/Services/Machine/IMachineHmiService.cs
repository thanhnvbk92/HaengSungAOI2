using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.Services.Machine
{
    public class HmiLampStateChangedEventArgs : EventArgs
    {
        public string LampName { get; }
        public bool IsOn { get; }

        public HmiLampStateChangedEventArgs(string lampName, bool isOn)
        {
            LampName = lampName;
            IsOn = isOn;
        }
    }

    public class HmiQuantityChangedEventArgs : EventArgs
    {
        public string TagName { get; }
        public int Value { get; }

        public HmiQuantityChangedEventArgs(string tagName, int value)
        {
            TagName = tagName;
            Value = value;
        }
    }

    public interface IMachineHmiService
    {
        event EventHandler<HmiLampStateChangedEventArgs> LampStateChanged;
        event EventHandler<HmiQuantityChangedEventArgs> QuantityChanged;
        Task HandleButtonPressAsync(string tag, bool isPressed);
        bool GetLampState(string lampName);
        int GetQuantity(string tagName);
    }
}
