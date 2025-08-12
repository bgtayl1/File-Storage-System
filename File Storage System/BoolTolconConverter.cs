// BoolToIconConverter.cs
// This converter changes a boolean value (IsFolder) into a Segoe MDL2 Assets icon character.

using System;
using System.Globalization;
using System.Windows.Data;

namespace FileFlow
{
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isFolder && isFolder) ? "\uE8B7" : "\uE7C3";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
