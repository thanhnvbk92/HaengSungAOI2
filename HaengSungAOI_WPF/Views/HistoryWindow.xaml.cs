using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using HaengSungAOI_WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace HaengSungAOI_WPF.Views
{
    /// <summary>
    /// Interaction logic for HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow : Window
    {
        public HistoryWindow()
        {
            InitializeComponent();
            
            // Resolve ViewModel from DI container
            if (Application.Current is App app)
            {
                this.DataContext = app.ServiceProvider.GetRequiredService<HistoryViewModel>();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ViewFullSizeImage_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is HistoryViewModel vm && vm.SelectedImage != null)
            {
                try
                {
                    // Mở ảnh bằng ứng dụng mặc định của hệ thống
                    var result = vm.SelectedInspectionResult;
                    if (result != null && !string.IsNullOrEmpty(result.ImagePath) && File.Exists(result.ImagePath))
                    {
                        Process.Start(new ProcessStartInfo(result.ImagePath) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("Image file not found.", "Not Found",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Cannot open image: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is HistoryViewModel vm && vm.SelectedImage != null)
            {
                try
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                        FileName = $"Inspection_{DateTime.Now:yyyyMMdd_HHmmss}"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(vm.SelectedImage));
                        using (var stream = File.OpenWrite(saveDialog.FileName))
                        {
                            encoder.Save(stream);
                        }

                        MessageBox.Show("Image saved successfully.", "Saved",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving image: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenImageFolder_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is HistoryViewModel vm)
            {
                try
                {
                    var result = vm.SelectedInspectionResult;
                    if (result != null && !string.IsNullOrEmpty(result.ImagePath))
                    {
                        string folder = Path.GetDirectoryName(result.ImagePath);
                        if (Directory.Exists(folder))
                        {
                            Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
                            return;
                        }
                    }

                    // Fallback: mở thư mục ảnh mặc định
                    string defaultFolder = @"E:\History\Data\Images";
                    if (Directory.Exists(defaultFolder))
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe", defaultFolder) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("Image folder not found.", "Not Found",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}


