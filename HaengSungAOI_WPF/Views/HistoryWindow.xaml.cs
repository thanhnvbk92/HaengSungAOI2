using Apps.Data;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Models;
using ImageSourceModuleCs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HaengSungAOI_WPF
{
    /// <summary>
    /// Interaction logic for HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow : Window
    {
        private InspectionHistoryManager _historyManager;
        private ObservableCollection<InspectionResult> _inspectionResults;
        private ObservableCollection<DefectResult> _currentDefects;
        private InspectionResult _selectedInspectionResult;
        private InspectionStatistics _currentStatistics;
        private string _currentSelectedImagePath;
        private int _selectedImageIndex = 1; // Default to first image

        // Image controls array for easier management
        private Image[] _thumbnailImages;
        private Button[] _thumbnailButtons;

        public HistoryWindow()
        {
            InitializeComponent();
            InitializeImageControls();
            InitializeData();
            InitializeControls();
            LoadData();
        }

        private void InitializeImageControls()
        {
            // Initialize arrays for easier thumbnail management
            _thumbnailImages = new Image[] 
            { 
                Thumbnail1Image, Thumbnail2Image, Thumbnail3Image, 
                Thumbnail4Image, Thumbnail5Image, Thumbnail6Image 
            };
            
            _thumbnailButtons = new Button[] 
            { 
                Thumbnail1Button, Thumbnail2Button, Thumbnail3Button, 
                Thumbnail4Button, Thumbnail5Button, Thumbnail6Button 
            };
        }

        private void InitializeData()
        {
            try
            {
                _historyManager = new InspectionHistoryManager();
                _inspectionResults = new ObservableCollection<InspectionResult>();
                _currentDefects = new ObservableCollection<DefectResult>();

                InspectionResultsDataGrid.ItemsSource = _inspectionResults;
                DefectsDataGrid.ItemsSource = _currentDefects;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing history data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeControls()
        {
            // Set default date range to today's records only
            DateTime today = DateTime.Now.Date; // Get today's date without time
            FromDatePicker.SelectedDate = today;
            ToDatePicker.SelectedDate = today; // Both set to today for today's records

            // Load model names into filter
            LoadModelNames();

            // Sample data creation code removed - no longer automatically creating demo data
        }

        private void LoadModelNames()
        {
            try
            {
                var modelNames = _historyManager.GetDistinctModelNames();
                ModelFilterComboBox.Items.Clear();
                ModelFilterComboBox.Items.Add(new ComboBoxItem { Content = "All", IsSelected = true });
                
                foreach (var modelName in modelNames)
                {
                    ModelFilterComboBox.Items.Add(new ComboBoxItem { Content = modelName });
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error loading model names: {ex.Message}");
            }
        }

        private async void LoadData()
        {
            try
            {
                await LoadInspectionResults();
                UpdateStatistics();
                UpdateWindowTitle(); // Update title to reflect current date range
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Update window title to reflect current date range
        /// </summary>
        private void UpdateWindowTitle()
        {
            try
            {
                string titleBase = "Inspection History";
                
                if (FromDatePicker.SelectedDate.HasValue && ToDatePicker.SelectedDate.HasValue)
                {
                    DateTime fromDate = FromDatePicker.SelectedDate.Value;
                    DateTime toDate = ToDatePicker.SelectedDate.Value;
                    
                    if (fromDate.Date == toDate.Date && fromDate.Date == DateTime.Now.Date)
                    {
                        // Today's records
                        this.Title = $"{titleBase} - Today's Records ({DateTime.Now:yyyy-MM-dd})";
                    }
                    else if (fromDate.Date == toDate.Date)
                    {
                        // Single day
                        this.Title = $"{titleBase} - {fromDate:yyyy-MM-dd}";
                    }
                    else
                    {
                        // Date range
                        this.Title = $"{titleBase} - {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}";
                    }
                }
                else
                {
                    this.Title = titleBase;
                }
            }
            catch (Exception ex)
            {
                // Fallback to default title if there's any error
                this.Title = "Inspection History";
            }
        }

        private async Task LoadInspectionResults()
        {
            try
            {
                // Show loading indicator
                this.Cursor = System.Windows.Input.Cursors.Wait;

                await Task.Run(() =>
                {
                    // Get filter values
                    DateTime? fromDate = null;
                    DateTime? toDate = null;
                    string resultFilter = null;
                    string modelFilter = null;
                    int limit = 500;

                    Dispatcher.Invoke(() =>
                    {
                        fromDate = FromDatePicker.SelectedDate;
                        toDate = ToDatePicker.SelectedDate + TimeSpan.FromHours(24);

                        if (ResultFilterComboBox.SelectedItem is ComboBoxItem resultItem && 
                            resultItem.Content.ToString() != "All")
                        {
                            resultFilter = resultItem.Content.ToString();
                        }

                        if (ModelFilterComboBox.SelectedItem is ComboBoxItem modelItem && 
                            modelItem.Content.ToString() != "All")
                        {
                            modelFilter = modelItem.Content.ToString();
                        }

                        if (RecordLimitComboBox.SelectedItem is ComboBoxItem limitItem && 
                            limitItem.Content.ToString() != "All")
                        {
                            if (int.TryParse(limitItem.Content.ToString(), out int parsedLimit))
                            {
                                limit = parsedLimit;
                            }
                        }
                        else if (RecordLimitComboBox.SelectedItem is ComboBoxItem allItem && 
                                 allItem.Content.ToString() == "All")
                        {
                            limit = int.MaxValue;
                        }
                    });

                    // Apply PCB Code filter
                    string pcbCodeFilter = null;
                    Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(PCBCodeFilterTextBox.Text))
                        {
                            pcbCodeFilter = PCBCodeFilterTextBox.Text.Trim();
                        }
                    });

                    // Load data from database
                    var results = _historyManager.GetInspectionResults(fromDate, toDate, resultFilter, modelFilter, limit);

                    // Apply PCB Code filter (client-side for contains search)
                    if (!string.IsNullOrEmpty(pcbCodeFilter))
                    {
                        results = results.Where(r => r.PCBCode.Contains(pcbCodeFilter)).ToList();
                    }

                    // Update UI on main thread
                    Dispatcher.Invoke(() =>
                    {
                        _inspectionResults.Clear();
                        foreach (var result in results)
                        {
                            _inspectionResults.Add(result);
                        }
                    });
                });
            }
            finally
            {
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                DateTime? fromDate = FromDatePicker.SelectedDate;
                DateTime? toDate = ToDatePicker.SelectedDate;
                string modelFilter = null;

                if (ModelFilterComboBox.SelectedItem is ComboBoxItem modelItem && 
                    modelItem.Content.ToString() != "All")
                {
                    modelFilter = modelItem.Content.ToString();
                }

                _currentStatistics = _historyManager.GetStatistics(fromDate, toDate, modelFilter);

                // Update statistics display
                TotalCountTextBlock.Text = _currentStatistics.TotalInspections.ToString();
                PassCountTextBlock.Text = _currentStatistics.PassCount.ToString();
                FailCountTextBlock.Text = _currentStatistics.FailCount.ToString();
                PassRateTextBlock.Text = _currentStatistics.PassRateString;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error updating statistics: {ex.Message}");
            }
        }

        private void InspectionResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _selectedInspectionResult = InspectionResultsDataGrid.SelectedItem as InspectionResult;
                
                if (_selectedInspectionResult != null)
                {
                    // Load defects for selected inspection
                    LoadDefectsForInspection(_selectedInspectionResult);
                    
                    // Load inspection image thumbnails
                    LoadInspectionImageThumbnails(_selectedInspectionResult);
                    
                    // Load the first image by default
                    LoadSelectedImage(1);
                }
                else
                {
                    _currentDefects.Clear();
                    ClearAllImages();
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error handling selection change: {ex.Message}");
            }
        }

        private void LoadDefectsForInspection(InspectionResult inspectionResult)
        {
            try
            {
                _currentDefects.Clear();
                
                if (inspectionResult.Defects != null)
                {
                    foreach (var defect in inspectionResult.Defects)
                    {
                        _currentDefects.Add(defect);
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error loading defects: {ex.Message}");
            }
        }

        private void LoadInspectionImageThumbnails(InspectionResult inspectionResult)
        {
            try
            {
                // Clear all thumbnails first
                for (int i = 0; i < _thumbnailImages.Length; i++)
                {
                    _thumbnailImages[i].Source = null;
                    _thumbnailButtons[i].IsEnabled = false;
                }

                if (string.IsNullOrEmpty(inspectionResult.ImagePath))
                    return;

                // Get the folder path from the primary image path
                string folderPath = Path.GetDirectoryName(inspectionResult.ImagePath);
                
                if (!Directory.Exists(folderPath))
                    return;

                // Load each thumbnail (1_r.jpg to 6_r.jpg)
                for (int i = 1; i <= 6; i++)
                {
                    string imagePath = Path.Combine(folderPath, $"{i}_r.jpg");
                    
                    if (File.Exists(imagePath))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(imagePath);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.DecodePixelWidth = 100; // Thumbnail size
                            bitmap.EndInit();
                            bitmap.Freeze(); // Make it thread-safe
                            
                            _thumbnailImages[i - 1].Source = bitmap;
                            _thumbnailButtons[i - 1].IsEnabled = true;
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine($"Error loading thumbnail {i}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error loading image thumbnails: {ex.Message}");
            }
        }

        private void LoadSelectedImage(int imageIndex)
        {
            try
            {
                if (_selectedInspectionResult == null || string.IsNullOrEmpty(_selectedInspectionResult.ImagePath))
                    return;

                string folderPath = Path.GetDirectoryName(_selectedInspectionResult.ImagePath);
                string imagePath = Path.Combine(folderPath, $"{imageIndex}_r.jpg");

                if (File.Exists(imagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    SelectedImage.Source = bitmap;
                    
                    _currentSelectedImagePath = imagePath;
                    _selectedImageIndex = imageIndex;
                    SelectedImageTitle.Text = $"Selected Image - Inspect {imageIndex}";
                    //ImageSourceModuleTool imageSource = new ImageSourceModuleTool();
                    //imageSource.ModuParams.ImageSourceType = ImageSourceParam.ImageSourceTypeEnum.LocalImage;
                    //imageSource.ModuParams.PixelFormat = ImageSourceParam.PixelFormatEnum.RGB24;
                    //imageSource.EnableResultCallback();
                    //imageSource.AddInputImageByPath(imagePath);
                    //imageSource.Run();

                    //byte[] data = File.ReadAllBytes(imagePath);
                    //ImageData imageData = new ImageData(5472, 3648, data, "rgb24");
                    //renderControl.ImageSource = imageData;
                    //renderControl.InitializeComponent();
                    // Highlight selected thumbnail
                    HighlightSelectedThumbnail(imageIndex);
                }
                else
                {
                    SelectedImage.Source = null;
                    _currentSelectedImagePath = null;
                    SelectedImageTitle.Text = "Selected Image - Not Available";
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error loading selected image: {ex.Message}");
                SelectedImage.Source = null;
                _currentSelectedImagePath = null;
            }
        }

        private void HighlightSelectedThumbnail(int selectedIndex)
        {
            // Reset all thumbnail borders
            for (int i = 0; i < _thumbnailButtons.Length; i++)
            {
                var parent = _thumbnailButtons[i].Parent as Grid;
                if (parent?.Parent is Border border)
                {
                    border.BorderBrush = System.Windows.Media.Brushes.Gray;
                    border.BorderThickness = new Thickness(1);
                }
            }

            // Highlight selected thumbnail
            if (selectedIndex >= 1 && selectedIndex <= 6)
            {
                var selectedParent = _thumbnailButtons[selectedIndex - 1].Parent as Grid;
                if (selectedParent?.Parent is Border selectedBorder)
                {
                    selectedBorder.BorderBrush = System.Windows.Media.Brushes.Cyan;
                    selectedBorder.BorderThickness = new Thickness(2);
                }
            }
        }

        private void ClearAllImages()
        {
            // Clear thumbnails
            for (int i = 0; i < _thumbnailImages.Length; i++)
            {
                _thumbnailImages[i].Source = null;
                _thumbnailButtons[i].IsEnabled = false;
            }

            // Clear selected image
            SelectedImage.Source = null;
            _currentSelectedImagePath = null;
            SelectedImageTitle.Text = "Selected Image";
        }

        private void Thumbnail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string tagString)
                {
                    if (int.TryParse(tagString, out int imageIndex))
                    {
                        LoadSelectedImage(imageIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error handling thumbnail click: {ex.Message}");
            }
        }

        #region Event Handlers

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await LoadInspectionResults();
            UpdateStatistics();
            UpdateWindowTitle(); // Update title to reflect new date range
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            // Reset to today's records
            DateTime today = DateTime.Now.Date;
            FromDatePicker.SelectedDate = today;
            ToDatePicker.SelectedDate = today; // Both set to today for today's records
            
            ResultFilterComboBox.SelectedIndex = 0;
            ModelFilterComboBox.SelectedIndex = 0;
            PCBCodeFilterTextBox.Text = "";
            RecordLimitComboBox.SelectedIndex = 1; // 500 records
            
            UpdateWindowTitle(); // Update title to reflect cleared filters
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"InspectionHistory_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportToFile(saveFileDialog.FileName);
                    MessageBox.Show($"Data exported successfully to:\n{saveFileDialog.FileName}", "Export Complete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting data: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToFile(string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Write CSV header
                    writer.WriteLine("STT,PCB Code,Model,Date/Time,Result,Total Defects,Inspection Time,Operator");
                    
                    // Write data
                    foreach (var result in _inspectionResults)
                    {
                        writer.WriteLine($"{result.STT},{result.PCBCode},{result.ModelName}," +
                                       $"{result.InspectionDateTimeString},{result.Result}," +
                                       $"{result.TotalDefects},{result.InspectionTime:F2},{result.OperatorName}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export data: {ex.Message}", ex);
            }
        }

        private void DeleteOld_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "This will delete inspection records older than 90 days.\nThis action cannot be undone.\n\nContinue?",
                    "Delete Old Records", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    int deletedCount = _historyManager.DeleteOldRecords(90);
                    MessageBox.Show($"Deleted {deletedCount} old inspection records.", "Delete Complete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh the data
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting old records: {ex.Message}", "Delete Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewFullSizeImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentSelectedImagePath) && File.Exists(_currentSelectedImagePath))
                {
                    // Open image in default image viewer
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _currentSelectedImagePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("No image available or image file not found.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentSelectedImagePath) && File.Exists(_currentSelectedImagePath))
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Image files (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|All files (*.*)|*.*",
                        DefaultExt = Path.GetExtension(_currentSelectedImagePath),
                        FileName = $"{_selectedInspectionResult?.PCBCode}_inspect{_selectedImageIndex}"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        File.Copy(_currentSelectedImagePath, saveFileDialog.FileName, true);
                        MessageBox.Show("Image saved successfully.", "Save Complete", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("No image available or image file not found.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenImageFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentSelectedImagePath) && File.Exists(_currentSelectedImagePath))
                {
                    // Open folder and select the file
                    string argument = $"/select,\"{_currentSelectedImagePath}\"";
                    Process.Start("explorer.exe", argument);
                }
                else if (_selectedInspectionResult != null && !string.IsNullOrEmpty(_selectedInspectionResult.ImagePath))
                {
                    // Try to open the folder even if specific image doesn't exist
                    string folderPath = Path.GetDirectoryName(_selectedInspectionResult.ImagePath);
                    if (Directory.Exists(folderPath))
                    {
                        Process.Start("explorer.exe", folderPath);
                    }
                    else
                    {
                        MessageBox.Show("Image folder not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Open default images folder
                    string defaultImageFolder = _historyManager.ImageStoragePath;
                    
                    if (Directory.Exists(defaultImageFolder))
                    {
                        Process.Start("explorer.exe", defaultImageFolder);
                    }
                    else
                    {
                        MessageBox.Show("No image folder available.", "Info", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Generate inspection report
                GenerateInspectionReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateInspectionReport()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "HTML Report (*.html)|*.html|PDF Report (*.pdf)|*.pdf|All files (*.*)|*.*",
                    DefaultExt = "html",
                    FileName = $"InspectionReport_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenerateHTMLReport(saveFileDialog.FileName);
                    
                    var result = MessageBox.Show($"Report generated successfully:\n{saveFileDialog.FileName}\n\nOpen the report now?", 
                        "Report Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate report: {ex.Message}", ex);
            }
        }

        private void GenerateHTMLReport(string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("<!DOCTYPE html>");
                    writer.WriteLine("<html><head>");
                    writer.WriteLine("<title>AOI Inspection Report</title>");
                    writer.WriteLine("<style>");
                    writer.WriteLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                    writer.WriteLine("table { border-collapse: collapse; width: 100%; }");
                    writer.WriteLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                    writer.WriteLine("th { background-color: #f2f2f2; }");
                    writer.WriteLine(".pass { color: green; font-weight: bold; }");
                    writer.WriteLine(".fail { color: red, font-weight: bold; }");
                    writer.WriteLine("</style>");
                    writer.WriteLine("</head><body>");
                    
                    writer.WriteLine("<h1>AOI Inspection Report</h1>");
                    writer.WriteLine($"<p>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
                    
                    if (_currentStatistics != null)
                    {
                        writer.WriteLine("<h2>Summary Statistics</h2>");
                        writer.WriteLine("<table>");
                        writer.WriteLine("<tr><th>Metric</th><th>Value</th></tr>");
                        writer.WriteLine($"<tr><td>Total Inspections</td><td>{_currentStatistics.TotalInspections}</td></tr>");
                        writer.WriteLine($"<tr><td>Pass Count</td><td class='pass'>{_currentStatistics.PassCount}</td></tr>");
                        writer.WriteLine($"<tr><td>Fail Count</td><td class='fail'>{_currentStatistics.FailCount}</td></tr>");
                        writer.WriteLine($"<tr><td>Pass Rate</td><td>{_currentStatistics.PassRateString}</td></tr>");
                        writer.WriteLine($"<tr><td>Total Defects</td><td>{_currentStatistics.TotalDefects}</td></tr>");
                        writer.WriteLine($"<tr><td>Average Inspection Time</td><td>{_currentStatistics.AverageInspectionTimeString}</td></tr>");
                        writer.WriteLine("</table>");
                    }
                    
                    writer.WriteLine("<h2>Inspection Results</h2>");
                    writer.WriteLine("<table>");
                    writer.WriteLine("<tr><th>STT</th><th>PCB Code</th><th>Model</th><th>Date/Time</th><th>Result</th><th>Defects</th><th>Time</th><th>Operator</th></tr>");
                    
                    foreach (var result in _inspectionResults)
                    {
                        string resultClass = result.Result == "PASS" ? "pass" : "fail";
                        writer.WriteLine($"<tr><td>{result.STT}</td><td>{result.PCBCode}</td><td>{result.ModelName}</td>");
                        writer.WriteLine($"<td>{result.InspectionDateTimeString}</td><td class='{resultClass}'>{result.Result}</td>");
                        writer.WriteLine($"<td>{result.TotalDefects}</td><td>{result.InspectionTimeString}</td><td>{result.OperatorName}</td></tr>");
                    }
                    
                    writer.WriteLine("</table>");
                    writer.WriteLine("</body></html>");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate HTML report: {ex.Message}", ex);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadInspectionResults();
            UpdateStatistics();
            LoadModelNames(); // Refresh model names in case new models were added
            UpdateWindowTitle(); // Update title to reflect current date range
        }

        #endregion
    }
}