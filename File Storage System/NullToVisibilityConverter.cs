// NullToVisibilityConverter.cs
// This converter is needed to show or hide the status message.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileFlow
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
