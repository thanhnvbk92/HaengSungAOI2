using System;
using System.Windows.Markup;
using System.Reflection;
using System.Resources;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HaengSungAOI_WPF
{
    [MarkupExtensionReturnType(typeof(string))]
    public class Loc : MarkupExtension
    {
        public string Key { get; set; }
        private static ResourceManager _resourceManager = Resource.Strings.Strings.ResourceManager;
        private static event EventHandler LanguageChanged;
        private DependencyObject _targetObject;
        private DependencyProperty _targetProperty;

        public Loc(string key)
        {
            Key = key;
            LanguageChanged += OnLanguageChanged;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var valueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            _targetObject = valueTarget?.TargetObject as DependencyObject;
            _targetProperty = valueTarget?.TargetProperty as DependencyProperty;
            return _resourceManager.GetString(Key, CultureInfo.CurrentUICulture) ?? $"!{Key}!";
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_targetObject != null && _targetProperty != null)
            {
                _targetObject.SetValue(_targetProperty, _resourceManager.GetString(Key, CultureInfo.CurrentUICulture) ?? $"!{Key}!");
            }
        }

        public static void RefreshAll()
        {
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
