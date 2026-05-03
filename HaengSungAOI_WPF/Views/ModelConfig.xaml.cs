using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Machine;
using VM.Core;
using VMControls.WPF.Release;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;

namespace HaengSungAOI_WPF
{
    /// <summary>
    /// Interaction logic for ModelConfig.xaml
    /// Main window class - UI initialization and model management
    /// See also:
    /// - ModelConfig.RobotPositions.cs - Robot position management
    /// - ModelConfig.PLCOperations.cs - PLC read/write operations
    /// </summary>
    public partial class ModelConfig : Window
    {
        private PCBModel _currentModel;
        private ModelDatabaseManager _modelDatabase;
        private MainWindow _mainWindow;
        private List<PCBModel> _allModels;
        private VisionSolutionManager _visionManager;
        private bool _isModelDataChanged = false;
        private bool _isLoadingModel = false;

        // Servo monitoring - REMOVED (no longer using AxisPositionReader)
        private DispatcherTimer _uiUpdateTimer;
        private bool _isRobotConfigTabActive = false;

        public ModelConfig()
        {
            InitializeComponent();
            InitializeRobotPositionCollections();
            InitializeDatabase();
            InitializeVisionManager();
            LoadDefaultModel();
        }

        public ModelConfig(PCBModel model, MainWindow mainWindow) : this()
        {
            // Don't clone the model - we want to edit the original
            _currentModel = model ?? new PCBModel();
            _mainWindow = mainWindow;
            _modelDatabase = mainWindow?.GetModelDatabase();
            LoadAllModels();
            LoadModelToUI();

            // Subscribe to model list selection changes
            ModelListBox.SelectionChanged += ModelListBox_SelectionChanged;

            // Subscribe to UI change events to track modifications
            SetupChangeTracking();
        }

        private void SetupChangeTracking()
        {
            // Track changes in text boxes
            ModelNameTextBox.TextChanged += OnModelDataChanged;
            DescriptionTextBox.TextChanged += OnModelDataChanged;
            VisionSolutionNameTextBox.TextChanged += OnModelDataChanged;
            VisionSolutionPathTextBox.TextChanged += OnModelDataChanged;
        }

        private void OnModelDataChanged(object sender, TextChangedEventArgs e)
        {
            // Only track changes if we're not currently loading a model
            if (!_isLoadingModel)
            {
                _isModelDataChanged = true;
            }
        }

