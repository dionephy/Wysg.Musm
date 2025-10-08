using System;
using System.Globalization;
using System.Windows.Data;

namespace Wysg.Musm.Radium.Converters
{
    /// <summary>
    /// Returns first numeric value minus second. If invalid, returns 0.
    /// </summary>
    public sealed class OffsetSubtractConverter : IMultiValueConverter    
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 0d;
            double a = ToDouble(values[0]);
            double b = ToDouble(values[1]);
            var result = a - b - 17;
            if (result < 0) result = 0; // clamp
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();

        private static double ToDouble(object v)
        {
            if (v is double d) return d;
            if (v is float f) return f;
            if (v is int i) return i;
            if (v is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) return p;
            return 0d;
        }
    }
}
