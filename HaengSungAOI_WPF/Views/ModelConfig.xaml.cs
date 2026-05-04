using System;
using System.Windows;
using System.Windows.Controls;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.ViewModels;
using VM.Core;
using VMControls.WPF.Release;
using HaengSungAOI_WPF.Services.Machine;

namespace HaengSungAOI_WPF.Views
{
    public partial class ModelConfig : Window
    {
        private readonly ModelConfigViewModel _viewModel;
        private readonly IMachineService _machineService;
        private bool _isLoadingModel;
        private bool _isModelDataChanged;

        public ModelConfig(ModelConfigViewModel viewModel, IMachineService machineService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _machineService = machineService;
            this.DataContext = _viewModel;

            // Initialize Vision control if needed
            InitializeVisionControl();
        }

        private void InitializeVisionControl()
        {
            // The VmMainViewConfigControl might need explicit initialization 
            // if it doesn't support full MVVM binding for its internal state
            try
            {
                // VmSolution.Instance is usually a singleton in VisionMaster SDK
                if (ProcedureConfigControl != null)
                {
                    // In VM 4.2, VmMainViewConfigControl typically uses ModuleSource
                    // ProcedureConfigControl.ModuleSource = VmSolution.Instance;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ModelConfig] Error initializing vision control: {ex.Message}");
            }
        }
        private void OnModelDataChanged(object sender, EventArgs e)
        {
            _isModelDataChanged = true;
        }
    }
}