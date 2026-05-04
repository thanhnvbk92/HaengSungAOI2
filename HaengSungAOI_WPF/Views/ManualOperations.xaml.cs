using System;
using System.Windows;
using HaengSungAOI_WPF.ViewModels;
using HaengSungAOI_WPF.Utils;

namespace HaengSungAOI_WPF.Views
{
    /// <summary>
    /// Interaction logic for ManualOperations.xaml
    /// Manual operations window - Refactored to MVVM
    /// </summary>
    public partial class ManualOperations : Window
    {
        private readonly ManualOperationsViewModel _viewModel;

        public ManualOperations(ManualOperationsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
            
            Closed += (s, e) => _viewModel.Cleanup();
        }

        private void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement refresh status via ViewModel command when available
            System.Diagnostics.Debug.WriteLine("[ManualOperations] RefreshStatus clicked");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}