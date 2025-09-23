using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wysg.Musm.Radium.Converters
{
    public sealed class CountToVisibilityConverter : IValueConverter
    {
        public int Minimum { get; set; } = 2;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var count = System.Convert.ToInt32(value);
                return count >= Minimum ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
