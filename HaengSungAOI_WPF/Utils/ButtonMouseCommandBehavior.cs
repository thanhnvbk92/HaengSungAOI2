using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HaengSungAOI_WPF.Utils
{
    public static class ButtonMouseCommandBehavior
    {
        public static readonly DependencyProperty MouseDownCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseDownCommand",
                typeof(ICommand),
                typeof(ButtonMouseCommandBehavior),
                new PropertyMetadata(null, OnMouseDownCommandChanged));

        public static readonly DependencyProperty MouseUpCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseUpCommand",
                typeof(ICommand),
                typeof(ButtonMouseCommandBehavior),
                new PropertyMetadata(null, OnMouseUpCommandChanged));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "CommandParameter",
                typeof(object),
                typeof(ButtonMouseCommandBehavior),
                new PropertyMetadata(null));

        // Alias properties for XAML convenience (both delegate to CommandParameter internally)
        public static readonly DependencyProperty MouseDownCommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "MouseDownCommandParameter",
                typeof(object),
                typeof(ButtonMouseCommandBehavior),
                new PropertyMetadata(null, OnMouseDownCommandParameterChanged));

        public static readonly DependencyProperty MouseUpCommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "MouseUpCommandParameter",
                typeof(object),
                typeof(ButtonMouseCommandBehavior),
                new PropertyMetadata(null, OnMouseUpCommandParameterChanged));

        public static ICommand GetMouseDownCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseDownCommandProperty);
        public static void SetMouseDownCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseDownCommandProperty, value);

        public static ICommand GetMouseUpCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseUpCommandProperty);
        public static void SetMouseUpCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseUpCommandProperty, value);

        public static object GetCommandParameter(DependencyObject obj) => obj.GetValue(CommandParameterProperty);
        public static void SetCommandParameter(DependencyObject obj, object value) => obj.SetValue(CommandParameterProperty, value);

        public static object GetMouseDownCommandParameter(DependencyObject obj) => obj.GetValue(MouseDownCommandParameterProperty);
        public static void SetMouseDownCommandParameter(DependencyObject obj, object value) => obj.SetValue(MouseDownCommandParameterProperty, value);

        public static object GetMouseUpCommandParameter(DependencyObject obj) => obj.GetValue(MouseUpCommandParameterProperty);
        public static void SetMouseUpCommandParameter(DependencyObject obj, object value) => obj.SetValue(MouseUpCommandParameterProperty, value);

        private static void OnMouseDownCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Sync to shared CommandParameter if not already set
            if (d.GetValue(CommandParameterProperty) == null || d.GetValue(CommandParameterProperty) == e.OldValue)
            {
                d.SetValue(CommandParameterProperty, e.NewValue);
            }
        }

        private static void OnMouseUpCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // MouseUpCommandParameter currently uses same CommandParameter
            // This alias exists for XAML symmetry
        }

        private static void OnMouseDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as Button;
            if (button == null) return;
            button.PreviewMouseDown -= ButtonOnPreviewMouseDown;
            if (e.NewValue != null) button.PreviewMouseDown += ButtonOnPreviewMouseDown;
        }

        private static void OnMouseUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as Button;
            if (button == null) return;
            button.PreviewMouseUp -= ButtonOnPreviewMouseUp;
            if (e.NewValue != null) button.PreviewMouseUp += ButtonOnPreviewMouseUp;
        }

        private static void ButtonOnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            var button = sender as Button;
            var command = button == null ? null : GetMouseDownCommand(button);
            var parameter = button == null ? null : GetCommandParameter(button);
            if (command != null && command.CanExecute(parameter))
            {
                command.Execute(parameter);
                e.Handled = true;
            }
        }

        private static void ButtonOnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            var button = sender as Button;
            var command = button == null ? null : GetMouseUpCommand(button);
            var parameter = button == null ? null : GetCommandParameter(button);
            if (command != null && command.CanExecute(parameter))
            {
                command.Execute(parameter);
                e.Handled = true;
            }
        }
    }
}
