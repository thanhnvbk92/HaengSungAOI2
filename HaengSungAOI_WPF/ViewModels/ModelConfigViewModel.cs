using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Services.Machine;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace HaengSungAOI_WPF.ViewModels
{
    public partial class ModelConfigViewModel : ObservableObject
    {
        private readonly ILogger<ModelConfigViewModel> _logger;
        private readonly IPlcService _plcService;
        private readonly IModelDatabaseManager _dbManager;
        private readonly IIoConfigService _ioConfigService;
        private readonly RobotPositionManager _robotManager;

        private PCBModel _selectedModel;
        public PCBModel SelectedModel
        {
            get => _selectedModel;
            set 
            {
                if (SetProperty(ref _selectedModel, value))
                {
                    OnSelectedModelChanged(value);
                }
            }
        }

        private void OnSelectedModelChanged(PCBModel value)
        {
            if (value != null)
            {
                _robotManager.LoadInfeedPositions(value);
                _robotManager.LoadTransferPositions(value);
                _robotManager.LoadOutfeedPositions(value);
                _robotManager.LoadInspect1Positions(value);
                _robotManager.LoadInspect2Positions(value);
                _isDataChanged = false;
            }
        }

        private ObservableCollection<PCBModel> _models;
        public ObservableCollection<PCBModel> Models
        {
            get => _models;
            set => SetProperty(ref _models, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _busyMessage;
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private bool _isDataChanged;

        public RobotPositionManager RobotPositionManager => _robotManager;

        public IRelayCommand AddModelCommand { get; }
        public IRelayCommand DuplicateModelCommand { get; }
        public IRelayCommand DeleteModelCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand BrowseVisionFileCommand { get; }
        public IRelayCommand<Window> CloseCommand { get; }
        public IAsyncRelayCommand DownloadToPlcCommand { get; }
        public IAsyncRelayCommand UploadFromPlcCommand { get; }

        public ModelConfigViewModel(
            ILogger<ModelConfigViewModel> logger,
            IPlcService plcService,
            IModelDatabaseManager dbManager,
            IIoConfigService ioConfigService)
        {
            _logger = logger;
            _plcService = plcService;
            _dbManager = dbManager;
            _ioConfigService = ioConfigService;
            _robotManager = new RobotPositionManager();

            AddModelCommand = new RelayCommand(AddModel);
            DuplicateModelCommand = new RelayCommand(DuplicateModel);
            DeleteModelCommand = new RelayCommand(DeleteModel);
            SaveCommand = new RelayCommand(Save);
            BrowseVisionFileCommand = new RelayCommand(BrowseVisionFile);
            CloseCommand = new RelayCommand<Window>(Close);
            DownloadToPlcCommand = new AsyncRelayCommand(DownloadToPlcAsync);
            UploadFromPlcCommand = new AsyncRelayCommand(UploadFromPlcAsync);
            
            LoadModels();
        }

        private async void LoadModels()
        {
            try
            {
                var list = await Task.Run(() => _dbManager.GetAllModels());
                Models = new ObservableCollection<PCBModel>(list);
                
                // Select active model or first one
                SelectedModel = Models.FirstOrDefault(m => m.IsActive) ?? Models.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading models");
                MessageBox.Show($"Lỗi tải danh sách model: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddModel()
        {
            var newModel = new PCBModel
            {
                Name = "New Model " + (Models.Count + 1),
                Description = "Mô tả model mới",
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };
            
            _dbManager.SaveModel(newModel);
            Models.Add(newModel);
            SelectedModel = newModel;
        }

        private void DuplicateModel()
        {
            if (SelectedModel == null) return;

            var copy = SelectedModel.Clone();
            copy.Name = SelectedModel.Name + " (Copy)";
            copy.IsActive = false;

            _dbManager.SaveModel(copy);
            Models.Add(copy);
            SelectedModel = copy;
        }

        private void DeleteModel()
        {
            if (SelectedModel == null) return;
            if (SelectedModel.IsActive)
            {
                MessageBox.Show("Không thể xóa model đang kích hoạt.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Xác nhận xóa model '{SelectedModel.Name}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _dbManager.DeleteModel(SelectedModel.Id);
                Models.Remove(SelectedModel);
                SelectedModel = Models.FirstOrDefault();
            }
        }

        private void Save()
        {
            if (SelectedModel == null) return;

            // Update model from robot manager data
            _robotManager.SaveInfeedPositions(SelectedModel);
            _robotManager.SaveTransferPositions(SelectedModel);
            _robotManager.SaveOutfeedPositions(SelectedModel);
            _robotManager.SaveInspect1Positions(SelectedModel);
            _robotManager.SaveInspect2Positions(SelectedModel);

            SelectedModel.ModifiedDate = DateTime.Now;
            _dbManager.SaveModel(SelectedModel);
            _isDataChanged = false;
            
            MessageBox.Show("Đã lưu cấu hình model thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BrowseVisionFile()
        {
            if (SelectedModel == null) return;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Vision Solutions (*.SOL)|*.SOL|All Files (*.*)|*.*",
                Title = "Chọn file Vision Solution"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedModel.VisionSolutionName = System.IO.Path.GetFileName(openFileDialog.FileName);
                SelectedModel.VisionSolutionPath = openFileDialog.FileName;
                OnPropertyChanged(nameof(SelectedModel)); // Trigger binding update
            }
        }

        private void Close(Window window)
        {
            if (_isDataChanged)
            {
                var result = MessageBox.Show("Bạn có thay đổi chưa lưu. Vẫn muốn đóng?", "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }
            window?.Close();
        }

        private async Task DownloadToPlcAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Đang nạp dữ liệu xuống PLC...";
                ProgressValue = 0;

                if (_plcService == null || !_plcService.IsConnected)
                {
                    MessageBox.Show("PLC chưa kết nối. Không thể nạp dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await Task.Run(() =>
                {
                    WriteInfeedPositionsToPLC(_plcService);
                    ProgressValue = 20;
                    WriteTransferPositionsToPLC(_plcService);
                    ProgressValue = 40;
                    WriteOutfeedPositionsToPLC(_plcService);
                    ProgressValue = 60;
                    WriteInspect1PositionsToPLC(_plcService);
                    ProgressValue = 80;
                    WriteInspect2PositionsToPLC(_plcService);
                    ProgressValue = 100;
                });

                MessageBox.Show("Nạp dữ liệu PLC thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi DownloadToPlc");
                MessageBox.Show($"Lỗi nạp PLC: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UploadFromPlcAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Đang đọc dữ liệu từ PLC...";
                ProgressValue = 0;

                if (_plcService == null || !_plcService.IsConnected)
                {
                    MessageBox.Show("PLC chưa kết nối. Không thể đọc dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await Task.Run(() =>
                {
                    ReadInfeedPositionsFromPLC(_plcService);
                    ProgressValue = 20;
                    ReadTransferPositionsFromPLC(_plcService);
                    ProgressValue = 40;
                    ReadOutfeedPositionsFromPLC(_plcService);
                    ProgressValue = 60;
                    ReadInspect1PositionsFromPLC(_plcService);
                    ProgressValue = 80;
                    ReadInspect2PositionsFromPLC(_plcService);
                    ProgressValue = 100;
                });

                MessageBox.Show("Đọc dữ liệu PLC thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi UploadFromPlc");
                MessageBox.Show($"Lỗi đọc PLC: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region PLC Write Helpers

        private void WriteInfeedPositionsToPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.InfeedPositions;
            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                plc.WriteDouble("X1_Pos1_Idle", idle.X);
                plc.WriteDouble("Y1_Pos1_Idle", idle.Y);
                plc.WriteDouble("R1_Pos1_Idle", idle.R);
                plc.WriteDouble("X1_Speed_Pos1", idle.SpeedX);
                plc.WriteDouble("Y1_Speed_Pos1", idle.SpeedY);
                plc.WriteDouble("R1_Speed_Pos1", idle.SpeedR);
            }

            var pickup = positions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                plc.WriteDouble("X1_Pos2_Pickup", pickup.X);
                plc.WriteDouble("Y1_Pos2_Pickup", pickup.Y);
                plc.WriteDouble("R1_Pos2_Pickup", pickup.R);
                plc.WriteDouble("X1_Speed_Pos2", pickup.SpeedX);
                plc.WriteDouble("Y1_Speed_Pos2", pickup.SpeedY);
                plc.WriteDouble("R1_Speed_Pos2", pickup.SpeedR);
            }

            var place = positions.FirstOrDefault(p => p.Position == "Place");
            if (place != null)
            {
                plc.WriteDouble("X1_Pos3_Place", place.X);
                plc.WriteDouble("Y1_Pos3_Place", place.Y);
                plc.WriteDouble("R1_Pos3_Place", place.R);
                plc.WriteDouble("X1_Speed_Pos3", place.SpeedX);
                plc.WriteDouble("Y1_Speed_Pos3", place.SpeedY);
                plc.WriteDouble("R1_Speed_Pos3", place.SpeedR);
            }
        }

        private void WriteTransferPositionsToPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.TransferPositions;
            WriteTransferPosition(plc, positions, "Idle", 1);
            WriteTransferPosition(plc, positions, "Prepare Pickup", 2);
            WriteTransferPosition(plc, positions, "Pickup", 3);
            WriteTransferPosition(plc, positions, "Prepare Place", 4);
            WriteTransferPosition(plc, positions, "Place", 5);
            WriteTransferPosition(plc, positions, "NG Position", 6);
        }

        private void WriteTransferPosition(IPlcService plc, ObservableCollection<RobotPositionEntry> positions, string positionName, int posNumber)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                string posLabel = positionName.Replace(" ", "");
                plc.WriteDouble($"X2_Pos{posNumber}_{posLabel}", position.X);
                plc.WriteDouble($"Z2_Pos{posNumber}_{posLabel}", position.Z);
                plc.WriteDouble($"X2_Speed_Pos{posNumber}", position.SpeedX);
                plc.WriteDouble($"Z2_Speed_Pos{posNumber}", position.SpeedZ);
            }
        }

        private void WriteOutfeedPositionsToPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.OutfeedPositions;
            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                plc.WriteDouble("X3_Pos1_Idle", idle.X);
                plc.WriteDouble("Y3_Pos1_Idle", idle.Y);
                plc.WriteDouble("X3_Speed_Pos1", idle.SpeedX);
                plc.WriteDouble("Y3_Speed_Pos1", idle.SpeedY);
            }

            var pickup = positions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                plc.WriteDouble("X3_Pos2_Pickup", pickup.X);
                plc.WriteDouble("Y3_Pos2_Pickup", pickup.Y);
                plc.WriteDouble("X3_Speed_Pos2", pickup.SpeedX);
                plc.WriteDouble("Y3_Speed_Pos2", pickup.SpeedY);
            }

            for (int i = 1; i <= 6; i++)
            {
                WriteOutfeedOKPlace(plc, positions, $"OK Place {i}", i + 2);
            }

            var ngPlace = positions.FirstOrDefault(p => p.Position == "NG Place");
            if (ngPlace != null)
            {
                plc.WriteDouble("X3_Pos9_NGPlace", ngPlace.X);
                plc.WriteDouble("Y3_Pos9_NGPlace", ngPlace.Y);
                plc.WriteDouble("X3_Speed_Pos9", ngPlace.SpeedX);
                plc.WriteDouble("Y3_Speed_Pos9", ngPlace.SpeedY);
            }

            var pickupTray = positions.FirstOrDefault(p => p.Position == "Pickup Tray");
            if (pickupTray != null)
            {
                plc.WriteDouble("X3_Pos10_PickupTray", pickupTray.X);
                plc.WriteDouble("Y3_Pos10_PickupTray", pickupTray.Y);
                plc.WriteDouble("X3_Speed_Pos10", pickupTray.SpeedX);
                plc.WriteDouble("Y3_Speed_Pos10", pickupTray.SpeedY);
            }

            var placeTray = positions.FirstOrDefault(p => p.Position == "Place Tray");
            if (placeTray != null)
            {
                plc.WriteDouble("X3_Pos11_PlaceTray", placeTray.X);
                plc.WriteDouble("Y3_Pos11_PlaceTray", placeTray.Y);
                plc.WriteDouble("X3_Speed_Pos11", placeTray.SpeedX);
                plc.WriteDouble("Y3_Speed_Pos11", placeTray.SpeedY);
            }
        }

        private void WriteOutfeedOKPlace(IPlcService plc, ObservableCollection<RobotPositionEntry> positions, string positionName, int posNumber)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                plc.WriteDouble($"X3_Pos{posNumber}_OKPlace{posNumber - 2}", position.X);
                plc.WriteDouble($"Y3_Pos{posNumber}_OKPlace{posNumber - 2}", position.Y);
                plc.WriteDouble($"X3_Speed_Pos{posNumber}", position.SpeedX);
                plc.WriteDouble($"Y3_Speed_Pos{posNumber}", position.SpeedY);
            }
        }

        private void WriteInspect1PositionsToPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.Inspect1Positions;
            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                plc.WriteDouble("Z4_Pos1_Idle", idle.Z);
                plc.WriteDouble("C4_Pos1_Idle", idle.C);
                plc.WriteDouble("Z4_Speed_Pos1", idle.SpeedZ);
                plc.WriteDouble("C4_Speed_Pos1", idle.Speed);
            }

            WriteInspectFocusPosition(plc, positions, "Focus 1", "Z4_Pos2_Focus1", "C4_Pos2_Focus1", "Z4_Speed_Pos2", "C4_Speed_Pos2");
            WriteInspectFocusPosition(plc, positions, "Focus 2", "Z4_Pos3_Focus2", "C4_Pos3_Focus2", "Z4_Speed_Pos3", "C4_Speed_Pos3");
            WriteInspectFocusPosition(plc, positions, "Focus 3", "Z4_Pos4_Focus3", "C4_Pos4_Focus3", "Z4_Speed_Pos4", "C4_Speed_Pos4");
        }

        private void WriteInspect2PositionsToPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.Inspect2Positions;
            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                plc.WriteDouble("Z5_Pos1_Idle", idle.Z);
                plc.WriteDouble("C5_Pos1_Idle", idle.C);
                plc.WriteDouble("Z5_Speed_Pos1", idle.SpeedZ);
                plc.WriteDouble("C5_Speed_Pos1", idle.Speed);
            }

            WriteInspectFocusPosition(plc, positions, "Focus 1", "Z5_Pos2_Focus1", "C5_Pos2_Focus1", "Z5_Speed_Pos2", "C5_Speed_Pos2");
            WriteInspectFocusPosition(plc, positions, "Focus 2", "Z5_Pos3_Focus2", "C5_Pos3_Focus2", "Z5_Speed_Pos3", "C5_Speed_Pos3");
            WriteInspectFocusPosition(plc, positions, "Focus 3", "Z5_Pos4_Focus3", "C5_Pos4_Focus3", "Z5_Speed_Pos4", "C5_Speed_Pos4");
            WriteInspectFocusPosition(plc, positions, "Unload", "Z5_Pos5_Unload", "C5_Pos5_Unload", "Z5_Speed_Pos5", "C5_Speed_Pos5");
        }

        private void WriteInspectFocusPosition(IPlcService plc, ObservableCollection<RobotPositionEntry> positions,
            string positionName, string zTag, string cTag, string speedZTag, string speedCTag)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                plc.WriteDouble(zTag, position.Z);
                plc.WriteDouble(cTag, position.C);
                plc.WriteDouble(speedZTag, position.SpeedZ);
                plc.WriteDouble(speedCTag, position.Speed);
            }
        }

        #endregion

        #region PLC Read Helpers

        private void ReadInfeedPositionsFromPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.InfeedPositions;
            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.X = (float)plc.GetDoubleValue("X1_Pos1_Idle");
                idle.Y = (float)plc.GetDoubleValue("Y1_Pos1_Idle");
                idle.R = (float)plc.GetDoubleValue("R1_Pos1_Idle");
                idle.SpeedX = (float)plc.GetDoubleValue("X1_Speed_Pos1");
                idle.SpeedY = (float)plc.GetDoubleValue("Y1_Speed_Pos1");
                idle.SpeedR = (float)plc.GetDoubleValue("R1_Speed_Pos1");
            }

            var pickup = positions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                pickup.X = (float)plc.GetDoubleValue("X1_Pos2_Pickup");
                pickup.Y = (float)plc.GetDoubleValue("Y1_Pos2_Pickup");
                pickup.R = (float)plc.GetDoubleValue("R1_Pos2_Pickup");
                pickup.SpeedX = (float)plc.GetDoubleValue("X1_Speed_Pos2");
                pickup.SpeedY = (float)plc.GetDoubleValue("Y1_Speed_Pos2");
                pickup.SpeedR = (float)plc.GetDoubleValue("R1_Speed_Pos2");
            }

            var place = positions.FirstOrDefault(p => p.Position == "Place");
            if (place != null)
            {
                place.X = (float)plc.GetDoubleValue("X1_Pos3_Place");
                place.Y = (float)plc.GetDoubleValue("Y1_Pos3_Place");
                place.R = (float)plc.GetDoubleValue("R1_Pos3_Place");
                place.SpeedX = (float)plc.GetDoubleValue("X1_Speed_Pos3");
                place.SpeedY = (float)plc.GetDoubleValue("Y1_Speed_Pos3");
                place.SpeedR = (float)plc.GetDoubleValue("R1_Speed_Pos3");
            }
        }

        private void ReadTransferPositionsFromPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.TransferPositions;
            ReadTransferPosition(plc, positions, "Idle", 1);
            ReadTransferPosition(plc, positions, "Prepare Pickup", 2);
            ReadTransferPosition(plc, positions, "Pickup", 3);
            ReadTransferPosition(plc, positions, "Prepare Place", 4);
            ReadTransferPosition(plc, positions, "Place", 5);
            ReadTransferPosition(plc, positions, "NG Position", 6);
        }

        private void ReadTransferPosition(IPlcService plc, ObservableCollection<RobotPositionEntry> positions, string positionName, int posNumber)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                string posLabel = positionName.Replace(" ", "");
                position.X = (float)plc.GetDoubleValue($"X2_Pos{posNumber}_{posLabel}");
                position.Z = (float)plc.GetDoubleValue($"Z2_Pos{posNumber}_{posLabel}");
                position.SpeedX = (float)plc.GetDoubleValue($"X2_Speed_Pos{posNumber}");
                position.SpeedZ = (float)plc.GetDoubleValue($"Z2_Speed_Pos{posNumber}");
            }
        }

        private void ReadOutfeedPositionsFromPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.OutfeedPositions;
            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.X = (float)plc.GetDoubleValue("X3_Pos1_Idle");
                idle.Y = (float)plc.GetDoubleValue("Y3_Pos1_Idle");
                idle.SpeedX = (float)plc.GetDoubleValue("X3_Speed_Pos1");
                idle.SpeedY = (float)plc.GetDoubleValue("Y3_Speed_Pos1");
            }

            var pickup = positions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                pickup.X = (float)plc.GetDoubleValue("X3_Pos2_Pickup");
                pickup.Y = (float)plc.GetDoubleValue("Y3_Pos2_Pickup");
                pickup.SpeedX = (float)plc.GetDoubleValue("X3_Speed_Pos2");
                pickup.SpeedY = (float)plc.GetDoubleValue("Y3_Speed_Pos2");
            }

            for (int i = 1; i <= 6; i++)
            {
                ReadOutfeedOKPlace(plc, positions, $"OK Place {i}", i + 2);
            }

            var ngPlace = positions.FirstOrDefault(p => p.Position == "NG Place");
            if (ngPlace != null)
            {
                ngPlace.X = (float)plc.GetDoubleValue("X3_Pos9_NGPlace");
                ngPlace.Y = (float)plc.GetDoubleValue("Y3_Pos9_NGPlace");
                ngPlace.SpeedX = (float)plc.GetDoubleValue("X3_Speed_Pos9");
                ngPlace.SpeedY = (float)plc.GetDoubleValue("Y3_Speed_Pos9");
            }

            var pickupTray = positions.FirstOrDefault(p => p.Position == "Pickup Tray");
            if (pickupTray != null)
            {
                pickupTray.X = (float)plc.GetDoubleValue("X3_Pos10_PickupTray");
                pickupTray.Y = (float)plc.GetDoubleValue("Y3_Pos10_PickupTray");
                pickupTray.SpeedX = (float)plc.GetDoubleValue("X3_Speed_Pos10");
                pickupTray.SpeedY = (float)plc.GetDoubleValue("Y3_Speed_Pos10");
            }

            var placeTray = positions.FirstOrDefault(p => p.Position == "Place Tray");
            if (placeTray != null)
            {
                placeTray.X = (float)plc.GetDoubleValue("X3_Pos11_PlaceTray");
                placeTray.Y = (float)plc.GetDoubleValue("Y3_Pos11_PlaceTray");
                placeTray.SpeedX = (float)plc.GetDoubleValue("X3_Speed_Pos11");
                placeTray.SpeedY = (float)plc.GetDoubleValue("Y3_Speed_Pos11");
            }
        }

        private void ReadOutfeedOKPlace(IPlcService plc, ObservableCollection<RobotPositionEntry> positions, string positionName, int posNumber)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                position.X = (float)plc.GetDoubleValue($"X3_Pos{posNumber}_OKPlace{posNumber - 2}");
                position.Y = (float)plc.GetDoubleValue($"Y3_Pos{posNumber}_OKPlace{posNumber - 2}");
                position.SpeedX = (float)plc.GetDoubleValue($"X3_Speed_Pos{posNumber}");
                position.SpeedY = (float)plc.GetDoubleValue($"Y3_Speed_Pos{posNumber}");
            }
        }

        private void ReadInspect1PositionsFromPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.Inspect1Positions;
            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.Z = (float)plc.GetDoubleValue("Z4_Pos1_Idle");
                idle.C = (float)plc.GetDoubleValue("C4_Pos1_Idle");
                idle.SpeedZ = (float)plc.GetDoubleValue("Z4_Speed_Pos1");
                idle.Speed = (float)plc.GetDoubleValue("C4_Speed_Pos1");
            }

            ReadInspectFocusPositionRead(plc, positions, "Focus 1", "Z4_Pos2_Focus1", "C4_Pos2_Focus1", "Z4_Speed_Pos2", "C4_Speed_Pos2");
            ReadInspectFocusPositionRead(plc, positions, "Focus 2", "Z4_Pos3_Focus2", "C4_Pos3_Focus2", "Z4_Speed_Pos3", "C4_Speed_Pos3");
            ReadInspectFocusPositionRead(plc, positions, "Focus 3", "Z4_Pos4_Focus3", "C4_Pos4_Focus3", "Z4_Speed_Pos4", "C4_Speed_Pos4");
        }

        private void ReadInspect2PositionsFromPLC(IPlcService plc)
        {
            var positions = RobotPositionManager.Inspect2Positions;
            var idle = positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.Z = (float)plc.GetDoubleValue("Z5_Pos1_Idle");
                idle.C = (float)plc.GetDoubleValue("C5_Pos1_Idle");
                idle.SpeedZ = (float)plc.GetDoubleValue("Z5_Speed_Pos1");
                idle.Speed = (float)plc.GetDoubleValue("C5_Speed_Pos1");
            }

            ReadInspectFocusPositionRead(plc, positions, "Focus 1", "Z5_Pos2_Focus1", "C5_Pos2_Focus1", "Z5_Speed_Pos2", "C5_Speed_Pos2");
            ReadInspectFocusPositionRead(plc, positions, "Focus 2", "Z5_Pos3_Focus2", "C5_Pos3_Focus2", "Z5_Speed_Pos3", "C5_Speed_Pos3");
            ReadInspectFocusPositionRead(plc, positions, "Focus 3", "Z5_Pos4_Focus3", "C5_Pos4_Focus3", "Z5_Speed_Pos4", "C5_Speed_Pos4");
            ReadInspectFocusPositionRead(plc, positions, "Unload", "Z5_Pos5_Unload", "C5_Pos5_Unload", "Z5_Speed_Pos5", "C5_Speed_Pos5");
        }

        private void ReadInspectFocusPositionRead(IPlcService plc, ObservableCollection<RobotPositionEntry> positions,
            string positionName, string zTag, string cTag, string speedZTag, string speedCTag)
        {
            var position = positions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                position.Z = (float)plc.GetDoubleValue(zTag);
                position.C = (float)plc.GetDoubleValue(cTag);
                position.SpeedZ = (float)plc.GetDoubleValue(speedZTag);
                position.Speed = (float)plc.GetDoubleValue(speedCTag);
            }
        }

        #endregion
    }
}
