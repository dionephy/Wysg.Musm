using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Wysg.Musm.Radium.ViewModels.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = Brushes.Red;
        public Brush FalseBrush { get; set; } = Brushes.White;
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? TrueBrush : FalseBrush;
            return FalseBrush;
        }
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
