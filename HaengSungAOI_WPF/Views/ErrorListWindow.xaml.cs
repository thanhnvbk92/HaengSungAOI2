using HaengSungAOI_WPF.Core;
using HaengSungAOI_WPF.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace HaengSungAOI_WPF.Views
{
    /// <summary>
    /// Interaction logic for ErrorListWindow.xaml
    /// </summary>
    public partial class ErrorListWindow : Window
    {
        // Reference to the machine error list
        private readonly MachineErrorList _errorList;

        // Currently selected error
        private MachineError _selectedError;

        // Flag to prevent triggering events during initialization
        private bool _isInitializing = true;
        Machine AOImachine;

        public ErrorListWindow(Machine machine)
        {
            InitializeComponent();

            try
            {
                // Get the singleton instance
                _errorList = MachineErrorList.Instance;

                if (_errorList == null)
                {
                    MessageBox.Show("Error system is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Subscribe to error added event
                _errorList.ErrorAdded += OnErrorAdded;

                // Subscribe to loaded event 
                this.Loaded += ErrorListWindow_Loaded;
                this.Closing += (s, e) =>
                {
                    e.Cancel = true; // Cancel the close event
                    this.Hide();
                };
                AOImachine = machine;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing error list window: {ex.Message}", "Initialization Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Error initializing error list window: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Window loaded event handler
        /// </summary>
        private void ErrorListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize the window after it's loaded to ensure UI elements are ready
                _isInitializing = true;

                // Set default ComboBox selections
                if (cbErrorType != null) cbErrorType.SelectedIndex = 0;
                if (cbStatus != null) cbStatus.SelectedIndex = 0;

                // Use the dispatcher to ensure UI is fully loaded before refreshing
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    _isInitializing = false;
                    RefreshErrorList();

                    // Force initial UI update to ensure buttons are in correct state
                    UpdateUI();

                    Console.WriteLine("ErrorListWindow initialization completed");
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during window load: {ex.Message}", "Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Error during window load: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Refreshes the error list with filtered data
        /// </summary>
        private void RefreshErrorList()
        {
            try
            {
                // Don't refresh during initialization
                if (_isInitializing)
                    return;

                // Verify error list is available
                if (_errorList == null)
                {
                    Console.WriteLine("Error list is null, cannot refresh");
                    return;
                }

                // Verify UI controls are initialized
                if (cbErrorType == null || cbStatus == null || txtSearch == null || dgErrors == null)
                {
                    Console.WriteLine("RefreshErrorList called before UI controls were initialized");
                    return;
                }

                // Debug: Log current error counts before filtering
                Console.WriteLine($"RefreshErrorList: Total errors: {_errorList.ErrorCount}, Unacknowledged: {_errorList.UnacknowledgedErrorCount}");

                // Get filter values with safety checks
                ErrorType? errorTypeFilter = null;
                if (cbErrorType != null && cbErrorType.SelectedIndex > 0)
                {
                    errorTypeFilter = (ErrorType)(cbErrorType.SelectedIndex - 1);
                }

                bool? acknowledgedState = null;
                if (cbStatus != null)
                {
                    if (cbStatus.SelectedIndex == 1) // Acknowledged
                        acknowledgedState = true;
                    else if (cbStatus.SelectedIndex == 2) // Unacknowledged
                        acknowledgedState = false;
                }

                string searchText = txtSearch?.Text?.Trim() ?? string.Empty;

                // Get filtered errors
                IEnumerable<MachineError> filteredErrors = _errorList.GetFilteredErrors(
                    errorTypeFilter,
                    string.IsNullOrEmpty(searchText) ? null : searchText,
                    acknowledgedState);

                // Further filter by message if search text is provided
                if (!string.IsNullOrEmpty(searchText))
                {
                    filteredErrors = filteredErrors.Where(e =>
                        e.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        e.Source.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                // Sort by time descending (newest first)
                filteredErrors = filteredErrors.OrderByDescending(e => e.Timestamp).ToList();

                // Update the DataGrid
                dgErrors.ItemsSource = filteredErrors;

                // Force UI update to ensure everything is refreshed
                dgErrors.UpdateLayout();

                // Update UI state
                UpdateUI();

                // Debug: Log filtered results
                Console.WriteLine($"Error list refreshed. Found {filteredErrors.Count()} errors after filtering.");
                Console.WriteLine($"Filter - Type: {errorTypeFilter?.ToString() ?? "All"}, Status: {(acknowledgedState.HasValue ? (acknowledgedState.Value ? "Acknowledged" : "Unacknowledged") : "All")}, Search: '{searchText}'");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing error list: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Error refreshing list: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Updates the UI based on current state
        /// </summary>
        private void UpdateUI()
        {
            try
            {
                if (_errorList == null)
                    return;

                // Update counts in window title
                Title = $"Machine Error List - {_errorList.ErrorCount} Errors ({_errorList.UnacknowledgedErrorCount} Unacknowledged)";

                // Update button states - check for null references first
                //if (btnAcknowledge != null && btnAcknowledgeAll != null && btnClear != null && btnClearSelected != null)
                //{
                //    // Make acknowledge and clear buttons always available
                //    btnAcknowledge.IsEnabled = true;
                //    btnClearSelected.IsEnabled = true;
                //    btnAcknowledgeAll.IsEnabled = true;
                //    btnClear.IsEnabled = true;

                //    // Update button visual states to make them always appear enabled
                //    UpdateButtonAppearance(btnAcknowledge, true);
                //    UpdateButtonAppearance(btnClearSelected, true);
                //    UpdateButtonAppearance(btnAcknowledgeAll, true);
                //    UpdateButtonAppearance(btnClear, true);

                //    // Debug output
                //    Console.WriteLine($"UI Update - All buttons enabled permanently");
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating UI: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Update button appearance to make enabled/disabled state more obvious
        /// </summary>
        private void UpdateButtonAppearance(Button button, bool isEnabled)
        {
            if (button == null) return;

            if (isEnabled)
            {
                button.Background = new SolidColorBrush(Color.FromRgb(0x23, 0x23, 0x36)); // Normal color
                button.Foreground = new SolidColorBrush(Colors.White);
                button.Opacity = 1.0;
            }
            else
            {
                button.Background = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)); // Darker gray when disabled
                button.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)); // Gray text when disabled
                button.Opacity = 0.6;
            }
        }

        /// <summary>
        /// Event handler for error added event
        /// </summary>
        private void OnErrorAdded(object sender, MachineErrorEventArgs e)
        {
            // Update UI on the UI thread
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                RefreshErrorList();
            }));
        }

        /// <summary>
        /// Handle filter changed events
        /// </summary>
        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing)
                RefreshErrorList();
        }

        /// <summary>
        /// Handle search text changed
        /// </summary>
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing)
                RefreshErrorList();
        }

        /// <summary>
        /// Handle refresh button click
        /// </summary>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshErrorList();
        }

        /// <summary>
        /// Handle selection changed in the error list
        /// </summary>
        private void dgErrors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _selectedError = dgErrors.SelectedItem as MachineError;

                if (_selectedError != null)
                {
                    // Update details
                    txtDetailsSource.Text = _selectedError.Source;
                    txtDetailsType.Text = _selectedError.ErrorType.ToString();

                    // Format details including exception if available
                    string details = _selectedError.Details;
                    if (_selectedError.Exception != null)
                    {
                        details += $"\n\nException: {_selectedError.Exception.GetType().Name}";
                        details += $"\nMessage: {_selectedError.Exception.Message}";
                        if (_selectedError.Exception.StackTrace != null)
                        {
                            details += $"\n\nStack Trace:\n{_selectedError.Exception.StackTrace}";
                        }
                    }
                    txtDetails.Text = details;

                    Console.WriteLine($"Selected error: {_selectedError.Message}, Acknowledged: {_selectedError.Acknowledged}");
                }
                else
                {
                    // Clear details
                    txtDetailsSource.Text = string.Empty;
                    txtDetailsType.Text = string.Empty;
                    txtDetails.Text = string.Empty;

                    Console.WriteLine("No error selected");
                }

                // Update UI immediately after selection change
                UpdateUI();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in selection changed handler: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Acknowledge selected error
        /// </summary>
        private void btnAcknowledge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedError != null && _errorList != null)
                {
                    if (!_selectedError.Acknowledged)
                    {
                        _errorList.AcknowledgeError(_selectedError);
                        RefreshErrorList();
                    }
                    else
                    {
                        MessageBox.Show("Selected error is already acknowledged.", "Information",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please select an error to acknowledge.", "No Selection",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error acknowledging error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Acknowledge all errors
        /// </summary>
        private void btnAcknowledgeAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_errorList != null)
                {
                    // Always attempt to acknowledge all errors, regardless of count
                    // This fixes the issue where the count might not be accurate after buzzer reset
                    int unacknowledgedCount = _errorList.UnacknowledgedErrorCount;

                    Console.WriteLine($"btnAcknowledgeAll_Click: Before acknowledgment - Unacknowledged count: {unacknowledgedCount}");

                    // Perform the acknowledgment regardless of count
                    _errorList.AcknowledgeAllErrors();

                    // Force multiple refreshes to ensure UI is updated properly
                    RefreshErrorList();

                    //// Ghi đè cập nhật thời gian hoàn toàn cho tất cả các lỗi trong CSDL của máy đang chạy
                    //if (App.ActualMachineId.HasValue)
                    //{
                    //    System.Threading.Tasks.Task.Run(async () =>
                    //    {
                    //        try
                    //        {
                    //            var dbService = new Services.Database.AutoVisionDbService();
                    //            await dbService.UpdateEndAllVisionErrorsAsync(App.ActualMachineId.Value);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            Console.WriteLine($"Failed to update end all vision errors via DB: {ex.Message}");
                    //        }
                    //    });
                    //}

                    // Additional UI update to make sure everything is synchronized
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        RefreshErrorList();
                        UpdateUI();
                    }));

                    // Check the count after acknowledgment for debugging
                    int remainingUnacknowledged = _errorList.UnacknowledgedErrorCount;
                    Console.WriteLine($"btnAcknowledgeAll_Click: After acknowledgment - Remaining unacknowledged: {remainingUnacknowledged}");

                    // Show appropriate message based on what was actually done
                    if (unacknowledgedCount > 0)
                    {
                        MessageBox.Show($"Acknowledged {unacknowledgedCount} unacknowledged error(s).", "Acknowledge Complete",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("All errors were already acknowledged, but the operation was completed.", "Acknowledge Complete",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in btnAcknowledgeAll_Click: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show($"Error acknowledging all errors: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clear all errors
        /// </summary>
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_errorList == null)
                    return;

                if (_errorList.ErrorCount == 0)
                {
                    MessageBox.Show("No errors to clear.", "Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show confirmation dialog
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to clear all errors? This cannot be undone, Machine will be re initialized.",
                    "Confirm Clear",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        AOImachine.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error clearing machine field errors: {ex.Message}");
                    }

                    // Then clear the error list
                    _errorList.ClearAllErrors();
                    RefreshErrorList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing errors: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clear selected error
        /// </summary>
        private void btnClearSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedError != null && _errorList != null)
                {
                    // Show confirmation dialog
                    MessageBoxResult result = MessageBox.Show(
                        "Are you sure you want to clear this error?\n\nThis will allow the same error to be detected again if it reoccurs.",
                        "Confirm Clear Error",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Clear the error
                        _errorList.ClearError(_selectedError);

                        // Refresh the list
                        RefreshErrorList();

                        // Clear selection and details
                        _selectedError = null;
                        txtDetailsSource.Text = string.Empty;
                        txtDetailsType.Text = string.Empty;
                        txtDetails.Text = string.Empty;

                        UpdateUI();
                    }
                }
                else
                {
                    MessageBox.Show("Please select an error to clear.", "No Selection",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing selected error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Close the window
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events when window is closed
            if (_errorList != null)
            {
                _errorList.ErrorAdded -= OnErrorAdded;
            }

            base.OnClosed(e);
        }
    }

    /// <summary>
    /// Converter to display "Acknowledged" or "Unacknowledged" based on boolean value
    /// </summary>
    public class BoolToAcknowledgedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool acknowledged)
            {
                return acknowledged ? "Acknowledged" : "Unacknowledged";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to set color based on acknowledged state
    /// </summary>
    public class BoolToAcknowledgedBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool acknowledged)
            {
                return acknowledged ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.Orange);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


