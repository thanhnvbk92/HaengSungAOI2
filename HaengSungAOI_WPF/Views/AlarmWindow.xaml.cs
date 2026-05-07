using System;
using System.Windows;
using HaengSungAOI_WPF.ViewModels;

namespace HaengSungAOI_WPF.Views
{
    public partial class AlarmWindow : Window
    {
        public AlarmWindow(AlarmViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += (s, e) => 
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => Close());
                }
                else
                {
                    Close();
                }
            };
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
