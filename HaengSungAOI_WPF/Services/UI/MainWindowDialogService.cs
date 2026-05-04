using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HaengSungAOI_WPF.Views;

namespace HaengSungAOI_WPF.Services.UI
{
    public class MainWindowDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindowDialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public ushort? ShowTrayQuantityEditDialog(Window owner, int currentQty, string requiredPassword)
        {
            var dlg = new Window
            {
                Title = "Chinh sua Tray Quantity",
                Width = 360,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x3c)),
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true
            };

            var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };
            panel.Children.Add(new TextBlock
            {
                Text = "Mat khau:",
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

            panel.Children.Add(new TextBlock
            {
                Text = "Gia tri moi:",
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
            valueBox.PreviewTextInput += (s, ev) => { ev.Handled = !ev.Text.All(c => c >= '0' && c <= '9'); };
            DataObject.AddPastingHandler(valueBox, (s, ev) =>
            {
                if (ev.DataObject.GetDataPresent(DataFormats.Text))
                {
                    var text = (string)ev.DataObject.GetData(DataFormats.Text);
                    if (!text.All(c => c >= '0' && c <= '9')) ev.CancelCommand();
                }
                else
                {
                    ev.CancelCommand();
                }
            });
            panel.Children.Add(valueBox);

            bool? confirmed = false;
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button
            {
                Content = "Xac nhan",
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
                Content = "Huy",
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
            dlg.Loaded += (s, ev) => { pwdBox.Focus(); valueBox.SelectAll(); };
            dlg.ShowDialog();

            if (confirmed != true) return null;
            if (pwdBox.Password != requiredPassword)
            {
                MessageBox.Show("Mat khau khong dung.", "Xac thuc that bai", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            if (!ushort.TryParse(valueBox.Text, out ushort newValue) || newValue > 48)
            {
                MessageBox.Show("Gia tri khong hop le. Vui long nhap so nguyen (0 - 48).", "Gia tri khong hop le", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return newValue;
        }

        public string ShowEbrEditDialog(Window owner, string currentEbr)
        {
            var dlg = new Window
            {
                Title = "Nhap EBR moi",
                Width = 360,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x3c)),
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true
            };

            var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };
            panel.Children.Add(new TextBlock
            {
                Text = "Gia tri EBR moi:",
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var valueBox = new TextBox
            {
                Text = currentEbr == "Not Set" ? string.Empty : currentEbr,
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
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button { Content = "Xac nhan", Width = 90, Height = 32, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            btnOk.Click += (s, ev) => { confirmed = true; dlg.Close(); };
            var btnCancel = new Button { Content = "Huy", Width = 80, Height = 32, IsCancel = true };
            btnCancel.Click += (s, ev) => { confirmed = false; dlg.Close(); };
            valueBox.KeyDown += (s, ev) => { if (ev.Key == Key.Enter) { confirmed = true; dlg.Close(); } };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);

            dlg.Content = panel;
            dlg.Loaded += (s, ev) => { valueBox.Focus(); valueBox.SelectAll(); };
            dlg.ShowDialog();

            return confirmed == true ? valueBox.Text.Trim() : null;
        }

        public bool ShowBypassPasswordDialog(Window owner, string requiredPassword)
        {
            var dlg = new Window
            {
                Title = "Xac thuc kich hoat By Pass",
                Width = 300,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x3c)),
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock
            {
                Text = "Nhap mat khau de kich hoat By Pass:",
                Foreground = new SolidColorBrush(Colors.LightGray),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var pwdBox = new PasswordBox { MaxLength = 20, FontSize = 15, Height = 34 };
            panel.Children.Add(pwdBox);

            bool? confirmed = false;
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            var btnOk = new Button { Content = "Xac nhan", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
            btnOk.Click += (s, ev) => { confirmed = true; dlg.Close(); };
            var btnCancel = new Button { Content = "Huy", Width = 80, Height = 30, IsCancel = true };
            btnCancel.Click += (s, ev) => { confirmed = false; dlg.Close(); };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);

            dlg.Content = panel;
            dlg.Loaded += (s, ev) => pwdBox.Focus();
            dlg.ShowDialog();

            if (confirmed == true && pwdBox.Password == requiredPassword) return true;
            if (confirmed == true)
            {
                MessageBox.Show("Mat khau khong dung!", "Xac thuc that bai", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return false;
        }

        public bool ShowCriticalConfirmation(Window owner, string title, string message)
        {
            return MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        public void ShowHistoryWindow(Window owner)
        {
            var win = new HistoryWindow();
            win.Owner = owner;
            win.ShowDialog();
        }

        public void ShowManualOperationsWindow(Window owner)
        {
            var win = _serviceProvider.GetService(typeof(ManualOperations)) as ManualOperations;
            if (win != null)
            {
                win.Owner = owner;
                win.Show();
            }
        }

        public void ShowSettingsWindow(Window owner)
        {
            var win = _serviceProvider.GetService(typeof(SettingsWindow)) as SettingsWindow;
            if (win != null)
            {
                win.Owner = owner;
                win.ShowDialog();
            }
        }
    }
}
