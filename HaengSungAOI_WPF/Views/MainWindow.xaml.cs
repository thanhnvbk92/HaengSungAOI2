using Apps.Data.Common;
using HaengSungAOI_WPF.Machine;
using HaengSungAOI_WPF.Machine.PLC;
using HaengSungAOI_WPF.Machine.PLC.PLC;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Resource.Strings;
using HaengSungAOI_WPF.Services.Database;
using HaengSungAOI_WPF.Services.Vision;
using HaengSungAOI_WPF.Utils;
using Spire.Xls;
using Excel = Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using VMControls.RenderInterface;
using ZXing;
using ZXing.QrCode;


namespace HaengSungAOI_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        // ====== EBR: 2-way binding property ======
        private string _currentEbrValue = "Not Set";
        public string CurrentEbrValue
        {
            get => _currentEbrValue;
            set
            {
                if (_currentEbrValue == value) return;
                _currentEbrValue = value;
                OnPropertyChanged(nameof(CurrentEbrValue));

                // Đồng bộ xuống Machine static variable
                HaengSungAOI_WPF.Machine.Machine.CurrentEbr = (value == "Not Set") ? "" : value;
            }
        }
        Machine.Machine AOImachine;
        private ModelDatabaseManager _modelDatabase;
        private PCBModel _currentModel;
        private MachineErrorList _errorList;
        private DispatcherTimer _errorStatusTimer;
        private DispatcherTimer _trayQuantityTimer; // Timer for updating tray quantities

        // Mật khẩu bảo vệ ô Tray Quantity
        private const string PCBTrayQtyPassword = "111";

        // History-related fields
        private InspectionHistoryManager _historyManager;
        private ObservableCollection<InspectionResult> _recentInspectionResults;

        // Error status tracking for UI updates
        private bool _ngConveyorFullAlarmActive = false;
        private bool _trayAlarmActive = false;

        // Static brushes dùng chung — tránh cấp phát heap mỗi giây trong UpdateErrorStatusForAlarms
        private static readonly SolidColorBrush BrushErrorCritical = new SolidColorBrush(Colors.Red);       // TextBlock critical
        private static readonly SolidColorBrush BrushErrorWarning = new SolidColorBrush(Colors.Orange);    // TextBlock warning/alarm
        private static readonly SolidColorBrush BrushErrorOk = new SolidColorBrush(Colors.LimeGreen); // TextBlock ok
        private static readonly SolidColorBrush BrushBtnCritical = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0x00)); // Button dark red
        private static readonly SolidColorBrush BrushBtnWarning = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00)); // Button orange
        private static readonly SolidColorBrush BrushBtnNormal = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x36)); // Button normal
        private static readonly SolidColorBrush BrushFontWhite = new SolidColorBrush(Colors.White);
        private static readonly SolidColorBrush BrushTrayGood = new SolidColorBrush(Color.FromRgb(0x87, 0xCE, 0xEB)); // Light blue — tray OK
        private static readonly SolidColorBrush BrushNa = new SolidColorBrush(Colors.Gray);      // N/A khi machine chưa init

        // HMI Control fields - now using Machine.PLC instead of separate controller
        private DispatcherTimer _hmiLampUpdateTimer;
        // Timer for updating HMI lamp states
        private Dictionary<string, Ellipse> _hmiLamps; // Map button tags to lamp ellipses

        // Caching brushes for HMI lamps to avoid thousands of allocations per day
        private static readonly SolidColorBrush LampOnBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x00)); // Green
        private static readonly SolidColorBrush LampOffBrush = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)); // Gray
        private readonly Dictionary<string, bool> _lastLampStates = new Dictionary<string, bool>();

        public int? ActualMachineId => App.ActualMachineId;

        string labelFilePath;

        public MainWindow()
        {
            LampOnBrush.Freeze();
            LampOffBrush.Freeze();
            
            InitializeComponent();
            InitializeLogging();
            InitializeErrorHandling();
            InitializeModelDatabase();
            InitializeHistoryManagement(); // Initialize history functionality
            InitializeHMIControls(); // Initialize HMI controls

            //InitializeMachine();
            LoadCurrentModelSolution();
            AOImachine = new Machine.Machine();
            EnableScanOutCheckBox.IsChecked = AOImachine.EnableScanOut;
            IsByPassCheckBox.IsChecked = AOImachine.IsByPass;

            // Gọi thử test Oracle Database connection trên background thread
            _ = TestHsmesDatabaseAsync();


            // Subscribe to window closing event for cleanup
            this.Closing += MainWindow_Closing;

            // Subscribe to window activated event for dynamic monitoring group switching
            this.Activated += MainWindow_Activated;



            PopulateHMILampMapping(); // Populate HMI lamp mapping after UI is loaded

            labelFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Pallet_label_result.xlsx");

            // EBR: khai báo DataContext cho phép binding trong code-behind
            DataContext = this;

            // Khởi tạo global variable (đồng bộ Machine.CurrentEbr lần đầu)
            CurrentEbrValue = CurrentEbrTextBlock.Text;

            //Logger.Info("MainWindow", "MainWindow initialized");
        }
        async Task TestHsmesDatabaseAsync()
        {
            try
            {
                var dbService = new Services.Database.AutoVisionDbService();
                var testResult = await dbService.TestHsmesConnectionAsync();

                if (testResult.isSuccess)
                {
                    Logger.Info("Database", $"Test Oracle DB Success: {testResult.errorMessage}");
                }
                else
                {
                    Logger.Warning("Database", $"Test Oracle DB Failed: {testResult.errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Database", "Oracle DB test error", ex);
            }
        }
        /// <summary>
        /// Handle MainWindow activation - switch to MainWindow monitoring groups
        /// </summary>
        private void MainWindow_Activated(object sender, EventArgs e)
        {
            try
            {
                // When MainWindow becomes active, switch to default monitoring groups
                // This monitors only MainWindow HMI buttons/lamps and servo errors
                AOImachine?.PLC?.SetActiveMonitoringGroups(PLCConstants.DEFAULT_MONITORING_GROUPS);
                //Logger.Debug("MainWindow", "MainWindow activated - switched to DEFAULT_MONITORING_GROUPS");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error switching monitoring groups on MainWindow activation", ex);
            }
        }

        /// <summary>
        /// Initialize logging system
        /// </summary>
        private void InitializeLogging()
        {
            try
            {
                // Configure logging
                LogManager.Instance.MinimumLogLevel = LogLevel.Info;
                LogManager.Instance.ConsoleLoggingEnabled = true;
                LogManager.Instance.FileLoggingEnabled = true;

                //Logger.Info("MainWindow", "Logging system initialized");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize logging: {ex.Message}", "Logging Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Initialize error handling system
        /// </summary>
        private void InitializeErrorHandling()
        {
            try
            {
                // Get error list instance
                _errorList = MachineErrorList.Instance;

                // Subscribe to critical errors
                _errorList.CriticalErrorAdded += OnCriticalErrorAdded;
                _errorList.ErrorAdded += OnErrorAdded;

                // Set up timer to update error status
                _errorStatusTimer = new DispatcherTimer();
                _errorStatusTimer.Interval = TimeSpan.FromSeconds(1);
                _errorStatusTimer.Tick += UpdateErrorStatusDisplay;
                _errorStatusTimer.Start();

                // Set up timer to update tray quantities
                _trayQuantityTimer = new DispatcherTimer();
                _trayQuantityTimer.Interval = TimeSpan.FromMilliseconds(500); // Update every 500ms for real-time display
                _trayQuantityTimer.Tick += UpdateTrayQuantities;
                _trayQuantityTimer.Start();

                //Logger.Info("MainWindow", "Error handling system initialized");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Failed to initialize error handling", ex);
                MessageBox.Show($"Failed to initialize error handling: {ex.Message}", "Error System Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Initialize history management system
        /// </summary>
        private void InitializeHistoryManagement()
        {
            try
            {
                // Initialize history data collection immediately
                _recentInspectionResults = new ObservableCollection<InspectionResult>();

                // Bind the data grid to the collection
                historyDataGrid.ItemsSource = _recentInspectionResults;

                //Logger.Info("MainWindow", "History management system initialized");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Failed to initialize history management", ex);
                MessageBox.Show($"Failed to initialize history management: {ex.Message}", "History Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Updates the history grid with a new result from MySQL database
        /// </summary>
        public void AddVisionResultToHistory(TbAutoVisionResult resultData)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Create view model matching the DataGrid columns from TbAutoVisionResult
                    var displayResult = new InspectionResult
                    {
                        STT = 1, // Start with 1, will adjust below
                        PCBCode = resultData.Pid ?? "-",
                        ModelName = resultData.Ebr ?? "-",
                        InspectionDateTime = resultData.InspectionTime ?? DateTime.Now,
                        Station = resultData.Station ?? "-",
                        Result = string.IsNullOrEmpty(resultData.Result) ? "FAIL" : resultData.Result,
                        Note = resultData.Note ?? "",
                        ImagePath = resultData.ImagePath ?? "",
                        InspectionTime = resultData.TackTime ?? 0,
                        TotalDefects = resultData.Result == "OK" ? 0 : 1, // Basic mapping
                        //InspectionTimeMs = 0,
                        OperatorName = "Auto"
                    };

                    _recentInspectionResults.Insert(0, displayResult);

                    // Keep only last 50 items to prevent memory issues
                    if (_recentInspectionResults.Count > 50)
                    {
                        _recentInspectionResults.RemoveAt(_recentInspectionResults.Count - 1);
                    }

                    // Re-calculate STT correctly (1 at top, increasing downwards)
                    for (int i = 0; i < _recentInspectionResults.Count; i++)
                    {
                        _recentInspectionResults[i].STT = i + 1;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error adding vision result to history UI", ex);
            }
        }

        private void ViewImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string imagePath && !string.IsNullOrWhiteSpace(imagePath))
            {
                try
                {
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = imagePath,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        MessageBox.Show($"Image file not found at path:\n{imagePath}", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("MainWindow", $"Failed to open image at {imagePath}", ex);
                    MessageBox.Show($"Failed to open image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        /// <summary>
        /// Update ErrorStatus display based on active alarms
        /// </summary>
        private void UpdateErrorStatusForAlarms()
        {
            try
            {
                bool hasActiveAlarms = _ngConveyorFullAlarmActive || _trayAlarmActive;

                int totalErrors = _errorList?.ErrorCount ?? 0;
                int unacknowledgedErrors = _errorList?.UnacknowledgedErrorCount ?? 0;
                int criticalErrors = _errorList?.UnacknowledgedCriticalErrorCount ?? 0;

                // ── ErrorStatusTextBlock ──────────────────────────────────────────
                if (ErrorStatusTextBlock != null)
                {
                    string newText;
                    Brush newFore;
                    FontWeight newWeight;

                    if (criticalErrors > 0)
                    {
                        newText = $"CRITICAL: {criticalErrors} critical error(s)";
                        newFore = BrushErrorCritical;
                        newWeight = FontWeights.Bold;
                    }
                    else if (_ngConveyorFullAlarmActive && _trayAlarmActive)
                    {
                        newText = "ALARMS: NG Conveyor Full + Tray System";
                        newFore = BrushErrorWarning;
                        newWeight = FontWeights.Bold;
                    }
                    else if (_ngConveyorFullAlarmActive)
                    {
                        newText = "ALARM: NG Conveyor Full - Empty Required";
                        newFore = BrushErrorWarning;
                        newWeight = FontWeights.Bold;
                    }
                    else if (_trayAlarmActive)
                    {
                        newText = "ALARM: Tray System - Attention Required";
                        newFore = BrushErrorWarning;
                        newWeight = FontWeights.Bold;
                    }
                    else if (unacknowledgedErrors > 0)
                    {
                        newText = $"ERRORS: {unacknowledgedErrors} unacknowledged error(s)";
                        newFore = BrushErrorWarning;
                        newWeight = FontWeights.Normal;
                    }
                    else
                    {
                        newText = "System OK";
                        newFore = BrushErrorOk;
                        newWeight = FontWeights.Normal;
                    }

                    // Dirty-check: chỉ gán khi giá trị thực sự thay đổi → tránh re-render mỗi giây
                    if (ErrorStatusTextBlock.Text != newText) ErrorStatusTextBlock.Text = newText;
                    if (!ReferenceEquals(ErrorStatusTextBlock.Foreground, newFore)) ErrorStatusTextBlock.Foreground = newFore;
                    if (ErrorStatusTextBlock.FontWeight != newWeight) ErrorStatusTextBlock.FontWeight = newWeight;
                }

                // ── ErrorListButton ───────────────────────────────────────────────
                if (ErrorListButton != null)
                {
                    string newContent;
                    Brush newBg;
                    Brush newFg = BrushFontWhite;

                    if (unacknowledgedErrors > 0 || hasActiveAlarms)
                    {
                        newContent = hasActiveAlarms
                            ? $"Error List ({totalErrors} + Alarms)"
                            : $"Error List ({totalErrors})";

                        newBg = criticalErrors > 0 ? BrushBtnCritical : BrushBtnWarning;
                    }
                    else
                    {
                        newContent = totalErrors > 0 ? $"Error List ({totalErrors})" : "Error List";
                        newBg = BrushBtnNormal;
                    }

                    // Dirty-check
                    if (ErrorListButton.Content as string != newContent) ErrorListButton.Content = newContent;
                    if (!ReferenceEquals(ErrorListButton.Background, newBg)) ErrorListButton.Background = newBg;
                    if (!ReferenceEquals(ErrorListButton.Foreground, newFg)) ErrorListButton.Foreground = newFg;
                    // UpdateLayout() đã bỏ: WPF tự invalidate layout khi property thay đổi
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error updating ErrorStatus for alarms", ex);
            }
        }


        /// <summary>
        /// Update tray quantity display values
        /// </summary>
        private void UpdateTrayQuantities(object sender, EventArgs e)
        {
            try
            {
                if (AOImachine != null)
                {
                    // ── PCB Slot ────────────────────────────────────────────────
                    if (PCBSlotTextBlock != null)
                    {
                        int pcbSlot = AOImachine.PCB_Quantity;
                        string slotTxt = $"{pcbSlot}/48";
                        Brush slotClr = GetSlotColorBrush(pcbSlot);

                        if (PCBSlotTextBlock.Text != slotTxt) PCBSlotTextBlock.Text = slotTxt;
                        if (!ReferenceEquals(PCBSlotTextBlock.Foreground, slotClr)) PCBSlotTextBlock.Foreground = slotClr;
                    }

                    // ── PCB Tray Quantity — hiển thị trên label, PLC → UI qua timer ────────
                    if (PCBTrayQuantityLabel != null)
                    {
                        int qty = AOImachine.PCBTrayQuantity;
                        string qtyTxt = qty.ToString();
                        Brush qtyClr = GetTrayColorBrush(qty, 2, 4);

                        if (PCBTrayQuantityLabel.Text != qtyTxt) PCBTrayQuantityLabel.Text = qtyTxt;
                        if (!ReferenceEquals(PCBTrayQuantityLabel.Foreground, qtyClr)) PCBTrayQuantityLabel.Foreground = qtyClr;
                    }

                    // ── Blank Tray Quantity ────────────────────────────────────────
                    if (BlankTrayQuantityTextBlock != null)
                    {
                        int blank = AOImachine.BlankTrayQuantity;
                        string blankTxt = blank.ToString();
                        Brush blankClr = GetTrayColorBrush(blank, 2, 4);

                        if (BlankTrayQuantityTextBlock.Text != blankTxt) BlankTrayQuantityTextBlock.Text = blankTxt;
                        if (!ReferenceEquals(BlankTrayQuantityTextBlock.Foreground, blankClr)) BlankTrayQuantityTextBlock.Foreground = blankClr;
                    }
                }
                else
                {
                    // Machine chưa init — hiển thị N/A, chỉ gán nếu chưa đúng
                    if (PCBSlotTextBlock != null)
                    {
                        if (PCBSlotTextBlock.Text != "N/A") PCBSlotTextBlock.Text = "N/A";
                        if (!ReferenceEquals(PCBSlotTextBlock.Foreground, BrushNa)) PCBSlotTextBlock.Foreground = BrushNa;
                    }
                    if (PCBTrayQuantityLabel != null)
                    {
                        if (PCBTrayQuantityLabel.Text != "N/A") PCBTrayQuantityLabel.Text = "N/A";
                        if (!ReferenceEquals(PCBTrayQuantityLabel.Foreground, BrushNa)) PCBTrayQuantityLabel.Foreground = BrushNa;
                    }
                    if (BlankTrayQuantityTextBlock != null)
                    {
                        if (BlankTrayQuantityTextBlock.Text != "N/A") BlankTrayQuantityTextBlock.Text = "N/A";
                        if (!ReferenceEquals(BlankTrayQuantityTextBlock.Foreground, BrushNa)) BlankTrayQuantityTextBlock.Foreground = BrushNa;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error updating tray quantities", ex);
            }
        }
       
      

        /// <summary>Trả về brush màu cảnh báo cho PCB Slot (full=đỏ, gần full=cam, ok=xanh)</summary>
        private static Brush GetSlotColorBrush(int value)
        {
            if (value >= 48) return BrushErrorCritical;
            if (value >= 40) return BrushErrorWarning;
            return BrushTrayGood;
        }

        /// <summary>Trả về brush màu cảnh báo cho tray quantity (thấp=đỏ, gần thấp=cam, ok=xanh)</summary>
        private static Brush GetTrayColorBrush(int value, int redThreshold, int warnThreshold)
        {
            if (value <= redThreshold) return BrushErrorCritical;
            if (value <= warnThreshold) return BrushErrorWarning;
            return BrushTrayGood;
        }

        /// <summary>
        /// Handle critical error notifications
        /// </summary>
        private void OnCriticalErrorAdded(object sender, MachineErrorEventArgs e)
        {
            // Log ngay lập tức trên thread gọi vào (không cần Dispatcher)
            //Logger.Critical("MainWindow", $"Critical error detected: {e.Error.Message} from {e.Error.Source}");

            // Capture để dùng trong lambda (tránh closure trực tiếp vào e sau khi scope kết thúc)
            var error = e.Error;

            // Chạy toàn bộ logic dừng máy trên background thread
            // để KHÔNG block UI thread trong khi giao tiếp PLC/IO đang xử lý
            Task.Run(() =>
            {
                if (AOImachine == null)
                {
                    //Logger.Warning("MainWindow", "OnCriticalErrorAdded: AOImachine is null, skipping shutdown steps");
                    return;
                }

                //Logger.Critical("MainWindow", "Initiating emergency machine shutdown due to critical error");

                // Thực hiện từng bước shutdown theo thứ tự ưu tiên an toàn.
                // Mỗi bước độc lập — lỗi ở bước này không chặn bước tiếp theo.
                ExecuteSafeShutdownStep("StopMachine", () => AOImachine.StopMachine());
                ExecuteSafeShutdownStep("DisableAllMotors", () => AOImachine.DisableAllMotors());
                ExecuteSafeShutdownStep("EmergencyStop", () => AOImachine.EmergencyStop());
                ExecuteSafeShutdownStep("StopAllRobotSequences", () => AOImachine.StopAllRobotSequences());
                // GlobalCancellationManager tự động xử lý huỷ task khi nhận CriticalError event
                ExecuteSafeShutdownStep("SetAllRobotsToManual", () => SetAllRobotsToManualMode());

                //Logger.Critical("MainWindow", "Critical error handling completed - machine in safe state");

                // Chỉ cập nhật UI sau khi toàn bộ shutdown hoàn tất
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try { UpdateMachineControlButtons(); }
                    catch (Exception uiEx)
                    {
                        Logger.Error("MainWindow", "Failed to update UI during critical error handling", uiEx);
                    }

                    try { FlashErrorIndicator(); }
                    catch { /* Đã làm hết có thể, bỏ qua nếu indicator lỗi */ }
                }));
            }).ContinueWith(t =>
            {
                // Xử lý trường hợp Task.Run bản thân bị lỗi không mong đợi
                if (t.IsFaulted && t.Exception != null)
                {
                    Logger.Fatal("MainWindow", "FATAL: Emergency shutdown task failed unexpectedly", t.Exception.InnerException);

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            MessageBox.Show(
                                $"FATAL ERROR IN ERROR HANDLING\n\n" +
                                $"Original Error: {error.Message}\n" +
                                $"Handler Error: {t.Exception.InnerException?.Message}\n\n" +
                                $"SYSTEM MAY BE UNSAFE - MANUAL INTERVENTION REQUIRED",
                                "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                        }
                        catch
                        {
                            Console.WriteLine($"FATAL: Critical error handler failed. Original: {error.Message}");
                        }
                    }));
                }
            });
        }

        /// <summary>
        /// Thực thi một bước shutdown an toàn: log kết quả và không để lỗi lan sang bước kế tiếp.
        /// </summary>
        private void ExecuteSafeShutdownStep(string stepName, Action action)
        {
            try
            {
                action();
                Logger.Critical("MainWindow", $"Shutdown step '{stepName}' completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Fatal("MainWindow", $"Shutdown step '{stepName}' failed", ex);
            }
        }

        /// <summary>
        /// Handle general error notifications
        /// </summary>
        private void OnErrorAdded(object sender, MachineErrorEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
               {
                   // Update error status display
                   UpdateErrorStatusDisplay();

                   //// Only open ErrorListWindow for actual errors (not Information events)
                   //// Information events are automatically acknowledged and don't require user attention
                   //if (e.Error.ErrorType != ErrorType.Information)
                   //{
                   //    try
                   //    {
                   //        Logger.Info("MainWindow", $"Opening ErrorListWindow for error: {e.Error.ErrorType} - {e.Error.Message}");

                   //        // Open ErrorListWindow if not already open
                   //        // Check if ErrorListWindow is already open
                   //        foreach (Window window in Application.Current.Windows)
                   //        {
                   //            if (window is ErrorListWindow)
                   //            {
                   //                // Window already open, just activate it
                   //                window.Activate();
                   //                return;
                   //            }
                   //        }

                   //        // Open new ErrorListWindow
                   //        var errorListWindow = new ErrorListWindow(AOImachine);
                   //        errorListWindow.Owner = this;
                   //        errorListWindow.Show(); // Use Show() instead of ShowDialog() to not block
                   //    }
                   //    catch (Exception ex)
                   //    {
                   //        Logger.Error("MainWindow", "Error opening error list window", ex);
                   //    }
                   //}
               }));
        }

        /// <summary>
        /// Cập nhật ErrorStatus trên UI — dùng trực tiếp cho timer Tick và event handler.
        /// </summary>
        private void UpdateErrorStatusDisplay(object sender = null, EventArgs e = null)
        {
            if (_errorList == null) return;
            UpdateErrorStatusForAlarms();
        }

        /// <summary>
        /// Flash error indicator for critical errors
        /// </summary>
        private void FlashErrorIndicator()
        {
            if (ErrorStatusTextBlock == null) return;

            var originalBrush = ErrorStatusTextBlock.Background;
            ErrorStatusTextBlock.Background = BrushErrorCritical; // static — không cấp phát heap

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (s, ev) =>
            {
                ErrorStatusTextBlock.Background = originalBrush;
                ((DispatcherTimer)s).Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// Set all robots to Manual control mode
        /// </summary>
        private void SetAllRobotsToManualMode()
        {
            try
            {
                if (AOImachine != null)
                {
                    Logger.Info("MainWindow", "All robots set to Manual mode");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error setting robots to Manual mode", ex);
                _errorList.AddRobotError("All Robots", "Failed to set manual mode", null, ex);
            }
        }

        private void InitializeModelDatabase()
        {
            try
            {
                _modelDatabase = new ModelDatabaseManager();
                //Logger.Info("MainWindow", "Database initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error initializing database", ex);
                _errorList.AddException("Database", "Failed to initialize database", ex);
                MessageBox.Show($"Error initializing database: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCurrentModelSolution()
        {
            try
            {
                _currentModel = _modelDatabase?.GetActiveModel();
                if (_currentModel == null)
                {
                    //Logger.Warning("MainWindow", "No active model found, creating default");
                    _errorList.AddError(ErrorType.Warning, "Model", "No active model found, creating default");

                    // Create and set a default model if none exists
                    _currentModel = new PCBModel
                    {
                        Name = "Default Model",
                        Description = "Default configuration",
                        IsActive = true
                    };
                    _modelDatabase?.SaveModel(_currentModel);
                    _modelDatabase?.SetActiveModel(_currentModel.Id);
                }
                UpdateCurrentModelDisplay();
                Logger.Info("MainWindow", $"Current model loaded: {_currentModel.Name}");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error loading current model", ex);
                _errorList.AddException("Model", "Failed to load current model", ex);
                MessageBox.Show($"Error loading current model: {ex.Message}", "Model Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                CurrentModelNameTextBlock.Text = "Error Loading Model";
            }
        }

        private void UpdateCurrentModelDisplay()
        {
            if (_currentModel != null)
            {
                CurrentModelNameTextBlock.Text = _currentModel.Name;
                CurrentModelNameTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x87, 0xCE, 0xEB)); // Light blue
            }
            else
            {
                CurrentModelNameTextBlock.Text = "No Model Selected";
                CurrentModelNameTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)); // White
            }
        }

        public void SetCurrentModel(PCBModel model)
        {
            try
            {
                if (model != null)
                {
                    _currentModel = model;
                    _modelDatabase?.SetActiveModel(model.Id);
                    UpdateCurrentModelDisplay();

                    // Apply model settings to machine
                    ApplyModelToMachine(model);
                    //Logger.Info("MainWindow", $"Current model set to: {model.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error setting current model", ex);
                _errorList.AddException("Model", "Failed to set current model", ex);
                MessageBox.Show($"Error setting current model: {ex.Message}", "Model Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyModelToMachine(PCBModel model)
        {
            try
            {
                if (AOImachine != null && model != null)
                {
                    // Update the machine with the new model (this will also load the vision solution)
                    AOImachine.UpdateModel(model);

                    Logger.Info("MainWindow", $"Applied model '{model.Name}' to machine with vision solution: {model.VisionSolutionName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error applying model to machine", ex);
                _errorList.AddException("Model", "Failed to apply model to machine", ex);
            }
        }

        public PCBModel GetCurrentModel()
        {
            return _currentModel;
        }

        public ModelDatabaseManager GetModelDatabase()
        {
            return _modelDatabase;
        }

        // Existing methods
        public void ChangeLanguage(string cultureName)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);
        }

        private void FlagVN_Click(object sender, RoutedEventArgs e)
        {
            ChangeLanguage("vi");
        }

        private void FlagKR_Click(object sender, RoutedEventArgs e)
        {
            ChangeLanguage("ko");
        }

        private void FlagEN_Click(object sender, RoutedEventArgs e)
        {
            ChangeLanguage("en");
        }

        private void Model_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var modelConfigWindow = new ModelConfig(_currentModel, this);
                modelConfigWindow.Owner = this;
                var result = modelConfigWindow.ShowDialog();

                if (result == true)
                {
                    // Reload the current model in case it was changed
                    LoadCurrentModelSolution();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error opening model configuration", ex);
                _errorList.AddException("UI", "Failed to open model configuration", ex);
                MessageBox.Show($"Error opening model configuration: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ErrorList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var errorListWindow = new ErrorListWindow(AOImachine);
                errorListWindow.Owner = this;
                errorListWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error opening error list", ex);
                MessageBox.Show($"Error opening error list: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMachineControlButtons()
        {
            try
            {
                if (AOImachine != null && AOImachine.IsInitialized)
                {
                    bool isRunning = AOImachine.IsMachineEnabled;

                    // Keep buttons always enabled - don't disable them
                    //StartButton.IsEnabled = true; // Always enable for operator
                    //StopButton.IsEnabled = true;  // Always enable for operator

                    //// Update button appearance to show state but keep them clickable
                    //if (isRunning)
                    //{
                    //    StartButton.Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)); // Gray when machine running
                    //    StartButton.Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)); // Lighter gray text
                    //    StopButton.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x4C, 0x4C)); // Bright red when active
                    //    StopButton.Foreground = new SolidColorBrush(Colors.White); // White text
                    //}
                    //else
                    //{
                    //    StartButton.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0xFF, 0x4C)); // Bright green when active
                    //    StartButton.Foreground = new SolidColorBrush(Colors.White); // White text
                    //    StopButton.Background = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0x00)); // Dark red when machine stopped
                    //    StopButton.Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)); // Lighter gray text
                    //}

                    // Update status bar
                    if (isRunning)
                    {
                        MachineStatusTextBlock.Text = $"Machine: Running ({AOImachine.Mode})";
                        MachineStatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xFF, 0x4C)); // Green
                    }
                    else
                    {
                        MachineStatusTextBlock.Text = "Machine: Stopped";
                        MachineStatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00)); // Orange
                    }
                }
                else
                {
                    // Machine not initialized - Keep buttons enabled but show different appearance
                    //StartButton.IsEnabled = true; // Always enable for operator
                    //StopButton.IsEnabled = true;  // Always enable for operator
                    //StartButton.Background = new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x6A)); // Medium gray for not initialized
                    //StartButton.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)); // Light gray text
                    //StopButton.Background = new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x6A)); // Medium gray for not initialized
                    //StopButton.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)); // Light gray text

                    if (AOImachine == null)
                    {
                        MachineStatusTextBlock.Text = "Machine: Not Initialized";
                    }
                    else
                    {
                        MachineStatusTextBlock.Text = "Machine: Initializing...";
                    }
                    MachineStatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x00)); // Red
                }

                // Force UI update
                //StartButton.UpdateLayout();
                //StopButton.UpdateLayout();
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error updating machine control buttons", ex);
            }
        }

        public Machine.Machine GetMachine()
        {
            return AOImachine;
        }

        /// <summary>
        /// Get the PLC controller from the Machine (centralized PLC access)
        /// </summary>
        public PLCController GetPLCController()
        {
            return AOImachine?.PLC;
        }




        /// <summary>
        /// Get the recent inspection results collection (for external access)
        /// </summary>
        /// <returns>The ObservableCollection of recent inspection results</returns>
        public ObservableCollection<InspectionResult> GetRecentInspectionResults()
        {
            return _recentInspectionResults;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.F11)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.WindowState = WindowState.Normal;
                    this.Topmost = false;
                    this.ResizeMode = ResizeMode.CanResize;
                }
                else
                {
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                    this.Topmost = true;
                    this.ResizeMode = ResizeMode.NoResize;
                }
            }

            // Emergency stop hotkey
            if (e.Key == Key.F12)
            {
                try
                {
                    Logger.Warning("MainWindow", "Emergency stop hotkey pressed (F12)");
                    AOImachine?.EmergencyStop();
                    AOImachine?.StopMachine();
                    MessageBox.Show("EMERGENCY STOP ACTIVATED!", "Emergency",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    UpdateMachineControlButtons();
                }
                catch (Exception ex)
                {
                    Logger.Fatal("MainWindow", "Error during emergency stop", ex);
                    _errorList.AddException("Emergency", "Emergency stop failed", ex);
                    MessageBox.Show($"Error during emergency stop: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnMachineEnabledStateChanged(bool isEnabled)
        {
            // Update UI on main thread
            Dispatcher.Invoke(() =>
            {
                UpdateMachineControlButtons();
                Logger.Info("MainWindow", $"Machine state changed - Enabled: {isEnabled}");
            });
        }



        private async void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            // Overlay đã hiển thị từ XAML (Visibility="Visible")
            // Cập nhật text từng bước để user biết đang làm gì

            try
            {
                // Bước 1: HMI lamp mapping
                SetLoadingStatus("Đang tải giao diện HMI...");
                await Task.Delay(50); // nhường UI thread render
                PopulateHMILampMapping();

                // Bước 2: Gắn frontend và init machine
                // ⚠ Initialize() phải chạy trên STA UI thread vì LoadFrontendSource() là WPF component
                SetLoadingStatus("Đang kết nối PLC và Vision...");
                await Task.Delay(80); // nhường UI thread để spinner render trước khi bị block
                AOImachine.frontendControl = FrontEnd;
                AOImachine.Initialize();

                // Bước 3: Subscribe events & update UI
                SetLoadingStatus("Đang cấu hình hệ thống...");
                await Task.Delay(50);
                AOImachine.OnMachineEnabledStateChanged += OnMachineEnabledStateChanged;
                UpdateMachineControlButtons();

                // Bước 4: PLC monitoring
                SetLoadingStatus("Đang kích hoạt monitoring PLC...");
                AOImachine.PLC?.SetActiveMonitoringGroups(PLCConstants.DEFAULT_MONITORING_GROUPS);
                Logger.Info("MainWindow", "Set PLC monitoring to DEFAULT_MONITORING_GROUPS");

                SetLoadingStatus("Sẵn sàng!");
                await Task.Delay(400); // hiển thị "Sẵn sàng!" ngắn trước khi ẩn
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error during MainWindow initialization", ex);
                SetLoadingStatus($"Lỗi khởi tạo: {ex.Message}");
                await Task.Delay(2000); // giữ thông báo lỗi 2 giây
            }
            finally
            {
                // Ẩn overlay với animation fade-out
                await HideLoadingOverlayAsync();
            }
        }

        /// <summary>Cập nhật text trạng thái trên loading overlay (phải gọi từ UI thread)</summary>
        private void SetLoadingStatus(string message)
        {
            if (LoadingStatusText != null)
                LoadingStatusText.Text = message;
        }

        /// <summary>Fade-out và collapse loading overlay</summary>
        private async Task HideLoadingOverlayAsync()
        {
            if (LoadingOverlay == null) return;

            // Fade out qua animation
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase
                { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };

            var tcs = new TaskCompletionSource<bool>();
            fadeOut.Completed += (_, __) => tcs.SetResult(true);
            LoadingOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            await tcs.Task;

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private void btnSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow(AOImachine);
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error opening settings window", ex);
                _errorList.AddException("UI", "Failed to open settings window", ex);
                MessageBox.Show($"Error opening settings window: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handle window closing to properly dispose of timers and resources
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Logger.Info("MainWindow", "MainWindow closing - cleaning up resources");

                // Stop and dispose of timers
                if (_errorStatusTimer != null)
                {
                    _errorStatusTimer.Stop();
                    _errorStatusTimer.Tick -= UpdateErrorStatusDisplay;
                    _errorStatusTimer = null;
                }

                if (_trayQuantityTimer != null)
                {
                    _trayQuantityTimer.Stop();
                    _trayQuantityTimer.Tick -= UpdateTrayQuantities;
                    _trayQuantityTimer = null;
                }


                if (_hmiLampUpdateTimer != null)
                {
                    _hmiLampUpdateTimer.Stop();
                    _hmiLampUpdateTimer.Tick -= UpdateHMILamps;
                    _hmiLampUpdateTimer = null;
                }

                // Unsubscribe from events
                if (_errorList != null)
                {
                    _errorList.CriticalErrorAdded -= OnCriticalErrorAdded;
                    _errorList.ErrorAdded -= OnErrorAdded;
                }

                if (AOImachine != null)
                {
                    AOImachine.OnMachineEnabledStateChanged -= OnMachineEnabledStateChanged;

                    // Dispose machine resources (this will also dispose PLC)
                    AOImachine.Dispose();
                }

                // Stop UI timers to prevent callbacks during shutdown
                _hmiLampUpdateTimer?.Stop();
                _errorStatusTimer?.Stop();
                _trayQuantityTimer?.Stop();

                try
                {

                    if (ActualMachineId.HasValue)
                    {
                        var dbService = new AutoVisionDbService();
                        Task.Run(async () => await dbService.UpdateVisionOperatingEndAsync(ActualMachineId.Value)).Wait();
                        Logger.Info("MainWindow", "Updated Vision Operating End Time during cleanup");
                    }
                }
                catch (Exception dbEx)
                {
                    Logger.Error("MainWindow", $"Error updating Vision Operating End Time during cleanup: {dbEx.Message}");
                }

                Logger.Info("MainWindow", "MainWindow cleanup completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error during MainWindow cleanup", ex);
            }
        }


        /// <summary>
        /// Initialize HMI control system
        /// </summary>
        private void InitializeHMIControls()
        {
            try
            {
                // Initialize the lamp mapping dictionary
                // Note: these references are available after InitializeComponent() is called
                _hmiLamps = new Dictionary<string, Ellipse>();

                // Set up timer to update lamp states
                _hmiLampUpdateTimer = new DispatcherTimer();
                _hmiLampUpdateTimer.Interval = TimeSpan.FromMilliseconds(200); // Update every 200ms
                _hmiLampUpdateTimer.Tick += UpdateHMILamps;
                _hmiLampUpdateTimer.Start();

                Logger.Info("MainWindow", "HMI control system initialized");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Failed to initialize HMI controls", ex);
                MessageBox.Show($"Failed to initialize HMI controls: {ex.Message}", "HMI Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Populate the lamp mapping dictionary after UI is loaded
        /// </summary>
        private void PopulateHMILampMapping()
        {
            try
            {
                // Find all lamp ellipses by name in the visual tree
                _hmiLamps = new Dictionary<string, Ellipse>();

                // Get lamps from button content
                _hmiLamps["HMI_Auto_PB"] = FindLampInButton(btnHMIAuto);
                _hmiLamps["HMI_Manual_PB"] = FindLampInButton(btnHMIManual);
                _hmiLamps["HMI_Reset_PB"] = FindLampInButton(btnHMIReset);
                _hmiLamps["HMI_Origin"] = FindLampInButton(btnHMIOrigin);
                _hmiLamps["HMI_Start"] = FindLampInButton(btnHMIStart);
                _hmiLamps["HMI_Stop"] = FindLampInButton(btnHMIStop);
                //_hmiLamps["HMI_Pause_System"] = FindLampInButton(btnHMIPause);
                //_hmiLamps["HMI_Single_Block_Mode"] = FindLampInButton(btnHMISingleBlock);
                //_hmiLamps["HMI_Next_Step_PB"] = FindLampInButton(btnHMINextStep);
                _hmiLamps["HMI_Buzzer_Off"] = FindLampInButton(btnHMIBuzzerOff);
                _hmiLamps["HMI_End_Cycle"] = FindLampInButton(btnHMIEndCycle);
                _hmiLamps["HMI_Counter_Reset_PB"] = FindLampInButton(btnHMICounterReset);

                // Trạng thái mặc định: Start enabled, Stop disabled
                //UpdateStartStopButtonState(isRunning: false);

                Logger.Info("MainWindow", "HMI lamp mapping populated successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Failed to populate HMI lamp mapping", ex);
            }
        }

        /// <summary>
        /// Find the lamp ellipse within a button's content
        /// </summary>
        private Ellipse FindLampInButton(Button button)
        {
            if (button == null || button.Content == null)
                return null;

            // The content is a StackPanel containing an Ellipse and TextBlock
            if (button.Content is StackPanel stackPanel)
            {
                foreach (var child in stackPanel.Children)
                {
                    if (child is Ellipse ellipse)
                        return ellipse;
                }
            }

            return null;
        }

        /// <summary>
        /// Handle HMI button mouse down - set holding register to 1 while button is pressed
        /// </summary>
        /// <summary>
        /// Cập nhật trạng thái Enable/Disable của nút HMI_Start và HMI_Stop.
        /// isRunning = true  → Stop enabled,  Start disabled (máy đang chạy)
        /// isRunning = false → Start enabled,  Stop disabled (máy đang dừng)
        /// </summary>
        private void UpdateStartStopButtonState(bool isRunning)
        {
            try
            {
                if (btnHMIStart != null)
                {
                    btnHMIStart.IsEnabled = !isRunning;
                    btnHMIStart.Opacity = isRunning ? 0.4 : 1.0;
                    btnHMIStart.ToolTip = isRunning ? "Máy đang chạy — nhấn Stop trước" : "Khởi động máy";
                }
                if (btnHMIStop != null)
                {
                    btnHMIStop.IsEnabled = isRunning;
                    btnHMIStop.Opacity = isRunning ? 1.0 : 0.4;
                    btnHMIStop.ToolTip = isRunning ? "Dừng máy" : "Máy chưa chạy";
                }

                _machineRunning = isRunning;
                Logger.Info("MainWindow", $"Start/Stop button state updated: isRunning={isRunning}");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", "Error updating Start/Stop button state", ex);
            }
        }

        bool autoMode = false;
        bool _machineRunning = false; // false = stopped (Start enabled), true = running (Stop enabled)
        private async void HMIButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null)
            {
                return;
            }
            // Only handle left mouse button
            if (e.ChangedButton != MouseButton.Left)
                return;

            string buttonTag = button.Tag.ToString();

            try
            {
                // Get PLC from Machine
                var plc = GetPLCController();

                if (plc == null || !plc.IsConnected)
                {
                    Logger.Warning("MainWindow", $"HMI button '{buttonTag}' pressed but PLC not connected");
                    return;
                }
                
                // Chặn kiểm tra hỏi khi nhấn Counter Reset với cảnh báo nghiêm trọng
                if (buttonTag == "HMI_Counter_Reset_PB")
                {
                    bool confirmed = ShowCriticalConfirmation("CẢNH BÁO", 
                        "BẠN CÓ CHẮC CHẮN MUỐN RESET KHÔNG?\n\nLưu ý: Chỉ thực hiện khi đã LẤY HẾT TRAY OUT ra khỏi máy!");

                    if (!confirmed)
                    {
                        Logger.Info("MainWindow", "Counter Reset cancelled by user (Critical Warning).");
                        return; // Hủy lệnh
                    }
                }

                // Kiểm tra trạng thái báo lỗi chưa clear khi ấn nút Start
                if (buttonTag == "HMI_Start" || buttonTag == "Start")
                {
                    if (_errorList != null && _errorList.UnacknowledgedErrorCount > 0)
                    {
                        MessageBox.Show("Vẫn còn mã lỗi chưa được xác nhận trên hệ thống.\nVui lòng mở 'Danh sách lỗi', kiểm tra và nhấn 'Acknowledge All' để xác nhận clear toàn bộ lỗi trước khi Start máy.",
                            "Cảnh báo Lỗi Máy", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return; // Chặn không cho gửi lệnh tới PLC và Database
                    }
                }

                //Logger.Info("MainWindow", $"HMI button pressed: {buttonTag}");

                // Write 1 to holding register (MW address) - ASYNC
                await plc.WriteHoldingRegisterAsync(buttonTag, 1);

                // Visual feedback - change button color while pressed
                button.Background = LampOnBrush;

                // Handle Auto/Manual mode switching for button visibility
                if (buttonTag == "HMI_Auto_PB")
                {
                    await Task.Delay(100);
                    await plc.WriteHoldingRegisterAsync(buttonTag, 0);
                    autoMode = true;
                    App.IsAutoMode = true;
                    button.Background = BrushBtnNormal;
                    HideHMIButtonsExceptManual();
                }
                else if (buttonTag == "HMI_Manual_PB")
                {
                    await Task.Delay(100);
                    await plc.WriteHoldingRegisterAsync(buttonTag, 0);
                    autoMode = false;
                    App.IsAutoMode = false;
                    button.Background = BrushBtnNormal;
                    ShowAllHMIButtons();
                }
                else if (buttonTag == "HMI_Counter_Reset_PB")
                {
                    await Task.Delay(100);
                    await plc.WriteHoldingRegisterAsync(buttonTag, 0);
                    button.Background = BrushBtnNormal;
                }

                if (buttonTag == "HMI_Start")
                {
                    if (autoMode && ActualMachineId.HasValue)
                    {
                        _ = InitializeSessionAsync(ActualMachineId.Value);
                    }
                }
                else if (buttonTag == "HMI_Stop")
                {
                    if (autoMode && ActualMachineId.HasValue)
                    {
                        _ = UpdateEndAsync(ActualMachineId.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", $"Error handling HMI button '{buttonTag}' mouse down", ex);
                _errorList.AddException("HMI", $"Failed to handle HMI button '{buttonTag}' mouse down", ex);
            }
        }
        async Task InitializeSessionAsync(int machineId)
        {
            try
            {
                var dbService = new AutoVisionDbService();
                await dbService.InitializeOperatingSessionAsync(machineId);
            }
            catch (Exception innerEx)
            {
                Logger.Error("MainWindow", $"VisionOperatingTime Start Error: {innerEx.Message}");
            }
        }
        async Task UpdateEndAsync(int machineId)
        {
            try
            {
                var dbService = new AutoVisionDbService();
                await dbService.UpdateVisionOperatingEndAsync(machineId);
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", $"VisionOperatingTime End Error: {ex.Message}");
            }
        }
        /// <summary>
        /// Handle HMI button mouse up - set holding register to 0 when button is released
        /// </summary>
        private async void HMIButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null)
            {
                Logger.Warning("MainWindow", "HMI button mouse up with invalid sender or tag");
                return;
            }

            // Only handle left mouse button
            if (e.ChangedButton != MouseButton.Left)
                return;

            string buttonTag = button.Tag.ToString();

            try
            {
                var plc = GetPLCController();

                if (plc == null || !plc.IsConnected)
                {
                    // If PLC is not connected, at least restore button appearance
                    button.Background = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x36)); // Normal color
                    return;
                }

                // Write 0 to holding register (MW address) - ASYNC
                await plc.WriteHoldingRegisterAsync(buttonTag, 0);

                // Restore button appearance
                button.Background = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x36)); // Normal color

                //Logger.Debug("MainWindow", $"HMI button '{buttonTag}' set to 0 (holding register)");
            }
            catch (Exception ex)
            {
                // Restore button appearance even on error
                button.Background = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x36)); // Normal color

                Logger.Error("MainWindow", $"Error handling HMI button '{buttonTag}' mouse up", ex);
                _errorList.AddException("HMI", $"Failed to handle HMI button '{buttonTag}' mouse up", ex);
                MessageBox.Show($"Error handling button release:\n{ex.Message}", "HMI Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Update HMI lamp states from PLC
        /// </summary>
        private void UpdateHMILamps(object sender, EventArgs e)
        {
            var plc = GetPLCController();
            if (plc == null || !plc.IsConnected)
                return;

            if (_hmiLamps == null || _hmiLamps.Count == 0)
                return;

            try
            {
                foreach (var kvp in _hmiLamps)
                {
                    string buttonTag = kvp.Key;
                    Ellipse lamp = kvp.Value;

                    if (lamp == null)
                        continue;

                    // Get corresponding lamp tag (HMI_Lamp_XXX)
                    string lampTag = buttonTag.Replace("HMI_", "HMI_Lamp_");

                    // Get lamp state from PLC (holding register MW200-MW210)
                    var dataPoint = plc.GetDataPoint(lampTag);
                    if (dataPoint != null)
                    {
                        bool isOn = false;
                        if (dataPoint.Value is ushort regValue)
                        {
                            isOn = regValue != 0;
                        }
                        else if (dataPoint.Value is bool boolValue)
                        {
                            isOn = boolValue;
                        }

                        // Optimization: Only update UI if state has actually changed to save CPU cycles and reduce UI thread load
                        if (!_lastLampStates.TryGetValue(lampTag, out bool lastState) || lastState != isOn)
                        {
                            _lastLampStates[lampTag] = isOn;
                            
                            // Tick event of DispatcherTimer is already on the UI thread
                            lamp.Fill = isOn ? LampOnBrush : LampOffBrush;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log errors occasionally to avoid flooding
                if (DateTime.Now.Second % 10 == 0)
                {
                    Logger.Debug("MainWindow", $"Error updating HMI lamps: {ex.Message}");
                }
            }
        }




        #region PCB Tray Quantity Label Handlers

        /// <summary>
        /// Hiển thị dialog tổng hợp: nhập mật khẩu + giá trị mới.
        /// Trả về giá trị mới nếu hợp lệ, null nếu hủy hoặc mật khẩu sai.
        /// </summary>
        private ushort? ShowTrayQuantityEditDialog()
        {
            // Lấy giá trị hiện tại để pre-fill
            int currentQty = AOImachine?.PCBTrayQuantity ?? 0;

            var dlg = new Window
            {
                Title = "Chỉnh sửa Tray Quantity",
                Width = 360,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x3c)),
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true
            };

            var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };

            // ── Mật khẩu ──────────────────────────────────
            panel.Children.Add(new TextBlock
            {
                Text = "Mật khẩu:",
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var pwdBox = new PasswordBox
            {
                MaxLength = 20,
                FontSize = 15,
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x2f)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77)),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 14)
            };
            panel.Children.Add(pwdBox);

            // ── Giá trị mới ───────────────────────────────
            panel.Children.Add(new TextBlock
            {
                Text = "Giá trị mới:",
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var valueBox = new TextBox
            {
                Text = currentQty.ToString(),
                MaxLength = 2,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Height = 44,
                Background = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x2f)),
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x7F)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77)),
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 16)
            };
            // Chỉ cho phép nhập số 0 ~ 48
            valueBox.PreviewTextInput += (s, ev) => { ev.Handled = !ev.Text.All(c => c >= '0' && c <= '9'); };
            DataObject.AddPastingHandler(valueBox, (s, ev) =>
            {
                if (ev.DataObject.GetDataPresent(DataFormats.Text))
                {
                    string text = (string)ev.DataObject.GetData(DataFormats.Text);
                    if (!text.All(c => c >= '0' && c <= '9')) ev.CancelCommand();
                }
                else ev.CancelCommand();
            });
            panel.Children.Add(valueBox);

            // ── Nút OK / Hủy ──────────────────────────────
            bool? confirmed = false;

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnOk = new Button
            {
                Content = "Xác nhận",
                Width = 90,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x36)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x88, 0xCC)),
                IsDefault = true
            };
            btnOk.Click += (s, ev) => { confirmed = true; dlg.Close(); };

            var btnCancel = new Button
            {
                Content = "Hủy",
                Width = 80,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x36)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                IsCancel = true
            };
            btnCancel.Click += (s, ev) => { confirmed = false; dlg.Close(); };

            // Enter trong valueBox cũng submit
            valueBox.KeyDown += (s, ev) => { if (ev.Key == Key.Enter) { confirmed = true; dlg.Close(); } };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);

            dlg.Content = panel;
            dlg.Loaded += (s, ev) => { pwdBox.Focus(); valueBox.SelectAll(); };
            dlg.ShowDialog();

            if (confirmed != true) return null;

            // Kiểm tra mật khẩu
            if (pwdBox.Password != PCBTrayQtyPassword)
            {
                MessageBox.Show("Mật khẩu không đúng.", "Xác thực thất bại",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            // Kiểm tra giá trị hợp lệ 0 ~ 5
            if (!ushort.TryParse(valueBox.Text, out ushort newValue) || newValue > 48)
            {
                MessageBox.Show("Giá trị không hợp lệ. Vui lòng nhập số nguyên (0 – 48).",
                    "Giá trị không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return newValue;
        }

        /// <summary>
        /// Hiển thị một popup cảnh báo nghiêm trọng có viền đỏ để thu hút sự chú ý
        /// </summary>
        private bool ShowCriticalConfirmation(string title, string message)
        {
            bool confirmed = false;

            var dlg = new Window
            {
                Title = title,
                Width = 650,
                Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
                WindowStyle = WindowStyle.None, // Loại bỏ tiêu đề chuẩn để tạo cảm giác nghiêm trọng
                AllowsTransparency = true,
                Topmost = true
            };

            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Colors.Red),
                BorderThickness = new Thickness(6),
                Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
                Padding = new Thickness(30)
            };

            var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            // Warning Icon
            panel.Children.Add(new TextBlock
            {
                Text = "⚠",
                FontSize = 90,
                Foreground = new SolidColorBrush(Colors.Red),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Title
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Red),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Message with specialized formatting
            var messageBlock = new TextBlock
            {
                FontSize = 20,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 40)
            };

            if (message.Contains("LẤY HẾT TRAY OUT"))
            {
                string target = "LẤY HẾT TRAY OUT";
                int index = message.IndexOf(target);
                
                // Phần trước
                messageBlock.Inlines.Add(new System.Windows.Documents.Run(message.Substring(0, index)));
                
                // Phần nhấn mạnh
                var highlight = new System.Windows.Documents.Run(target)
                {
                    FontSize = 36,
                    Foreground = new SolidColorBrush(Colors.Yellow),
                    FontWeight = FontWeights.ExtraBold,
                    TextDecorations = TextDecorations.Underline
                };
                messageBlock.Inlines.Add(highlight);
                
                // Phần sau
                messageBlock.Inlines.Add(new System.Windows.Documents.Run(message.Substring(index + target.Length)));
            }
            else
            {
                messageBlock.Text = message;
            }
            panel.Children.Add(messageBlock);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var btnOk = new Button
            {
                Content = "XÁC NHẬN (YES)",
                Width = 260,
                Height = 85,
                Margin = new Thickness(0, 0, 30, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0x00)), // DarkRed
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.White)
            };
            btnOk.Click += (s, ev) => { confirmed = true; dlg.Close(); };

            var btnCancel = new Button
            {
                Content = "HỦY (NO)",
                Width = 200,
                Height = 85,
                Background = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0)
            };
            btnCancel.Click += (s, ev) => { confirmed = false; dlg.Close(); };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);

            border.Child = panel;
            dlg.Content = border;

            // Hỗ trợ kéo di chuyển popup
            border.MouseLeftButtonDown += (s, ev) => { if (ev.ButtonState == MouseButtonState.Pressed) dlg.DragMove(); };

            dlg.ShowDialog();
            return confirmed;
        }

        /// <summary>
        /// Khi user click vào label Tray Quantity, hiện dialog nhập mật khẩu + giá trị mới.
        /// </summary>
        private void PCBTrayQuantityLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ushort? newValue = ShowTrayQuantityEditDialog();
            if (newValue.HasValue)
            {
                WritePCBTrayQuantityToPLC(newValue.Value);
            }
        }

        /// <summary>
        /// Ghi giá trị Tray Quantity xuống PLC.
        /// </summary>
        private async void WritePCBTrayQuantityToPLC(ushort value)
        {
            try
            {
                var plc = GetPLCController();
                if (plc == null || !plc.IsConnected)
                {
                    Logger.Warning("MainWindow", "Cannot write PCB Tray Quantity: PLC not connected");
                    MessageBox.Show("PLC chưa kết nối. Không thể cập nhật tray quantity.",
                        "PLC Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PLCAddresses.TrayQuantity_Registers.TryGetValue("PCB_Slot", out ushort address))
                {
                    await Task.Run(() => plc.WriteHoldingRegistersDirect(address, new ushort[] { value }));
                    //Logger.Info("MainWindow", $"PCB Tray Quantity written to PLC: {value} at MW{address}");

                    // Cập nhật label ngay (không đợi timer)
                    if (PCBTrayQuantityLabel != null)
                    {
                        PCBTrayQuantityLabel.Text = value.ToString();
                        PCBTrayQuantityLabel.Foreground = GetTrayColorBrush(value, 2, 4);
                    }
                    if (AOImachine != null) AOImachine.PCBTrayQuantity = value;

                    // Visual feedback ngắn: label chớp xanh lá
                    if (PCBTrayQuantityLabel != null)
                    {
                        var origFg = PCBTrayQuantityLabel.Foreground;
                        PCBTrayQuantityLabel.Foreground = new SolidColorBrush(Colors.LimeGreen);
                        var flashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                        flashTimer.Tick += (s, args) =>
                        {
                            PCBTrayQuantityLabel.Foreground = GetTrayColorBrush(value, 2, 4);
                            flashTimer.Stop();
                        };
                        flashTimer.Start();
                    }
                }
                else
                {
                    Logger.Error("MainWindow", "PCB_Trays address not found in PLCAddresses");
                    MessageBox.Show("Lỗi cấu hình địa chỉ PLC. Không thể cập nhật tray quantity.",
                        "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", $"Error writing PCB Tray Quantity to PLC: {ex.Message}", ex);
                _errorList?.AddException("PLC", "Failed to write PCB Tray Quantity to PLC", ex);
                MessageBox.Show($"Lỗi ghi PLC: {ex.Message}",
                    "PLC Write Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region EBR Input Dialog

        private string ShowEbrEditDialog()
        {
            var dlg = new Window
            {
                Title = "Nhập EBR mới",
                Width = 360,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x3c)),
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true
            };

            var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };

            panel.Children.Add(new TextBlock
            {
                Text = "Giá trị EBR mới:",
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var valueBox = new TextBox
            {
                Text = CurrentEbrTextBlock.Text != "Not Set" ? CurrentEbrTextBlock.Text : "",
                MaxLength = 50,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x2f)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77)),
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 16)
            };
            panel.Children.Add(valueBox);

            bool? confirmed = false;

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnOk = new Button
            {
                Content = "Xác nhận",
                Width = 90,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x36)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x88, 0xCC)),
                IsDefault = true
            };
            btnOk.Click += (s, ev) => { confirmed = true; dlg.Close(); };

            var btnCancel = new Button
            {
                Content = "Hủy",
                Width = 80,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x36)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                IsCancel = true
            };
            btnCancel.Click += (s, ev) => { confirmed = false; dlg.Close(); };

            valueBox.KeyDown += (s, ev) => { if (ev.Key == Key.Enter) { confirmed = true; dlg.Close(); } };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);

            dlg.Content = panel;
            dlg.Loaded += (s, ev) => { valueBox.Focus(); valueBox.SelectAll(); };
            dlg.ShowDialog();

            if (confirmed != true) return null;

            return valueBox.Text.Trim();
        }

        private void CurrentEbrTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string newEbr = ShowEbrEditDialog();
            if (!string.IsNullOrEmpty(newEbr))
            {
                // Gán qua property để tự động cập nhật UI + Machine.CurrentEbr cùng lúc
                CurrentEbrValue = newEbr;
                Logger.Info("EBR", $"User change EBR to: {newEbr}");
            }
        }

        private void ResetEbr_Click(object sender, RoutedEventArgs e)
        {
            CurrentEbrValue = null;
            Logger.Info("EBR", "Current EBR reset to null");
        }

        /// <summary>
        /// Gọi từ background thread (Machine.PLC.cs) để đẩy EBR mới lên UI một cách thread-safe.
        /// </summary>
        public void SetEbrFromBackend(string newEbr)
        {
            if (string.IsNullOrEmpty(newEbr)) return;
            Dispatcher.InvokeAsync(() =>
            {
                CurrentEbrValue = newEbr;
                //Logger.Info("EBR", $"EBR tự động cập nhật từ backend: {newEbr}");
            });
        }

        private void EnableScanOutCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (AOImachine != null)
                AOImachine.EnableScanOut = true;
        }

        private void EnableScanOutCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (AOImachine != null)
                AOImachine.EnableScanOut = false;
        }

        private bool ShowBypassPasswordDialog()
        {
            var dlg = new Window
            {
                Title = "Xác thực kích hoạt By Pass",
                Width = 300,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x3c)),
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true
            };

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = "Nhập mật khẩu để kích hoạt By Pass:",
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var pwdBox = new PasswordBox
            {
                MaxLength = 20,
                FontSize = 15,
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x2f)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x77)),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(pwdBox);

            bool? confirmed = false;
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var btnOk = new Button { Content = "Xác nhận", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
            btnOk.Click += (s, ev) => { confirmed = true; dlg.Close(); };

            var btnCancel = new Button { Content = "Hủy", Width = 80, Height = 30, IsCancel = true };
            btnCancel.Click += (s, ev) => { confirmed = false; dlg.Close(); };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);

            dlg.Content = panel;
            dlg.Loaded += (s, ev) => pwdBox.Focus();
            dlg.ShowDialog();

            if (confirmed == true && pwdBox.Password == PCBTrayQtyPassword)
            {
                return true;
            }

            if (confirmed == true)
            {
                MessageBox.Show("Mật khẩu không đúng!", "Xác thực thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return false;
        }

        private void IsByPassCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (AOImachine != null)
            {
                if (ShowBypassPasswordDialog())
                {
                    AOImachine.IsByPass = true;
                    Logger.Info("MainWindow", "By Pass mode activated");
                }
                else
                {
                    IsByPassCheckBox.IsChecked = false;
                    AOImachine.IsByPass = false;
                }
            }
        }

        private void IsByPassCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (AOImachine != null)
                AOImachine.IsByPass = false;
        }

        private void btnClearQueues_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Xác nhận xóa toàn bộ WIP Queue?\n\nThao tác này sẽ:\n• Xóa tất cả board đang trong máy khỏi hàng đợi\n• Reset Edge-Detection của PLC\n• Cho phép Vision trigger chạy lại từ đầu\n\nChỉ thực hiện khi đã dừng máy và lấy hàng ra!",
                "🗑️ Xóa Queue Xác Nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                AOImachine?.ClearAllQueues();
                //Logger.Info("MainWindow", "ClearAllQueues called manually by operator");
                MessageBox.Show("Đã xóa sạch Queue thành công!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        /// <summary>
        /// Hide all HMI Control buttons except the Manual button
        /// Called when Auto mode is activated
        /// </summary>
        private void HideHMIButtonsExceptManual()
        {
            try
            {
                // Hide all buttons except Manual
                //if (btnHMIAuto != null) btnHMIAuto.Visibility = Visibility.Collapsed;
                //if (btnHMIReset != null) btnHMIReset.Visibility = Visibility.Collapsed;
                if (btnHMIOrigin != null) btnHMIOrigin.Visibility = Visibility.Collapsed;
                //if (btnHMIStart != null) btnHMIStart.Visibility = Visibility.Collapsed;
                //if (btnHMIStop != null) btnHMIStop.Visibility = Visibility.Collapsed;
                //if (btnHMIPause != null) btnHMIPause.Visibility = Visibility.Collapsed;
                //if (btnHMISingleBlock != null) btnHMISingleBlock.Visibility = Visibility.Collapsed;
                //if (btnHMINextStep != null) btnHMINextStep.Visibility = Visibility.Collapsed;
                //if (btnHMIBuzzerOff != null) btnHMIBuzzerOff.Visibility = Visibility.Collapsed;
                //if (btnHMIEndCycle != null) btnHMIEndCycle.Visibility = Visibility.Collapsed;
                //if (btnHMICounterReset != null) btnHMICounterReset.Visibility = Visibility.Collapsed;

                // Keep Manual button visible
                if (btnHMIManual != null) btnHMIManual.Visibility = Visibility.Visible;

                //Logger.Info("MainWindow", "HMI Controls hidden (Auto mode) - Manual button remains visible");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", $"Error hiding HMI buttons: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Show all HMI Control buttons
        /// Called when Manual mode is activated
        /// </summary>
        private void ShowAllHMIButtons()
        {
            try
            {
                // Show all buttons
                if (btnHMIAuto != null) btnHMIAuto.Visibility = Visibility.Visible;
                if (btnHMIManual != null) btnHMIManual.Visibility = Visibility.Visible;
                if (btnHMIReset != null) btnHMIReset.Visibility = Visibility.Visible;
                if (btnHMIOrigin != null) btnHMIOrigin.Visibility = Visibility.Visible;
                if (btnHMIStart != null) btnHMIStart.Visibility = Visibility.Visible;
                if (btnHMIStop != null) btnHMIStop.Visibility = Visibility.Visible;
                //if (btnHMIPause != null) btnHMIPause.Visibility = Visibility.Visible;
                //if (btnHMISingleBlock != null) btnHMISingleBlock.Visibility = Visibility.Visible;
                //if (btnHMINextStep != null) btnHMINextStep.Visibility = Visibility.Visible;
                if (btnHMIBuzzerOff != null) btnHMIBuzzerOff.Visibility = Visibility.Visible;
                if (btnHMIEndCycle != null) btnHMIEndCycle.Visibility = Visibility.Visible;
                if (btnHMICounterReset != null) btnHMICounterReset.Visibility = Visibility.Visible;

                //Logger.Info("MainWindow", "All HMI Controls shown (Manual mode)");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", $"Error showing HMI buttons: {ex.Message}", ex);
            }
        }

        // ...existing code...
    }

    public class PbaData
    {
        public PbaData() { }
        public string PbaID { get; set; }
        public int Qty { get; set; }
        public string WORK_ORDER { get; set; }
        public string ITEM_CODE { get; set; }
        public string AEN { get; set; }
        public string ModelSuffix { get; set; }
        public string ModelName { get; set; }
        public DateTime pbaCreationTime { get; set; }
        public List<string> pidList { get; set; }
    }
}
