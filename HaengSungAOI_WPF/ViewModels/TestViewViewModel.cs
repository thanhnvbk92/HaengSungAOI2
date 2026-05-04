using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class TestViewViewModel : ObservableObject
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public TestViewViewModel()
        {
            Name = "Hello WPF";
            SayHelloCommand = new RelayCommand(SayHello);
        }

        public IRelayCommand SayHelloCommand { get; }

        private void SayHello()
        {
            Name = $"Xin chào {Name}";
        }
    }
}



