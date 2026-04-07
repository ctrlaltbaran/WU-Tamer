using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Wu_change.Models;

namespace Wu_change
{
    public class SeverityColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (UpdateSeverity)value switch
            {
                UpdateSeverity.Critical  => new SolidColorBrush(Color.FromRgb(196, 43,  28)),
                UpdateSeverity.Important => new SolidColorBrush(Color.FromRgb(218, 120,  0)),
                UpdateSeverity.Moderate  => new SolidColorBrush(Color.FromRgb(  0, 103, 192)),
                UpdateSeverity.Low       => new SolidColorBrush(Color.FromRgb( 16, 140,  80)),
                _                        => new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? "Yes" : "No";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Hidden"      => new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                "Installed"   => new SolidColorBrush(Color.FromRgb( 16, 124,  16)),
                "Pending (!)" => new SolidColorBrush(Color.FromRgb(218, 120,   0)),
                _             => new SolidColorBrush(Color.FromRgb(  0,  99, 177)),
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
