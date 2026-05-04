using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services.Database;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly InspectionHistoryManager _historyManager;

        private ObservableCollection<InspectionResult> _inspectionResults = new();
        public ObservableCollection<InspectionResult> InspectionResults
        {
            get => _inspectionResults;
            set => SetProperty(ref _inspectionResults, value);
        }

        private ObservableCollection<DefectResult> _currentDefects = new();
        public ObservableCollection<DefectResult> CurrentDefects
        {
            get => _currentDefects;
            set => SetProperty(ref _currentDefects, value);
        }

        private InspectionResult _selectedInspectionResult;
        public InspectionResult SelectedInspectionResult
        {
            get => _selectedInspectionResult;
            set
            {
                if (SetProperty(ref _selectedInspectionResult, value))
                {
                    OnSelectedInspectionResultChanged(value);
                }
            }
        }

        private InspectionStatistics _statistics = new();
        public InspectionStatistics Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        private DateTime _fromDate = DateTime.Now.Date;
        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        private DateTime _toDate = DateTime.Now.Date;
        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        private string _modelFilter = "All";
        public string ModelFilter
        {
            get => _modelFilter;
            set => SetProperty(ref _modelFilter, value);
        }

        private ObservableCollection<string> _availableModels = new() { "All" };
        public ObservableCollection<string> AvailableModels
        {
            get => _availableModels;
            set => SetProperty(ref _availableModels, value);
        }

        private string _resultFilter = "All";
        public string ResultFilter
        {
            get => _resultFilter;
            set => SetProperty(ref _resultFilter, value);
        }

        private string _pcbCodeFilter = "";
        public string PcbCodeFilter
        {
            get => _pcbCodeFilter;
            set => SetProperty(ref _pcbCodeFilter, value);
        }

        private int _recordLimitIndex = 1; // Default to 500
        public int RecordLimitIndex
        {
            get => _recordLimitIndex;
            set => SetProperty(ref _recordLimitIndex, value);
        }

        private readonly int[] _recordLimits = { 100, 500, 1000, 5000, 10000, int.MaxValue };

        private BitmapImage _selectedImage;
        public BitmapImage SelectedImage
        {
            get => _selectedImage;
            set => SetProperty(ref _selectedImage, value);
        }

        private string _selectedImageTitle = "Selected Image";
        public string SelectedImageTitle
        {
            get => _selectedImageTitle;
            set => SetProperty(ref _selectedImageTitle, value);
        }

        private ObservableCollection<ThumbnailItem> _thumbnails = new();
        public ObservableCollection<ThumbnailItem> Thumbnails
        {
            get => _thumbnails;
            set => SetProperty(ref _thumbnails, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public HistoryViewModel(InspectionHistoryManager historyManager)
        {
            _historyManager = historyManager;
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadModelNamesAsync();
            await SearchAsync();
        }

        private async Task LoadModelNamesAsync()
        {
            var models = await _historyManager.GetDistinctModelNamesAsync();
            AvailableModels.Clear();
            AvailableModels.Add("All");
            foreach (var model in models)
            {
                AvailableModels.Add(model);
            }
        }

        public IRelayCommand SearchCommand => new AsyncRelayCommand(SearchAsync);
        public IRelayCommand ClearCommand => new RelayCommand(Clear);
        public IRelayCommand ExportCommand => new AsyncRelayCommand(ExportAsync);
        public IRelayCommand DeleteOldCommand => new AsyncRelayCommand(DeleteOldAsync);
        public IRelayCommand SelectThumbnailCommand => new RelayCommand<ThumbnailItem>(SelectThumbnail);

        private async Task SearchAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                int limit = _recordLimits[RecordLimitIndex];
                var results = await _historyManager.GetInspectionResultsAsync(FromDate, ToDate, ResultFilter, ModelFilter, limit);
                
                InspectionResults.Clear();
                foreach (var result in results)
                {
                    InspectionResults.Add(result);
                }

                Statistics = await _historyManager.GetStatisticsAsync(FromDate, ToDate, ModelFilter);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Clear()
        {
            FromDate = DateTime.Now.Date;
            ToDate = DateTime.Now.Date;
            ResultFilter = "All";
            ModelFilter = "All";
            PcbCodeFilter = "";
            RecordLimitIndex = 1;
        }

        private void OnSelectedInspectionResultChanged(InspectionResult value)
        {
            if (value != null)
            {
                LoadDetailsAsync(value);
            }
            else
            {
                CurrentDefects.Clear();
                SelectedImage = null;
                SelectedImageTitle = "Selected Image";
                Thumbnails.Clear();
            }
        }

        private async void LoadDetailsAsync(InspectionResult result)
        {
            // Load defects
            var defects = await _historyManager.GetDefectsForInspectionAsync(result.STT);
            CurrentDefects.Clear();
            foreach (var defect in defects)
            {
                CurrentDefects.Add(defect);
            }

            // Load thumbnails
            LoadThumbnails(result);
        }

        private void LoadThumbnails(InspectionResult result)
        {
            Thumbnails.Clear();
            string imgPath = result.ImagePath; // Base image path
            if (string.IsNullOrEmpty(imgPath)) return;

            string dir = Path.GetDirectoryName(imgPath);
            if (!Directory.Exists(dir)) return;

            // Simple logic to find related images (e.g. by prefix)
            var files = Directory.GetFiles(dir, "*.jpg");
            foreach (var file in files)
            {
                Thumbnails.Add(new ThumbnailItem
                {
                    ImagePath = file,
                    Title = Path.GetFileNameWithoutExtension(file),
                    IsSelected = file == imgPath
                });
            }

            if (Thumbnails.Any(t => t.IsSelected))
            {
                SelectThumbnail(Thumbnails.First(t => t.IsSelected));
            }
            else if (Thumbnails.Any())
            {
                SelectThumbnail(Thumbnails.First());
            }
        }

        private void SelectThumbnail(ThumbnailItem item)
        {
            if (item == null) return;

            foreach (var t in Thumbnails)
            {
                t.IsSelected = (t == item);
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(item.ImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                SelectedImage = bitmap;
                SelectedImageTitle = item.Title;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading image: {ex.Message}");
            }
        }

        private async Task ExportAsync()
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"InspectionHistory_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (sfd.ShowDialog() == true)
            {
                // Export logic here
                await Task.Run(() => { /* TODO: Implement actual export */ });
                MessageBox.Show("Export complete (Simulation)");
            }
        }

        private async Task DeleteOldAsync()
        {
            if (MessageBox.Show("Are you sure you want to delete records older than 30 days?", "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                int count = await _historyManager.DeleteOldRecordsAsync(30);
                MessageBox.Show($"Deleted {count} records.");
                await SearchAsync();
            }
        }
    }

    public class ThumbnailItem : ObservableObject
    {
        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}



