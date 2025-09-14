using System;
using System.Globalization;
using System.Windows.Data;

namespace Wysg.Musm.Radium.Converters
{
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value ?? false;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value ?? false;
    }
}
