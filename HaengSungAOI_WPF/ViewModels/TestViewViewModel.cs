using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class TestViewViewModel: ObservableObject
    {
        [ObservableProperty]
        private string name;

        public TestViewViewModel()
        {
            Name = "Hello WPF";
        }

        [RelayCommand]
        private void SayHello()
        {
            Name = $"Xin chào {Name}";
        }
    }
}
