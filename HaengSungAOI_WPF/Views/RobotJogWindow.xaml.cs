using System;
using System.Windows;
using HaengSungAOI_WPF.Core;
using HaengSungAOI_WPF.Services.Machine;
using HaengSungAOI_WPF.ViewModels;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Utils;

namespace HaengSungAOI_WPF.Views
{
    /// <summary>
    /// Interaction logic for RobotJogWindow.xaml
    /// Fully migrated to MVVM pattern. Logic is handled by RobotJogViewModel.
    /// </summary>
    public partial class RobotJogWindow : Window
    {
        public RobotJogWindow(IMachineService machineService, RobotType robotType)
        {
            InitializeComponent();
            
            // Initialize ViewModel and set as DataContext
            var servoMonitor = ((App)Application.Current).ServiceProvider.GetService(typeof(IServoMonitorService)) as IServoMonitorService;
            var viewModel = new RobotJogViewModel(machineService, servoMonitor, robotType);
            DataContext = viewModel;
            
            // Cleanup resources when window closes
            this.Closing += (s, e) => 
            {
                if (DataContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
            
            Logger.Info("RobotJogWindow", $"RobotJogWindow initialized with MVVM for {robotType}");
        }
    }
}