        private void ModelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is PCBModel selectedModel)
            {
                // Only ask to save if there are actual changes
                if (_isModelDataChanged && _currentModel != null && _currentModel.Id > 0)
                {
                    var result = MessageBox.Show("Do you want to save changes to the current model before switching?",
                             "Save Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel)
                    {
                        // Cancel the selection change by preventing the event
                        _isLoadingModel = true;

                        // Find and restore previous selection
                        foreach (ListBoxItem item in ModelListBox.Items)
                        {
                            if (item.Tag is PCBModel model && model.Id == _currentModel.Id)
                            {
                                ModelListBox.SelectedItem = item;
                                break;
                            }
                        }

                        _isLoadingModel = false;
                        return;
                    }
                    else if (result == MessageBoxResult.Yes)
                    {
                        if (!SaveUIToModel())
                        {
                            // If save failed, don't change selection
                            _isLoadingModel = true;
                            foreach (ListBoxItem item in ModelListBox.Items)
                            {
                                if (item.Tag is PCBModel model && model.Id == _currentModel.Id)
                                {
                                    ModelListBox.SelectedItem = item;
                                    break;
                                }
                            }
                            _isLoadingModel = false;
                            return;
                        }
                        _modelDatabase?.SaveModel(_currentModel);
                    }
                }

                // Load the selected model (don't clone it - we want to edit the original)
                _currentModel = selectedModel;
                LoadModelToUI();

                // Reset change tracking after loading new model
                _isModelDataChanged = false;
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                _modelDatabase = new ModelDatabaseManager();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing database: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeVisionManager()
        {
            try
            {
                _visionManager = new VisionSolutionManager();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing vision manager: {ex.Message}", "Vision Manager Error",
             MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDefaultModel()
        {
            _currentModel = new PCBModel();
            LoadAllModels();
            LoadModelToUI();
            _isModelDataChanged = false; // Reset change tracking
        }

        private void LoadAllModels()
        {
            try
            {
                _allModels = _modelDatabase?.GetAllModels() ?? new List<PCBModel>();

                // Temporarily disable selection change handling
                _isLoadingModel = true;

                // Update the model list box
                ModelListBox.Items.Clear();
                foreach (var model in _allModels)
                {
                    var listItem = new ListBoxItem
                    {
                        Content = model.Name,
                        Tag = model,
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x23, 0x23, 0x36)),
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
                    };

                    if (model.IsActive)
                    {
                        listItem.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3c, 0x3c, 0x54));
                        listItem.FontWeight = FontWeights.Bold;
                    }

                    ModelListBox.Items.Add(listItem);
                }

                // Select the current model if it exists in the list
                if (_currentModel != null && _currentModel.Id > 0)
                {
                    foreach (ListBoxItem item in ModelListBox.Items)
                    {
                        if (item.Tag is PCBModel model && model.Id == _currentModel.Id)
                        {
                            ModelListBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                _isLoadingModel = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading models: {ex.Message}", "Database Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                _isLoadingModel = false;
            }
        }

        private void LoadModelToUI()
        {
            if (_currentModel == null) return;

            try
            {
                _isLoadingModel = true; // Prevent change tracking during load

                // Load model metadata
                ModelNameTextBox.Text = _currentModel.Name ?? "";
                DescriptionTextBox.Text = _currentModel.Description ?? "";

                // Load vision solution information
                VisionSolutionNameTextBox.Text = _currentModel.VisionSolutionName ?? "";
                VisionSolutionPathTextBox.Text = _currentModel.VisionSolutionPath ?? "";

                // Load Robot positions using manager
                LoadInfeedRobotPositions();
                LoadTransferRobotPositions();
                LoadOutfeedRobotPositions();
                LoadInspect1RobotPositions();
                LoadInspect2RobotPositions();

                _isLoadingModel = false;
                _isModelDataChanged = false; // Reset change tracking after successful load
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading model to UI: {ex.Message}", "UI Error",
             MessageBoxButton.OK, MessageBoxImage.Error);
                _isLoadingModel = false;
            }
        }

        private bool SaveUIToModel()
        {
            Debug.WriteLine("[ModelConfig] SaveUIToModel started");
            try
            {
                if (_currentModel == null)
                {
                    Debug.WriteLine("[ModelConfig] Creating new PCBModel");
                    _currentModel = new PCBModel();
                }

                // Save model metadata
                _currentModel.Name = ModelNameTextBox.Text?.Trim() ?? "Unnamed Model";
                _currentModel.Description = DescriptionTextBox.Text?.Trim() ?? "";

                Debug.WriteLine($"[ModelConfig] Model Name: {_currentModel.Name}");
                Debug.WriteLine($"[ModelConfig] Model Description: {_currentModel.Description}");

                // Save vision solution information
                _currentModel.VisionSolutionName = VisionSolutionNameTextBox.Text?.Trim() ?? "";
                _currentModel.VisionSolutionPath = VisionSolutionPathTextBox.Text?.Trim() ?? "";

                Debug.WriteLine($"[ModelConfig] VisionSolutionName: {_currentModel.VisionSolutionName}");
                Debug.WriteLine($"[ModelConfig] VisionSolutionPath: {_currentModel.VisionSolutionPath}");

                // Validate model name
                if (string.IsNullOrWhiteSpace(_currentModel.Name))
                {
                    Debug.WriteLine("[ModelConfig] Validation failed: Model name is empty");
                    MessageBox.Show("Model name cannot be empty.", "Validation Error",
                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                Debug.WriteLine("[ModelConfig] Saving robot positions from DataGrid collections...");

                // Save robot positions using manager
                SaveInfeedRobotPositions();
                Debug.WriteLine("[ModelConfig] Infeed positions saved");

                SaveTransferRobotPositions();
                Debug.WriteLine("[ModelConfig] Transfer positions saved");

                SaveOutfeedRobotPositions();
                Debug.WriteLine("[ModelConfig] Outfeed positions saved");

                SaveInspect1RobotPositions();
                Debug.WriteLine("[ModelConfig] Inspect1 positions saved");

                SaveInspect2RobotPositions();
                Debug.WriteLine("[ModelConfig] Inspect2 positions saved");

                _isModelDataChanged = false; // Reset change tracking after successful save
                Debug.WriteLine("[ModelConfig] SaveUIToModel completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error in SaveUIToModel: {ex.Message}");
                Debug.WriteLine($"[ModelConfig] Stack: {ex.StackTrace}");
                MessageBox.Show($"Error parsing values: {ex.Message}", "Validation Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        #region Event Handlers

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddModel_Click(object sender, RoutedEventArgs e)
        {
            var newModel = new PCBModel
            {
                Name = "New Model",
                Description = "New model configuration",
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                IsActive = false
            };

            _modelDatabase?.SaveModel(newModel);
            LoadAllModels();

            // Select the newly created model
            foreach (ListBoxItem item in ModelListBox.Items)
            {
                if (item.Tag is PCBModel model && model.Id == newModel.Id)
                {
                    ModelListBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void DuplicateModel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null || _currentModel.Id == 0)
            {
                MessageBox.Show("Please select a model to duplicate.", "No Model Selected",
                   MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var duplicatedModel = new PCBModel();
            // Copy all properties from current model
            foreach (var prop in typeof(PCBModel).GetProperties())
            {
                if (prop.CanWrite && prop.Name != "Id")
                {
                    prop.SetValue(duplicatedModel, prop.GetValue(_currentModel));
                }
            }
            foreach (var field in typeof(PCBModel).GetFields())
            {
                field.SetValue(duplicatedModel, field.GetValue(_currentModel));
            }

            duplicatedModel.Id = 0;
            duplicatedModel.Name = _currentModel.Name + " (Copy)";
            duplicatedModel.IsActive = false;
            duplicatedModel.CreatedDate = DateTime.Now;
            duplicatedModel.ModifiedDate = DateTime.Now;

            _modelDatabase?.SaveModel(duplicatedModel);
            LoadAllModels();
        }

        private void DeleteModel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null || _currentModel.Id == 0)
            {
                MessageBox.Show("Please select a model to delete.", "No Model Selected",
                     MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentModel.IsActive)
            {
                MessageBox.Show("Cannot delete the active model. Please set another model as active first.",
                              "Cannot Delete Active Model", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete the model '{_currentModel.Name}'?",
                       "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _modelDatabase?.DeleteModel(_currentModel.Id);
                _currentModel = null;
                LoadAllModels();
                LoadDefaultModel();
            }
        }

        private async void SetActiveModel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null || _currentModel.Id == 0)
            {
                MessageBox.Show("Please select a model to set as active.", "No Model Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1. Activate model trong database
            _modelDatabase?.SetActiveModel(_currentModel.Id);
            _currentModel.IsActive = true;
            LoadAllModels();

            // 2. Tự động ghi positions xuống PLC với spinner overlay
            await WritePositionsToPLCWithSpinnerAsync();
        }

        /// <summary>
        /// Hiển thị spinner overlay (loading popup) trong khi thực hiện công việc.
        /// Trả về action để đóng overlay khi xong.
        /// </summary>
        private (Window overlay, Action close) ShowSpinnerOverlay(string message)
        {
            var overlay = new Window
            {
                Title                 = "",
                Width                 = 340,
                Height                = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner                 = this,
                ResizeMode            = ResizeMode.NoResize,
                WindowStyle           = WindowStyle.None,
                AllowsTransparency    = true,
                Background            = Brushes.Transparent,
                Topmost               = true,
                ShowInTaskbar         = false
            };

            // Nền bo góc
            var border = new Border
            {
                Background    = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x3c)),
                CornerRadius  = new CornerRadius(14),
                Effect        = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color       = Colors.Black,
                    BlurRadius  = 30,
                    Opacity     = 0.6,
                    ShadowDepth = 0
                }
            };

            var panel = new StackPanel
            {
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(30)
            };

            // Spinner (vòng xoay)
            var spinnerGrid = new Grid
            {
                Width               = 50,
                Height              = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 0, 0, 18)
            };

            // Track
            spinnerGrid.Children.Add(new Ellipse
            {
                Width           = 50,
                Height          = 50,
                Stroke          = new SolidColorBrush(Color.FromRgb(0x3c, 0x3c, 0x54)),
                StrokeThickness = 5
            });

            // Arc xoay
            var arc = new Ellipse
            {
                Width           = 50,
                Height          = 50,
                StrokeThickness = 5,
                StrokeDashArray = new DoubleCollection { 38, 114 },
                StrokeEndLineCap   = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round,
                Stroke          = new SolidColorBrush(Color.FromRgb(0x44, 0x88, 0xCC)),
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
            };

            var rotateTransform = new RotateTransform(0);
            arc.RenderTransform = rotateTransform;

            // Animation quay
            var spin = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(0.9)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, spin);

            spinnerGrid.Children.Add(arc);

            panel.Children.Add(spinnerGrid);
            panel.Children.Add(new TextBlock
            {
                Text                = message,
                Foreground          = new SolidColorBrush(Color.FromRgb(0xA0, 0xB0, 0xCC)),
                FontSize            = 13,
                TextAlignment       = TextAlignment.Center,
                TextWrapping        = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            border.Child  = panel;
            overlay.Content = border;

            overlay.Show();

            Action close = () =>
            {
                try { overlay.Close(); } catch { }
            };

            return (overlay, close);
        }

        /// <summary>
        /// Ghi position xuống PLC (async) với spinner overlay che trong lúc xử lý.
        /// Không hiển hộp confirm vì được gọi tự động sau khi set active model.
        /// </summary>
        private async Task WritePositionsToPLCWithSpinnerAsync()
        {
            var plc = GetPLCController();
            if (plc == null || !plc.IsConnected)
            {
                MessageBox.Show("PLC chưa kết nối. Không thể ghi positions xuống PLC.",
                    "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (_, closeOverlay) = ShowSpinnerOverlay(
                $"Activating model '{_currentModel?.Name}'\u2026\nĐang ghi positions xuống PLC");

            Exception error = null;
            try
            {
                // Capture plc ref để dùng trong Task.Run
                var plcRef = plc;
                var modelRef = _currentModel;

                await Task.Run(() =>
                {
                    // Ghi tất cả positions xuống PLC (blocking I/O trên background thread)
                    WriteInfeedPositionsToPLC(plcRef);
                    WriteTransferPositionsToPLC(plcRef);
                    WriteOutfeedPositionsToPLC(plcRef);
                    WriteInspect1PositionsToPLC(plcRef);
                    WriteInspect2PositionsToPLC(plcRef);

                    Debug.WriteLine($"[ModelConfig] WritePositionsToPLC done for model '{modelRef?.Name}'");
                });
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                closeOverlay();
            }

            if (error != null)
            {
                MessageBox.Show($"Lỗi khi ghi positions xuống PLC:\n{error.Message}",
                    "Lỗi ghi PLC", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show($"Model '{_currentModel?.Name}' đã được kích hoạt và ghi positions xuống PLC thành công!",
                    "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BrowseVisionSolution_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Vision Solutions (*.SOL)|*.SOL|All Files (*.*)|*.*",
                Title = "Select Vision Solution"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                VisionSolutionPathTextBox.Text = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
                VisionSolutionNameTextBox.Text = System.IO.Path.GetFileName(openFileDialog.FileName);
            }
        }

        private void CopyVisionSolution_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Copy Vision Solution functionality not yet implemented.",
          "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenVisionEditor_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open Vision Editor functionality not yet implemented.",
            "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Dialog Buttons
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[ModelConfig] Apply_Click started");
            Debug.WriteLine($"[ModelConfig] Current model Id={_currentModel?.Id}, Name={_currentModel?.Name}");

            if (SaveUIToModel())
            {
                Debug.WriteLine("[ModelConfig] SaveUIToModel succeeded, calling SaveModel...");
                try
                {
                    _modelDatabase?.SaveModel(_currentModel);
                    _isModelDataChanged = false;
                    Debug.WriteLine("[ModelConfig] Apply completed successfully");

                    // Reload the model on the main process (MainWindow's Machine)
                    ReloadModelOnMainProcess();

                    MessageBox.Show("Changes applied successfully.", "Applied",
                 MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ModelConfig] Error in Apply_Click: {ex.Message}");
                    Debug.WriteLine($"[ModelConfig] Stack: {ex.StackTrace}");
                    MessageBox.Show($"Error saving model: {ex.Message}", "Save Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Debug.WriteLine("[ModelConfig] SaveUIToModel returned false");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            VmSolution.Save();
            Debug.WriteLine("[ModelConfig] Save_Click started");
            Debug.WriteLine($"[ModelConfig] Current model Id={_currentModel?.Id}, Name={_currentModel?.Name}");

            if (SaveUIToModel())
            {
                Debug.WriteLine("[ModelConfig] SaveUIToModel succeeded, calling SaveModel...");
                try
                {
                    _modelDatabase?.SaveModel(_currentModel);
                    _isModelDataChanged = false;
                    Debug.WriteLine("[ModelConfig] Save completed successfully");

                    // Reload the model on the main process (MainWindow's Machine)
                    ReloadModelOnMainProcess();

                    Debug.WriteLine("[ModelConfig] Model reloaded on main process, closing window");
                    this.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ModelConfig] Error in Save_Click: {ex.Message}");
                    Debug.WriteLine($"[ModelConfig] Stack: {ex.StackTrace}");
                    MessageBox.Show($"Error saving model: {ex.Message}", "Save Error",
                         MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Debug.WriteLine("[ModelConfig] SaveUIToModel returned false");
            }
        }

        /// <summary>
        /// Reload the current model on the main process (MainWindow's Machine)
        /// This will update the vision solution and position parameters
        /// </summary>
        private void ReloadModelOnMainProcess()
        {
            try
            {
                if (_mainWindow == null)
                {
                    Debug.WriteLine("[ModelConfig] MainWindow reference is null, cannot reload model");
                    return;
                }

                var machine = _mainWindow.GetMachine();
                if (machine == null)
                {
                    Debug.WriteLine("[ModelConfig] Machine reference is null, cannot reload model");
                    return;
                }

                Debug.WriteLine($"[ModelConfig] Reloading model '{_currentModel?.Name}' on main process...");

                // Update the machine with the saved model
                // This will reload the vision solution and update all position parameters
                machine.UpdateModel(_currentModel);

                // If this is the active model, also update MainWindow's current model reference
                if (_currentModel?.IsActive == true)
                {
                    _mainWindow.SetCurrentModel(_currentModel);
                    Debug.WriteLine("[ModelConfig] Active model updated on MainWindow");
                }

                Debug.WriteLine("[ModelConfig] Model reloaded successfully on main process");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelConfig] Error reloading model on main process: {ex.Message}");
                Debug.WriteLine($"[ModelConfig] Stack: {ex.StackTrace}");
                // Don't throw - just log the error, the model is already saved to database
                MessageBox.Show($"Model saved, but failed to reload on main process: {ex.Message}\n\nPlease restart the application to apply changes.",
                    "Reload Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isModelDataChanged)
            {
                var result = MessageBox.Show("You have unsaved changes. Are you sure you want to cancel?",
            "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;
            }
            this.Close();
        }

        #endregion

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if Robot Config tab is selected
            if (MainTabControl.SelectedItem is TabItem selectedTab)
            {
                // Check if this is the Robot Config tab by checking the header or name
                bool isRobotConfigTab = selectedTab.Header?.ToString() == "Robot Configuration" ||
                     selectedTab.Name == "RobotConfigTab";

                _isRobotConfigTabActive = isRobotConfigTab;

                // Start or stop position monitoring based on tab
                OnRobotConfigTabActivated(isRobotConfigTab);

                Debug.WriteLine($"[ModelConfig] Tab changed: {selectedTab.Header}, RobotConfig active: {isRobotConfigTab}");
            }
        }
    }
}